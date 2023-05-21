using GameplayFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorProvider : GameplayObject
{
    [SerializeField]
    private Animator targetAnimator;
    private Pawn PawnOwner;

    public override void SetOwner(GameplayObject obj)
    {
        base.SetOwner(obj);
        PawnOwner = obj as Pawn;
    }

    private void LateUpdate()
    {
        if(PawnOwner && targetAnimator)
        {
            targetAnimator.SetFloat("Speed", PawnOwner.GetVelocity().magnitude);
        }
    }
}
