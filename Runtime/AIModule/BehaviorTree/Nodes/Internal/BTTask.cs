using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public abstract class BTTask : BTNode
    {
        [SerializeField] private bool ignoreRestartSelf = false;

        public List<BTService> services = new List<BTService>();
        public BTTaskStatus Status { get; set; }

        protected bool tickIntervals = false;
        private float nextTickRemainingTime;
        private float accumulatedDeltaTime;

        public virtual BTNodeResult AbortTask(BehaviorTreeComponent behaviorTreeComponent)
        {
            return BTNodeResult.Aborted;
        }

        public virtual BTNodeResult ExecuteTask(BehaviorTreeComponent behaviorTreeComponent)
        {
            return BTNodeResult.Succeeded;
        }

        public bool WrappedTick(BehaviorTreeComponent ownerComp, float deltaTime, ref float nextNeededDeltaTime)
        {
            if (tickIntervals)
            {
                nextTickRemainingTime -= deltaTime;
                accumulatedDeltaTime += deltaTime;

                bool tick = nextTickRemainingTime <= 0f;
                if (tick)
                {
                    float useDeltaTime = accumulatedDeltaTime;
                    accumulatedDeltaTime = 0f;

                    Tick(ownerComp, accumulatedDeltaTime);
                }

                if (nextTickRemainingTime < nextNeededDeltaTime)
                {
                    nextNeededDeltaTime = nextTickRemainingTime;
                }

                return tick;
            }
            else
            {
                Tick(ownerComp, deltaTime);
                nextTickRemainingTime = 0f;
                return true;
            }
        }

        protected virtual void Tick(BehaviorTreeComponent ownerComp, float deltaSeconds)
        {
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

        protected void SetNextTickTime(float remainingTime)
        {
            if (tickIntervals)
            {
                nextTickRemainingTime = remainingTime;
            }
        }

        protected float GetNextTickRemainingTime()
        {
            if (tickIntervals)
            {
                return Mathf.Max(0, nextTickRemainingTime);
            }

            return 0f;
        }
    }
}
