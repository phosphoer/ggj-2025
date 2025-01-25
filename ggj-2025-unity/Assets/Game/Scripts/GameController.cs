using Rewired;
using System.Collections.Generic;
using UnityEngine;

public class GameController : Singleton<GameController>
{
  [SerializeField]
  private LevelGenerator _levelManager;
  public LevelGenerator LevelManager => _levelManager;

  [SerializeField]
  private LevelCameraController _cameraController;

  [SerializeField]
  private PlayerActorController _playerPrefab;

  [SerializeField]
  private LavaController _lavaController;

  [SerializeField]
  private int _desiredPlayerCount = 1;

  public int WinningPlayerID { get; set; } = -1;

  private List<PlayerActorController> _spawnedPlayers = new List<PlayerActorController>();
  public List<PlayerActorController> SpawnedPlayers => _spawnedPlayers;

  public enum eGameState
  {
    None,
    Intro,
    SingleplayerGame,
    MultiplayerGame,
    PostGame
  }
  private eGameState _currentGameState = eGameState.None;
  public eGameState GameState => _currentGameState;

  [SerializeField]
  private eGameState _initialGameState = eGameState.MultiplayerGame;

  void Start()
  {
    GameController.Instance = this;
    _lavaController.gameObject.SetActive(false);

    SetGameState(_initialGameState);
  }

  public void SetGameState(eGameState newState)
  {
    if (newState != _currentGameState)
    {
      OnExitState(_currentGameState);
      OnEnterState(newState);

      _currentGameState = newState;
    }
  }

  void OnEnterState(eGameState newState)
  {
    switch (newState)
    {
    case eGameState.Intro:
      ShowUI<MainMenuUI>();
      break;
    case eGameState.MultiplayerGame:
      SpawnLevel(_desiredPlayerCount);
      break;
    case eGameState.SingleplayerGame:
      SpawnLevel(1);
      break;
    case eGameState.PostGame:
      ShowUI<PostGameUI>();
      break;
    }
  }

  void OnExitState(eGameState oldState)
  {
    switch (oldState)
    {
    case eGameState.Intro:
      HideUI<MainMenuUI>();
      break;
    case eGameState.MultiplayerGame:
      break;
    case eGameState.SingleplayerGame:
      break;
    case eGameState.PostGame:
      ClearLevel();
      HideUI<PostGameUI>();
      break;
    }
  }

  private void OnDestroy()
  {
    GameController.Instance = null;
  }

  public void ShowUI<T>() where T : UIPageBase
  {
    PlayerUI playerUI = PlayerUI.Instance;
    if (playerUI != null)
    {
      var uiPage = playerUI.GetPage<T>();
      if (uiPage != null)
      {
        uiPage.Show();
      }
    }
  }

  public void HideUI<T>() where T : UIPageBase
  {
    PlayerUI playerUI = PlayerUI.Instance;
    if (playerUI != null)
    {
      var uiPage = playerUI.GetPage<T>();
      if (uiPage != null)
      {
        uiPage.Hide();
      }
    }
  }

  void SpawnLevel(int playerCount)
  {
    // Use the rising game camera
    if (MainCamera.Instance != null)
    {
      MainCamera.Instance.CameraStack.PushController(_cameraController);
    }

    // No winning player ID
    WinningPlayerID = -1;


    // Enable the lava plane
    _lavaController.gameObject.SetActive(true);

    // Spawn the level sections
    _levelManager.GenerateLevel(false);

    // Spawn the desired number of players
    SpawnPlayers(playerCount);
  }

  void SpawnPlayers(int playerCount)
  {
    for (int playerIndex = 0; playerIndex < playerCount; playerIndex++)
    {
      SpawnPlayer(playerIndex);
    }
  }

  void DespawnPlayers()
  {
    foreach (PlayerActorController player in _spawnedPlayers)
    {
      Destroy(player.gameObject);
    }
    _spawnedPlayers.Clear();
  }

  void SpawnPlayer(int playerIndex)
  {
    if (_playerPrefab != null)
    {
      Transform spawnTransform = _levelManager.PickSpawnPoint();

      if (spawnTransform != null)
      {
        var playerGO = Instantiate(_playerPrefab.gameObject, spawnTransform.position, spawnTransform.rotation);
        var playerController = playerGO.GetComponent<PlayerActorController>();
        playerController.SetPlayerIndex(playerIndex);

        playerController.OnPlayerKilled += OnPlayerKilled;
        playerController.OnPlayerSectionChanged += OnPlayerSectionChanged;
        _spawnedPlayers.Add(playerController);
      }
    }
  }

  private void OnPlayerKilled(PlayerActorController playerController)
  {
    playerController.OnPlayerKilled -= OnPlayerKilled;
    playerController.OnPlayerSectionChanged -= OnPlayerSectionChanged;
    _spawnedPlayers.Remove(playerController);

    if (_currentGameState == eGameState.SingleplayerGame) 
    {
      OnLastPlayerKilled();
    }
    else if (_currentGameState == eGameState.MultiplayerGame)
    {
      if (_spawnedPlayers.Count == 1)
      {
        WinningPlayerID= _spawnedPlayers[0].PlayerIndex;
        OnLastPlayerKilled();
      }
    }
  }

  private void OnPlayerSectionChanged(int newSectionIndex, int oldSectionIndex)
  {
    if (newSectionIndex >= 1)
    {
      _lavaController.StartRising();
      _cameraController.StartRising();
    }
  }

  private void OnLastPlayerKilled()
  {
    SetGameState(eGameState.PostGame);
  }

  void ClearLevel()
  {
    _lavaController.Reset();
    _lavaController.gameObject.SetActive(false);
    _cameraController.Reset();
    _levelManager.DestroyLevel(false);
    MainCamera.Instance.CameraStack.PopController(_cameraController);
  }
}