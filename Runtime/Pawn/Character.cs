using Xesin.GameplayFramework;
using PlasticPipe.PlasticProtocol.Server;
using System;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
public class Character : Pawn
{
    public bool pressedJump = false;
    public bool wasJumping = false;
    public float jumpKeyHoldTime = 0f;
    public int jumpMaxCount = 1;
    public float jumpMaxHoldTime = 0.25f;

    public int jumpCurrentCount = 0;
    public int jumpCurrentCountPreJump = 0;
    public float jumpForceTimeRemaining = 0f;

    // The object we are walking on
    private Transform baseObject;

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

    public void OnMovementModeChanged(MovementMode prevMovementMode)
    {
        if (!pressedJump || !characterMovement.IsFalling())
        {
            ResetJumpState();
        }
    }

    public virtual void Jump()
    {
        pressedJump = true;
        jumpKeyHoldTime = 0f;
    }

    public virtual void StopJumping()
    {
        pressedJump = false;
        ResetJumpState();
    }

    public virtual void CheckJumpInput(float deltaTime)
    {
        jumpCurrentCountPreJump = jumpCurrentCount;

        if (characterMovement)
        {
            if (pressedJump)
            {
                bool firstJump = jumpCurrentCount == 0;

                // If it's the first jump and the player is already
                // falling, increment the jump count to compensate
                if (firstJump && characterMovement.IsFalling())
                {
                    jumpCurrentCount++;
                }

                bool didJump = CanJump() && characterMovement.DoJump();
                if (didJump)
                {
                    if (!wasJumping)
                    {
                        jumpCurrentCount++;
                        jumpForceTimeRemaining = GetJumpMaxHoldTime();
                        OnJumped();
                    }
                }

                wasJumping = didJump;
            }
        }
    }

    public bool CanJump()
    {
        return JumpIsAllowed();
    }

    public virtual void ResetJumpState()
    {
        pressedJump = false;
        wasJumping = false;
        jumpKeyHoldTime = 0f;
        jumpForceTimeRemaining = 0f;

        if (characterMovement && !characterMovement.IsFalling())
        {
            jumpCurrentCount = 0;
            jumpCurrentCountPreJump = 0;
        }
    }

    public virtual void ClearJumpInput(float deltaTime)
    {
        if (pressedJump)
        {
            jumpKeyHoldTime += deltaTime;

            // Don't disable bPressedJump right away if it's still held.
            // Don't modify JumpForceTimeRemaining because a frame of update may be remaining.
            if (jumpKeyHoldTime >= GetJumpMaxHoldTime())
            {
                pressedJump = false;
            }
        }
        else
        {
            jumpForceTimeRemaining = 0.0f;
            wasJumping = false;
        }
    }

    public virtual void OnJumped()
    {

    }

    protected bool JumpIsAllowed()
    {
        bool jumpIsAllowed = characterMovement.CanAttempJump();

        if (jumpIsAllowed)
        {
            if (!wasJumping || GetJumpMaxHoldTime() <= 0f)
            {
                if (jumpCurrentCount == 0 && characterMovement.IsFalling())
                {
                    jumpIsAllowed = jumpCurrentCount + 1 < jumpMaxCount;
                }
                else
                {
                    jumpIsAllowed = jumpCurrentCount < jumpMaxCount;
                }
            }
            else
            {
                bool jumpKeyHeld = (pressedJump && jumpKeyHoldTime < GetJumpMaxHoldTime());
                jumpIsAllowed = jumpKeyHeld &&
                    ((jumpCurrentCount < jumpMaxCount) || (wasJumping && jumpCurrentCount == jumpMaxCount));
            }
        }

        return jumpIsAllowed;
    }

    protected virtual float GetJumpMaxHoldTime()
    {
        return jumpMaxHoldTime;
    }

    public virtual void SetBase(Transform newBase)
    {
        bool baseChanged = newBase != baseObject;

        if(baseChanged)
        {
            //// TODO: Verify no recursion
            //foreach(Transform child in newBase)
            //{
            //    if(child.gameObject == gameObject)
            //    {
            //        Debug.LogWarning("Set base failed! Recursion decected.");
            //        return;
            //    }

            //}

            baseObject = newBase;

            if(characterMovement)
            {
                if(newBase)
                {
                    // Force base location and relative position to be computed since we have a new base or bone so the old relative offset is meaningless.
                    characterMovement.SaveBaseLocation();
                }
                else
                {
                    characterMovement.currentFloor.Clear();
                }
            }
        }
    }

    public virtual Transform GetMovementBase()
    {
        return baseObject;
    }
}
