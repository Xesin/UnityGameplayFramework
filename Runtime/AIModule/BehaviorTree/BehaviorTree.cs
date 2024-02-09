using System;
using System.Collections.Generic;
using UnityEditor;
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
        public BTComposite rootNode;

#if UNITY_EDITOR
        public List<BTCompositeChild> compositeNodes = new List<BTCompositeChild>();
#endif

        [NonSerialized] public BTNode activeNode;
        [NonSerialized] public BTActiveNode activeNodeType;

        public List<BTAuxiliaryNode> ActiveAuxNodes { get; private set; } = new List<BTAuxiliaryNode>();
        public List<BTTask> ParallelTasks { get; private set; } = new List<BTTask>();

        internal void Initialize(BehaviorTreeComponent ownerComp, BTComposite Node, int instanceIndex)
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

        internal void AddToParallelTasks(BTTask taskNode)
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

        internal void ExecuteOnEachParallelTask(Action<BTTask, int> value)
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
                BTTask task = ParallelTasks[i];
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
                BTTask task = ParallelTasks[i];
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

#if UNITY_EDITOR

        public BTCompositeChild CreateNode(Type type)
        {
            var newNode = ScriptableObject.CreateInstance(type) as BTNode;
            newNode.name = type.Name;
            newNode.nodeId = GUID.Generate().ToString();

            var result = new BTCompositeChild();

            compositeNodes.Add(result);
            if (newNode is BTComposite composite)
            {
                result.childComposite = composite;
            }
            else if (newNode is BTTask taskNode)
            {
                result.childTask = taskNode;
            }

            AssetDatabase.AddObjectToAsset(newNode, this);
            AssetDatabase.SaveAssets();

            return result;
        }

        public void DeleteNode(BTNode node)
        {
            BTCompositeChild child = GetChildByID(node.nodeId);
            BTCompositeChild parent = node.GetParentNode() ? GetChildByID(node.GetParentNode().nodeId) : null;
            if (child != null && parent != null)
            {
                RemoveChild(parent, child);
            }
            for (int i = compositeNodes.Count - 1; i >= 0; i--)
            {
                if (compositeNodes[i].childComposite == node || compositeNodes[i].childTask == node)
                {
                    compositeNodes.RemoveAt(i);
                    continue;
                }
            }

            if (rootNode == node)
                rootNode = null;

            AssetDatabase.RemoveObjectFromAsset(node);
            AssetDatabase.SaveAssets();
        }

        public void AddChild(BTCompositeChild parent, BTCompositeChild child)
        {
            if (parent == null)
            {
                var composite = child.childComposite;
                rootNode = composite;
                composite.SetExecutionIndex(0);
                composite.InitializeComposite((ushort)composite.children.Count);
            }
            else if (parent.childComposite && parent.childComposite is BTComposite composite)
            {
                composite.children.Add(child);

                BTNode childNode = child.GetNode();
                if(childNode)
                {
                    childNode.SetParent(parent.childComposite);
                }
            }
        }

        public void AddAuxNode(BTCompositeChild parent, BTAuxiliaryNode auxNode)
        {
            if (parent == null)
            {

            }
            else
            {
                if (auxNode is BTDecorator decorator)
                {
                    decorator.SetChildIndex(parent.childComposite, parent.Decorators.Count);
                    parent.Decorators.Add(decorator);
                }
                else if (parent.childComposite)
                {
                    parent.childComposite.services.Add(auxNode as BTService);
                }
                else if (parent.childTask)
                {
                    parent.childTask.services.Add(auxNode as BTService);
                }
            }
        }

        public void RemoveChild(BTCompositeChild parent, BTCompositeChild child)
        {
            if(parent == null)
            {
                rootNode = null;
            } else if (parent.childComposite)
            {
                var children = parent.childComposite.children;
                BTNode node = child.GetNode();
                node.SetParent(null);
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    if ((children[i].childComposite == node) ||
                        (children[i].childTask == node))
                    {
                        children.RemoveAt(i);
                    }
                }
            }
        }

        public BTCompositeChild GetChildByID(string guid)
        {
            for (int i = 0; i < compositeNodes.Count; i++)
            {
                BTNode node = compositeNodes[i].childComposite ? compositeNodes[i].childComposite : compositeNodes[i].childTask;

                if (node.nodeId == guid)
                    return compositeNodes[i];
            }

            return null;
        }

        //public List<BTNode> GetChildren(BTNode parent)
        //{

        //}
#endif
    }
}
