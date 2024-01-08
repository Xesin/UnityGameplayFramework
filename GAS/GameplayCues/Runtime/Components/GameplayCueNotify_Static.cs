using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Xesin.GameplayCues
{
    public abstract class GameplayCueNotify_Static : GameplayCueNotify_GameObject
    {

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
