using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    public enum BTNodeResult
    {
        Succeeded,
        Failed,
        Aborted,
        InProgress
    }

    public abstract class BTNode : ScriptableObject
    {
        [SerializeField] protected BehaviorTree treeAsset;
        [SerializeField] protected BTComposite parentNode;
        [SerializeField] protected ushort executionIndex;
        public string nodeName = string.Empty;

#if UNITY_EDITOR
        [HideInInspector] public Vector2 position;
        [HideInInspector] public string nodeId;
#endif


        protected SceneObject Owner;

        public byte TreeDepth { get; private set; }

        public BTNode()
        {
#if UNITY_EDITOR
            executionIndex = BTComposite.NOT_INITIALIZED_CHILD;
            OnValidate();
#endif
        }

        internal virtual void InitializeInSubtree(BehaviorTreeComponent ownerComp, BehaviorTree behaviorTree, int instanceIndex)
        {
            SetOwner(ownerComp.Owner);
            InitializeFromAsset(behaviorTree);
        }

        public void InitializeNode(BTComposite parentNode, ushort executionIndex)
        {
            this.parentNode = parentNode;
            this.executionIndex = executionIndex;
        }

        public void InitializeFromAsset(BehaviorTree behaviorTree)
        {
            treeAsset = behaviorTree;
        }


        public BTComposite GetParentNode()
        {
            return parentNode;
        }

        public BehaviorTree GetTreeAsset()
        {
            return treeAsset;
        }

        public BlackboardData GetBlackboardData()
        {
            return treeAsset ? treeAsset.blackboardAsset : null;
        }

        public ushort GetExecutionIndex()
        {
            return executionIndex;
        }

        public ushort GetNexExecutionIndex(ushort execIndex)
        {
            return GetParentNode() || treeAsset.rootNode == this ? (ushort) (execIndex + 1): BTComposite.NOT_INITIALIZED_CHILD;
        }

        protected void SetOwner(SceneObject owner)
        {
            Owner = owner;
        }


#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if(string.IsNullOrEmpty(nodeName))
            {
                nodeName = GetDefaultName();
            }
        }

        protected abstract string GetDefaultName();

        public void SetExecutionIndex(int newIndex)
        {
            executionIndex = (ushort) newIndex;
        }

        public void SetParent(BTComposite newParent)
        {
            parentNode = newParent;

            if(newParent == null)
            { 
                executionIndex = BTComposite.NOT_INITIALIZED_CHILD;

                for (int i = 0; i < treeAsset.compositeNodes.Count; i++)
                {
                    if (treeAsset.compositeNodes[i].GetNode() == this)
                    {
                        for (int decoratorIdx = 0; decoratorIdx < treeAsset.compositeNodes[i].Decorators.Count; decoratorIdx++)
                        {
                            treeAsset.compositeNodes[i].Decorators[decoratorIdx].SetExecutionIndex(executionIndex);
                        }
                    }
                }
            }
        }
#endif
    }
}
