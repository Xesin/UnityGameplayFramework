using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.AddressablesExtensions
{
    public class MonoBehaviourReleaseEvent : MonoBehaviour, IReleaseEvent
    {
        public event Action OnDispatch;

        private void OnDestroy()
        {
            OnDispatch?.Invoke();
        }
    }
}
