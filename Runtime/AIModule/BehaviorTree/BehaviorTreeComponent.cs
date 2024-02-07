using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    [Flags]
    public enum BTBranchAction : ushort
    {
        None = 0x0,
        DecoratorEvaluate = 0x1,
        DecoratorActivate_IfNotExecuting = 0x2,
        DecoratorActivate_EvenIfExecuting = 0x4,
        DecoratorActivate = DecoratorActivate_IfNotExecuting | DecoratorActivate_EvenIfExecuting,
        DecoratorDeactivate = 0x8,
        UnregisterAuxNodes = 0x10,
        StopTree_Safe = 0x20,
        StopTree_Forced = 0x40,
        ActiveNodeEvaluate = 0x80,
        SubTreeEvaluate = 0x100,
        ProcessPendingInitialize = 0x200,
        Cleanup = 0x400,
        UninitializeComponent = 0x800,
        StopTree = StopTree_Safe | StopTree_Forced,
        Changing_Topology_Actions = UnregisterAuxNodes | StopTree | ProcessPendingInitialize | Cleanup | UninitializeComponent,
        All = DecoratorEvaluate | DecoratorActivate_IfNotExecuting | DecoratorActivate_EvenIfExecuting | DecoratorDeactivate | Changing_Topology_Actions | ActiveNodeEvaluate | SubTreeEvaluate,
    }

    public struct BTNodeExecutionInfo
    {
        /** index of first task allowed to be executed */
        public BTNodeIndex searchStart;

        /** index of last task allowed to be executed */
        public BTNodeIndex searchEnd;

        /** node to be executed */
        public BTCompositeNode executeNode;

        /** subtree index */
        public ushort executeInstanceIdx;

        /** result used for resuming execution */
        public BTNodeResult continueWithResult;

        /** if set, tree will try to execute next child of composite instead of forcing branch containing SearchStart */
        public bool tryNextChild;

        /** if set, request was not instigated by finishing task/initialization but is a restart (e.g. decorator) */
        public bool isRestart;

        public BTNodeExecutionInfo(BTCompositeNode InExecuteNode = null)
        {
            tryNextChild = false;
            isRestart = false;
            executeNode = InExecuteNode;
            continueWithResult = BTNodeResult.Succeeded;
            searchStart = default;
            searchEnd = default;
            executeInstanceIdx = ushort.MaxValue;
        }
    }

    public struct BTPendingExecutionInfo
    {
        /** next task to execute */
        public BTTaskNode nextTask;

        /** if set, tree ran out of nodes */
        public bool outOfNodes;

        /** if set, request can't be executed */
        public bool locked;

        BTPendingExecutionInfo(BTTaskNode inNextTask = null)
        {
            nextTask = inNextTask;
            outOfNodes = false;
            locked = false;
        }

        public bool IsSet() { return (nextTask || outOfNodes) && !locked; }
        public bool IsLocked() { return locked; }

        public void Lock() { locked = true; }
        public void Unlock() { locked = false; }
    }

    public struct BTTreeStartInfo
    {
        public BehaviorTree asset;
        public TreeExecutionMode executeMode;
        public bool pendingInitialize;

        BTTreeStartInfo(BehaviorTree inAsset = null)
        {
            asset = inAsset;
            executeMode = TreeExecutionMode.Looped;
            pendingInitialize = false;
        }

        public bool IsSet() { return asset != null; }
        public bool HasPendingInitialize() { return pendingInitialize && IsSet(); }
    };

    public class BehaviorTreeComponent : BrainComponent
    {
        [SerializeField] private BehaviorTree defaultBehaviorTree;

        public bool IsRunningTree { get; protected set; } = false;
        public bool IsPausedTree { get; protected set; } = false;


        internal List<BehaviorTree> knownInstances = new List<BehaviorTree>();

        protected bool loopExecution = false;
        protected bool waitingForLatentAborts;

        BTNodeExecutionInfo executionRequest;
        BTPendingExecutionInfo pendingExecution;
        protected BTTreeStartInfo treeStartInfo;
        protected List<BranchActionInfo> pendingBranchActionRequests = new List<BranchActionInfo>();
        protected BTBranchAction suspendedBranchActions;
        protected ushort activeInstanceIndex = 0;
        protected BehaviorTreeSearchData SearchData;
        private bool requestedFlowUpdate;

        private void Update()
        {
            if (!IsRunning()) return;

            if (requestedFlowUpdate)
            {
                ProcessExecutionRequest();
            }
        }

        private void ProcessExecutionRequest()
        {
            requestedFlowUpdate = false;

            if (!knownInstances.IsValidIndex(activeInstanceIndex))
                return;

            if (IsPaused())
                return;

            if (waitingForLatentAborts)
                return;

            if (pendingExecution.IsSet())
            {
                ProcessPendingExecution();
            }
        }

        private void ProcessPendingExecution()
        {
            if (waitingForLatentAborts || !pendingExecution.IsSet())
                return;

            BTPendingExecutionInfo savedInfo = pendingExecution;
            pendingExecution = default;

            BTNodeIndex nextTaskIdx = savedInfo.nextTask ? new BTNodeIndex(activeInstanceIndex, savedInfo.nextTask.GetExecutionIndex()) : new BTNodeIndex(0, 0);
            UnregisterAuxNodesUpTo(nextTaskIdx);

            ApplySearchData(savedInfo.nextTask);

            if (savedInfo.nextTask && knownInstances.IsValidIndex(activeInstanceIndex))
            {
                ExecuteTask(savedInfo.nextTask);
            }
            else
            {
                ResumeBranchActions();
                OnTreeFinished();
            }
        }

        private void OnTreeFinished()
        {
            activeInstanceIndex = 0;

            if (loopExecution && knownInstances.Count > 0)
            {
                BehaviorTree topInstance = knownInstances[0];
                topInstance.activeNode = null;
                topInstance.activeNodeType = BTActiveNode.Composite;

                UnregisterAuxNodesUpTo(new BTNodeIndex(0, 0));

                RequestExecution(topInstance.rootNode, 0, topInstance.rootNode, 0, BTNodeResult.InProgress);
            }
            else
            {
                StopTree(BTStopMode.Safe);
            }
        }

        private void ResumeBranchActions()
        {
            suspendedBranchActions = BTBranchAction.None;

            while (pendingBranchActionRequests.Count > 0)
            {
                List<BranchActionInfo> pendingBranchActionsToProcess = pendingBranchActionRequests.ToList();
                pendingBranchActionRequests.Clear();

                for (int i = 0; i < pendingBranchActionsToProcess.Count; i++)
                {
                    BranchActionInfo info = pendingBranchActionsToProcess[i];

                    switch (info.action)
                    {
                        case BTBranchAction.DecoratorEvaluate:
                            {
                                BTDecorator requestedBy = (BTDecorator)info.node;

                                if (!IsAuxNodeActive(requestedBy))
                                {
                                    break;
                                }

                                EvaluateBranch(requestedBy);
                                break;
                            }
                        case BTBranchAction.DecoratorActivate_IfNotExecuting:
                        case BTBranchAction.DecoratorActivate_EvenIfExecuting:
                            {
                                BTDecorator requestedBy = (BTDecorator)info.node;

                                // Since we have been queued up, decorator might have been removed from active nodes, need to make sure it is still there.
                                if (!IsAuxNodeActive(requestedBy))
                                {
                                    break;
                                }

                                ActivateBranch(requestedBy, info.action == BTBranchAction.DecoratorActivate_EvenIfExecuting);
                                break;
                            }
                        case BTBranchAction.DecoratorDeactivate:
                            {
                                BTDecorator requestedBy = (BTDecorator)info.node;
                                // Since we have been queued up, decorator might have been removed from active nodes, need to make sure it is still there.
                                if (!IsAuxNodeActive(requestedBy))
                                {
                                    break;
                                }

                                DeactivateBranch(requestedBy);
                                break;
                            }
                        case BTBranchAction.UnregisterAuxNodes:
                            {
                                BTCompositeNode branchRoot = (BTCompositeNode)info.node;
                                UnregisterAuxNodesInBranch(branchRoot, true);
                                break;
                            }
                        case BTBranchAction.StopTree_Safe:
                        case BTBranchAction.StopTree_Forced:
                            StopTree(info.action == BTBranchAction.StopTree_Forced ? BTStopMode.Forced : BTStopMode.Safe);
                            break;
                        case BTBranchAction.ActiveNodeEvaluate:
                            {
                                BTNode activeNode = GetActiveNode();
                                if (activeNode != info.node)
                                {
                                    break;
                                }

                                EvaluateBranch(info.continueWithResult);
                                break;
                            }
                        case BTBranchAction.SubTreeEvaluate:
                            {
                                BTCompositeNode branchRoot = (BTCompositeNode)info.node;
                                BTNode rootNode = knownInstances.Count > 0 ? knownInstances[activeInstanceIndex].rootNode : null;
                                if (rootNode != branchRoot)
                                {
                                    break;
                                }

                                RequestExecution(branchRoot, activeInstanceIndex, branchRoot, 0, BTNodeResult.InProgress);

                                break;
                            }
                        case BTBranchAction.ProcessPendingInitialize:
                            {
                                ProcessPendingInitialize();
                            }
                            break;
                        case BTBranchAction.Cleanup:
                            Cleanup();
                            break;
                        case BTBranchAction.UninitializeComponent:
                            RemoveAllInstances();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void RemoveAllInstances()
        {
            if (knownInstances.Count > 0)
            {
                StopTree(BTStopMode.Forced);
            }

            for (int i = 0; i < knownInstances.Count; i++)
            {
                Destroy(knownInstances[i]);
            }

            knownInstances.Clear();
        }

        private void UnregisterAuxNodesInBranch(BTCompositeNode node, bool applyImmediately)
        {
            int instanceIdx = FindInstanceContainingNode(node);

            if (instanceIdx != -1)
            {

                ushort instanceIdxUShort = (ushort)instanceIdx;

                List<BehaviorTreeSearchUpdate> listCopy = null;
                if (applyImmediately)
                {
                    listCopy = SearchData.PendingUpdates.ToList();
                    SearchData.PendingUpdates.Clear();
                }

                BTNodeIndex fromIndex = new BTNodeIndex(instanceIdxUShort, node.GetExecutionIndex());
                BTNodeIndex toIndex = new BTNodeIndex(instanceIdxUShort, node.GetLastExecutionIndex());

                UnregisterAuxNodesInRange(fromIndex, toIndex);

                if (applyImmediately)
                {
                    ApplySearchUpdates(SearchData.PendingUpdates, 0);
                    SearchData.PendingUpdates = listCopy;
                }
            }
        }

        private void ApplySearchUpdates(List<BehaviorTreeSearchUpdate> updateList, int newNodeExecutionIndex, bool postUpdate = false)
        {
            for (int i = 0; i < updateList.Count; i++)
            {
                BehaviorTreeSearchUpdate updateInfo = updateList[i];

                if (updateInfo.postUpdate != postUpdate)
                {
                    continue;
                }

                if (!knownInstances.IsValidIndex(updateInfo.instanceIndex))
                {
                    continue;
                }

                BehaviorTree updateInstance = knownInstances[updateInfo.instanceIndex];
                int taskId = -1;
                bool isComponentActive = false;

                if (updateInfo.auxNode)
                {
                    isComponentActive = updateInstance.ActiveAuxNodes.Contains(updateInfo.auxNode);
                }
                else if (updateInfo.taskNode)
                {
                    taskId = updateInstance.ParallelTasks.IndexOf(updateInfo.taskNode);
                    isComponentActive = (taskId != -1 && updateInstance.ParallelTasks[taskId].Status == BTTaskStatus.Active);
                }

                BTNode updateNode = updateInfo.auxNode ? updateInfo.auxNode : updateInfo.taskNode;

                if ((updateInfo.mode == BTNodeUpdateMode.Remove && !isComponentActive) ||
                        (updateInfo.mode == BTNodeUpdateMode.Add && (isComponentActive || updateNode.GetExecutionIndex() > newNodeExecutionIndex)))
                {
                    updateInfo.applySkipped = true;
                    continue;
                }

                if (updateInfo.auxNode)
                {
                    // Skip remove/re-add on service running on root node
                    if (loopExecution && updateInfo.auxNode.GetMyNode() == knownInstances[0].rootNode &&
                        updateInfo.auxNode is BTService)
                    {
                        if (updateInfo.mode == BTNodeUpdateMode.Remove || knownInstances[0].ActiveAuxNodes.Contains(updateInfo.auxNode))
                        {
                            continue;
                        }
                    }

                    if (updateInfo.mode == BTNodeUpdateMode.Remove)
                    {
                        updateInstance.RemoveFromActiveAuxNodes(updateInfo.auxNode);
                        updateInfo.auxNode.OnCeaseRelevant();
                    }
                    else
                    {
                        updateInstance.AddToActiveAuxNodes(updateInfo.auxNode);
                        updateInfo.auxNode.OnBecomeRelevant();
                    }
                }
                else if (updateInfo.taskNode)
                {
                    if (updateInfo.mode == BTNodeUpdateMode.Remove)
                    {
                        BTNodeResult nodeResult = updateInfo.taskNode.AbortTask(this);

                        // check if task node is still valid, could've received LatentAbortFinished during AbortTask call
                        bool stillValid = knownInstances.IsValidIndex(updateInfo.instanceIndex) &&
                            knownInstances[updateInfo.instanceIndex].ParallelTasks.IsValidIndex(taskId) &&
                            knownInstances[updateInfo.instanceIndex].ParallelTasks[taskId] == updateInfo.taskNode;

                        if (stillValid)
                        {
                            if (nodeResult == BTNodeResult.InProgress)
                            {
                                updateInstance.MarkParallelTaskAsAbortingAt(taskId);
                                waitingForLatentAborts = true;
                            }

                            OnTaskFinished(updateInfo.taskNode, nodeResult);
                        }
                    }
                    else
                    {
                        updateInfo.taskNode.Status = BTTaskStatus.Active;
                        updateInstance.AddToParallelTasks(updateInfo.taskNode);
                    }
                }

            }
        }


        private void UnregisterAuxNodesInRange(BTNodeIndex fromIndex, BTNodeIndex toIndex)
        {
            for (int i = 0; i < knownInstances.Count; i++)
            {
                BehaviorTree instanceInfo = knownInstances[i];
                List<BTAuxiliaryNode> auxiliaryNodes = instanceInfo.ActiveAuxNodes;
                for (int j = 0; j < auxiliaryNodes.Count; j++)
                {
                    BTAuxiliaryNode auxNode = auxiliaryNodes[i];
                    ushort instanceIndexUshort = (ushort)i;
                    BTNodeIndex auxIdx = new BTNodeIndex(instanceIndexUshort, auxNode.GetExecutionIndex());
                    if (fromIndex.TakesPriorityOver(auxIdx) && auxIdx.TakesPriorityOver(toIndex))
                    {
                        SearchData.AddUniqueUpdate(new BehaviorTreeSearchUpdate(auxNode, instanceIndexUshort, BTNodeUpdateMode.Remove));
                    }
                }
            }
        }

        private void DeactivateBranch(BTDecorator requestedBy)
        {
            if (IsExecutingBranch(requestedBy, requestedBy.ChildIndex))
            {
                EvaluateBranch(requestedBy);
            }
            else if (requestedBy.GetParentNode() && requestedBy.GetParentNode().children.IsValidIndex(requestedBy.ChildIndex))
            {
                bool abortPending = IsAbortPending();
                if (abortPending)
                {
                    int instanceIdx = FindInstanceContainingNode(requestedBy);
                    RequestExecution(requestedBy.GetParentNode(), instanceIdx, requestedBy, -1, BTNodeResult.Aborted);
                }
            }

            BTCompositeNode branchRoot = requestedBy.GetParentNode().children[requestedBy.ChildIndex].childComposite;
            if (branchRoot)
            {
                if ((suspendedBranchActions & BTBranchAction.UnregisterAuxNodes) != BTBranchAction.None)
                {
                    pendingBranchActionRequests.Add(new BranchActionInfo(branchRoot, BTBranchAction.UnregisterAuxNodes));
                }
                else
                {
                    UnregisterAuxNodesInBranch(branchRoot, true);
                }
            }
            else
            {
                Debug.LogError("The decorator does not have a parent or is not a valid child");
            }
        }

        private bool IsAbortPending()
        {
            return waitingForLatentAborts || pendingExecution.IsSet();
        }

        private void ActivateBranch(BTDecorator requestedBy, bool forceRequestEvenIfExecuting)
        {
            int instanceIdx = FindInstanceContainingNode(requestedBy);
            if (instanceIdx == -1)
                return;

            bool isExecutingBranch = IsExecutingBranch(requestedBy, requestedBy.ChildIndex);
            bool abortPending = IsAbortPending();

            bool isDeactivatingBranchRoot = executionRequest.continueWithResult == BTNodeResult.Failed && executionRequest.searchStart == new BTNodeIndex((ushort)instanceIdx, requestedBy.GetExecutionIndex());

            if (!isExecutingBranch)
            {
                EvaluateBranch(requestedBy);
            }
            else if (forceRequestEvenIfExecuting || abortPending || isDeactivatingBranchRoot)
            {
                RequestExecution(requestedBy.GetParentNode(), instanceIdx, requestedBy, requestedBy.ChildIndex, BTNodeResult.Aborted);
            }
        }

        public bool IsAuxNodeActive(BTAuxiliaryNode auxNode)
        {
            if (auxNode == null)
                return false;

            ushort auxExecutionIndex = auxNode.GetExecutionIndex();
            for (int instanceIndex = 0; instanceIndex < knownInstances.Count; instanceIndex++)
            {
                BehaviorTree instanceInfo = knownInstances[instanceIndex];

                for (int i = 0; i < instanceInfo.ActiveAuxNodes.Count; i++)
                {
                    BTAuxiliaryNode testAuxNode = instanceInfo.ActiveAuxNodes[i];

                    if (testAuxNode == auxNode)
                        return true;
                }
            }

            return false;
        }

        public bool IsAuxNodeActive(BTAuxiliaryNode auxNode, int instanceIdx)
        {
            return knownInstances.IsValidIndex(instanceIdx) && knownInstances[instanceIdx].ActiveAuxNodes.Contains(auxNode);
        }

        private void EvaluateBranch(BTDecorator requestedBy)
        {
            BTFlowAbortMode abortMode = requestedBy.GetFlowAbortMode();

            if (abortMode == BTFlowAbortMode.None)
            {
                return;
            }

            int instanceIdx = FindInstanceContainingNode(requestedBy);
            if (instanceIdx == -1)
            {
                return;
            }

            if (abortMode == BTFlowAbortMode.Both)
            {
                bool isExecutingChildNodes = IsExecutingBranch(requestedBy, requestedBy.ChildIndex);
                abortMode = isExecutingChildNodes ? BTFlowAbortMode.Self : BTFlowAbortMode.LowerPriority;
            }

            BTNodeResult continueResult = (abortMode == BTFlowAbortMode.Self) ? BTNodeResult.Failed : BTNodeResult.Aborted;

            RequestExecution(requestedBy.GetParentNode(), instanceIdx, requestedBy, requestedBy.ChildIndex, continueResult);
        }

        private void EvaluateBranch(BTNodeResult continueWithResult)
        {
            if (knownInstances.IsValidIndex(activeInstanceIndex))
            {
                BehaviorTree activeInstance = knownInstances[activeInstanceIndex];
                BTCompositeNode executeParent = (activeInstance.activeNode == null) ? activeInstance.rootNode :
                    (activeInstance.activeNodeType == BTActiveNode.Composite) ? (BTCompositeNode)activeInstance.activeNode :
                    activeInstance.activeNode.GetParentNode();

                RequestExecution(executeParent, activeInstanceIndex, activeInstance.activeNode ? activeInstance.activeNode : activeInstance.rootNode, -1, continueWithResult);
            }
        }

        private void RequestBranchEvaluation(BTNodeResult continueWithResult)
        {
            if (continueWithResult == BTNodeResult.Aborted || continueWithResult == BTNodeResult.InProgress)
                return;

            if ((suspendedBranchActions & BTBranchAction.ActiveNodeEvaluate) != BTBranchAction.None)
            {
                BTNode activeNode = GetActiveNode();

                pendingBranchActionRequests.Add(new BranchActionInfo(activeNode, BTBranchAction.ActiveNodeEvaluate));
                return;
            }

            EvaluateBranch(continueWithResult);
        }

        private void ExecuteTask(BTTaskNode taskNode)
        {
            BehaviorTree activeInstance = knownInstances[activeInstanceIndex];

            for (int i = 0; i < taskNode.services.Count; i++)
            {
                BTService serviceNode = taskNode.services[i];

                activeInstance.AddToActiveAuxNodes(serviceNode);

                serviceNode.OnBecomeRelevant(this);

                serviceNode.TickNode(this, Time.deltaTime);
            }

            activeInstance.activeNode = taskNode;
            activeInstance.activeNodeType = BTActiveNode.ActiveTask;


            BTNodeResult taskResult = taskNode.ExecuteTask(this);

            BTNode activeNodeAfterExecution = GetActiveNode();
            if (activeNodeAfterExecution == taskNode)
            {
                OnTaskFinished(taskNode, taskResult);
            }

            ResumeBranchActions();
        }

        private void OnTaskFinished(BTTaskNode taskNode, BTNodeResult taskResult)
        {
            if (taskNode == null || knownInstances.Count == 0 || !this)
            {
                return;
            }

            int taskInstanceIdx = FindInstanceContainingNode(taskNode);
            if (!knownInstances.IsValidIndex(taskInstanceIdx))
                return;

            if (taskResult != BTNodeResult.InProgress)
            {
                taskNode.OnTaskFinished(this, taskResult);

                if (knownInstances.IsValidIndex(activeInstanceIndex) && knownInstances[activeInstanceIndex].activeNode == taskNode)
                {
                    BehaviorTree activeInstance = knownInstances[activeInstanceIndex];
                    bool wasAborting = (activeInstance.activeNodeType == BTActiveNode.AbortingTask);
                    activeInstance.activeNodeType = BTActiveNode.InactiveTask;

                    if (!wasAborting)
                    {
                        RequestBranchEvaluation(taskResult);
                    }
                }
                else if (taskResult == BTNodeResult.Aborted && knownInstances.IsValidIndex(taskInstanceIdx) && knownInstances[taskInstanceIdx].activeNode == taskNode)
                {
                    knownInstances[activeInstanceIndex].activeNodeType = BTActiveNode.InactiveTask;
                }
            }

            TrackNewLatentAborts();

            if (treeStartInfo.HasPendingInitialize())
            {
                ProcessPendingInitialize();
            }
        }

        private void TrackNewLatentAborts()
        {
            if (waitingForLatentAborts)
                return;

            waitingForLatentAborts = HasActiveLatentAborts();
        }

        private bool HasActiveLatentAborts()
        {
            bool hasActiveLatentAborts = knownInstances.Count > 0 ? (knownInstances[^1].activeNodeType == BTActiveNode.AbortingTask) : false;

            for (int i = 0; i < knownInstances.Count && !hasActiveLatentAborts; i++)
            {
                BehaviorTree behaviorTree = knownInstances[i];
                for (int j = 0; j < behaviorTree.ParallelTasks.Count; j++)
                {
                    if (behaviorTree.ParallelTasks[j].Status == BTTaskStatus.Aborting)
                    {
                        hasActiveLatentAborts = true;
                        break;
                    }
                }
            }

            return hasActiveLatentAborts;
        }

        private void ApplySearchData(BTNode newActivenode)
        {
            SearchData.rollbackInstanceIdx = -1;
            SearchData.RollbackDeactivatedBranchStart = default;
            SearchData.RollbackDeactivatedBranchEnd = default;

            //for (int Idx = 0; Idx < SearchData.PendingNotifies.Count.; Idx++)
            //{

            //}

            int newNodeExecutionIndex = newActivenode ? newActivenode.GetExecutionIndex() : 0;

            SearchData.filterOutRequestFromDeactivatedBranch = true;
            ApplySearchUpdates(SearchData.PendingUpdates, newNodeExecutionIndex);
            ApplySearchUpdates(SearchData.PendingUpdates, newNodeExecutionIndex, true);

            SearchData.filterOutRequestFromDeactivatedBranch = false;

            float currentFrameDeltaSeconds = Time.deltaTime;

            for (int Idx = 0; Idx < SearchData.PendingUpdates.Count; Idx++)
            {
                BehaviorTreeSearchUpdate updateInfo = SearchData.PendingUpdates[Idx];
                if (!updateInfo.applySkipped && updateInfo.mode == BTNodeUpdateMode.Add && updateInfo.auxNode && knownInstances.IsValidIndex(updateInfo.instanceIndex))
                {
                    BehaviorTree instanceInfo = knownInstances[updateInfo.instanceIndex];
                    updateInfo.auxNode.TickNode(this, currentFrameDeltaSeconds);
                }
            }

            SearchData.PendingUpdates.Clear();
            SearchData.DeactivatedBranchStart = default;
            SearchData.DeactivatedBranchEnd = default;
        }

        private void ApplyDiscardedSearch()
        {
            SearchData.PendingUpdates.Clear();
        }

        private void UnregisterAuxNodesUpTo(BTNodeIndex index)
        {
            for (int instanceIndex = 0; instanceIndex < knownInstances.Count; instanceIndex++)
            {
                BehaviorTree instanceInfo = knownInstances[instanceIndex];
                for (int i = 0; i < instanceInfo.ActiveAuxNodes.Count; i++)
                {
                    BTAuxiliaryNode auxNode = instanceInfo.ActiveAuxNodes[i];
                    ushort instanceIndexUshort = (ushort)instanceIndex;
                    BTNodeIndex auxIdx = new BTNodeIndex(instanceIndexUshort, auxNode.GetExecutionIndex());
                    if (index.TakesPriorityOver(auxIdx))
                    {
                        SearchData.AddUniqueUpdate(new BehaviorTreeSearchUpdate(auxNode, instanceIndexUshort, BTNodeUpdateMode.Remove));
                    }
                }
            }
        }

        public override void StartLogic()
        {
            if (IsRunningTree)
            {
                return;
            }

            if (!treeStartInfo.IsSet())
            {
                treeStartInfo.asset = defaultBehaviorTree;
            }

            if (treeStartInfo.IsSet())
            {
                treeStartInfo.pendingInitialize = true;
                ProcessPendingInitialize();
            }
            else
            {
                Debug.Log("Clould not find BehaviourTree to run");
            }
        }

        private void ProcessPendingInitialize()
        {
            if ((suspendedBranchActions & BTBranchAction.ProcessPendingInitialize) != BTBranchAction.None)
            {
                pendingBranchActionRequests.Add(new BranchActionInfo(BTBranchAction.ProcessPendingInitialize));
                return;
            }

            StopTree(BTStopMode.Safe);
            if (waitingForLatentAborts)
            {
                return;
            }

            RemoveAllInstances();

            loopExecution = treeStartInfo.executeMode == TreeExecutionMode.Looped;
            IsRunningTree = true;

            bool pushed = PushInstance(treeStartInfo.asset);
            treeStartInfo.pendingInitialize = false;
        }

        protected bool PushInstance(BehaviorTree treeAsset)
        {
            if (treeAsset.BlackboardAsset && blackboardComponent && !blackboardComponent.IsCompatibleWith(treeAsset.BlackboardAsset))
            {
                return false;
            }

            var newInstance = Instantiate(treeAsset);
            newInstance.activeNode = null;


            knownInstances.Add(newInstance);
            activeInstanceIndex = (ushort)(knownInstances.Count - 1);

            var rootNode = newInstance.rootNode;
            newInstance.Initialize(this, rootNode, 0);

            for (int i = 0; i < rootNode.services.Count; i++)
            {
                var serviceNode = rootNode.services[i];
                serviceNode.NotifyParentActivation();
            }

            RequestExecution(rootNode, activeInstanceIndex, rootNode, 0, BTNodeResult.InProgress);

            return true;
        }

        private void RequestExecution(BTCompositeNode RequestedOn, int instanceIndex, BTNode RequestedBy, int requestedByChildIndex, BTNodeResult continueWithResult)
        {
            if (!IsRunningTree || instanceIndex >= knownInstances.Count)
            {
                Debug.Log("skip: tree is not running");
            }

            ushort instanceIdx = unchecked((ushort)instanceIndex);
            bool switchToHigherPriority = continueWithResult == BTNodeResult.Aborted;
            bool alreadyHasRequest = (executionRequest.executeNode != null);

            ushort lastExecutionIndex = ushort.MaxValue;
            BTNodeIndex executionIdx = new BTNodeIndex();
            executionIdx.instanceIndex = instanceIdx;
            executionIdx.executionIndex = RequestedBy.GetExecutionIndex();

            // make sure that the request is not coming from a node that has pending branch actions since it won't be accessible anymore
            if (suspendedBranchActions != BTBranchAction.None)
            {
                if ((suspendedBranchActions & ~(BTBranchAction.Changing_Topology_Actions)) != BTBranchAction.None)
                {
                    Debug.LogWarning("Caller should be converted to new Evaluate/Activate/DeactivateBranch API instead of using this RequestExecution directly");
                }

                for (int i = 0; i < pendingBranchActionRequests.Count; i++)
                {
                    var info = pendingBranchActionRequests[i];

                    BTCompositeNode branchRoot = null;
                    switch (info.action)
                    {
                        case BTBranchAction.DecoratorDeactivate:
                            if (info.node is BTDecorator decorator)
                            {
                                if (decorator.GetParentNode() && decorator.GetParentNode().children.IsValidIndex(decorator.ChildIndex))
                                {
                                    branchRoot = decorator.GetParentNode().children[decorator.ChildIndex].childComposite;
                                }
                            }
                            break;
                        case BTBranchAction.UnregisterAuxNodes:
                            if (info.node is BTCompositeNode compNode)
                            {
                                branchRoot = compNode;
                            }
                            break;
                        default:
                            break;
                    }

                    if (branchRoot)
                    {
                        int branchRootInstanceIdx = FindInstanceContainingNode(branchRoot);
                        if (branchRootInstanceIdx != -1)
                        {
                            ushort branchRootInstanceIdxUShort = checked((ushort)branchRootInstanceIdx);
                            BTNodeIndexRange Range = new BTNodeIndexRange(new BTNodeIndex(branchRootInstanceIdxUShort, branchRoot.GetExecutionIndex()), new BTNodeIndex(branchRootInstanceIdxUShort, branchRoot.GetLastExecutionIndex()));
                            if (Range.Contains(executionIdx))
                            {
                                Debug.Log($"skip: request by {RequestedBy} is in deactivaded branch {branchRoot} and was deactivaded by {info.node}");
                                return;
                            }
                        }
                    }
                }
            }

            if (switchToHigherPriority && requestedByChildIndex >= 0)
            {
                executionIdx.executionIndex = RequestedOn.GetChildExecutionIndex(requestedByChildIndex, BTChildIndex.FirstNode);

                lastExecutionIndex = RequestedOn.GetChildExecutionIndex(requestedByChildIndex + 1, BTChildIndex.FirstNode);
            }

            BTNodeIndex SearchEnd = new BTNodeIndex(instanceIdx, lastExecutionIndex);

            if (alreadyHasRequest && executionRequest.searchStart.TakesPriorityOver(executionIdx))
            {
                if (switchToHigherPriority)
                {
                    if (executionRequest.searchEnd.IsSet() && executionRequest.searchEnd.TakesPriorityOver(SearchEnd))
                    {
                        executionRequest.searchEnd = SearchEnd;
                    }
                }
                else
                {
                    if (executionRequest.searchEnd.IsSet())
                    {
                        executionRequest.searchEnd = default;
                    }
                }

                return;
            }

            if (SearchData.filterOutRequestFromDeactivatedBranch || waitingForLatentAborts)
            {
                // request on same node or with higher priority doesn't require additional checks
                if (SearchData.searchRootNode != executionIdx && SearchData.searchRootNode.TakesPriorityOver(executionIdx) && SearchData.DeactivatedBranchStart.IsSet())
                {

                    if (executionIdx.instanceIndex > SearchData.DeactivatedBranchStart.instanceIndex)
                    {
                        Debug.Log("> skip: node index in a deactivated instance");
                        return;
                    }
                    else if (executionIdx.instanceIndex == SearchData.DeactivatedBranchStart.instanceIndex &&
                            executionIdx.instanceIndex >= SearchData.DeactivatedBranchStart.executionIndex &&
                            executionIdx.instanceIndex < SearchData.DeactivatedBranchEnd.executionIndex)
                    {
                        Debug.Log("> skip: node index in a deactivated  [%s..%s[ (applying search data for %s)");
                        return;
                    }
                }
            }

            if (switchToHigherPriority)
            {
                bool bShouldCheckDecorators = (requestedByChildIndex >= 0) && !IsExecutingBranch(RequestedBy, requestedByChildIndex);
                bool bCanExecute = !bShouldCheckDecorators || RequestedOn.DoDecoratorsAllowExecution(this, instanceIndex, requestedByChildIndex);

                if (!bCanExecute)
                {
                    Debug.Log("skip: decorators are not allowing execution");
                }

                BTCompositeNode currentNode = executionRequest.executeNode;
                ushort currentInstanceIdx = executionRequest.executeInstanceIdx;

                if (executionRequest.executeNode == null)
                {
                    BehaviorTree activeInstance = knownInstances[activeInstanceIndex];
                    currentNode = (activeInstance.activeNode == null) ? activeInstance.rootNode :
                        (activeInstance.activeNodeType == BTActiveNode.Composite) ? activeInstance.activeNode as BTCompositeNode : activeInstance.activeNode.GetParentNode();

                    currentInstanceIdx = activeInstanceIndex;
                }

                if (executionRequest.executeNode != RequestedOn)
                {
                    BTCompositeNode commonParent = null;
                    ushort commonInstanceIdx = ushort.MaxValue;

                    FindCommonParent(knownInstances, RequestedOn, instanceIdx, currentNode, currentInstanceIdx, out commonParent, out commonInstanceIdx);

                    int itInstanceIdx = instanceIndex;

                    for (BTCompositeNode It = RequestedOn; It && It != commonParent;)
                    {
                        BTCompositeNode parentNode = It.GetParentNode();

                        int childIdx = -1;

                        if (parentNode == null)
                        {
                            if (itInstanceIdx > 0)
                            {
                                itInstanceIdx--;
                                BTNode subtreeTaskNode = knownInstances[itInstanceIdx].activeNode;
                                parentNode = subtreeTaskNode.GetParentNode();
                                childIdx = parentNode.GetChildIndex(subtreeTaskNode);
                            }
                            else
                            {
                                // Something bad happened
                                break;
                            }
                        }
                        else
                        {
                            childIdx = parentNode.GetChildIndex(It);
                        }

                        bool canExecuteTest = parentNode.DoDecoratorsAllowExecution(this, itInstanceIdx, childIdx);
                        if (!canExecuteTest)
                        {
                            Debug.Log("skip: decorators are not allowing execution");
                            return;
                        }

                        It = parentNode;
                    }

                    executionRequest.executeNode = commonParent;
                    executionRequest.executeInstanceIdx = commonInstanceIdx;
                }
            }
            else
            {
                bool shouldCheckDecorators =
                    RequestedOn.children.IsValidIndex(requestedByChildIndex) &&
                    (RequestedOn.children[requestedByChildIndex].decoratorsOps.Count > 0)
                    && RequestedBy is BTDecorator;

                bool canExecute = shouldCheckDecorators && RequestedOn.DoDecoratorsAllowExecution(this, instanceIdx, requestedByChildIndex);

                if (canExecute)
                {
                    Debug.Log("skip: decorators are still allowing execution");
                    return;
                }

                executionRequest.executeNode = RequestedOn;
                executionRequest.executeInstanceIdx = instanceIdx;
            }

            if ((!alreadyHasRequest && switchToHigherPriority) ||
                (executionRequest.searchEnd.IsSet() && executionRequest.searchEnd.TakesPriorityOver(SearchEnd)))
            {
                executionRequest.searchEnd = SearchEnd;
            }

            executionRequest.searchStart = executionIdx;
            executionRequest.continueWithResult = continueWithResult;
            executionRequest.tryNextChild = !switchToHigherPriority;
            executionRequest.isRestart = RequestedBy != GetActiveNode();

            pendingExecution.Lock();

            if (SearchData.searchInProgress)
            {
                SearchData.postponeSearch = true;
            }

            bool bIsActiveNodeAborting = knownInstances.Count > 0 && knownInstances[^1].activeNodeType == BTActiveNode.AbortingTask;
            bool bInvalidateCurrentSearch = waitingForLatentAborts || bIsActiveNodeAborting;
            bool bScheduleNewSearch = !waitingForLatentAborts;

            if (bInvalidateCurrentSearch)
            {
                // We are aborting the current search, but in the case we were searching to a next child, we cannot look for only higher priority as sub decorator might still fail
                // Previous search might have been a different range, so just open it up to cover all cases
                if (executionRequest.searchEnd.IsSet())
                {
                    executionRequest.searchEnd = new BTNodeIndex();
                }
                RollbackSearchChanges();
            }

            requestedFlowUpdate = true;
        }

        private void RollbackSearchChanges()
        {
            if (SearchData.rollbackInstanceIdx >= 0)
            {
                activeInstanceIndex = (ushort)(SearchData.rollbackInstanceIdx);
                SearchData.DeactivatedBranchStart = SearchData.RollbackDeactivatedBranchStart;
                SearchData.DeactivatedBranchEnd = SearchData.RollbackDeactivatedBranchEnd;

                SearchData.rollbackInstanceIdx = -1;
                SearchData.RollbackDeactivatedBranchStart = new BTNodeIndex();
                SearchData.RollbackDeactivatedBranchEnd = new BTNodeIndex();

                // apply new observer changes
                ApplyDiscardedSearch();
            }
        }

        private BTNode GetActiveNode()
        {
            return knownInstances.Count > 0 ? knownInstances[activeInstanceIndex].activeNode : null;
        }

        private void FindCommonParent(List<BehaviorTree> Instances, BTCompositeNode InNodeA, ushort InstanceIdxA, BTCompositeNode InNodeB, ushort InstanceIdxB, out BTCompositeNode CommonParentNode, out ushort CommonInstanceIdx)
        {
            // find two nodes in the same instance (choose lower index = closer to root)
            CommonInstanceIdx = (InstanceIdxA <= InstanceIdxB) ? InstanceIdxA : InstanceIdxB;

            BTCompositeNode NodeA = (CommonInstanceIdx == InstanceIdxA) ? InNodeA : Instances[CommonInstanceIdx].activeNode.GetParentNode();
            BTCompositeNode NodeB = (CommonInstanceIdx == InstanceIdxB) ? InNodeB : Instances[CommonInstanceIdx].activeNode.GetParentNode();

            // special case: node was taken from CommonInstanceIdx, but it had ActiveNode set to root (no parent)
            if (!NodeA && CommonInstanceIdx != InstanceIdxA)
            {
                NodeA = Instances[CommonInstanceIdx].rootNode;
            }
            if (!NodeB && CommonInstanceIdx != InstanceIdxB)
            {
                NodeB = Instances[CommonInstanceIdx].rootNode;
            }

            // if one of nodes is still empty, we have serious problem with execution flow - crash and log details
            if (!NodeA || !NodeB)
            {
                Debug.LogError("Fatal error in FindCommonParent()");
                CommonParentNode = null;
                return;
            }

            // find common parent of two nodes
            int NodeADepth = NodeA.TreeDepth;
            int NodeBDepth = NodeB.TreeDepth;

            while (NodeADepth > NodeBDepth)
            {
                NodeA = NodeA.GetParentNode();
                NodeADepth = NodeA.TreeDepth;
            }

            while (NodeBDepth > NodeADepth)
            {
                NodeB = NodeB.GetParentNode();
                NodeBDepth = NodeB.TreeDepth;
            }

            while (NodeA != NodeB)
            {
                NodeA = NodeA.GetParentNode();
                NodeB = NodeB.GetParentNode();
            }

            CommonParentNode = NodeA;
        }

        private int FindInstanceContainingNode(BTNode node)
        {
            int instanceIdx = -1;

            if (knownInstances.Count > 0)
            {
                if (knownInstances[activeInstanceIndex].activeNode != node)
                {
                    BTNode rootNode = node;

                    while (rootNode.GetParentNode())
                    {
                        rootNode = rootNode.GetParentNode();
                    }

                    for (int i = 0; i < knownInstances.Count; i++)
                    {
                        if (knownInstances[i].rootNode == rootNode)
                        {
                            instanceIdx = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                instanceIdx = activeInstanceIndex;
            }

            return instanceIdx;
        }

        public override void RestartLogic()
        {

        }

        public override void StopLogic()
        {

        }

        public override void PauseLogic()
        {

        }

        public override void ResumeLogic()
        {

        }


        protected void StopTree(BTStopMode bTStopMode)
        {

        }

        protected void RestartTree()
        {

        }

        public bool IsExecutingBranch(BTNode node, int childIndex)
        {
            int testInstanceIdx = FindInstanceContainingNode(node);

            if (knownInstances.IsValidIndex(testInstanceIdx) || knownInstances[testInstanceIdx].activeNode == null)
            {
                return false;
            }

            BehaviorTree testInstance = knownInstances[testInstanceIdx];

            if (node == testInstance.rootNode || node == testInstance.activeNode)
            {
                return true;
            }

            ushort activeExecutionIdx = testInstance.activeNode.GetExecutionIndex();
            ushort nextChildExecutionIdx = node.GetParentNode().GetChildExecutionIndex(childIndex + 1);

            return (activeExecutionIdx >= node.GetExecutionIndex()) && (activeExecutionIdx < nextChildExecutionIdx);
        }
    }
}
