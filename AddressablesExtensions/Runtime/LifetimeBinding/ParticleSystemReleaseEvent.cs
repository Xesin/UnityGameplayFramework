using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.AddressablesExtensions
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemReleaseEvent : MonoBehaviour, IReleaseEvent
    {
        [SerializeField] private ParticleSystem particle;
        private bool _isAliveAtLastFrame;

        public event Action OnDispatch;

        private void Awake()
        {
            if (particle == null)
                particle = GetComponent<ParticleSystem>();
        }

        private void Reset()
        {
            particle = GetComponent<ParticleSystem>();
        }

        private void LateUpdate()
        {
            var isAlive = particle.IsAlive(true);
            if (_isAliveAtLastFrame && !isAlive)
                OnDispatch?.Invoke();

            _isAliveAtLastFrame = isAlive;
        }
    }
}
