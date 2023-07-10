using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework
{
    [RequireComponent(typeof(Renderer))]
    public class EnhancedRenderer : GameplayObject
    {
        public bool OwnerCanSee = true;

        private Renderer renderer;

        private void LateUpdate()
        {
            renderer.enabled = true;
        }


        private void OnWillRenderObject()
        {
            if(!Camera.current)
            {
                renderer.enabled = true;
                return;
            }

            var gCamera = Camera.current.GetComponent<GameplayCamera>();
            if (!gCamera)
            {
                renderer.enabled = true;
                return;
            }

            if (!renderer) renderer = GetComponent<Renderer>();

            if (!OwnerCanSee)
            {
                if (gCamera.Owner == Owner)
                {
                    renderer.enabled = false;
                }
                else
                {
                    renderer.enabled = true;
                }
            }
            else
            {
                renderer.enabled = true;
            }
        }
    }
}
