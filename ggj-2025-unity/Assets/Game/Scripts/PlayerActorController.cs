using UnityEngine;
using UnityEngine.UIElements;

public class PlayerActorController : MonoBehaviour, ISlappable
{
  public event System.Action<PlayerActorController> OnPlayerTouchedLava;
  public event System.Action<int, int> OnPlayerSectionChanged;

  public Rewired.Player PlayerInput => _playerInput;
  public int PlayerIndex => _playerIndex;
  public CapsuleCollider PlayerCapsule => _actor.Motor.Capsule;

  public float BubbleShrinkSpeed = 0.25f;
  public float BubbleFloatPower = 0.25f;
  public float ThrowStrength = 15;
  public float AttackCooldown = 0.25f;
  public float LaunchPower = 10;
  public float LaunchSpinRate = 90;

  public SoundBank SfxFootstep;
  public SoundBank SfxJump;
  public SoundBank SfxLand;
  public SoundBank SfxSwallow;
  public SoundBank SfxInflate;
  public SoundBank SfxDeflate;
  public SoundBank SfxBubbleInflate;
  public SoundBank SfxBubbleDeflate;
  public SoundBank SfxBubblePop;
  public SoundBank SfxSlap;
  public SoundBank SfxReceiveSlap;
  public SoundBank SfxThrow;

  [SerializeField] private ActorController _actor = null;
  [SerializeField] private PlayerAnimation _playerAnimation = null;
  [SerializeField] private InteractionController _interaction = null;
  [SerializeField] private Transform _slapAnchor = null;
  [SerializeField] private float _slapRadius = 0.5f;
  [SerializeField] private LayerMask _slapMask = default;
  [SerializeField] private ThrowUI _throwUIPrefab = null;
  [SerializeField] private ParticleSystem _fxSplashPrefab = null;

  private Rewired.Player _playerInput;
  private Item _heldItem;
  private float _bubbleGumMass;
  private float _bubbleStoredMass;
  private float _attackCooldownTimer;
  private int _levelSectionIndex = 0;
  private bool _didBubbleThisJump;
  private bool _isThrowing;
  private bool _isInBubbleJump;
  private Vector3 _currentThrowAxis;
  private Vector3 _lastThrowAxis;
  private Vector3 _lastNonNegativeThrowAxis;
  private float _currentThrowT;
  private float _lastNonNegativeThrowT;
  private bool _isMidFlick;
  private RectTransform _throwUIRoot;
  private ThrowUI _throwUI;
  private Collider[] _slapColliders = new Collider[4];
  private bool _hasStartedLaunch = false;
  private Vector3 _launchVelocity = Vector3.zero;
  private int _playerIndex = -1;

  public void SetGumMass(float gumAmount)
  {
    gumAmount = Mathf.Max(0, gumAmount);
    _bubbleGumMass = gumAmount;
    _playerAnimation.SetGumMass(gumAmount);
  }

  public void PopBubble()
  {
    // Pop bubble
    if (_bubbleStoredMass > 0)
    {
      _playerAnimation.PopBubble();
      _bubbleStoredMass = 0f;
      AudioManager.Instance.PlaySound(gameObject, SfxBubblePop);
    }
  }

  private void Awake()
  {
    SetPlayerIndex(0);
    SetGumMass(0);
    _playerAnimation.SetBubbleSize(0);
    _playerAnimation.Footstep += OnFootStep;
    _actor.Landed += OnLanded;
  }

  public void SetPlayerIndex(int playerIndex)
  {
    _playerIndex = playerIndex;
    _playerInput = Rewired.ReInput.players.GetPlayer(playerIndex);
  }

  public void Kill()
  {
    // Tell the game manager that the player was killed
    OnPlayerTouchedLava?.Invoke(this);

    //TODO: Play death FX
    //TODO: Play death audio

    Instantiate(_fxSplashPrefab, transform.position, _fxSplashPrefab.transform.rotation);

    // Clean up the player game object
    Destroy(gameObject);
  }

  private void OnEnable()
  {
    _interaction.Interacted += OnInteracted;
  }

  private void OnDisable()
  {
    _interaction.Interacted -= OnInteracted;
  }

