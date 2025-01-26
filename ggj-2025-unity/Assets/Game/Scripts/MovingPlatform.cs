using System.Collections;
using KinematicCharacterController;
using UnityEngine;

public class MovingPlatform : MonoBehaviour, IMoverController
{
  [Header("Movement Settings")] public bool UseTranslatePoints = true;
  public Transform pointA; // First point
  public Transform pointB; // Second point
  public float speed = 2f; // Movement speed

  public Vector3 RotateAxis = Vector3.up;
  public Space Space;
  public float RotateSpeed = 0.0f;

  public PhysicsMover Mover;

  private Vector3 targetPosition; // Current target position

  private void Awake()
  {
    if (!Mover)
    {
      Mover = gameObject.GetOrAddComponent<PhysicsMover>();
      Mover.MoveWithPhysics = false;
    }

    Mover.MoverController = this;
  }

  private void Start()
  {
    targetPosition = UseTranslatePoints ? pointA.position : transform.position;

    PhysicsMoverState state = default;
    state.Position = targetPosition;
    state.Rotation = transform.rotation;

    Mover.ApplyState(state);
  }

  public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
  {
    if (UseTranslatePoints)
    {
      // Move platform towards the target position
      goalPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * deltaTime);

      // Switch target when platform reaches a point
      if (Vector3.Distance(transform.position, targetPosition) < 0.1f && pointA && pointB)
      {
        targetPosition = targetPosition == pointA.position ? pointB.position : pointA.position;
      }
    }
    else
      goalPosition = transform.position;

    if (Space == Space.Self)
      goalRotation = Quaternion.Euler(RotateAxis * RotateSpeed * deltaTime) * transform.rotation;
    else
      goalRotation = transform.rotation * Quaternion.Euler(RotateAxis * RotateSpeed * deltaTime);
  }
}