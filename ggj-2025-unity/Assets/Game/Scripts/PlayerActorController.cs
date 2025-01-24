using System;
using UnityEngine;

public class PlayerActorController : MonoBehaviour
{
  [SerializeField] private ActorController _actor = null;
  [SerializeField] private PlayerAnimation _playerAnimation = null;
  [SerializeField] private InteractionController _interaction = null;

  private Rewired.Player _playerInput;

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

    _actor.MoveAxis = Vector2.right * inputMoveAxis;

    if (inputJumpButton)
    {
      _actor.Jump();
    }

    if (inputInteractButton)
    {
      _interaction.TriggerInteract();
    }
  }

  private void OnInteracted(InteractableObject interactable)
  {
    Item item = interactable.GetComponent<Item>();
    if (item)
    {
      interactable.enabled = false;
      _playerAnimation.HoldItem(item);
    }
  }
}