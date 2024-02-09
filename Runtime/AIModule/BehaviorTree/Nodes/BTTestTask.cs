using System;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    [CreateAssetMenu(fileName = "LogTask", menuName = "TEstTask")]
    public class BTTestTask : BTTask
    {
        public bool isSucceeded = true;
        private float startTime = 0;
        public override BTNodeResult ExecuteTask(BehaviorTreeComponent behaviorTreeComponent)
        {
            Debug.Log("Executed task" + GetExecutionIndex());
            startTime = Time.time;
            return isSucceeded ? BTNodeResult.InProgress : BTNodeResult.Failed;
        }

        protected override void Tick(BehaviorTreeComponent ownerComp, float deltaSeconds)
        {
            Debug.Log("Tick task " + GetExecutionIndex());
            if (Time.time - startTime > 2) FinishLatentTask(ownerComp, BTNodeResult.Succeeded);
        }

#if UNITY_EDITOR
        protected override string GetDefaultName()
        {
            return "TestTask";
        }
#endif

    }
}
