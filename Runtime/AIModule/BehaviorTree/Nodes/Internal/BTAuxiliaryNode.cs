using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public abstract class BTAuxiliaryNode : BTNode
    {
        [SerializeField] protected ushort childIndex;
        [SerializeField] protected bool tickIntervals;

        private float nextTickRemainingTime;
        private float accumulatedDeltaTime;

        public ushort ChildIndex => childIndex;

        public void TickNode(BehaviorTreeComponent owner, float deltaTime, ref float nextNeededDeltaTime)
        {
            if(tickIntervals)
            {
                nextTickRemainingTime -= deltaTime;
                accumulatedDeltaTime += deltaTime;

                bool tick = nextTickRemainingTime <= 0f;
                if(tick)
                {
                    float useDeltaTime = accumulatedDeltaTime;
                    accumulatedDeltaTime = 0f;

                    InternalTickNode(owner, accumulatedDeltaTime);
                }

                if(nextTickRemainingTime < nextNeededDeltaTime)
                {
                    nextNeededDeltaTime = nextTickRemainingTime;
                }
            }
            else
            {
                InternalTickNode(owner, deltaTime);
                nextTickRemainingTime = 0f;
            }
        }

        protected virtual void InternalTickNode(BehaviorTreeComponent owner, float deltaTime)
        {

        }

        public virtual void OnBecomeRelevant(BehaviorTreeComponent ownerComp)
        {
            
        }

        public virtual void OnCeaseRelevant(BehaviorTreeComponent ownerComp)
        {
            
        }

        protected void SetNextTickTime(float remainingTime)
        {
            if(tickIntervals)
            {
                nextTickRemainingTime = remainingTime;
            }
        }

        protected float GetNextTickRemainingTime()
        {
            if(tickIntervals)
            {
                return Mathf.Max(0, nextTickRemainingTime);
            }

            return 0f;
        }

        internal BTNode GetMyNode()
        {
            return (ChildIndex == BTSpecialChild.OwnedByComposite) ? GetParentNode() : (GetParentNode() ? GetParentNode().GetChildNode(childIndex) : null);
        }
    }
}
