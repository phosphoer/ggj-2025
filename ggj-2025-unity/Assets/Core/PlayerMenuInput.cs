using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerMenuInput : Singleton<PlayerMenuInput>
{
  public static bool IsMouseInUse => Rewired.ReInput.controllers.GetLastActiveController() == Rewired.ReInput.controllers.Mouse;
  public static bool IsKeyboardInUse => Rewired.ReInput.controllers.GetLastActiveController() == Rewired.ReInput.controllers.Keyboard;

  public static event System.Action MenuLeftPressed;
  public static event System.Action MenuRightPressed;
  public static event System.Action MenuUpPressed;
  public static event System.Action MenuDownPressed;
  public static event System.Action MenuLeftReleased;
  public static event System.Action MenuRightReleased;
  public static event System.Action MenuUpReleased;
  public static event System.Action MenuDownReleased;
  public static event System.Action MenuAcceptPressed;
  public static event System.Action MenuAcceptReleased;
  public static event System.Action MenuBackPressed;

  public static Vector2 MenuAxis => _menuAxis;
  public static float MenuScrollAxis => _scrollAxis;
  public static bool MenuAcceptDown => _acceptDown;
  public static bool MenuAccept => _acceptState;

  private static float _scrollAxis;

  private static Vector2 _menuAxis;

  private static bool _acceptDown;
  private static bool _acceptState;
  private static AxisState[] _axisStates = new AxisState[4];
  private static System.Action[] _axisPressEvents;
  private static System.Action[] _axisReleaseEvents;

  [System.Serializable]
  private struct AxisState
  {
    public bool State;
    public float RepeatTimer;
    public int ComponentIndex;
    public int AxisSign;
  }

  private enum AxisDirection
  {
    Left,
    Right,
    Up,
    Down,
  }

  private void Awake()
  {
    Instance = this;

    _axisStates[(int)AxisDirection.Left].ComponentIndex = 0;
    _axisStates[(int)AxisDirection.Left].AxisSign = -1;
    _axisStates[(int)AxisDirection.Right].ComponentIndex = 0;
    _axisStates[(int)AxisDirection.Right].AxisSign = 1;
    _axisStates[(int)AxisDirection.Up].ComponentIndex = 1;
    _axisStates[(int)AxisDirection.Up].AxisSign = 1;
    _axisStates[(int)AxisDirection.Down].ComponentIndex = 1;
    _axisStates[(int)AxisDirection.Down].AxisSign = -1;

    _axisPressEvents = new System.Action[]
    {
      () => MenuLeftPressed?.Invoke(),
      () => MenuRightPressed?.Invoke(),
      () => MenuUpPressed?.Invoke(),
      () => MenuDownPressed?.Invoke(),
    };

    _axisReleaseEvents = new System.Action[]
    {
      () => MenuLeftReleased?.Invoke(),
      () => MenuRightReleased?.Invoke(),
      () => MenuUpReleased?.Invoke(),
      () => MenuDownReleased?.Invoke(),
    };
  }

  private void OnEnable()
  {
    _acceptState = false;
    _acceptDown = false;
  }

  private void Update()
  {
    if (!Rewired.ReInput.isReady || !MenuFocus.AnyFocusTaken)
      return;

    for (int i = 0; i < Rewired.ReInput.players.playerCount; ++i)
    {
      Rewired.Player player = Rewired.ReInput.players.GetPlayer(i);
      EvaluateInputForPlayer(player);
    }
  }

  private void EvaluateInputForPlayer(Rewired.Player p)
  {
    // TODO: Use mapping actions when those exist
    const int actionMenuAxisX = RewiredConsts.Action.MenuAxisX;
    const int actionMenuAxisY = RewiredConsts.Action.MenuAxisY;
    const int actionMenuAccept = RewiredConsts.Action.MenuAccept;
    const int actionMenuBack = RewiredConsts.Action.MenuBack;
    const int actionMenuScroll = RewiredConsts.Action.MenuAxisScroll;

    // Gather axis input
    _scrollAxis = p.GetAxis(actionMenuScroll);
    _menuAxis = new Vector2(p.GetAxis(actionMenuAxisX), p.GetAxis(actionMenuAxisY));

    // Send menu axis events
    const float kRepeatTime = 0.25f;
    const float kAxisThreshold = 0.25f;
    for (int i = 0; i < _axisStates.Length; i++)
    {
      // Decrement repeat timer on the axis to allow holding the button to repeat axis presses
      AxisState axisState = _axisStates[i];
      axisState.RepeatTimer -= Time.unscaledDeltaTime;

      // Get the relevant axis component (x or y) and compare it with the threshold to find the state of the axis
      float menuAxisComponent = _menuAxis[axisState.ComponentIndex];
      bool state = axisState.AxisSign > 0 ? menuAxisComponent > kAxisThreshold : menuAxisComponent < -kAxisThreshold;

      // Allow state changes if the repeat timer has elapsed or if the axis was released
      if (axisState.RepeatTimer <= 0 || !state)
      {
        if (axisState.State)
          _axisReleaseEvents[i]?.Invoke();

        axisState.State = state;
        axisState.RepeatTimer = state ? kRepeatTime : 0;

        if (state) _axisPressEvents[i]?.Invoke();
        else _axisReleaseEvents[i]?.Invoke();
      }

      _axisStates[i] = axisState;
    }

    // Listen for menu actions and fire relevant event
    _acceptDown = false;
    if (p.GetButtonDown(actionMenuAccept))
    {
      _acceptDown = true;
      _acceptState = true;
      MenuAcceptPressed?.Invoke();
    }
    else if (p.GetButtonUp(actionMenuAccept))
    {
      _acceptState = false;
      MenuAcceptReleased?.Invoke();
    }
    else if (!p.GetButton(actionMenuAccept))
    {
      if (_acceptState)
        MenuAcceptReleased?.Invoke();

      _acceptState = false;
      _acceptDown = false;
    }

    if (p.GetButtonDown(actionMenuBack))
    {
      MenuBackPressed?.Invoke();
    }
  }
}