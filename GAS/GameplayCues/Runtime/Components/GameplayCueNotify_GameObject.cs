using System;
using UnityEngine;
using Xesin.AddressablesExtensions;

namespace Xesin.GameplayCues
{
    [RequireComponent(typeof(MonoBehaviourReleaseEvent))]
    public class GameplayCueNotify_GameObject : MonoBehaviour
    {
        [NonSerialized]
        private bool hasHandledOnActiveEvent;
        [NonSerialized]
        private bool hasHandledWhileActiveEvent;
        [NonSerialized]
        private bool hasHandledOnRemoveEvent;
        [SerializeField]
        protected GameplayCueNotify_PlacementInfo defaultPlacementInfo = GameplayCueNotify_PlacementInfo.Default;

        [field: SerializeField, Tooltip("Tag this notify is activated by")]
        public GameplayTag TriggerTag { get; private set; }

        [NonSerialized]
        public bool inRecycleQueue = false;

        /// <summary>
        // *	Does this cue get a new instance for each instigator? For example if two instigators apply a GC to the same source, do we create two of these GameplayCue Notify actors or just one?
        // *	If the notify is simply playing FX or sounds on the source, it should not need unique instances. If this Notify is attaching a beam from the instigator to the target, it does need a unique instance per instigator.
        /// </summary>
        public bool uniqueInstancePerInstigator = true;

        /// <summary>
        // *	Does this cue get a new instance for each source object? For example if two source objects apply a GC to the same source, do we create two of these GameplayCue Notify actors or just one?
        // *	If the notify is simply playing FX or sounds on the source, it should not need unique instances. If this Notify is attaching a beam from the source object to the target, it does need a unique instance per instigator.
        // */
        /// </summary>
        public bool uniqueInstancePerSourceObject;

        /// <summary>
        ///
        /// Does this cue trigger its OnActive event if it's already been triggered?
        /// This can occur when the associated tag is triggered by multiple sources and there is no unique instancing.
        ///
        /// </summary>
        [SerializeField]
        private bool allowMultipleOnActiveEvents;

        /// <summary>
        ///
        /// Does this cue trigger its WhileActive event if it's already been triggered?
        /// This can occur when the associated tag is triggered by multiple sources and there is no unique instancing.
        ///
        /// </summary>
        [SerializeField, Tooltip("Does this cue trigger its WhileActive event if it's already been triggered?\nThis can occur when the associated tag is triggered by multiple sources and there is no unique instancing.")]
        private bool allowMultipleWhileActiveEvents;

        /// <summary>
        ///
        /// We will auto destroy (recycle) this GameplayCueActor when the OnRemove event fires (after OnRemove is called)
        /// 
        /// </summary>
        [SerializeField, Tooltip("We will auto destroy (recycle) this GameplayCueActor when the OnRemove event fires (after OnRemove is called)")]
        protected bool autoDestroyOnRemove = true;

        [Tooltip("This indicates if all tags will be overriden by this. ex: when true, if this has the tag Events.Weapons.Fire whis will not trigger Events.Weapons")]
        public bool isOverride = true;

        [SerializeField]
        protected float autoDestroyDelay;

        [NonSerialized]
        public GameObject cueInstigator;

        [NonSerialized]
        public GameObject sourceObject;

        public virtual bool HandlesEvent(GameplayCueEvent EventType)
        {
            return true;
        }

        public virtual void HandleGameplayCue(GameObject MyTarget, GameplayCueEvent EventType, GameplayCueParameters Parameters)
        {
            // Multiple event gating
            {
                if (EventType == GameplayCueEvent.OnActive && !allowMultipleOnActiveEvents && hasHandledOnActiveEvent)
                {
                    return;
                }
                if (EventType == GameplayCueEvent.WhileActive && !allowMultipleWhileActiveEvents && hasHandledWhileActiveEvent)
                {
                    return;
                }

                if (EventType == GameplayCueEvent.Removed && hasHandledOnRemoveEvent)
                {
                    return;
                }
            }

            if (MyTarget)
            {
                SetLifeSpan(0);
                CancelInvoke(nameof(GameplayCueFinishedCallback));
                switch (EventType)
                {
                    case GameplayCueEvent.OnActive:
                        gameObject.SetActive(true);
                        OnActive(MyTarget, Parameters);
                        hasHandledOnActiveEvent = true;
                        break;
                    case GameplayCueEvent.WhileActive:
                        WhileActive(MyTarget, Parameters);
                        hasHandledWhileActiveEvent = true;
                        break;
                    case GameplayCueEvent.Executed:
                        OnExecute(MyTarget, Parameters);
                        break;
                    case GameplayCueEvent.Removed:
                        hasHandledOnRemoveEvent = true;
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


        /// <summary>
        /// Called when a GameplayCue is executed, this is used for instant effects or periodic ticks 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected virtual bool OnExecute(GameObject target, GameplayCueParameters parameters) { return false; }

        /// <summary>
        /// Called when a GameplayCue with duration is first activated, this will only be called if the client witnessed the activation
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected virtual bool OnActive(GameObject target, GameplayCueParameters parameters) { return false; }

        /// <summary>
        /// Called when a GameplayCue with duration is first seen as active, even if it wasn't actually just applied
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected virtual bool WhileActive(GameObject target, GameplayCueParameters parameters) { return false; }

        /// <summary>
        /// Called when a GameplayCue with duration is removed
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected virtual bool OnRemove(GameObject target, GameplayCueParameters parameters) { return false; }

        public virtual bool CanBeRecycled()
        {
            return true;
        }

        public virtual bool Recycle()
        {
            hasHandledOnActiveEvent = false;
            hasHandledWhileActiveEvent = false;
            hasHandledOnRemoveEvent = false;

            cueInstigator = null;
            sourceObject = null;

            gameObject.SetActive(false);

            transform.parent = null;

            return true;
        }

        protected virtual void GameplayCueFinishedCallback()
        {
            // Make sure OnRemoved has been called at least once if WhileActive was called (for possible cleanup)
            if (hasHandledWhileActiveEvent && !hasHandledOnRemoveEvent)
            {
                // Force onremove to be called with null parameters
                hasHandledOnRemoveEvent = true;
                OnRemove(null, default);
            }

            GameplayCueManager.Instance.NotifyGameplayCueActorFinished(this);
        }

        public void SetLifeSpan(float seconds)
        {
            if (seconds == 0)
            {
                CancelInvoke(nameof(DestroySelf));
            }
            else
            {
                Invoke(nameof(DestroySelf), seconds);
            }
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }

        public bool IsPendingDestroy()
        {
            return hasHandledOnRemoveEvent;
        }

        protected virtual void OnDestroy()
        {

        }
    }
}
