using System;
using System.Collections.Generic;

namespace Xesin.GameplayFramework.AI
{
    public enum BTActiveNode
    {
        Composite,
        ActiveTask,
        AbortingTask,
        InactiveTask,
    };


    public enum BTStopMode
    {
        Safe,
        Forced,
    }

    public enum BTRestartMode
    {
        ForceReevaluateRootNode,
        CompleteRestart,
    }

    public enum BTFlowAbortMode
    {
        None,
        LowerPriority,
        Self,
        Both
    }

    public enum BTNodeUpdateMode
    {
        Unknown,
        Add,
        Remove
    }

    public enum BTTaskStatus
    {
        Active,
        Aborting,
        Inactive,
    }

    static class BTSpecialChild
    {
        public const int NotInitialized = -1; // special value for child indices: needs to be initialized
        public const int ReturnToParent = -2; // special value for child indices: return to parent node

        public const byte OwnedByComposite = byte.MaxValue;    // special value for aux node's child index: owned by composite node instead of a task
    }

    public struct BranchActionInfo
    {
        public BranchActionInfo(BTBranchAction InAction)
        {
            action = InAction;
            node = null;
            continueWithResult = BTNodeResult.Succeeded;
        }

        public BranchActionInfo(BTNode InNode, BTBranchAction InAction)
        {
            node = InNode;
            action = InAction;
            continueWithResult = BTNodeResult.Succeeded;
        }

        public BranchActionInfo(BTNode InNode, BTNodeResult InContinueWithResult, BTBranchAction InAction)
        {
            node = InNode;
            continueWithResult = InContinueWithResult;
            action = InAction;
        }

        public BTNode node;
        public BTNodeResult continueWithResult;
        public BTBranchAction action;
    };

    public struct BTNodeIndex
    {
        const ushort InvalidIndex = ushort.MaxValue;

        public ushort instanceIndex;
        public ushort executionIndex;

        public BTNodeIndex(ushort inInstanceIndex, ushort inExecutionIndex)
        {
            instanceIndex = inInstanceIndex;
            executionIndex = inExecutionIndex;
        }

        public bool TakesPriorityOver(BTNodeIndex other)
        {
            if (instanceIndex != other.instanceIndex)
            {
                return instanceIndex < other.instanceIndex;
            }

            return executionIndex < other.executionIndex;
        }

        public bool IsSet()
        {
            return instanceIndex < InvalidIndex;
        }

        public static bool operator ==(BTNodeIndex c1, BTNodeIndex c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(BTNodeIndex c1, BTNodeIndex c2)
        {
            return !(c1 == c2);
        }

        public override bool Equals(object obj)
        {
            if (obj is not BTNodeIndex other) return false;

            return other.executionIndex == executionIndex && other.instanceIndex == instanceIndex;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(instanceIndex, executionIndex);
        }
    }

    public struct BehaviorTreeSearchData
    {
        private static int NextSearchId = 1;

        public BehaviorTreeComponent ownerComp;

        public List<BehaviorTreeSearchUpdate> PendingUpdates;

        public BTNodeIndex searchRootNode;
        public BTNodeIndex searchStart;
        public BTNodeIndex searchEnd;

        public int searchId;
        public int rollbackInstanceIdx;

        public BTNodeIndex DeactivatedBranchStart;

        public BTNodeIndex DeactivatedBranchEnd;

        public BTNodeIndex RollbackDeactivatedBranchStart;

        public BTNodeIndex RollbackDeactivatedBranchEnd;

        public bool filterOutRequestFromDeactivatedBranch;
        public bool postponeSearch;
        public bool searchInProgress;
        public bool bPreserveActiveNodeMemoryOnRollback;

