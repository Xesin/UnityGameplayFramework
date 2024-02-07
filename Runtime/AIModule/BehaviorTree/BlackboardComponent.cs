using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    public class BlackboardComponent : GameplayObject
    {
        [SerializeField] BlackboardData defaultBlackboardAsset;

        protected BrainComponent brainComponent;
        private BlackboardData blackboardAsset;

        private Dictionary<Blackbloard.Key, Action<Blackbloard.Key>> observers = new Dictionary<Blackbloard.Key, Action<Blackbloard.Key>>();

        protected virtual void Start()
        {
            if (Owner)
            {
                brainComponent = Owner.GetComponent<BrainComponent>();
                if (brainComponent)
                {
                    brainComponent.CacheBlackboardComponent(this);
                }
            }

            if (!blackboardAsset && defaultBlackboardAsset)
            {
                InitializeBlackboard(defaultBlackboardAsset);
            }
        }

        private bool InitializeBlackboard(BlackboardData newAsset)
        {
            if (newAsset == blackboardAsset)
            {
                return true;
            }

            blackboardAsset = newAsset;

            return true;
        }

        public string GetKeyName(Blackbloard.Key keyID)
        {
            return blackboardAsset ? blackboardAsset.GetKeyName(keyID) : null;
        }

        public Blackbloard.Key GetKeyID(string keyName)
        {
            return blackboardAsset ? blackboardAsset.GetKeyID(keyName) : Blackbloard.Key.invalid;
        }

        public Type GetKeyTyp(Blackbloard.Key keyID)
        {
            return blackboardAsset ? blackboardAsset.GetKeyType(keyID) : null;
        }

        public BrainComponent GetBrainComponent()
        {
            return brainComponent;
        }

        public BlackboardData GetBlackboardAsset()
        {
            return blackboardAsset;
        }

        public void CacheBrainComponent(BrainComponent brainComponent)
        {
            if (brainComponent != this.brainComponent)
            {
                this.brainComponent = brainComponent;
            }
        }

        public T GetValue<T>(string keyName)
        {
            Blackbloard.Key key = GetKeyID(keyName);
            return GetValue<T>(key);
        }

        public T GetValue<T>(Blackbloard.Key keyID)
        {
            var blackboardEntry = blackboardAsset.IsValid() ? blackboardAsset.GetKey(keyID) : default;
            if (!blackboardEntry.IsValid() || blackboardEntry.ValueType == null || blackboardEntry.ValueType != typeof(T))
            {
                return default;
            }

            return (T)blackboardEntry.Value;
        }

        public bool SetValue<T>(Blackbloard.Key keyID, T value)
        {
            var blackboardEntry = blackboardAsset.IsValid() ? blackboardAsset.GetKey(keyID) : default;
            if (!blackboardEntry.IsValid() || blackboardEntry.ValueType == null || !typeof(T).IsAssignableFrom(blackboardEntry.ValueType))
            {
                return false;
            }

            var prevValue = blackboardEntry.Value;

            blackboardEntry.Value = value;

            bool changed = prevValue != blackboardEntry.Value;

            if (changed)
            {
                NotifyObservers(keyID);
            }

            return true;
        }

        public void RegisterObserver(Blackbloard.Key keyID, Action<Blackbloard.Key> onModifiedKeyHandle)
        {
            if (!observers.ContainsKey(keyID))
            {
                observers.Add(keyID, null);
            }

            observers[keyID] += onModifiedKeyHandle;
        }

        public void UnregisterObserver(Blackbloard.Key keyID, Action<Blackbloard.Key> onModifiedKeyHandle)
        {
            if (!observers.ContainsKey(keyID))
            {
                return;
            }

            observers[keyID] -= onModifiedKeyHandle;
        }

        List<Action<Blackbloard.Key>> toRemove = new List<Action<Blackbloard.Key>>(30);
        public void UnregisterObserversFrom(object notifyOwner)
        {
            foreach (var observer in observers)
            {
                toRemove.Clear();
                var invocationList = observer.Value.GetInvocationList();
                for (int i = 0; i < invocationList.Length; i++)
                {
                    if (invocationList[i].Target == notifyOwner)
                        toRemove.Add((Action<Blackbloard.Key>) invocationList[i]);
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    observers[observer.Key] -= toRemove[i];
                }

            }
        }

        private void NotifyObservers(Blackbloard.Key keyID)
        {
            if (observers.ContainsKey(keyID))
                observers[keyID]?.Invoke(keyID);
        }

        public bool IsCompatibleWith(BlackboardData blackboardAsset)
        {
            if (blackboardAsset.GetType().IsAssignableFrom(this.blackboardAsset.GetType()))
                return true;

            bool hasAllKeysInSameOrder = true;
            for (int i = 0; i < this.blackboardAsset.Count; i++)
            {
                if(blackboardAsset.Count <= i)
                {
                    hasAllKeysInSameOrder = false;
                    break;
                }

                if (!blackboardAsset.Keys[i].Equals(this.blackboardAsset.Keys[i]))
                {
                    hasAllKeysInSameOrder = false;
                    break;
                }
            }

            return hasAllKeysInSameOrder;
        }
    }
}
