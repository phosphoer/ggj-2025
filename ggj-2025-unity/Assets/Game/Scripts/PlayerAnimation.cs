using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
  public float BiteAnimSpeed = 1;
  public float BiteAnimAngle = 15;

  [SerializeField] private Transform _jawUpper = null;
  [SerializeField] private Transform _jawLower = null;

  private float _biteAnimTimer;

  private void Update()
  {
    _biteAnimTimer += Time.deltaTime * BiteAnimSpeed;

    float jawAngle = Mathf.Abs(Mathf.Sin(_biteAnimTimer)) * BiteAnimAngle;
    _jawUpper.localRotation = Quaternion.Euler(-jawAngle, 0, 0);
    _jawLower.localRotation = Quaternion.Euler(jawAngle, 0, 0);
  }
}