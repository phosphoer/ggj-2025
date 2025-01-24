using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
  public float BiteAnimSpeed = 1;
  public float BiteAnimAngle = 15;

  [SerializeField] private Transform _jawUpper = null;
  [SerializeField] private Transform _jawLower = null;
  [SerializeField] private Transform _mouthItemRoot = null;

  private float _biteAnimTimer;
  private Item _mouthItem;

  public void HoldItem(Item item)
  {
    _mouthItem = item;
    item.transform.parent = _mouthItemRoot;
  }

  private void Update()
  {
    float dt = Time.deltaTime;

    if (_mouthItem)
    {
      _mouthItem.transform.localPosition = Mathfx.Damp(_mouthItem.transform.localPosition, Vector3.zero, 0.25f, dt * 5);

      float jawAngle = 20;
      _jawUpper.localRotation = Quaternion.Euler(-jawAngle, 0, 0);
      _jawLower.localRotation = Quaternion.Euler(jawAngle, 0, 0);
    }
    else
    {
      _biteAnimTimer += dt * BiteAnimSpeed;

      float jawAngle = Mathf.Abs(Mathf.Sin(_biteAnimTimer)) * BiteAnimAngle;
      _jawUpper.localRotation = Quaternion.Euler(-jawAngle, 0, 0);
      _jawLower.localRotation = Quaternion.Euler(jawAngle, 0, 0);
    }
  }
}