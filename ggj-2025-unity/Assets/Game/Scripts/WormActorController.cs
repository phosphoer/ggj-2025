using UnityEngine;

public class WormActorController : MonoBehaviour
{
  public Rewired.Player PlayerInput => _playerInput;

  [SerializeField] private ActorController _actor = null;
  //[SerializeField] private WormAnimation _wormAnimation = null;

  public event System.Action<WormActorController, PlayerActorController> OnWormTouchedPlayer;

  private Rewired.Player _playerInput;

  private void Awake()
  {
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
    bool inputChewButton = _playerInput.GetButtonDown(RewiredConsts.Action.Chew);
    Vector2 inputThrowAxis = new Vector2(
      _playerInput.GetAxis(RewiredConsts.Action.ThrowHorizontal),
      _playerInput.GetAxis(RewiredConsts.Action.ThrowVertical));

    // Apply movement state to actor
    _actor.MoveAxis = Vector2.right * inputMoveAxis;

    // Make the worm face their movement direction
    var currentRotation= _actor.Motor.Transform.rotation;
    var moveDirection= Mathf.Abs(inputMoveAxis) > 0.01f ? Vector3.Normalize(_actor.MoveAxis) : Vector3.forward;
    var targetRotation= Quaternion.LookRotation(moveDirection);
    _actor.Motor.RotateCharacter(Mathfx.Damp(currentRotation, targetRotation, 0.25f, dt * 5));

    // Keep the character clamped above the lava plane
    var lavaYPosition = GameController.Instance.LavaController.LavaYPosition;
    var clampedPosition= _actor.Motor.Transform.position;
    clampedPosition.y= Mathf.Max(clampedPosition.y, lavaYPosition);
    _actor.Motor.SetPosition(clampedPosition);

    // Worm always is ungrounded
    _actor.Motor.ForceUnground();

    // Teleporting
    CheckSideTeleport();
  }

  private void CheckSideTeleport()
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
        TeleportWorm(newLocation);
      }
    }
  }

  private void TeleportWorm(Vector3 NewLocation)
  {
    _actor.Motor.SetPosition(NewLocation);
  }
}