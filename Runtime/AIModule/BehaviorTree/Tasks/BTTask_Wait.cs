using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    [CreateAssetMenu(fileName = "WaitTask", menuName = "WaitTask")]
    public class BTTask_Wait : BTTaskNode
    {
        [SerializeField] private float waitTime;
        [SerializeField] private float randomDeviation;

        private void Awake()
        {
            tickIntervals = true;
        }

        public override BTNodeResult ExecuteTask(BehaviorTreeComponent behaviorTreeComponent)
        {
            float reaminingWaitingTime = UnityEngine.Random.Range(Math.Max(0f, waitTime - randomDeviation), (waitTime + randomDeviation));
            SetNextTickTime(reaminingWaitingTime);

            return BTNodeResult.InProgress;
        }

        protected override void Tick(BehaviorTreeComponent ownerComp, float deltaSeconds)
        {
            FinishLatentTask(ownerComp, BTNodeResult.Succeeded);
        }
    }
}
