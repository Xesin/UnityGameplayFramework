using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders.Simulation;

namespace GameplayFramework
{
    public abstract class GameplayObject : MonoBehaviour
    {
        public bool updateVelocity = false;

        public GameplayObject Owner { get; private set; }
        public Vector3 Velocity { get; protected set; }

        protected Vector3 oldPosition;

        protected virtual void Awake()
        {
            oldPosition = transform.position;
        }

        public virtual void SetOwner(GameplayObject obj)
        {
            Owner = obj;
        }

        protected virtual void FixedUpdate()
        {
            if (!updateVelocity) return;

            Velocity = (transform.position - oldPosition) / Time.fixedDeltaTime;
            oldPosition = transform.position;
        }
    }
}