  private void Update()
  {
    if (_playerInput == null)
      return;

    float dt = Time.deltaTime;

    // If the player got launched 
    if (_hasStartedLaunch)
    {
      ApplyLaunchMovement(dt);
      return;
    }

    _attackCooldownTimer -= dt;

    // Gather input state
    float inputMoveAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveAxis);
    bool inputJumpButton = _playerInput.GetButtonDown(RewiredConsts.Action.Jump);
    bool inputJumpButtonReleased = _playerInput.GetButtonUp(RewiredConsts.Action.Jump);
    bool inputInteractButton = _playerInput.GetButtonDown(RewiredConsts.Action.Interact);
    bool inputChewButton = _playerInput.GetButtonDown(RewiredConsts.Action.Chew);
    Vector2 inputThrowAxis = new Vector2(
      _playerInput.GetAxis(RewiredConsts.Action.ThrowHorizontal),
      _playerInput.GetAxis(RewiredConsts.Action.ThrowVertical));

    // Apply movement state to actor
    _actor.MoveAxis = Vector2.right * inputMoveAxis;
    _playerAnimation.MoveAnimSpeed = Mathfx.Damp(_playerAnimation.MoveAnimSpeed, Mathf.Abs(inputMoveAxis), 0.25f, dt * 5);
    _playerAnimation.IsGrounded = _actor.Motor.GroundingStatus.IsStableOnGround;

    // Reset bubble jump once we hit ground
    if (_actor.Motor.GroundingStatus.IsStableOnGround)
      _didBubbleThisJump = false;

    // Apply capsule size to actor based on current gum mass
    float capsuleRadius = Mathf.Max(0.6f, _bubbleGumMass * 0.5f);
    float capsuleHeight = Mathf.Max(1.3f, _bubbleGumMass + 0.3f);
    float capsuleOffset = _actor.Motor.Capsule.height * 0.5f;
    _actor.Motor.SetCapsuleDimensions(capsuleRadius, capsuleHeight, capsuleOffset);

    // Apply bubble floating state
    if (_isInBubbleJump)
    {
      // Animate bubble size
      float bubbleShrinkAmount = Mathf.Min(dt * BubbleShrinkSpeed, _bubbleStoredMass);
      _bubbleStoredMass -= bubbleShrinkAmount;
      _playerAnimation.SetBubbleSize(_bubbleStoredMass);
      _isInBubbleJump &= _bubbleStoredMass > 0;

      // Apply floating state to actor
      _actor.AntiGravityScalar = _actor.GravityScalar + _bubbleStoredMass * BubbleFloatPower;
      _actor.Motor.ForceUnground();
      SetGumMass(_bubbleGumMass + bubbleShrinkAmount);
    }
    // Reset anti grav back to 0 when no bubble
    else
    {
      _actor.AntiGravityScalar = Mathfx.Damp(_actor.AntiGravityScalar, 0, 0.25f, dt * 3);
    }

    // Jumping
    if (inputJumpButton)
    {
      // Jump when on the ground, and when in the air we will inflate our bubble
      // if we have enough bubble mass
      if (_actor.Motor.GroundingStatus.IsStableOnGround)
      {
        _actor.Jump();
        _playerAnimation.Jump();
        AudioManager.Instance.PlaySound(gameObject, SfxJump);
      }
      else if (_bubbleGumMass > 0 && !_didBubbleThisJump && !_isInBubbleJump)
      {
        _bubbleStoredMass = _bubbleGumMass;
        _didBubbleThisJump = true;
        _isInBubbleJump = true;
        DropItem();
        AudioManager.Instance.PlaySound(gameObject, SfxBubbleInflate);
        SetGumMass(0);
      }
    }

    if (inputJumpButtonReleased && _isInBubbleJump)
    {
      SetGumMass(_bubbleStoredMass + _bubbleGumMass);
      _bubbleStoredMass = 0;
      AudioManager.Instance.PlaySound(gameObject, SfxBubbleDeflate);
    }

    // Interaction
    if (inputInteractButton)
    {
      if (_interaction.CurrentInteractable)
      {
        _interaction.TriggerInteract();
      }
      else if (_attackCooldownTimer <= 0)
      {
        Slap();
      }
    }

