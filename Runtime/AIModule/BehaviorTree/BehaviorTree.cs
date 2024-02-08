using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public enum TreeExecutionMode
    {
        Looped,
        SingleRun
    }

    [CreateAssetMenu(fileName = "BehaviorTree.asset", menuName = "Gameplay/AI/Behavior Tree")]
    public class BehaviorTree : ScriptableObject
    {
        public BlackboardData blackboardAsset;
        public BTCompositeNode rootNode;
        public List<BTNode> nodes;

        [NonSerialized] public BTNode activeNode;
        [NonSerialized] public BTActiveNode activeNodeType;

        public List<BTAuxiliaryNode> ActiveAuxNodes { get; private set; } = new List<BTAuxiliaryNode>();
        public List<BTTaskNode> ParallelTasks { get; private set; } = new List<BTTaskNode>();

        internal void Initialize(BehaviorTreeComponent ownerComp, BTCompositeNode Node, int instanceIndex)
        {
            for (int serviceIndex = 0; serviceIndex < Node.services.Count; serviceIndex++)
            {
                Node.services[serviceIndex].InitializeInSubtree(ownerComp, this, instanceIndex);
            }

            Node.InitializeInSubtree(ownerComp, this, instanceIndex);

            for (int childIndex = 0; childIndex < Node.children.Count; childIndex++)
            {
                BTCompositeChild childInfo = Node.children[childIndex];

                for (int decoratorIndex = 0; decoratorIndex < childInfo.Decorators.Count; decoratorIndex++)
                {
                    BTDecorator decorator = childInfo.Decorators[decoratorIndex];
                    decorator.InitializeInSubtree(ownerComp, this, instanceIndex);
                }

                if (childInfo.childComposite)
                {
                    Initialize(ownerComp, childInfo.childComposite, instanceIndex);
                }
                else if (childInfo.childTask)
                {
                    for (int serviceIndex = 0; serviceIndex < childInfo.childTask.services.Count; serviceIndex++)
                    {
                        BTService service = childInfo.childTask.services[serviceIndex];
                        service.InitializeInSubtree(ownerComp, this, instanceIndex);
                    }

                    childInfo.childTask.InitializeInSubtree(ownerComp, this, instanceIndex);
                }
            }
        }

        internal void AddToActiveAuxNodes(BTAuxiliaryNode auxNode)
        {
            ActiveAuxNodes.Add(auxNode);
        }

        internal void RemoveFromActiveAuxNodes(BTAuxiliaryNode auxNode)
        {
            ActiveAuxNodes.Remove(auxNode);
        }

        internal void MarkParallelTaskAsAbortingAt(int taskId)
        {
            ParallelTasks[taskId].Status = BTTaskStatus.Aborting;
        }

        internal void AddToParallelTasks(BTTaskNode taskNode)
        {
            ParallelTasks.Add(taskNode);
        }

        internal void ExecuteOnEachAuxNode(Action<BTAuxiliaryNode> value)
        {
            for (int i = 0; i < ActiveAuxNodes.Count; i++)
            {
                value.Invoke(ActiveAuxNodes[i]);
            }
        }

        internal void ExecuteOnEachParallelTask(Action<BTTaskNode, int> value)
        {
            for (int i = 0; i < ParallelTasks.Count; i++)
            {
                value.Invoke(ParallelTasks[i], i);
            }
        }

        internal void ResetActiveAuxNodes()
        {
            ActiveAuxNodes.Clear();
        }

        internal bool IsValidParallelTaskIndex(int taskIndex)
        {
            return ParallelTasks.IsValidIndex(taskIndex);
        }

        internal void DeactivateNodes(BehaviorTreeSearchData searchData, ushort instanceIndex)
        {
            for (int Idx = searchData.PendingUpdates.Count - 1; Idx >= 0; Idx--)
            {
                BehaviorTreeSearchUpdate updateInfo = searchData.PendingUpdates[Idx];
                if (updateInfo.instanceIndex == instanceIndex && updateInfo.mode == BTNodeUpdateMode.Add)
                {
                    searchData.PendingUpdates.RemoveAt(Idx);
                }
            }

            for (int i = 0; i < ParallelTasks.Count; i++)
            {
                BTTaskNode task = ParallelTasks[i];
                if (task && task.Status == BTTaskStatus.Active)
                {
                    searchData.AddUniqueUpdate(new BehaviorTreeSearchUpdate(task, instanceIndex, BTNodeUpdateMode.Remove));
                }
            }

            for (int i = 0; i < ActiveAuxNodes.Count; i++)
            {
                BTAuxiliaryNode auxNode = ActiveAuxNodes[i];
                if (auxNode)
                {
                    searchData.AddUniqueUpdate(new BehaviorTreeSearchUpdate(auxNode, instanceIndex, BTNodeUpdateMode.Remove));
                }
            }
        }

        internal bool HasActiveNode(ushort testExecutionIndex)
        {
            if (activeNode && activeNode.GetExecutionIndex() == testExecutionIndex)
            {
                return (activeNodeType == BTActiveNode.ActiveTask);
            }

            for (int i = 0; i < ParallelTasks.Count; i++)
            {
                BTTaskNode task = ParallelTasks[i];
                if (task.GetExecutionIndex() == testExecutionIndex)
                {
                    return task.Status == BTTaskStatus.Active;
                }
            }

            for (int i = 0; i < ActiveAuxNodes.Count; i++)
            {
                if (ActiveAuxNodes[i] && ActiveAuxNodes[i].GetExecutionIndex() == testExecutionIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
