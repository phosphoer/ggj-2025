
using UnityEngine;

public class PostGameUI : UIPageBase
{
  [SerializeField]
  private MenuItemUI _buttonOk = null;

  public TMPro.TMP_Text WinLabel;

  protected override void Awake()
  {
    base.Awake();
    Shown += OnShown;
    _buttonOk.Activated += OnOkClicked;
  }

  public void OnOkClicked()
  {
    GameController.Instance.SetGameState(GameController.eGameState.Intro);
  }

  private void OnShown()
  {
    int playerIndex= GameController.Instance.WinningPlayerID;

    if (playerIndex >= 0)
    {
      WinLabel.text = string.Format("Player {0} was the last player standing!", playerIndex+1);
    }
    else
    {
      WinLabel.text = string.Format("No players survived!");
    }
  }
}
