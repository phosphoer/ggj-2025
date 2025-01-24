using UnityEngine;

public class PlayerActorController : MonoBehaviour
{
  [SerializeField] private ActorController _actor = null;
  [SerializeField] private PlayerAnimation _playerAnimation = null;

  private Rewired.Player _playerInput;

  private void Awake()
  {
    _playerInput = Rewired.ReInput.players.GetPlayer(0);
  }

  private void Update()
  {
    float inputMoveAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveAxis);
    bool inputJumpButton = _playerInput.GetButtonDown(RewiredConsts.Action.Jump);
    bool inputInteractButton = _playerInput.GetButtonDown(RewiredConsts.Action.Interact);

    _actor.MoveAxis = Vector2.right * inputMoveAxis;

    if (inputJumpButton)
    {
      _actor.Jump();
    }
  }
}