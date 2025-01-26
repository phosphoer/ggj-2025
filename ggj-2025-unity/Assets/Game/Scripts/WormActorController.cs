using UnityEngine;

public class WormActorController : MonoBehaviour
{
  public Rewired.Player PlayerInput => _playerInput;

  public float MuckMovementSpeed = 5.0f;
  public float AirMovementSpeed = 5.0f;
  public float JumpSpeed = 5.0f;
  public float GravityScalar = 5.0f; 
  public float Drag = 1.0f;
  public float TransformationTime = 1.5f;

  public event System.Action<WormActorController, PlayerActorController> OnWormTouchedPlayer;
  public event System.Action<WormActorController> OnWormTransformComplete;

  private Rewired.Player _playerInput;
  private SphereCollider _headCollider;
  private Vector3 _moveAxis = Vector3.zero;
  private Vector3 _velocity= Vector3.zero;
  private float _transformationTimer= 0;

  public enum eMovementState
  {
    muckMovement,
    airborne,
    transforming
  }
  eMovementState _movementMode = eMovementState.muckMovement;
  public bool IsTransforming => _movementMode == eMovementState.transforming;

  private void Awake()
  {
    _headCollider= gameObject.GetComponent<SphereCollider>();
    SetPlayerIndex(0);
  }

  private int _playerIndex = -1;
  public int PlayerIndex => _playerIndex;

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

    // Gather input state
    float inputMoveAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveAxis);
    bool inputJumpButton = _playerInput.GetButtonDown(RewiredConsts.Action.Jump);
    Vector2 inputJumpAxis = new Vector2(
      -_playerInput.GetAxis(RewiredConsts.Action.ThrowHorizontal),
      _playerInput.GetAxis(RewiredConsts.Action.ThrowVertical));

    // Update the move axis
    _moveAxis = Vector2.left * inputMoveAxis;

    // Handle jump button input 
    if (inputJumpButton && _movementMode == eMovementState.muckMovement)
    {
      Jump(new Vector3(inputJumpAxis.x, inputJumpAxis.y, 0.0f));
    }

    // Update the velocity and position based on movement mode
    switch(_movementMode)
    {
    case eMovementState.muckMovement:
      UpdateMuckMovement(dt);
      break;
    case eMovementState.airborne:
      UpdateJumpMovement(dt);
      break;
    case eMovementState.transforming:
      UpdateTransforming(dt);
      break;
    }

    // Keep the worm at or above the muck
    ClampYPositionAboveMuck();

    // Teleporting
    ClompToSides();
  }

  private void Jump(Vector3 jumpDirection)
  {
    var safeJumpDirection= Mathfx.Approx(jumpDirection, Vector3.zero, 0.01f) ? Vector3.up : Vector3.Normalize(jumpDirection);
    _velocity = safeJumpDirection * JumpSpeed;

    _movementMode = eMovementState.airborne;
  }

  private void UpdateMuckMovement(float dt)
  {
    // Update the movement velocity
    _velocity= _moveAxis * MuckMovementSpeed;

    // Update the worm position
    ApplyVelocityToPosition(dt);

    // Face toward your velocity
    UpdateFacing(dt);
  }

  private void ApplyVelocityToPosition(float dt)
  {
    // Update the worm position
    var newPosition = GetWormPosition() + _velocity * dt;
    SetWormPosition(newPosition);
  }

  private void UpdateJumpMovement(float dt)
  {
    // Update the movement velocity
    _velocity += _moveAxis*AirMovementSpeed*dt;
    
    // Apply gravity
    _velocity += Physics.gravity * (dt * GravityScalar);
    _velocity *= 1f / (1f + Drag * dt);

    // Update position based on velocity
    ApplyVelocityToPosition(dt);

    // Face toward your velocity
    UpdateFacing(dt);

    // See if we have touched another player
    CheckForPlayerOverlap();

    // See if we have landed
    bool isFalling = _velocity.y < 0;
    Vector3 wormPosition = GetWormPosition();
    float groundedY = GetMuckPlaneY();
    if (wormPosition.y <= groundedY && isFalling)
    {
      _movementMode = eMovementState.muckMovement;
    }
  }

  public void StartPlayerTransformation()
  {
    _transformationTimer = TransformationTime;
    _movementMode= eMovementState.transforming;
  }

  private void UpdateTransforming(float dt)
  {
    _velocity= Vector3.zero;
    _transformationTimer-= dt;

    if (_transformationTimer <= 0)
    {
      OnWormTransformComplete?.Invoke(this);
      _movementMode = eMovementState.airborne;
    }
  }

  public void CheckForPlayerOverlap()
  {
    if (_headCollider == null)
      return;

    var players= GameController.Instance.SpawnedPlayers;

    foreach (var player in players)
    {
      float distanceToPlayer= Vector3.Distance(player.transform.position, _headCollider.transform.position);

      if (distanceToPlayer < _headCollider.radius)
      {
        OnWormTouchedPlayer(this, player); 
        break;
      }
    }
  }

  private void UpdateFacing(float dt)
  {
    // Make the worm face their movement direction
    var currentRotation = gameObject.transform.rotation;
    var currentFacing = currentRotation * Vector3.forward;
    var targetFacing= Mathfx.Approx(_velocity, Vector3.zero, 0.01f) ? currentFacing : Vector3.Normalize(_velocity);
    var targetRotation= Quaternion.LookRotation(targetFacing);
    var blendedRotation= Mathfx.Damp(currentRotation, targetRotation, 0.25f, dt * 5);

    gameObject.transform.rotation = blendedRotation;
  }

  private float GetMuckPlaneY()
  {
    return GameController.Instance.LavaController.LavaYPosition;
  }

  private Vector3 GetWormPosition()
  {
    return gameObject.transform.position;
  }

  private void SetWormPosition(Vector3 newPosition)
  {
    gameObject.transform.position= newPosition;
  }

  private void ClampYPositionAboveMuck()
  {
    var clampedPosition = GetWormPosition();

    clampedPosition.y = Mathf.Max(clampedPosition.y, GetMuckPlaneY());
    gameObject.transform.position = clampedPosition;
  }

  private void ClompToSides()
  {
    if (GameController.Instance != null)
    {
      LevelSection[] sections = GameController.Instance.LevelManager.LevelSections;
      LevelSection section = sections[0];

      var playerXPos = gameObject.transform.position.x;
      var playerYPos = gameObject.transform.position.y;
      var playerZPos = gameObject.transform.position.z;
      var sectionXPos = section.SectionWorldCenter.x;
      var sectionHalfWidth = section.SectionWidth / 2.0f;
      var sectionLeft = sectionXPos - sectionHalfWidth;
      var sectionRight = sectionXPos + sectionHalfWidth;
      bool wantsClamp = false;

      var newPlayerXPos = playerXPos;
      if (playerXPos < sectionLeft)
      {
        newPlayerXPos = sectionLeft;
        wantsClamp = true;
      }
      else if (playerXPos > sectionRight)
      {
        newPlayerXPos = sectionRight;
        wantsClamp = true;
      }

      if (wantsClamp)
      {
        SetWormPosition(new Vector3(newPlayerXPos, playerYPos, playerZPos));
      }
    }
  }
}