using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public abstract class BTTaskNode : BTNode
    {
        [SerializeField] private bool ignoreRestartSelf = false;

        public List<BTService> services = new List<BTService>();
        public BTTaskStatus Status { get; set; }

        public virtual BTNodeResult AbortTask(BehaviorTreeComponent behaviorTreeComponent)
        {
            return BTNodeResult.Aborted;
        }

        public virtual BTNodeResult ExecuteTask(BehaviorTreeComponent behaviorTreeComponent)
        {
            return BTNodeResult.Succeeded;
        }

        internal virtual bool Tick(BehaviorTreeComponent ownerComp, float deltaSeconds)
        {
            return true;
        }

        public virtual void OnTaskFinished(BehaviorTreeComponent behaviorTreeComponent, BTNodeResult taskResult)
        {
        }

        public void FinishLatentTask(BehaviorTreeComponent ownerComp, BTNodeResult taskResult)
        {
            ownerComp.OnTaskFinished(this, taskResult);
        }

        internal bool ShouldIgnoreRestartSelf()
        {
            return ignoreRestartSelf;
        }
    }
}
