
public class PostGameUI : UIPageBase
{
  public TMPro.TMP_Text WinLabel;

  protected override void Awake()
  {
    base.Awake();
    Shown += OnShown;
  }

  private void OnShown()
  {
    //WinLabel.text = string.Format("Player {0} had the chonkiest pirate!", GameController.InstanceWinningPlayerID);
  }
}
