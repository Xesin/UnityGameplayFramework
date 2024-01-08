using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayCues.Animation
{
    public class GC_AnimStateCleanup : StateMachineBehaviour
    {
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent<GC_AnimationEventHandler>(out var eventHandler))
            {
                eventHandler.OnExitState(stateInfo);
            }
        }
    }
}