    // Throwing
    //
    if (_heldItem)
    {
      // Mouse controls
      if (_playerInput.controllers.hasMouse)
      {
        bool isThrowHeld = Input.GetMouseButton(0);
        if (isThrowHeld)
        {
          if (!_isThrowing)
          {
            _isThrowing = true;
            _throwUIRoot = WorldUIManager.Instance.ShowItem(transform, Vector3.up * 0.5f);
            _throwUI = Instantiate(_throwUIPrefab, _throwUIRoot);
          }

          Vector3 playerScreenPos = Mathfx.GetNormalizedScreenPos(MainCamera.Instance.Camera.WorldToScreenPoint(transform.position));
          Vector3 mousePos = Mathfx.GetNormalizedScreenPos(Input.mousePosition);
          _currentThrowAxis = (mousePos - playerScreenPos).WithZ(0);
          _currentThrowT = Mathf.Clamp01(_currentThrowAxis.magnitude / 0.25f);
          _throwUI.SetThrowVector(_currentThrowAxis, _currentThrowT);
        }
        else if (_isThrowing)
        {
          _isThrowing = false;
          WorldUIManager.Instance.HideItem(_throwUIRoot);
          if (_currentThrowAxis.magnitude > 0.1f)
          {
            ThrowItem(MainCamera.Instance.transform.rotation * _currentThrowAxis.normalized * _currentThrowT * -ThrowStrength);
            _currentThrowAxis = Vector3.zero;
            _currentThrowT = 0;
          }
        }
      }
      // Gamepad controls
      else
      {
        bool isThrowHeld = inputThrowAxis.magnitude > 0.1f;
        if (isThrowHeld || _isThrowing)
        {
          if (!_isThrowing)
          {
            _isThrowing = true;
            _throwUIRoot = WorldUIManager.Instance.ShowItem(transform, Vector3.up * 0.5f);
            _throwUI = Instantiate(_throwUIPrefab, _throwUIRoot);
          }

          _currentThrowAxis = inputThrowAxis;
          _currentThrowT = Mathf.Clamp01(_currentThrowAxis.magnitude);
          _throwUI.SetThrowVector(_currentThrowAxis, _currentThrowT);

          float lastThrowT = Mathf.Clamp01(_lastThrowAxis.magnitude);
          float throwDelta = _currentThrowT - lastThrowT;
          _lastThrowAxis = _currentThrowAxis;

          // While the throw axis delta is negative (heading towards zero)
          // and below some threshold, we'll consider ourselves to be in a flick state
          // Outside of the flick state, we'll track the last valid axis value to use as the
          // flick direction/magnitude
          if (throwDelta < -0.1f)
          {
            _isMidFlick = true;
          }
          else
          {
            if (_isMidFlick || _currentThrowT < 0.1f)
            {
              if (_isMidFlick && _lastNonNegativeThrowT > 0.1f)
              {
                ThrowItem(MainCamera.Instance.transform.rotation * _lastNonNegativeThrowAxis.normalized * _lastNonNegativeThrowT * -ThrowStrength);
              }

              WorldUIManager.Instance.HideItem(_throwUIRoot);
              _isThrowing = false;
              _isMidFlick = false;
              _lastNonNegativeThrowAxis = Vector3.zero;
              _currentThrowAxis = Vector3.zero;
              _lastNonNegativeThrowT = 0;
              _currentThrowT = 0;
              _lastThrowAxis = Vector3.zero;
            }

            _isMidFlick = false;
            _lastNonNegativeThrowAxis = _currentThrowAxis;
            _lastNonNegativeThrowT = _currentThrowT;
          }
        }
      }
    }

    // Chewing
    if (inputChewButton)
    {
      // Try to chew a held item
      if (_heldItem)
      {
        _playerAnimation.Chew();
        if (_heldItem.Chew(0.1f))
        {
          AudioManager.Instance.PlaySound(gameObject, SfxSwallow);

          if (_heldItem.GumMassValue > 0)
            AudioManager.Instance.PlaySound(gameObject, SfxInflate);
          else
            AudioManager.Instance.PlaySound(gameObject, SfxDeflate);

          SetGumMass(_bubbleGumMass + _heldItem.GumMassValue);
          Destroy(_heldItem.gameObject);
        }
      }
    }

