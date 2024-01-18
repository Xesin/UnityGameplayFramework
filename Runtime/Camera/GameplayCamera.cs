#if GAMEPLAY_USES_CINEMACHINE
using Cinemachine;
#endif
using UnityEngine;

namespace Xesin.GameplayFramework
{
#if GAMEPLAY_USES_CINEMACHINE 
    [RequireComponent(typeof(CinemachineVirtualCamera))]
#else
    [RequireComponent(typeof(Camera))]
#endif
    public class GameplayCamera : SceneObject
    {
#if GAMEPLAY_USES_CINEMACHINE
        private CinemachineVirtualCamera targetCamera;
#else
        private Camera targetCamera;
#endif


        protected override void Awake()
        {
            base.Awake();
#if GAMEPLAY_USES_CINEMACHINE
            targetCamera = GetComponent<CinemachineVirtualCamera>();
#else
            targetCamera = GetComponent<Camera>();
#endif
        }

        private void OnEnable()
        {
            targetCamera.enabled = true;
        }

        private void OnDisable()
        {
            targetCamera.enabled = false;
        }

        protected override void ApplyControlRotation(Quaternion rotation)
        {
            if (useControlRotation)
            {
#if !GAMEPLAY_USES_CINEMACHINE
                transform.rotation = rotation;
#endif
            }
        }
    }
}
