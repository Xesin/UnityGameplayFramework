using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework
{
    [ExecuteInEditMode]
    public class SpringArm : SceneObject
    {
        public float armLength = 1.4f;
        public bool traceCollision = true;
        public bool traceIgnoresOwner = true;
        public float traceRadius = 0.04f;
        public Vector3 armOffset = Vector3.zero;
        public LayerMask traceChannels = Physics.AllLayers;


        private List<Transform> attachedObjects = new List<Transform>();

        protected override void Awake()
        {
            base.Awake();
            foreach (Transform t in transform)
            {
                SetupAttachment(t);
            }
        }

        RaycastHit[] raycastHits = new RaycastHit[2];

        private void LateUpdate()
        {
            Vector3 armOrigin = transform.position + armOffset;
            Vector3 armDirection = -transform.forward;
            Vector3 newWorldPosition = armOrigin + armDirection * armLength;

            if (traceCollision && Application.isPlaying)
            {
                int hits = Physics.SphereCastNonAlloc(armOrigin, traceRadius, armDirection, raycastHits, armLength, traceChannels);
                for (int i = 0; i < hits; i++)
                {
                    if (!traceIgnoresOwner || raycastHits[i].collider.gameObject != Owner.gameObject)
                    {
                        newWorldPosition = armOrigin + armDirection * raycastHits[i].distance;
                        break;
                    }
                }

            }

            for (int i = 0; i < attachedObjects.Count; i++)
            {
                attachedObjects[i].position = newWorldPosition;
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
    }
}
