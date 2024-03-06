using UnityEngine;

namespace Xesin.GameplayCues
{
    public class GameplayCueNotify_Burst : GameplayCueNotify_Static
    {
        [SerializeField] protected GameplayCueNotify_BurstEffects applicationEffects;

        protected GameplayCueNotify_SpawnResult applicationSpawnResults = new GameplayCueNotify_SpawnResult(0);

        protected override bool OnActive(GameObject target, GameplayCueParameters parameters)
        {
            GameplayCueNotify_SpawnContext context = new GameplayCueNotify_SpawnContext(target, parameters);
            context.SetDefaultPlacementInfo(defaultPlacementInfo);

            applicationEffects.ExecuteEffects(context, ref applicationSpawnResults);

            return false;
        }

        public override bool Recycle()
        {
            base.Recycle();
            applicationSpawnResults.Reset();
            return true;
        }
    }
}
