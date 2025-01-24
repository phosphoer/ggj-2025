using UnityEngine;

public class PlayerActorController : MonoBehaviour
{
  [SerializeField] private ActorController _actor = null;
  [SerializeField] private PlayerAnimation _playerAnimation = null;
  [SerializeField] private InteractionController _interaction = null;

  private Rewired.Player _playerInput;
  private Item _heldItem;
  private float _bubbleGumMass;

  private void Awake()
  {
    _playerInput = Rewired.ReInput.players.GetPlayer(0);
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
    float inputMoveAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveAxis);
    bool inputJumpButton = _playerInput.GetButtonDown(RewiredConsts.Action.Jump);
    bool inputInteractButton = _playerInput.GetButtonDown(RewiredConsts.Action.Interact);
    bool inputInteractChew = _playerInput.GetButtonDown(RewiredConsts.Action.Chew);

    _actor.MoveAxis = Vector2.right * inputMoveAxis;

    if (inputJumpButton)
    {
      // Jump when on the ground, and when in the air we will inflate our bubble
      // if we have enough bubble mass
      if (_actor.Motor.GroundingStatus.IsStableOnGround)
      {
        _actor.Jump();
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