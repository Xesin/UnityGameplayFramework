using UnityEngine;

namespace Xesin.GameplayCues
{
    public class GameplayCueNotify_Loop : GameplayCueNotify_GameObject
    {
        [SerializeField] protected GameplayCueNotify_BurstEffects applicationEffects;
        [SerializeField] protected GameplayCueNotify_LoopEffects loopingEffects;
        [SerializeField] protected GameplayCueNotify_BurstEffects recurringEffects;
        [SerializeField] protected GameplayCueNotify_BurstEffects removalEffects;

        protected GameplayCueNotify_SpawnResult applicationSpawnResults = new GameplayCueNotify_SpawnResult(0);
        protected GameplayCueNotify_SpawnResult loopingSpawnResults = new GameplayCueNotify_SpawnResult(0);
        protected GameplayCueNotify_SpawnResult recurringSpawnResults = new GameplayCueNotify_SpawnResult(0);
        protected GameplayCueNotify_SpawnResult removalSpawnResults = new GameplayCueNotify_SpawnResult(0);

        private bool loopingEfectsRemoved = true;

        protected override bool OnActive(GameObject target, GameplayCueParameters parameters)
        {
            GameplayCueNotify_SpawnContext context = new GameplayCueNotify_SpawnContext(target, parameters);
            context.SetDefaultPlacementInfo(defaultPlacementInfo);

            applicationEffects.ExecuteEffects(context, ref applicationSpawnResults);

            return false;
        }

        protected override bool WhileActive(GameObject target, GameplayCueParameters parameters)
        {
            GameplayCueNotify_SpawnContext context = new GameplayCueNotify_SpawnContext(target, parameters);
            context.SetDefaultPlacementInfo(defaultPlacementInfo);

            loopingEfectsRemoved = false;
            loopingEffects.StartEffects(context, ref loopingSpawnResults);

            return false;
        }

        protected override bool OnExecute(GameObject target, GameplayCueParameters parameters)
        {
            GameplayCueNotify_SpawnContext context = new GameplayCueNotify_SpawnContext(target, parameters);
            context.SetDefaultPlacementInfo(defaultPlacementInfo);

            recurringEffects.ExecuteEffects(context, ref recurringSpawnResults);

            return false;
        }

        protected override bool OnRemove(GameObject target, GameplayCueParameters parameters)
        {
            RemoveLoopingEffects();

            if (target)
            {
                GameplayCueNotify_SpawnContext context = new GameplayCueNotify_SpawnContext(target, parameters);
                context.SetDefaultPlacementInfo(defaultPlacementInfo);

                removalEffects.ExecuteEffects(context, ref removalSpawnResults);
            }

            return false;
        }

        public override bool Recycle()
        {
            base.Recycle();

            RemoveLoopingEffects();

            applicationSpawnResults.Reset();
            loopingSpawnResults.Reset();
            recurringSpawnResults.Reset();
            removalSpawnResults.Reset();

            loopingEfectsRemoved = true;
            return true;
        }

        private void RemoveLoopingEffects()
        {
            if (loopingEfectsRemoved) return;

            loopingEfectsRemoved = true;

            loopingEffects.StopEffects(ref loopingSpawnResults);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RemoveLoopingEffects();
            applicationEffects.CleanUp();
            loopingEffects.CleanUp();
            recurringEffects.CleanUp();
            removalEffects.CleanUp();
        }
    }
}
