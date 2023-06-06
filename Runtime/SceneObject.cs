using UnityEngine;

namespace Xesin.GameplayFramework
{
    public abstract class SceneObject : GameplayObject
    {
        public bool useAbsoluteRotation = false;
        public bool updateVelocity = false;
        public Vector3 Velocity { get; protected set; }

        protected Vector3 oldPosition;

        protected virtual void Awake()
        {
            oldPosition = transform.position;
        }

        protected virtual void FixedUpdate()
        {
            if (!updateVelocity) return;

            Velocity = (transform.position - oldPosition) / Time.fixedDeltaTime;
            oldPosition = transform.position;
        }
    }
}
