using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework
{
    [RequireComponent(typeof(Camera))]
    public class GameplayCamera : GameplayObject
    {
        public bool useControlRotation = true;

        private Camera targetCamera;
        

        void Awake()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            targetCamera.enabled = true;
        }

        private void OnDisable()
        {
            targetCamera.enabled = false;
        }

        public void ApplyControlRotation(Quaternion rotation)
        {
            if(useControlRotation)
            {
                transform.rotation = rotation;
            }
        }
    }
}
