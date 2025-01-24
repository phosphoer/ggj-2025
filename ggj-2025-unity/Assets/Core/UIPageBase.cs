using System.Collections;
using UnityEngine;

public class UIPageBase : MonoBehaviour
{
  public event System.Action Shown;
  public event System.Action Hidden;

  public bool IsVisible => _isVisible;

  public bool IsAnyPartAnimating
  {
    get
    {
      bool animating = false;
      foreach (UIHydrate anim in _hydrateOnShow)
        animating |= anim.IsAnimating;

      return animating;
    }
  }

  public int ZOrder { get; set; }

  public bool ShowOnStart = false;
  public bool IsModal = false;

  [SerializeField] private UIHydrate[] _hydrateOnShow = null;

  private bool _isVisible = false;
  private int _dehydrateRefCount = 0;

  // Not using default parameter here just make [ContextMenu] work 
  [ContextMenu("Show")]
  public void Show()
  {
    Show(true);
  }

  public void Show(bool playAnim)
  {
    if (!_isVisible)
    {
      _isVisible = true;
      gameObject.SetActive(true);

      // Debug.Log($"{name} showing");

      if (playAnim)
      {
        foreach (UIHydrate hydrate in _hydrateOnShow)
        {
          hydrate.Hydrate();
        }
      }

      if (IsModal)
      {
        CanvasCursor.PushVisible();
      }

      Shown?.Invoke();
    }
  }

  [ContextMenu("Hide")]
  public void Hide()
  {
    Hide(true);
  }

  public void Hide(bool playAnim)
  {
    if (_isVisible)
    {
      _isVisible = false;
      // Debug.Log($"{name} hiding");

      _dehydrateRefCount = _hydrateOnShow.Length;
      if (playAnim)
      {
        foreach (UIHydrate hydrate in _hydrateOnShow)
        {
          hydrate.Dehydrate(OnDehydrateComplete);
        }
      }

      if (!playAnim || _hydrateOnShow.Length == 0)
      {
        _dehydrateRefCount = 0;
        OnDehydrateComplete();
      }

      if (IsModal && CanvasCursor.IsVisible)
      {
        CanvasCursor.PopVisible();
      }

      Hidden?.Invoke();
    }
  }

  public void Toggle()
  {
    if (_isVisible)
    {
      Hide();
    }
    else
    {
      Show();
    }
  }

  protected virtual void Awake()
  {
    _isVisible = gameObject.activeSelf;
    if (ShowOnStart)
      Show();
    else
      Hide(playAnim: false);
  }

  private void OnDehydrateComplete()
  {
    --_dehydrateRefCount;
    if (_dehydrateRefCount <= 0)
    {
      gameObject.SetActive(false);
    }
  }

#if UNITY_EDITOR
  [ContextMenu("Gather Hydrate-ables")]
  private void GatherHydrates()
  {
    UnityEditor.Undo.RecordObject(this, "Gather Hydrate-ables");
    _hydrateOnShow = GetComponentsInChildren<UIHydrate>(includeInactive: true);
  }
#endif
}