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


    public class BTNode : ScriptableObject
    {
        [SerializeField] protected BehaviorTree treeAsset;
        [SerializeField] protected BTCompositeNode parentNode;
        [SerializeField] private ushort executionIndex;

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
            return treeAsset ? treeAsset.BlackboardAsset : null;
        }

        internal ushort GetExecutionIndex()
        {
            return executionIndex;
        }
    }
}
