using UnityEngine;
using Xesin.GameplayFramework.Domain;

namespace Xesin.GameplayCues
{
    public abstract class GameplayCueNotify_Static : GameplayCueNotify_GameObject
    {

        [ExecuteOnReload]
        private static void OnReload()
        {
            var allCues = Resources.FindObjectsOfTypeAll<GameplayCueNotify_Static>();
            for (int i = 0; i < allCues.Length; i++)
            {
                allCues[i].ResetCues();
            }
        }

        protected virtual void ResetCues()
        {

        }

        public override bool CanBeRecycled()
        {
            return false;
        }

        public override bool HandlesEvent(GameplayCueEvent EventType)
        {
            return EventType == GameplayCueEvent.OnActive || EventType == GameplayCueEvent.Removed;
        }

        public override void HandleGameplayCue(GameObject MyTarget, GameplayCueEvent EventType, GameplayCueParameters Parameters)
        {
            if (MyTarget)
            {
                SetLifeSpan(0);
                CancelInvoke(nameof(GameplayCueFinishedCallback));
                switch (EventType)
                {
                    case GameplayCueEvent.OnActive:
                        gameObject.SetActive(true);
                        OnActive(MyTarget, Parameters);
                        break;
                    case GameplayCueEvent.Removed:
                        OnRemove(MyTarget, Parameters);

                        if (autoDestroyOnRemove)
                        {
                            if (autoDestroyDelay > 0f)
                            {
                                Invoke(nameof(GameplayCueFinishedCallback), autoDestroyDelay);
                            }
                            else
                            {
                                GameplayCueFinishedCallback();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Debug.LogWarning($"Null target called for event {EventType} on GameplayCueNotify_GameObject {name}");

                if (EventType == GameplayCueEvent.Removed)
                {
                    // Make sure the removed event is handled so that we don't leak GC notify actors
                    GameplayCueFinishedCallback();
                }
            }
        }
    }
}
