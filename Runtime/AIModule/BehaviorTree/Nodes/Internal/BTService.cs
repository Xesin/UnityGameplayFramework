using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{

    public class BTService : BTAuxiliaryNode
    {
        [SerializeField] private float interval = 0.5f;
        [SerializeField] private float randomDeviation = 0.1f;
        [SerializeField] private bool callTickOnSearchStart = false;
        [SerializeField] private bool restartTimerOnEachActivation = false;

        private void Awake()
        {
            tickIntervals = true;
        }

        protected override void InternalTickNode(BehaviorTreeComponent owner, float deltaTime)
        {
            ScheduleNextTick(owner);
        }

        public void NotifyParentActivation(BehaviorTreeSearchData searchData)
        {
            float remainingTime = restartTimerOnEachActivation ? 0 : GetNextTickRemainingTime();
            if(remainingTime <= 0f)
            {
                ScheduleNextTick(searchData.ownerComp);
            }

            OnSearchStart(searchData);

            if(callTickOnSearchStart)
            {
                InternalTickNode(searchData.ownerComp, 0f);
            }
        }

        protected virtual void OnSearchStart(BehaviorTreeSearchData searchData)
        {
            
        }

        protected virtual void ScheduleNextTick(BehaviorTreeComponent ownerComponent)
        {
            SetNextTickTime(UnityEngine.Random.Range(Math.Max(0f, interval - randomDeviation), (interval + randomDeviation)));
        }

#if UNITY_EDITOR
        protected override string GetDefaultName()
        {
            return "Service";
        }
#endif
    }
}
