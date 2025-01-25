using UnityEngine;

public class PlayerActorController : MonoBehaviour
{
  [SerializeField] private ActorController _actor = null;
  [SerializeField] private PlayerAnimation _playerAnimation = null;
  [SerializeField] private InteractionController _interaction = null;

  private Rewired.Player _playerInput;
  private Item _heldItem;
  private float _bubbleGumMass;

  public void SetGumMass(float gumAmount)
  {
    _bubbleGumMass = gumAmount;
    _playerAnimation.SetGumMass(gumAmount);
  }

  private void Awake()
  {
    SetPlayerInput(0);
    SetGumMass(0);
  }

  public void SetPlayerInput(int playerIndex)
  {
    _playerInput = Rewired.ReInput.players.GetPlayer(playerIndex);
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
    float dt = Time.deltaTime;
    float inputMoveAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveAxis);
    bool inputJumpButton = _playerInput.GetButtonDown(RewiredConsts.Action.Jump);
    bool inputInteractButton = _playerInput.GetButtonDown(RewiredConsts.Action.Interact);
    bool inputInteractChew = _playerInput.GetButtonDown(RewiredConsts.Action.Chew);

    _actor.MoveAxis = Vector2.right * inputMoveAxis;
    _playerAnimation.MoveAnimSpeed = Mathfx.Damp(_playerAnimation.MoveAnimSpeed, Mathf.Abs(inputMoveAxis), 0.25f, dt * 5);
    _playerAnimation.IsGrounded = _actor.Motor.GroundingStatus.IsStableOnGround;

    if (inputJumpButton)
    {
      // Jump when on the ground, and when in the air we will inflate our bubble
      // if we have enough bubble mass
      if (_actor.Motor.GroundingStatus.IsStableOnGround)
      {
        _actor.Jump();
        _playerAnimation.Jump();
      }
      else if (_bubbleGumMass >= 1)
      {
      }
    }

    if (inputInteractButton)
    {
      _interaction.TriggerInteract();
    }

    if (inputInteractChew)
    {
      // Try to chew a held item
      if (_heldItem)
      {
        _playerAnimation.Chew();
        if (_heldItem.Chew(0.1f))
        {
          SetGumMass(_bubbleGumMass + 1);
          Destroy(_heldItem.gameObject);
        }
      }
    }
  }

  private void OnInteracted(InteractableObject interactable)
  {
    Item item = interactable.GetComponent<Item>();
    if (item)
    {
      interactable.enabled = false;
      _playerAnimation.HoldItem(item);
      _heldItem = item;
    }
  }
}