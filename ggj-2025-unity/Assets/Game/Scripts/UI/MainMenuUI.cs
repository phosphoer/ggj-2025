using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : UIPageBase
{
  [SerializeField]
  private MenuItemUI _buttonNewSinglePlayerGame = null;

  [SerializeField]
  private MenuItemUI _buttonNewMultiPlayerGame = null;

  [SerializeField]
  private MenuItemUI _buttonQuit = null;

  protected override void Awake()
  {
    base.Awake();
    _buttonNewSinglePlayerGame.Activated+= OnNewGameSinglePlayerGameClicked;
    _buttonNewMultiPlayerGame.Activated+= OnNewGameMultiPlayerGameClicked;
    _buttonNewMultiPlayerGame.Activated+= OnQuitGameClicked;
  }

#if UNITY_EDITOR
  void Update()
  {
    // Detect if the spacebar is pressed down
    if (Input.GetKeyDown(KeyCode.S))
    {
      OnNewGameSinglePlayerGameClicked();
    }

    // Detect if the W key is being held down
    if (Input.GetKey(KeyCode.M))
    {
      OnNewGameMultiPlayerGameClicked();
    }
  }
#endif 

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
