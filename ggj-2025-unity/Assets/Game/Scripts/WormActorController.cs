using UnityEngine;

public class WormActorController : MonoBehaviour
{
  public event System.Action<WormActorController, PlayerActorController> OnWormTouchedPlayer;
  public event System.Action<WormActorController> OnWormTransformComplete;

  public Rewired.Player PlayerInput => _playerInput;
  public int PlayerIndex => _playerIndex;
  public float TransformationGumAmount => _transformationGum;
  public bool IsTransforming => _isTransforming;

  public float MuckMovementSpeed = 5.0f;
  public float AirMovementSpeed = 5.0f;
  public float JumpSpeed = 5.0f;
  public float GravityScalar = 5.0f;
  public float Drag = 1.0f;
  public float TransformationTime = 1.5f;

  [SerializeField] private ParticleSystem _fxSmokeBurstPrefab = null;
  [SerializeField] private ParticleSystem _fxMuckSpray = null;

  private int _playerIndex = -1;
  private Rewired.Player _playerInput;
  private SphereCollider _headCollider;
  private Vector3 _velocity = Vector3.zero;
  private bool _isInMuck;
  private bool _isTransforming;
  private float _jumpCharge;
  private float _transformationTimer = 0;
  private float _transformationGum;
  private float _jumpChargeTimer;

  private void Awake()
  {
    _headCollider = gameObject.GetComponent<SphereCollider>();
    SetPlayerIndex(0);
  }

  public void SetPlayerIndex(int playerIndex)
  {
    _playerIndex = playerIndex;
    _playerInput = Rewired.ReInput.players.GetPlayer(playerIndex);
  }

  private void Update()
  {
    if (_playerInput == null)
      return;

    float dt = Time.deltaTime;
    if (_isTransforming)
    {
      UpdateTransforming(dt);
      return;
    }

    // Gather input state
    float inputMoveAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveAxis);
    bool inputJumpButton = _playerInput.GetButton(RewiredConsts.Action.Jump);

    // Apply input to velocity
    float movementSpeed = _isInMuck ? MuckMovementSpeed : AirMovementSpeed;
    _velocity.x += -inputMoveAxis * dt * movementSpeed / (_jumpCharge + 1);

    // Calculate if we're in muck
    float muckYOffset = 0.5f;
    float heightAboveMuck = transform.position.y - (GameController.Instance.LavaController.LavaYPosition + muckYOffset);
    if (heightAboveMuck <= 0)
      _isInMuck = true;
    else if (heightAboveMuck > muckYOffset)
      _isInMuck = false;

    // Charge up a jump
    if (_isInMuck && inputJumpButton)
    {
      _jumpCharge = Mathf.Clamp(_jumpCharge + dt * JumpSpeed * 0.5f, 0, JumpSpeed);
      float jumpChargeT = _jumpCharge / JumpSpeed;
      _jumpChargeTimer += dt * jumpChargeT * 20;
    }

    // Look up and wiggle as we charge
    if (_jumpCharge > 0 && inputJumpButton)
    {
      float jumpChargeT = _jumpCharge / JumpSpeed;
      float wiggleAmount = Mathf.Sin(_jumpChargeTimer) * (2 - jumpChargeT) * 4;
      Quaternion jumpLookRot = Quaternion.LookRotation(Vector3.up, Vector3.right);
      transform.rotation = Mathfx.Damp(transform.rotation, jumpLookRot, 0.25f, dt * 5);
      transform.Rotate(wiggleAmount, 0, 0, Space.Self);
    }

    // Release jump charge
    if (!inputJumpButton && _jumpCharge > 0)
    {
      _velocity.y = _jumpCharge;
      _jumpCharge = 0;
      _jumpChargeTimer = 0;
      _isInMuck = false;
    }

    // Apply velocity to position
    transform.position += _velocity * dt;

    // Float in muck or fall into it
    if (_isInMuck)
      _velocity.y -= heightAboveMuck * dt * 5;
    else
      _velocity.y += Physics.gravity.y * dt * GravityScalar;

    // Apply drag to velocity
    _velocity *= 1f / (1f + Drag * dt);
    if (_isInMuck)
      _velocity.y *= 1f / (1f + Drag * 2 * dt);

    // Clamp position
    ClampPositionToSides();

    // Check for player collision and begin transforming if so
    if (!_isInMuck)
    {
      CheckForPlayerOverlap();
    }

    // Face movement dir and wiggle
    if (_jumpCharge <= 0)
    {
      Quaternion facingRot = Quaternion.LookRotation(_velocity);
      facingRot = Quaternion.Euler(0, Mathf.Sin(transform.position.x) * 40, 0) * facingRot;
      transform.rotation = Mathfx.Damp(transform.rotation, facingRot, 0.25f, dt * 3);
    }

    // Muck fx
    if (_isInMuck && !_fxMuckSpray.isPlaying)
      _fxMuckSpray.Play();
    else if (!_isInMuck && _fxMuckSpray.isPlaying)
      _fxMuckSpray.Stop();
  }

  private void ClampPositionToSides()
  {
    Vector3 screenPos = MainCamera.Instance.Camera.WorldToScreenPoint(transform.position);
    Vector3 normalizedPos = Mathfx.GetNormalizedScreenPos(screenPos);

    normalizedPos.x = Mathf.Clamp01(normalizedPos.x);

    Vector3 clampedScreenPos = Mathfx.GetScreenPosFromNormalized(normalizedPos);
    Vector3 clampedPos = MainCamera.Instance.Camera.ScreenToWorldPoint(clampedScreenPos);
    transform.position = clampedPos;
  }

  public void StartPlayerTransformation(float gumMass)
  {
    Instantiate(_fxSmokeBurstPrefab, transform.position, _fxSmokeBurstPrefab.transform.rotation);

    _transformationTimer = TransformationTime;
    _transformationGum = gumMass;
    _isTransforming = true;
  }

  private void UpdateTransforming(float dt)
  {
    _velocity = Vector3.zero;
    _transformationTimer -= dt;

    float transformT = 1 - _transformationTimer / TransformationTime;

    transform.Rotate(0, 0, transformT * 360 * dt, Space.Self);
    transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, transformT);

    if (_transformationTimer <= 0)
    {
      OnWormTransformComplete?.Invoke(this);
      _isTransforming = false;
    }
  }

  public void CheckForPlayerOverlap()
  {
    if (_headCollider == null)
      return;

    var players = GameController.Instance.SpawnedPlayers;

    foreach (var player in players)
    {
      float distanceToPlayer = Vector3.Distance(player.transform.position, _headCollider.transform.position);
      if (distanceToPlayer < _headCollider.radius)
      {
        OnWormTouchedPlayer?.Invoke(this, player);
        break;
      }
    }
  }
}