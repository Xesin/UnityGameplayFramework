using System.Collections.Generic;
using UnityEngine;

namespace GameplayFramework
{
    [ExecuteInEditMode]
    public class SpringArm : SceneObject
    {
        public float armLength = 1.4f;
        public bool useControlRotation = true;

        private List<Transform> attachedObjects = new List<Transform>();
        private Quaternion absoluteRotation = Quaternion.identity;

        private void Awake()
        {
            foreach (Transform t in transform)
            {
                SetupAttachment(t);
            }

            if (useAbsoluteRotation)
            {
                SetAbsoluteRotation(transform.rotation);
            }
        }

        private void LateUpdate()
        {
            if (useControlRotation)
            {
                if (Owner && Owner is Pawn pawn)
                {
                    transform.rotation = Quaternion.Euler(pawn.GetControlRotation());
                }
            }
            else if (useAbsoluteRotation)
            {
                if (Application.isPlaying)
                    transform.rotation = absoluteRotation;
            }

            Vector3 worldPosition = transform.position - transform.forward * armLength;

            for (int i = 0; i < attachedObjects.Count; i++)
            {
                attachedObjects[i].position = worldPosition;
            }
        }

        public void SetupAttachment(Transform child)
        {
            if (!attachedObjects.Contains(child))
            {
                attachedObjects.Add(child);
            }
        }

        public void Detacth(Transform child)
        {
            if (attachedObjects.Contains(child))
            {
                attachedObjects.Remove(child);
            }
        }

        public void SetAbsoluteRotation(Quaternion rotation)
        {
            absoluteRotation = rotation;
        }
    }
}
