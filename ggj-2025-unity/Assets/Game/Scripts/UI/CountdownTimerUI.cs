using UnityEngine;

public class CountdownTimerUI : UIPageBase
{
  [SerializeField] private TMPro.TMP_Text TitleLabel;
  [SerializeField] private TMPro.TMP_Text CountdownLabel;

  protected override void Awake()
  {
    base.Awake();

    Shown += OnShown;
  }

  protected void Update()
  {
    UpdateText();
  }

  private void OnShown()
  {    
    UpdateText();
  }

  private void UpdateText()
  {
    int intTime = (int)GameController.Instance.WinningPlayerCountdownTimer;
    int playerIndex = GameController.Instance.WinningPlayerID;
    string colorName = GameController.Instance.GetPlayerColorName(playerIndex);

    TitleLabel.text = string.Format("{0} Player victory in", colorName);
    CountdownLabel.text = string.Format("{0}", intTime);
  }
}