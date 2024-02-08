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

    public class BTNode : ScriptableObject
    {
        [SerializeField] protected BehaviorTree treeAsset;
        [SerializeField] protected BTCompositeNode parentNode;
        [SerializeField] private ushort executionIndex;

#if UNITY_EDITOR
        [SerializeField, HideInInspector] private Vector2 position;
        [SerializeField, HideInInspector] private Guid nodeId;
#endif

        protected SceneObject Owner;

        public byte TreeDepth { get; private set; }

        internal virtual void InitializeInSubtree(BehaviorTreeComponent ownerComp, BehaviorTree behaviorTree, int instanceIndex)
        {
            SetOwner(ownerComp.Owner);
            InitializeFromAsset(behaviorTree);
        }

        public void InitializeNode(BTCompositeNode parentNode, ushort executionIndex)
        {
            this.parentNode = parentNode;
            this.executionIndex = executionIndex;
        }

        public void InitializeFromAsset(BehaviorTree behaviorTree)
        {
            treeAsset = behaviorTree;
        }


        public BTCompositeNode GetParentNode()
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

        internal ushort GetExecutionIndex()
        {
            return executionIndex;
        }

        protected void SetOwner(SceneObject owner)
        {
            Owner = owner;
        }
    }
}
