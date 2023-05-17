using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayFramework
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

        public void ApplyControlRotation(Quaternion rotation)
        {
            if(useControlRotation)
            {
                transform.rotation = rotation;
            }
        }
    }
}
