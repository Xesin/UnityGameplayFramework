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

        private bool enabledState = false;

        private void OnWillRenderObject()
        {
            if (!renderer) renderer = GetComponent<Renderer>();
            enabledState = renderer.enabled;

            if (!Camera.current)
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
