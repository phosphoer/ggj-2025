using UnityEngine;

public class InteractionController : MonoBehaviour
{
  public event System.Action<InteractableObject> Interacted;

  public InteractableObject CurrentInteractable => _currentInteractable;

  private int _updateIndex;
  private InteractableObject _currentInteractable;
  private InteractableObject _closestInteractable;
  private float _currentMinInteractableDist = Mathf.Infinity;

  public void TriggerInteract()
  {
    if (_currentInteractable != null)
    {
      _currentInteractable.Interact(this);
      Interacted?.Invoke(_currentInteractable);
    }
  }

  private void Update()
  {
    // Check which interactables are in range, one per frame
    if (_updateIndex < InteractableObject.Instances.Count)
    {
      InteractableObject interactable = InteractableObject.Instances[_updateIndex];

      Vector3 toInteractable = interactable.transform.position - transform.position;
      float distance = toInteractable.magnitude;
      if (distance < interactable.InteractDistance && distance < _currentMinInteractableDist)
      {
        _closestInteractable = interactable;
        _currentMinInteractableDist = distance;
      }
    }

    // Evaluate what current interactable is once we've looked at them
    _updateIndex = Mathfx.Wrap(_updateIndex + 1, 0, InteractableObject.Instances.Count);
    if (_updateIndex == 0)
    {
      // Handle a change of current interactable
      if (_closestInteractable != _currentInteractable)
      {
        // Unregister current interactable
        if (_currentInteractable != null)
        {
          _currentInteractable.HidePrompt();
        }

        _currentInteractable = _closestInteractable;

        // Register new interactable
        if (_currentInteractable != null)
        {
          _currentInteractable.ShowPrompt();
        }
      }

      // Reset closest interactable
      _currentMinInteractableDist = Mathf.Infinity;
      _closestInteractable = null;
    }
  }
}