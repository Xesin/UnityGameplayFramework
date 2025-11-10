using UnityEngine;
using Xesin.GameplayFramework.Utils;

namespace Xesin.GameplayFramework
{
    internal interface IGameplaySubsystem
    {
        void OnRegistered();
        void OnDesregistered();
    }

    public class Subsystem<T> : MonoBehaviour, IGameplaySubsystem where T : Subsystem<T>
    {
        public virtual void OnRegistered()
        {

        }

        public virtual void OnDesregistered()
        {

        }

        private void OnDestroy()
        {
            Subsystems.UnregisterSubsystem<T>();
        }
    }
}
