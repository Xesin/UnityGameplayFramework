using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public abstract class BTTaskNode : BTNode
    {
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

        internal virtual void Tick(BehaviorTreeComponent ownerComp, float deltaSeconds)
        {

        }

        public virtual void OnTaskFinished(BehaviorTreeComponent behaviorTreeComponent, BTNodeResult taskResult)
        {
        }
    }
}
