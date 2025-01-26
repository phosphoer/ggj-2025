using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
  public event System.Action Footstep;

  public bool IsGrounded { get; set; }

  public float BiteAnimSpeed = 1;
  public float BiteAnimAngle = 15;
  public float MoveAnimSpeed = 0;
  public float KeyRotateSpeed = 90;
  public float FootStepAngle = 60;
  public float FootStepSpeed = 10;
  public float HeadBobScale = 0.2f;
  public float BodyBobScale = 0.2f;
  public float SlapAnimDuration = 0.2f;
  public float SlapAnimScale = 0.2f;

  [SerializeField] private Transform _visualRoot = null;
  [SerializeField] private Transform _headRoot = null;
  [SerializeField] private Transform _mouthAnchor = null;
  [SerializeField] private Transform _jawUpper = null;
  [SerializeField] private Transform _jawLower = null;
  [SerializeField] private Transform _keyRoot = null;
  [SerializeField] private Transform _footLeft = null;
  [SerializeField] private Transform _footRight = null;
  [SerializeField] private Transform _footTipLeft = null;
  [SerializeField] private Transform _footTipRight = null;
  [SerializeField] private Transform _mouthItemRoot = null;
  [SerializeField] private Transform _gumMassRoot = null;
  [SerializeField] private Transform _gumBubbleRoot = null;
  [SerializeField] private AnimationCurve _slapAnimCurve = null;
  [SerializeField] private ParticleSystem _fxBubblePop = null;

  private float _biteAnimTimer;
  private float _chewAmount;
  private float _currentGumMass;
  private float _currentGumBubbleSize;
  private float _walkAnimTimer;
  private float _bubbleSwingTimer;
  private float _slapTimer;
  private float _jumpTimer;
  private bool _isSlapping;
  private float _footStepDebounce;
  private Vector3 _gumMassLocalPos;
  private Item _mouthItem;

  public void HoldItem(Item item)
  {
    _mouthItem = item;
    item.transform.parent = _mouthItemRoot;
  }

  public void DropItem()
  {
    if (_mouthItem)
    {
      _mouthItem.transform.parent = null;
      _mouthItem = null;
    }
  }

  public void SetGumMass(float gumMass)
  {
    _currentGumMass = Mathf.Max(0, gumMass);
  }

  public void SetBubbleSize(float bubbleSize)
  {
    _currentGumBubbleSize = bubbleSize;
  }

  public void PopBubble()
  {
    Instantiate(_fxBubblePop, _gumBubbleRoot.position, Quaternion.identity);
  }

  public void Chew()
  {
    _chewAmount = 1;
  }

  public void Slap()
  {
    _isSlapping = true;
    _slapTimer = 0;
  }

  public void Jump()
  {
    _footLeft.localRotation = Quaternion.Euler(60, 0, 0);
    _footRight.localRotation = Quaternion.Euler(60, 0, 0);
    _jumpTimer = 0.5f;
  }

  private void Awake()
  {
    _gumMassLocalPos = _gumMassRoot.localPosition;
  }

  private void Start()
  {
    _gumMassRoot.localScale = Vector3.one * _currentGumMass;
  }

  private void Update()
  {
    float dt = Time.deltaTime;

    _jumpTimer -= dt;

    _keyRoot.Rotate(KeyRotateSpeed * MoveAnimSpeed * dt, 0, 0, Space.Self);

    // Scale gum pieces
    Vector3 gumMassScale = Vector3.one * _currentGumMass;
    _gumMassRoot.localScale = Mathfx.Damp(_gumMassRoot.localScale, gumMassScale, 0.25f, dt * 5);

    Vector3 gumBubbleScale = Vector3.one * _currentGumBubbleSize;
    _gumBubbleRoot.localScale = Mathfx.Damp(_gumBubbleRoot.localScale, gumBubbleScale, 0.25f, dt * 5);

    // Swing back and forth when floating with a bubble
    if (_currentGumBubbleSize > 0)
    {
      _bubbleSwingTimer += dt * 5;
      Quaternion swingRot = Quaternion.Euler(Mathf.Sin(_bubbleSwingTimer) * 20 - 25, 0, 0);
      _visualRoot.localRotation = Mathfx.Damp(_visualRoot.localRotation, swingRot, 0.25f, dt * 5);

      Quaternion bubbleRot = Quaternion.LookRotation(Vector3.up, -transform.forward);
      _gumBubbleRoot.rotation = Mathfx.Damp(_gumBubbleRoot.rotation, bubbleRot, 0.25f, dt * 5);
    }
    else
    {
      _visualRoot.localRotation = Mathfx.Damp(_visualRoot.localRotation, Quaternion.identity, 0.25f, dt * 3);
    }

    // Walking animation
    if (MoveAnimSpeed > 0 && IsGrounded && _jumpTimer <= 0)
    {
      _walkAnimTimer += dt * MoveAnimSpeed * FootStepSpeed;

      // Rotate feet
      float footAngleLeft = Mathf.Sin(_walkAnimTimer) * FootStepAngle;
      float footAngleRight = Mathf.Sin(_walkAnimTimer) * -FootStepAngle;
      _footLeft.localRotation = Quaternion.Euler(footAngleLeft, 0, 0);
      _footRight.localRotation = Quaternion.Euler(footAngleRight, 0, 0);

      _footStepDebounce -= dt;
      if (Mathf.Abs(footAngleLeft) < 5 && _footStepDebounce <= 0)
      {
        _footStepDebounce = 0.1f;
        Footstep?.Invoke();
      }
      else if (Mathf.Abs(footAngleRight) < 5 && _footStepDebounce <= 0)
      {
        _footStepDebounce = 0.1f;
        Footstep?.Invoke();
      }

      // Offset height by toe pos (keep toes on floor)
      float footTipLeftPos = _footTipLeft.position.y - _visualRoot.position.y;
      float footTipRightPos = _footTipRight.position.y - _visualRoot.position.y;
      float footTipMax = Mathf.Min(footTipLeftPos, footTipRightPos);
      float footOffsetHeight = Mathf.Abs(footTipMax);
      Vector3 footOffsetPos = Vector3.up * footOffsetHeight;
      _visualRoot.localPosition = Mathfx.Damp(_visualRoot.localPosition, footOffsetPos, 0.25f, dt * 10);

      // Bob head and body up and down
      float bobHeightHead = Mathf.Abs(Mathf.Sin(_walkAnimTimer)) * HeadBobScale;
      float bobHeightBody = Mathf.Sin(_walkAnimTimer * 2 + 0.2f) * BodyBobScale * Mathf.Clamp01(_currentGumMass);
      _gumMassRoot.localPosition = _gumMassLocalPos + Vector3.up * bobHeightBody;
      _headRoot.position = _mouthAnchor.position + Vector3.up * bobHeightHead;
    }
    // Standing still animation
    else
    {
      _footLeft.localRotation = Mathfx.Damp(_footLeft.localRotation, Quaternion.identity, 0.25f, dt * 3);
      _footRight.localRotation = Mathfx.Damp(_footRight.localRotation, Quaternion.identity, 0.25f, dt * 3);
      _visualRoot.localPosition = Mathfx.Damp(_visualRoot.localPosition, Vector3.zero, 0.25f, dt * 3);
      _walkAnimTimer = 0;

      _headRoot.position = Mathfx.Damp(_headRoot.position, _mouthAnchor.position, 0.25f, dt * 5);
      _gumMassRoot.localPosition = Mathfx.Damp(_gumMassRoot.localPosition, _gumMassLocalPos, 0.25f, dt * 5);
    }

    // Hold the mouth item and animation chewing
    if (_mouthItem)
    {
      _mouthItem.transform.localPosition = Mathfx.Damp(_mouthItem.transform.localPosition, Vector3.zero, 0.25f, dt * 5);

      float jawAngle = (1 - _chewAmount) * 30;
      _jawUpper.localRotation = Mathfx.Damp(_jawUpper.localRotation, Quaternion.Euler(-jawAngle, 0, 0), 0.2f, dt * 30);
      _jawLower.localRotation = Mathfx.Damp(_jawLower.localRotation, Quaternion.Euler(jawAngle, 0, 0), 0.2f, dt * 30);
      _chewAmount = Mathfx.Damp(_chewAmount, 0, 0.5f, dt * 10);
    }
    // Chatter teeth while moving
    else
    {
      _biteAnimTimer += dt * BiteAnimSpeed;

      float jawAngle = Mathf.Abs(Mathf.Sin(_biteAnimTimer)) * BiteAnimAngle;
      if (_currentGumBubbleSize > 0)
        jawAngle = 0;

      _jawUpper.localRotation = Quaternion.Euler(-jawAngle, 0, 0);
      _jawLower.localRotation = Quaternion.Euler(jawAngle, 0, 0);
    }

    // Slap Anim
    if (_isSlapping)
    {
      _slapTimer += dt;
      float slapT = Mathf.Clamp01(_slapTimer / SlapAnimDuration);
      _headRoot.position = _mouthAnchor.position + _mouthAnchor.forward * _slapAnimCurve.Evaluate(slapT) * SlapAnimScale;
    }
  }
}