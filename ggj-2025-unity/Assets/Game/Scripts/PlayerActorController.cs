using UnityEngine;

public class PlayerActorController : MonoBehaviour
{
  public float BubbleShrinkSpeed = 0.25f;
  public float BubbleFloatPower = 0.25f;

  [SerializeField] private ActorController _actor = null;
  [SerializeField] private PlayerAnimation _playerAnimation = null;
  [SerializeField] private InteractionController _interaction = null;
  [SerializeField] private Transform _slapAnchor = null;
  [SerializeField] private float _slapRadius = 0.5f;
  [SerializeField] private LayerMask _slapMask = default;

  public event System.Action<PlayerActorController> OnPlayerKilled;

  private Rewired.Player _playerInput;
  private Item _heldItem;
  private float _bubbleGumMass;
  private float _bubbleStoredMass;
  private Collider[] _slapColliders = new Collider[4];

  public void SetGumMass(float gumAmount)
  {
    _bubbleGumMass = gumAmount;
    _playerAnimation.SetGumMass(gumAmount);
  }

  private void Awake()
  {
    SetPlayerInput(0);
    SetGumMass(0);
    _playerAnimation.SetBubbleSize(0);
  }

  public void SetPlayerInput(int playerIndex)
  {
    _playerInput = Rewired.ReInput.players.GetPlayer(playerIndex);
  }

  public void Kill()
  {
    // Tell the game manager that the player was killed
    OnPlayerKilled?.Invoke(this);

    //TODO: Play death FX
    //TODO: Play death audio

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

    // Gather input state
    float inputMoveAxis = _playerInput.GetAxis(RewiredConsts.Action.MoveAxis);
    bool inputJumpButton = _playerInput.GetButtonDown(RewiredConsts.Action.Jump);
    bool inputInteractButton = _playerInput.GetButtonDown(RewiredConsts.Action.Interact);
    bool inputInteractChew = _playerInput.GetButtonDown(RewiredConsts.Action.Chew);

    // Apply movement state to actor
    _actor.MoveAxis = Vector2.right * inputMoveAxis;
    _playerAnimation.MoveAnimSpeed = Mathfx.Damp(_playerAnimation.MoveAnimSpeed, Mathf.Abs(inputMoveAxis), 0.25f, dt * 5);
    _playerAnimation.IsGrounded = _actor.Motor.GroundingStatus.IsStableOnGround;

    _actor.Motor.Capsule.radius = Mathf.Max(0.6f, _bubbleGumMass * 0.5f);
    _actor.Motor.Capsule.height = Mathf.Max(1.3f, _bubbleGumMass + 0.3f);
    _actor.Motor.Capsule.center = Vector3.up * _actor.Motor.Capsule.height * 0.5f;

    // Apply bubble floating state
    if (_bubbleStoredMass > 0)
    {
      // Animate bubble size
      float bubbleShrinkAmount = Mathf.Min(dt * BubbleShrinkSpeed, _bubbleStoredMass);
      _bubbleStoredMass -= bubbleShrinkAmount;
      _playerAnimation.SetBubbleSize(_bubbleStoredMass);

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
      }
      else if (_bubbleGumMass >= 0)
      {
        _bubbleStoredMass = _bubbleGumMass;
        SetGumMass(0);
      }
    }

    // Interaction
    if (inputInteractButton)
    {
      if (_interaction.CurrentInteractable)
      {
        _interaction.TriggerInteract();
      }
      else
      {
        Slap();
      }
    }

    // Chewing
    if (inputInteractChew)
    {
      // Try to chew a held item
      if (_heldItem)
      {
        _playerAnimation.Chew();
        if (_heldItem.Chew(0.1f))
        {
          SetGumMass(_bubbleGumMass + _heldItem.GumMassValue);
          Destroy(_heldItem.gameObject);
        }
      }
    }
  }

  private void Slap()
  {
    _playerAnimation.Slap();

    int overlapCount = Physics.OverlapSphereNonAlloc(_slapAnchor.position, _slapRadius, _slapColliders, _slapMask);
    for (int i = 0; i < overlapCount; ++i)
    {
      var c = _slapColliders[i];
      ISlappable slappable = c.GetComponent<ISlappable>();
      if (slappable != null)
      {
        slappable.ReceiveSlap(transform.position);
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
      item.Pickup();
      _heldItem = item;
    }
  }
}