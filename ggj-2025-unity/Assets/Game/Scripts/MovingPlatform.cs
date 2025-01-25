using System.Collections;
using KinematicCharacterController;
using UnityEngine;

public class MovingPlatform : MonoBehaviour, IMoverController
{
  [Header("Movement Settings")] public Transform pointA; // First point
  public Transform pointB; // Second point
  public float speed = 2f; // Movement speed

  public PhysicsMover Mover;

  private Vector3 targetPosition; // Current target position

  private void Awake()
  {
    Mover.MoverController = this;
  }

  void Start()
  {
    // Start moving towards point A
    targetPosition = pointA.position;
  }

  public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
  {
    // Move platform towards the target position
    goalPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * deltaTime);
    goalRotation = transform.rotation;

    // Switch target when platform reaches a point
    if (Vector3.Distance(transform.position, targetPosition) < 0.1f && pointA && pointB)
    {
      targetPosition = targetPosition == pointA.position ? pointB.position : pointA.position;
    }
  }
}