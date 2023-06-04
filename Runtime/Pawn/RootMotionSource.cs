using UnityEngine;

namespace Xesin.GameplayFramework
{

    public struct RootMotionParams
    {
        public Vector3 translation;
        public Quaternion rotation;

        public bool HasRootMotion => translation != Vector3.zero || rotation != Quaternion.identity;

        public void Accumulate(RootMotionParams rootMotionParams)
        {
            translation += rootMotionParams.translation;
            rotation *= rootMotionParams.rotation;
        }

        public void Clear()
        {
            translation = Vector3.zero;
            rotation = Quaternion.identity;
        }
    }

    [RequireComponent(typeof(Animator))]
    public class RootMotionSource : GameplayObject
    {
        Character characterOwner;
        Animator animator;
        Vector3 acumulatedPosition;
        Quaternion acumulatedRotation = Quaternion.identity;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public override void SetOwner(GameplayObject obj)
        {
            base.SetOwner(obj);
            characterOwner = (Character)obj;
        }

        private void OnAnimatorMove()
        {
            acumulatedPosition += animator.deltaPosition;
            acumulatedRotation *= animator.deltaRotation;
        }

        public RootMotionParams ConsumeRootMotion()
        {
            var rootMotion = new RootMotionParams()
            {
                translation = acumulatedPosition,
                rotation = acumulatedRotation
            };

            acumulatedPosition = Vector3.zero;
            acumulatedRotation = Quaternion.identity;

            return rootMotion;
        }
    }
}
