using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public enum TreeExecutionMode
    {
        Looped,
        SingleRun
    }


    public struct TreeStartInfo
    {
        public BehaviorTree asset;
        public bool pendingInitialize;
        public TreeExecutionMode executionMode;

        public bool IsSet()
        {
            return asset != null;
        }
    }

    public class BehaviorTree : ScriptableObject
    {
        public BlackboardData BlackboardAsset { get; set; }

        public BTCompositeNode rootNode;

        public BTNode activeNode;

        internal void Initialize(BehaviorTreeComponent behaviorTreeComponent)
        {
            throw new NotImplementedException();
        }
    }
}
