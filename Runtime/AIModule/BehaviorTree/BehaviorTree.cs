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

    public class BehaviorTree : ScriptableObject
    {
        public BlackboardData BlackboardAsset { get; set; }

        public BTCompositeNode rootNode;

        public BTNode activeNode;
        public BTActiveNode activeNodeType;

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

                for (int decoratorIndex = 0; decoratorIndex < childInfo.decorators.Count; decoratorIndex++)
                {
                    BTDecorator decorator = childInfo.decorators[decoratorIndex];
                    decorator.InitializeInSubtree(ownerComp, this, instanceIndex);
                }

                if(childInfo.childComposite)
                {
                    Initialize(ownerComp, childInfo.childComposite, instanceIndex);
                }
                else if(childInfo.childTask)
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
    }
}