        public BehaviorTreeSearchData(BehaviorTreeComponent inOwnerComp)
        {
            ownerComp = inOwnerComp;
            rollbackInstanceIdx = -1;
            filterOutRequestFromDeactivatedBranch = false;
            postponeSearch = false;
            searchInProgress = false;
            bPreserveActiveNodeMemoryOnRollback = false;

            searchRootNode = default;
            searchStart = default;
            searchEnd = default;

            searchId = 0;

            DeactivatedBranchStart = default;
            DeactivatedBranchEnd = default;
            RollbackDeactivatedBranchStart = default;
            RollbackDeactivatedBranchEnd = default;

            PendingUpdates = new List<BehaviorTreeSearchUpdate>();
        }

        internal void AddUniqueUpdate(BehaviorTreeSearchUpdate updateInfo)
        {
            bool skipAdding = false;

            for (int updateIndex = 0; updateIndex < PendingUpdates.Count; updateIndex++)
            {
                BehaviorTreeSearchUpdate info = PendingUpdates[updateIndex];
                if (info.auxNode == updateInfo.auxNode && info.taskNode == updateInfo.taskNode)
                {
                    // Duplicated, skip
                    if (info.mode == updateInfo.mode)
                    {
                        skipAdding = true;
                        break;
                    }
                }

                skipAdding = (info.mode == BTNodeUpdateMode.Remove) || (updateInfo.mode == BTNodeUpdateMode.Remove);

                PendingUpdates.RemoveAt(updateIndex);
            }

            if(!skipAdding && updateInfo.mode == BTNodeUpdateMode.Remove && updateInfo.auxNode)
            {
                bool isActive = ownerComp.IsAuxNodeActive(updateInfo.auxNode, updateInfo.instanceIndex);
                skipAdding = !isActive;
            }

            if(!skipAdding)
            {
                updateInfo.postUpdate = (updateInfo.mode == BTNodeUpdateMode.Add) && (updateInfo.auxNode is BTService);
                PendingUpdates.Add(updateInfo);
            }
        }

        public void AssignSearchId()
        {
            searchId = NextSearchId;
            NextSearchId++;
        }

        internal void Reset()
        {
            PendingUpdates.Clear();
            rollbackInstanceIdx = -1;
            filterOutRequestFromDeactivatedBranch = false;
            postponeSearch = false;
            searchInProgress = false;
            bPreserveActiveNodeMemoryOnRollback = false;

            searchRootNode = default;
            searchStart = default;
            searchEnd = default;

            DeactivatedBranchStart = default;
            DeactivatedBranchEnd = default;
            RollbackDeactivatedBranchStart = default;
            RollbackDeactivatedBranchEnd = default;
        }
    }

    public struct BTNodeIndexRange
    {
        /** first node index */
        BTNodeIndex fromIndex;

        /** last node index */
        BTNodeIndex toIndex;

        public BTNodeIndexRange(BTNodeIndex From, BTNodeIndex To)
        {
            fromIndex = From;
            toIndex = To;
        }

        public bool IsSet() { return fromIndex.IsSet() && toIndex.IsSet(); }

        public bool Contains(BTNodeIndex Index)
        {
            return Index.instanceIndex == fromIndex.instanceIndex && fromIndex.executionIndex <= Index.executionIndex && Index.executionIndex <= toIndex.executionIndex;
        }
    }

    public struct BehaviorTreeSearchUpdate
    {
        public BTAuxiliaryNode auxNode;
        public BTTask taskNode;

        public ushort instanceIndex;

        public BTNodeUpdateMode mode;

        public bool postUpdate;

        public bool applySkipped;

        public BehaviorTreeSearchUpdate(BTAuxiliaryNode inAuxNode, ushort inInstanceIndex, BTNodeUpdateMode inMode)
        {
            auxNode = inAuxNode;
            taskNode = null;
            instanceIndex = inInstanceIndex;
            mode = inMode;
            postUpdate = true;
            applySkipped = true;
        }

        public BehaviorTreeSearchUpdate(BTTask inTaskNode, ushort inInstanceIndex, BTNodeUpdateMode inMode)
        {
            taskNode = inTaskNode;
            auxNode = null;
            instanceIndex = inInstanceIndex;
            mode = inMode;
            postUpdate = true;
            applySkipped = true;
        }
    }
}
