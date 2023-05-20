using GameplayFramework;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
public class Character : Pawn
{
    private CharacterMovement characterMovement;

    protected override void Awake()
    {
        base.Awake();
        
        characterMovement = GetComponent<CharacterMovement>();
    }

    protected virtual void Update() { }

    public override void Restart()
    {
        base.Restart();
    }


}
