using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MenuNavigationManager : Singleton<MenuNavigationManager>
{
  public event System.Action<MenuItemUI> ItemActivated;
  public event System.Action<MenuItemUI> ItemSelected;

  public MenuItemUI SelectedItem => _selectionStack.Count > 0 ? _selectionStack[^1].Item : null;

  private List<SelectionState> _selectionStack = new();
  private MenuItemUI _holdToActivateItem;
  private float _holdToActivateTimer;
  private bool _isFakeDragActive;
  private bool _justFinishedDrag;
  private PointerEventData _fakeDragEventData;

  [System.Serializable]
  public struct SelectionState
  {
    public MenuItemUI Item;
    public Transform Scope;
  }

  public void SetSelectedItem(MenuItemUI menuItem)
  {
    if (SelectedItem != null)
      SelectedItem.SetHighlighted(false);

    SelectionState selection = _selectionStack[^1];
    selection.Item = menuItem;
    _selectionStack[^1] = selection;

    if (selection.Item != null)
    {
      selection.Item.SetHighlighted(true);
      ItemSelected?.Invoke(selection.Item);
    }
  }

  public void ClearSelection()
  {
    SetSelectedItem(null);
  }

  public void PushSelectionScope(MenuItemGroup group)
  {
    PushSelectionScope(group.transform);
    group.SelectIndex(0);
  }

  public void PopSelectionScope(MenuItemGroup group)
  {
    PopSelectionScope(group.transform);
  }

  public void PushSelectionScope(Transform scope)
  {
    _selectionStack.Add(new()
    {
      Item = null,
      Scope = scope,
    });

    EnsureSelectionScope();
  }

  public void PopSelectionScope(Transform scope)
  {
    for (int i = _selectionStack.Count - 1; i >= 0; i--)
    {
      if (_selectionStack[i].Scope == scope)
      {
        _selectionStack.RemoveAt(i);
        break;
      }
    }

    EnsureSelectionScope();
  }

  public bool IsItemInScope(MenuItemUI menuItem)
  {
    var selectionState = _selectionStack[^1];
    return selectionState.Scope == null || menuItem.transform.IsChildOf(selectionState.Scope);
  }

  private void Awake()
  {
    Instance = this;

    // Default selection scope covers everything
    PushSelectionScope(scope: null);
  }

  private void OnEnable()
  {
    MenuItemUI.ItemAdded += OnMenuItemAdded;
    MenuItemUI.ItemRemoved += OnMenuItemRemoved;
    MenuItemUI.PointerEnter += OnMenuItemPointerEnter;
    MenuItemUI.PointerExit += OnMenuItemPointerExit;

    PlayerMenuInput.MenuAcceptPressed += OnMenuAccept;
    PlayerMenuInput.MenuAcceptReleased += OnMenuAcceptUp;
    PlayerMenuInput.MenuUpPressed += OnMenuUp;
    PlayerMenuInput.MenuDownPressed += OnMenuDown;
    PlayerMenuInput.MenuRightPressed += OnMenuRight;
    PlayerMenuInput.MenuLeftPressed += OnMenuLeft;
  }

  private void OnDisable()
  {
    MenuItemUI.ItemAdded -= OnMenuItemAdded;
    MenuItemUI.ItemRemoved -= OnMenuItemRemoved;
    MenuItemUI.PointerEnter -= OnMenuItemPointerEnter;
    MenuItemUI.PointerExit -= OnMenuItemPointerExit;

    PlayerMenuInput.MenuAcceptPressed -= OnMenuAccept;
    PlayerMenuInput.MenuAcceptReleased -= OnMenuAcceptUp;
    PlayerMenuInput.MenuUpPressed -= OnMenuUp;
    PlayerMenuInput.MenuDownPressed -= OnMenuDown;
    PlayerMenuInput.MenuRightPressed -= OnMenuRight;
    PlayerMenuInput.MenuLeftPressed -= OnMenuLeft;
  }

  private void Update()
  {
    // Animate a hold to select item filling up
    if (_holdToActivateItem != null)
    {
      if (MenuFocus.AnyFocusTaken && PlayerMenuInput.MenuAccept)
      {
        _holdToActivateTimer += Time.unscaledDeltaTime;
        float fillT = Mathf.Clamp01(_holdToActivateTimer / _holdToActivateItem.HoldToFillTime);
        _holdToActivateItem.SetFillAmount(fillT);
        if (fillT >= 1)
        {
          ItemActivated?.Invoke(_holdToActivateItem);
          _holdToActivateItem.SetFillAmount(0);
          _holdToActivateItem = null;
        }
      }
      else
      {
        _holdToActivateItem.SetFillAmount(0);
        _holdToActivateItem = null;
      }
    }
  }

  private void OnAnyDragEnd(PointerEventData eventData)
  {
    // We store this so we can ignore the next mouse up for selection, we know it's due to dropping a drag item
    _justFinishedDrag = true;
  }

  private void OnMenuItemPointerEnter(MenuItemUI menuItem)
  {
    SetSelectedItem(menuItem);
  }

  private void OnMenuItemPointerExit(MenuItemUI menuItem)
  {
    ClearSelection();
  }

  private void OnMenuItemAdded(MenuItemUI menuItem)
  {
    menuItem.IsInSelectionScope = IsItemInScope(menuItem);
  }

  private void OnMenuItemRemoved(MenuItemUI menuItem)
  {
  }

  private void OnMenuAccept()
  {
    if (SelectedItem != null && !SelectedItem.IsDisabled)
    {
      // Handle selecting an item
      _justFinishedDrag = false;

      if (SelectedItem.HoldToActivate)
      {
        _holdToActivateItem = SelectedItem;
        _holdToActivateTimer = 0;
      }
    }
  }

  private void OnMenuAcceptUp()
  {
  }

  private void OnMenuUp()
  {
    SelectItemInDirection(Vector3.up);
  }

  private void OnMenuDown()
  {
    SelectItemInDirection(Vector3.down);
  }

  private void OnMenuRight()
  {
    SelectItemInDirection(Vector3.right);
  }

  private void OnMenuLeft()
  {
    SelectItemInDirection(Vector3.left);
  }

  private void EnsureSelectionScope()
  {
    for (int i = 0; i < MenuItemUI.Instances.Count; ++i)
    {
      MenuItemUI item = MenuItemUI.Instances[i];
      item.IsInSelectionScope = IsItemInScope(item);
    }

    SetSelectedItem(_selectionStack[^1].Item);
  }

  private void SelectItemInDirection(Vector3 selectDir)
  {
    // If there's no selection, just select the first item
    if (SelectedItem == null)
    {
      SetSelectedItem(MenuItemUI.NavigableInstances.Count > 0 ? MenuItemUI.NavigableInstances[0] : null);
      return;
    }

    // Find the nearest selectable using the auto navigation
    Selectable nextSelectable = SelectedItem.Selectable.FindSelectable(selectDir);
    if (nextSelectable == null)
      nextSelectable = SelectedItem.Selectable;

    // Try and get a menu item from the selectable, if we find one then we should try to select it
    MenuItemUI menuItem = nextSelectable.GetComponent<MenuItemUI>();
    if (menuItem != null)
    {
      SetSelectedItem(menuItem);
    }
  }
}