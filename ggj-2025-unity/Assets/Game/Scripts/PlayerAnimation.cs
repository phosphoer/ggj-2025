using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
  public float BiteAnimSpeed = 1;
  public float BiteAnimAngle = 15;
  public float KeyRotateSpeed = 90;

  [SerializeField] private Transform _jawUpper = null;
  [SerializeField] private Transform _jawLower = null;
  [SerializeField] private Transform _keyRoot = null;
  [SerializeField] private Transform _mouthItemRoot = null;

  private float _biteAnimTimer;
  private float _chewAmount;
  private Item _mouthItem;

  public void HoldItem(Item item)
  {
    _mouthItem = item;
    item.transform.parent = _mouthItemRoot;
  }

  public void Chew()
  {
    _chewAmount = 1;
  }

  private void Update()
  {
    float dt = Time.deltaTime;

    _keyRoot.Rotate(KeyRotateSpeed * dt, 0, 0, Space.Self);

    if (_mouthItem)
    {
      _mouthItem.transform.localPosition = Mathfx.Damp(_mouthItem.transform.localPosition, Vector3.zero, 0.25f, dt * 5);

      float jawAngle = (1 - _chewAmount) * 30;
      _jawUpper.localRotation = Mathfx.Damp(_jawUpper.localRotation, Quaternion.Euler(-jawAngle, 0, 0), 0.2f, dt * 30);
      _jawLower.localRotation = Mathfx.Damp(_jawLower.localRotation, Quaternion.Euler(jawAngle, 0, 0), 0.2f, dt * 30);
    }
    else
    {
      _biteAnimTimer += dt * BiteAnimSpeed;

      float jawAngle = Mathf.Abs(Mathf.Sin(_biteAnimTimer)) * BiteAnimAngle;
      _jawUpper.localRotation = Quaternion.Euler(-jawAngle, 0, 0);
      _jawLower.localRotation = Quaternion.Euler(jawAngle, 0, 0);
    }

    _chewAmount = Mathfx.Damp(_chewAmount, 0, 0.5f, dt * 10);
  }
}