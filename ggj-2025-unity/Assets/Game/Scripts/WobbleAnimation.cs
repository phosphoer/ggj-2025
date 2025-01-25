using UnityEngine;

public class WobbleAnimation : MonoBehaviour
{
  public float Spring = 200;
  public float Damping = 10;
  public Transform WobbleRoot;

  private float _currentWobble;
  private float _wobbleVelocity;
  private Vector3 _wobbleAxis;

  public void StartWobble(Vector3 wobbleAxis, float wobbleMagnitude)
  {
    _wobbleVelocity = wobbleMagnitude;
    _wobbleAxis = wobbleAxis;
  }

  private void Update()
  {
    float dt = Time.deltaTime;
    float wobbleDelta = 0 - _currentWobble;
    _wobbleVelocity += dt * wobbleDelta * Spring;
    _currentWobble += _wobbleVelocity * dt;
    _wobbleVelocity = Mathfx.Damp(_wobbleVelocity, 0, 0.25f, dt * Damping);

    WobbleRoot.rotation = Quaternion.AngleAxis(_currentWobble, _wobbleAxis);
  }
}