using GameplayFramework;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
public class Character : Pawn
{
    protected virtual void Update() { }

    public override void Restart()
    {
        base.Restart();
    }


}
