using UnityEngine;
using Xesin.GameplayFramework;

public class MovingPlatform : SceneObject
{
    public Transform startPosition; // The starting position of the platform
    public Transform endPosition; // The ending position of the platform
    public float time = 2f; // The movement speed of the platform

    private Vector3 targetPosition; // The current target position of the platform
    private Vector3 fromPosition; // The current target position of the platform
    private float elapsedTime;
    private void Start()
    {
        // Set the initial target position to the starting position
        fromPosition = transform.position;
        targetPosition = endPosition.position;
    }

    protected override void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
        // Move the platform towards the target position
        
        transform.position = Vector3.Lerp(fromPosition, targetPosition, elapsedTime / time);

        // If the platform has reached the target position, set the new target position
        if (transform.position == targetPosition)
        {
            elapsedTime = 0;
            if (targetPosition == startPosition.position)
            {
                fromPosition = startPosition.position;
                targetPosition = endPosition.position;
            }
            else
            {
                fromPosition = endPosition.position;
                targetPosition = startPosition.position;
            }
        }

        base.FixedUpdate();
    }
}