    // Teleporting
    UpdateCurrentLevelSection();
    CheckSideTeleport();
  }

  private void UpdateCurrentLevelSection()
  {
    int newSectionIndex = -1;

    if (GameController.Instance != null)
    {
      LevelSection[] sections = GameController.Instance.LevelManager.LevelSections;

      for (int sectionIndex = 0; sectionIndex < sections.Length; ++sectionIndex)
      {
        LevelSection section = sections[sectionIndex];
        var playerYPos = gameObject.transform.position.y;
        var sectionYPos = section.SectionWorldCenter.y;
        var sectionHalfHeight = section.SectionHeight / 2.0f;
        var sectionBottom = sectionYPos - sectionHalfHeight;
        var sectionTop = sectionYPos + sectionHalfHeight;

        if (section != null && playerYPos >= sectionBottom && playerYPos <= sectionTop)
        {
          newSectionIndex = sectionIndex;
          break;
        }
      }

      if (newSectionIndex != _levelSectionIndex)
      {
        OnPlayerSectionChanged?.Invoke(newSectionIndex, _levelSectionIndex);
        _levelSectionIndex = newSectionIndex;
      }
    }
  }

  private void CheckSideTeleport()
  {
    if (GameController.Instance != null)
    {
      LevelSection[] sections = GameController.Instance.LevelManager.LevelSections;

      if (_levelSectionIndex >= 0 && _levelSectionIndex < sections.Length)
      {
        LevelSection section = sections[_levelSectionIndex];
        var playerXPos = gameObject.transform.position.x;
        var playerYPos = gameObject.transform.position.y;
        var playerZPos = gameObject.transform.position.z;
        var sectionXPos = section.SectionWorldCenter.x;
        var sectionHalfWidth = section.SectionWidth / 2.0f;
        var sectionLeft = sectionXPos - sectionHalfWidth;
        var sectionRight = sectionXPos + sectionHalfWidth;
        bool wantsTeleport = false;

        var newPlayerXPos = playerXPos;
        if (playerXPos < sectionLeft)
        {
          newPlayerXPos = sectionRight - 0.1f;
          wantsTeleport = true;
        }
        else if (playerXPos > sectionRight)
        {
          newPlayerXPos = sectionLeft + 0.1f;
          wantsTeleport = true;
        }

        if (wantsTeleport)
        {
          Vector3 newLocation = new Vector3(newPlayerXPos, playerYPos, playerZPos);
          TeleportPlayer(newLocation);
        }
      }
    }
  }

  private void TeleportPlayer(Vector3 NewLocation)
  {
    _actor.Motor.SetPosition(NewLocation);
  }

  private void DropItem()
  {
    if (_heldItem)
    {
      _heldItem.Drop();
      _playerAnimation.DropItem();
      _heldItem = null;
    }
  }

  private void ThrowItem(Vector3 throwVec)
  {
    if (_heldItem)
    {
      _playerAnimation.DropItem();
      _heldItem.Throw(throwVec);
      _heldItem = null;

      AudioManager.Instance.PlaySound(gameObject, SfxThrow);
    }
  }

  private void Slap()
  {
    _attackCooldownTimer = AttackCooldown;
    _playerAnimation.Slap();

    if (SfxSlap)
      AudioManager.Instance.PlaySound(gameObject, SfxSlap);

    int overlapCount = Physics.OverlapSphereNonAlloc(_slapAnchor.position, _slapRadius, _slapColliders, _slapMask);
    for (int i = 0; i < overlapCount; ++i)
    {
      var c = _slapColliders[i];
      ISlappable slappable = c.GetComponentInParent<ISlappable>();
      if (slappable != null && slappable != this)
      {
        slappable.ReceiveSlap(transform.position);
      }
    }
  }

  public void ReceiveLaunch()
  {
    if (_hasStartedLaunch)
      return;

    DisableMovement();

    _launchVelocity = new Vector3(0, 0, LaunchPower);
    _hasStartedLaunch = true;
  }

  private void ApplyLaunchMovement(float dt)
  {
    // Apply gravity
    _launchVelocity += Physics.gravity * (dt * _actor.GravityScalar);
    _launchVelocity *= 1f / (1f + _actor.Drag * dt);

    // Spin the character as they are launched
    Quaternion deltaRotation = Quaternion.Euler(0f, 0f, LaunchSpinRate * dt);
    transform.rotation *= deltaRotation;

    // Upate the position
    var newPosition = transform.position + _launchVelocity * dt;
    transform.position = newPosition;
  }

  public void DisableMovement()
  {
    _actor.Motor.enabled = false;
    _actor.enabled = false;
  }

  void ISlappable.ReceiveSlap(Vector3 fromPos)
  {
    if (_heldItem)
    {
      DropItem();
    }

    AudioManager.Instance.PlaySound(gameObject, SfxReceiveSlap);

    PopBubble();

    if (_bubbleGumMass > 0)
    {
      SetGumMass(_bubbleGumMass - 0.1f);
    }
  }

  private void OnFootStep()
  {
    AudioManager.Instance.PlaySound(gameObject, SfxFootstep);
  }

  private void OnLanded()
  {
    AudioManager.Instance.PlaySound(gameObject, SfxLand);
  }

  private void OnInteracted(InteractableObject interactable)
  {
    Item item = interactable.GetComponent<Item>();
    if (item && !_heldItem)
    {
      interactable.enabled = false;
      _playerAnimation.HoldItem(item);
      item.Pickup();
      _heldItem = item;
    }
  }
}