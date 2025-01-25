using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : UIPageBase
{
  [SerializeField]
  private Button _buttonNewSinglePlayerGame = null;

  [SerializeField]
  private Button _buttonNewMultiPlayerGame = null;

  [SerializeField]
  private Button _buttonQuit = null;

  protected override void Awake()
  {
    base.Awake();
    _buttonNewSinglePlayerGame.onClick.AddListener(OnNewGameSinglePlayerGameClicked);
    _buttonNewMultiPlayerGame.onClick.AddListener(OnNewGameMultiPlayerGameClicked);
    _buttonNewMultiPlayerGame.onClick.AddListener(OnQuitGameClicked);
  }

  public void OnNewGameSinglePlayerGameClicked()
  {
    GameController.Instance.SetGameState(GameController.eGameState.SingleplayerGame);
  }

  public void OnNewGameMultiPlayerGameClicked()
  {
    GameController.Instance.SetGameState(GameController.eGameState.MultiplayerGame);
  }

  public void OnQuitGameClicked()
  {
    //If we are running in a standalone build of the game
#if UNITY_STANDALONE
    //Quit the application
    Application.Quit();
#endif

    //If we are running in the editor
#if UNITY_EDITOR
    //Stop playing the scene
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }
}
