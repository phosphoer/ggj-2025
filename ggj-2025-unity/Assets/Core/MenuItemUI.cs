using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class MenuItemUI : MonoBehaviour,
  IPointerEnterHandler,
  IPointerExitHandler,
  IPointerMoveHandler
{
  public static IReadOnlyList<MenuItemUI> Instances => _allInstances;
  public static IReadOnlyList<MenuItemUI> NavigableInstances => _navigableInstances;

  public static event System.Action<MenuItemUI> ItemAdded;
  public static event System.Action<MenuItemUI> ItemRemoved;
  public static event System.Action<MenuItemUI> PointerEnter;
  public static event System.Action<MenuItemUI> PointerExit;

  public event System.Action Activated;

  public string LabelText
  {
    get => _labelText.text;
    set => _labelText.text = value;
  }

  public bool IsNavigable
  {
    get => _isNavigable;
    set
    {
      _isNavigable = value;
      EnsureNavigable();
    }
  }

  public bool IsInSelectionScope
  {
    get => _isInSelectionScope;
    set
    {
      _isInSelectionScope = value;
      EnsureNavigable();
    }
  }

  public MenuItemGroup MenuGroup { get; set; }

  public TMPro.TMP_Text LabelTextMesh => _labelText;
  public Selectable Selectable => _selectable;
  public bool IsHighlighted => _isHighlighted;
  public bool IsDisabled => _isDisabled;

  public object UserData;
  public float HoldToFillTime = 1;
  public bool HoldToActivate = false;

  public SoundBank SfxHighlight;
  public SoundBank SfxActivated;

  [SerializeField, Tooltip("Optional")] private Selectable _selectable = null;
  [SerializeField] private bool _isNavigable = true;
  [SerializeField] private GameObject _highlightVisual = null;
  [SerializeField] private GameObject _selectedVisual = null;
  [SerializeField] private TMPro.TMP_Text _labelText = null;
  [SerializeField] private Transform _fillRoot = null;

  private bool _isHighlighted;
  private bool _isDisabled;
  private bool _isAddedToNavigableList;
  private bool _isInSelectionScope = true;

  private static List<MenuItemUI> _navigableInstances = new();
  private static List<MenuItemUI> _allInstances = new();

  public void Activate()
  {
    Activated?.Invoke();

    if (SfxActivated)
      AudioManager.Instance.PlaySound(SfxActivated);
  }

  public void SetFillAmount(float fillT)
  {
    _fillRoot.localScale = _fillRoot.localScale.WithX(fillT);
    _fillRoot.gameObject.SetActive(fillT > 0);
  }

  public void SetHighlighted(bool isHighlighted)
  {
    if (!_isHighlighted && SfxHighlight)
      AudioManager.Instance.PlaySound(SfxHighlight);

    _isHighlighted = isHighlighted;
    if (_highlightVisual != null)
      _highlightVisual.SetActive(_isHighlighted);
  }

  public void SetSelected(bool isSelected)
  {
    if (_selectedVisual != null)
      _selectedVisual.SetActive(isSelected);
  }

  public void SetDisabled(bool isDisabled)
  {
    _isDisabled = isDisabled;
    if (_labelText != null)
    {
      _labelText.color = _isDisabled ? _labelText.color.WithA(0.25f) : _labelText.color.WithA(1);
    }
  }

  private void Awake()
  {
    if (_selectable == null)
    {
      _selectable = gameObject.AddComponent<Selectable>();
      _selectable.navigation = Navigation.defaultNavigation;

      var colors = _selectable.colors;
      colors.disabledColor = Color.white;
      colors.highlightedColor = Color.white;
      colors.normalColor = Color.white;
      colors.pressedColor = Color.white;
      colors.selectedColor = Color.white;
      colors.colorMultiplier = 1;
      _selectable.colors = colors;
    }

    SetHighlighted(false);

    if (_fillRoot != null)
      _fillRoot.gameObject.SetActive(false);
  }

  private void OnEnable()
  {
    EnsureNavigable();
    _allInstances.Add(this);
    ItemAdded?.Invoke(this);
  }

  private void OnDisable()
  {
    _isAddedToNavigableList = false;
    _navigableInstances.Remove(this);
    _allInstances.Remove(this);
    ItemRemoved?.Invoke(this);
  }

  private void EnsureNavigable()
  {
    Selectable.enabled = _isNavigable && _isInSelectionScope;

    if (Selectable.enabled && !_isAddedToNavigableList)
      _navigableInstances.Add(this);
    else if (!Selectable.enabled && _isAddedToNavigableList)
      _navigableInstances.Remove(this);

    _isAddedToNavigableList = Selectable.enabled;
  }

  void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
  {
    PointerEnter?.Invoke(this);
  }

  void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
  {
    PointerExit?.Invoke(this);
  }

  void IPointerMoveHandler.OnPointerMove(PointerEventData eventData)
  {
    if (!_isHighlighted)
      PointerEnter?.Invoke(this);
  }
}