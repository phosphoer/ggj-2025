using UnityEngine;
using System.Collections.Generic;

public class InteractableObject : MonoBehaviour
{
  public static IReadOnlyList<InteractableObject> Instances => _instances;

  public event System.Action<InteractionController> Interacted;

  public float InteractDistance = 100;
  public BoolStateStack DisableInteraction = new();

  [SerializeField] private InteractPromptUI _promptUIPrefab = null;
  [SerializeField] private string _interactPromptText = "Pickup";

  private RectTransform _promptUIRoot;

  private static List<InteractableObject> _instances = new();

  public void Interact(InteractionController interactionController)
  {
    Interacted?.Invoke(interactionController);
  }

  public void ShowPrompt(Rewired.Player forPlayer)
  {
    HidePrompt();
    _promptUIRoot = WorldUIManager.Instance.ShowItem(transform, Vector3.up);
    InteractPromptUI uiPrompt = Instantiate(_promptUIPrefab, _promptUIRoot);
    uiPrompt.PromptText = _interactPromptText;
    uiPrompt.GlyphHint.playerId = forPlayer.id;
  }

  public void HidePrompt()
  {
    if (_promptUIRoot != null)
    {
      WorldUIManager.Instance.HideItem(_promptUIRoot);
      _promptUIRoot = null;
    }
  }

  private void OnEnable()
  {
    _instances.Add(this);
  }

  private void OnDisable()
  {
    _instances.Remove(this);
  }
}