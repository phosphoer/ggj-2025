using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
  public float BiteAnimSpeed = 1;
  public float BiteAnimAngle = 15;
  public float MoveAnimSpeed = 0;
  public float KeyRotateSpeed = 90;

  [SerializeField] private Transform _visualRoot = null;
  [SerializeField] private Transform _headRoot = null;
  [SerializeField] private Transform _jawUpper = null;
  [SerializeField] private Transform _jawLower = null;
  [SerializeField] private Transform _keyRoot = null;
  [SerializeField] private Transform _footLeft = null;
  [SerializeField] private Transform _footRight = null;
  [SerializeField] private Transform _footTipLeft = null;
  [SerializeField] private Transform _footTipRight = null;
  [SerializeField] private Transform _mouthItemRoot = null;

  private float _biteAnimTimer;
  private float _chewAmount;
  private float _walkAnimTimer;
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

  public void Jump()
  {
    _footLeft.localRotation = Quaternion.Euler(60, 0, 0);
    _footRight.localRotation = Quaternion.Euler(60, 0, 0);
  }

  private void Update()
  {
    float dt = Time.deltaTime;

    _keyRoot.Rotate(KeyRotateSpeed * dt, 0, 0, Space.Self);

    if (MoveAnimSpeed > 0)
    {
      _walkAnimTimer += dt * MoveAnimSpeed * 10;
      float footAngleLeft = Mathf.Sin(_walkAnimTimer) * 40;
      float footAngleRight = Mathf.Sin(_walkAnimTimer) * -40;
      Quaternion footRotLeft = Quaternion.Euler(footAngleLeft, 0, 0);
      Quaternion footRotRight = Quaternion.Euler(footAngleRight, 0, 0);
      _footLeft.localRotation = Mathfx.Damp(_footLeft.localRotation, footRotLeft, 0.25f, dt * 10);
      _footRight.localRotation = Mathfx.Damp(_footRight.localRotation, footRotRight, 0.25f, dt * 10);

      float footTipLeftPos = _footTipLeft.position.y - _visualRoot.position.y;
      float footTipRightPos = _footTipRight.position.y - _visualRoot.position.y;
      float footTipMax = Mathf.Min(footTipLeftPos, footTipRightPos);
      float footOffsetHeight = Mathf.Abs(footTipMax);
      Vector3 footOffsetPos = Vector3.up * footOffsetHeight;
      _visualRoot.localPosition = Mathfx.Damp(_visualRoot.localPosition, footOffsetPos, 0.25f, dt * 10);

      float bobHeight = Mathf.Abs(Mathf.Sin(_walkAnimTimer)) * 0.75f;
      Vector3 bobPos = Vector3.up * bobHeight;
      _headRoot.localPosition = bobPos;
      // _headRoot.localPosition = Mathfx.Damp(_headRoot.localPosition, bobPos, 0.25f, dt * 10);
    }
    else
    {
      _footLeft.localRotation = Mathfx.Damp(_footLeft.localRotation, Quaternion.identity, 0.25f, dt * 3);
      _footRight.localRotation = Mathfx.Damp(_footRight.localRotation, Quaternion.identity, 0.25f, dt * 3);
      _visualRoot.localPosition = Mathfx.Damp(_visualRoot.localPosition, Vector3.zero, 0.25f, dt * 3);
      _headRoot.localPosition = Mathfx.Damp(_headRoot.localPosition, Vector3.zero, 0.25f, dt * 3);
      _walkAnimTimer = 0;
    }

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