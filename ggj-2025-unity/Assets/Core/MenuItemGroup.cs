using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MenuItemGroup : MonoBehaviour
{
  public enum ListScrollType
  {
    Center,
    Bounds,
  }

  public event System.Action<MenuItemUI> ItemActivated;
  public event System.Action<MenuItemUI> ItemSelected;

  public IReadOnlyList<MenuItemUI> Items => _menuItems;
  public MenuItemUI SelectedItem { get; private set; }
  public MenuItemUI LastValidSelectedItem { get; private set; }
  public int SelectedIndex { get; private set; }

  public bool EnableAutoScroll = true;
  public float ItemScrollBoundsPadding = 10;
  public ListScrollType ScrollType = ListScrollType.Bounds;
  public List<MenuItemGroup> LinkedMenus;
  public bool EnableLinks = true;

  [SerializeField] private MenuItemUI _menuItemPrefab = null;
  [SerializeField] private Transform _menuItemRoot = null;
  [SerializeField] private ScrollRectTransform _scrollRect = null;

  private List<MenuItemUI> _menuItems = new();
  private MenuItemUI _lastSelectedItem;
  private Vector2 _desiredScroll;
  private Vector2 _currentScroll;

  public MenuItemUI AddItem()
  {
    MenuItemUI menuItem = Instantiate(_menuItemPrefab, _menuItemRoot);
    menuItem.transform.SetIdentityTransformLocal();
    menuItem.gameObject.SetActive(true);

    AddItem(menuItem);

    return menuItem;
  }

  public void AddItem(MenuItemUI menuItem)
  {
    _menuItems.Add(menuItem);
    menuItem.MenuGroup = this;
  }

  public bool RemoveItem(MenuItemUI menuItem, bool destroy = true)
  {
    if (_menuItems.Remove(menuItem))
    {
      Destroy(menuItem.gameObject);
      return true;
    }

    return false;
  }

  public void RemoveAllItems()
  {
    for (int i = 0; i < _menuItems.Count; ++i)
    {
      var menuItem = _menuItems[i];
      Destroy(menuItem.gameObject);
    }

    _menuItems.Clear();
  }

  public void SelectIndex(int index)
  {
    index = _menuItems.ClampIndex(index);
    MenuNavigationManager.Instance.SetSelectedItem(_menuItems[index]);
  }

  public void ScrollToSelectedItem()
  {
    if (SelectedItem != null && _scrollRect != null)
    {
      Canvas.ForceUpdateCanvases();
      if (ScrollType == ListScrollType.Bounds)
      {
        Vector2 scrollPos = _scrollRect.GetClampedScrollPos(SelectedItem.transform as RectTransform, ItemScrollBoundsPadding);
        _desiredScroll = scrollPos;
      }
      else if (ScrollType == ListScrollType.Center)
      {
        Vector2 scrollPos = _scrollRect.GetCenterScrollPos(SelectedItem.transform as RectTransform);
        _desiredScroll = scrollPos;
      }
    }
  }

  public void SetNavigable(bool isNavigable)
  {
    for (int i = 0; i < _menuItems.Count; ++i)
    {
      _menuItems[i].IsNavigable = isNavigable;
    }
  }

  private void Awake()
  {
    if (_menuItemPrefab != null && _menuItemPrefab.gameObject.scene.IsValid())
    {
      _menuItemPrefab.gameObject.SetActive(false);
    }
  }

  private void OnEnable()
  {
    MenuNavigationManager.Instance.ItemActivated += OnMenuItemActivated;
    MenuNavigationManager.Instance.ItemSelected += OnMenuItemSelected;
  }

  private void OnDisable()
  {
    MenuNavigationManager.Instance.ItemActivated -= OnMenuItemActivated;
    MenuNavigationManager.Instance.ItemSelected -= OnMenuItemSelected;
  }

  private void Update()
  {
    // Animate scroll position
    if (_scrollRect != null)
    {
      _currentScroll = Mathfx.Damp(_currentScroll, _desiredScroll, 0.25f, Time.unscaledDeltaTime * 3);
      _scrollRect.ScrollPosition = _currentScroll;
    }
  }

  private void OnMenuItemActivated(MenuItemUI menuItem)
  {
    if (menuItem.MenuGroup == this)
    {
      ItemActivated?.Invoke(menuItem);
    }
  }

  private void OnMenuItemSelected(MenuItemUI menuItem)
  {
    if (menuItem.MenuGroup == this)
    {
      SelectedItem = menuItem;
      LastValidSelectedItem = menuItem;
      SelectedIndex = _menuItems.IndexOf(menuItem);

      ItemSelected?.Invoke(menuItem);

      if (EnableAutoScroll)
      {
        ScrollToSelectedItem();
      }
    }
    else
    {
      SelectedItem = null;
      SelectedIndex = -1;
    }
  }
}