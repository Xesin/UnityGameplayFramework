using Xesin.GameplayFramework;
using UnityEngine;

public abstract class PawnMovement : GameplayObject
{
    protected Character characterOwner;

    protected Vector3 acceleration;
    protected Vector3 velocity;

    protected virtual void Awake()
    {

    }

    protected virtual void LateUpdate()
    {
    }

    public virtual bool HasValidData()
    {
        bool isValid = characterOwner;


        return isValid;
    }

    /// <summary>
    /// Sets the owner of a gameplay object as a pawn.
    /// </summary>
    /// <param name="GameplayObject">New Owner</param>
    public override void SetOwner(GameplayObject obj)
    {
        base.SetOwner(obj);
        characterOwner = obj as Character;
    }

    /// <summary>
    /// Returns and consumes the input vector of the pawn owner or a zero vector if there is no pawn
    /// owner.
    /// </summary>
    /// <returns>
    /// The input vector.
    /// </returns>
    protected Vector3 ConsumeInputVector()
    {
        return characterOwner ? characterOwner.Internal_ConsumeInputVector() : Vector3.zero;
    }

    /// <summary>
    /// Returns the maximum speed as a float value.
    /// </summary>
    /// <returns>
    /// Max speed value.
    /// </returns>
    public virtual float GetMaxSpeed()
    {
        return 0.0f;
    }

    /// <summary>
    /// Returns if the current velocity is exceeding the max speed passed by parameter
    /// </summary>
    /// <param name="MaxSpeed"></param>
    /// <returns></returns>
    public bool IsExceedingMaxSpeed(float MaxSpeed)
    {
        MaxSpeed = Mathf.Max(0f, MaxSpeed);
        float maxSpeedSquared = MaxSpeed * MaxSpeed;

        float overVelocityPercent = 1.01f;
        return (velocity.sqrMagnitude > maxSpeedSquared * overVelocityPercent);
    }

    protected virtual void StopMovementImmediately()
    {
        velocity = Vector3.zero;
    }

    public Vector3 GetVelocity()
    {
        return velocity;
    }
}
