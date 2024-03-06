using UnityEngine;

namespace Xesin.GameplayFramework
{
    [DefaultExecutionOrder(5)]
    public class SceneObject : GameplayObject
    {
        public bool useAbsoluteRotation = false;
        public bool updateVelocity = false;
        public bool useControlRotation = false;
        public Vector3 Velocity { get; protected set; }

        protected Vector3 oldPosition;
        protected Quaternion absoluteRotation = Quaternion.identity;

        protected virtual void Awake()
        {
            oldPosition = transform.position;

            if (useAbsoluteRotation)
            {
                SetAbsoluteRotation(transform.rotation);
            }

            var gameplayComponents = GetComponentsInChildren<GameplayObject>(true);

            for (int i = 0; i < gameplayComponents.Length; i++)
            {
                if (gameplayComponents[i] != this)
                {
                    gameplayComponents[i].SetOwner(this);
                }
            }
        }

        private void LateUpdate()
        {
            if (useControlRotation)
            {
                if (Owner && Owner is Pawn pawn)
                {
                    ApplyControlRotation(Quaternion.Euler(pawn.GetControlRotation()));
                }
            }
            else if (useAbsoluteRotation)
            {
                if (Application.isPlaying)
                    transform.rotation = absoluteRotation;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!updateVelocity) return;

            Velocity = (transform.position - oldPosition) / Time.fixedDeltaTime;
            oldPosition = transform.position;
        }

        public void SetAbsoluteRotation(Quaternion rotation)
        {
            absoluteRotation = rotation;
        }

        protected virtual void ApplyControlRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }
    }
}
