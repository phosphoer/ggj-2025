using Rewired;
using Rewired.Components;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.FilePathAttribute;
using UnityEngine.UIElements;

public class GameController : Singleton<GameController>
{
  [SerializeField] private LevelGenerator _levelManager;
  public LevelGenerator LevelManager => _levelManager;

  public SoundBank MusicTitle;
  public SoundBank MusicGame;
  public SoundBank MusicEnd;

  [SerializeField] private LevelCameraController _cameraController;
  [SerializeField] private PlayerActorController _playerPrefab;
  [SerializeField] private WormActorController _wormPrefab;

  [SerializeField] private LavaController _lavaController;
  public LavaController LavaController => _lavaController;

  public int WinningPlayerID { get; set; } = -1;

  private bool _isMatchStarted;
  private bool _isSpawningAllowed;
  private List<PlayerActorController> _spawnedPlayers = new List<PlayerActorController>();
  public List<PlayerActorController> SpawnedPlayers => _spawnedPlayers;
  private List<WormActorController> _spawnedWorms = new List<WormActorController>();
  public List<WormActorController> SpawnedWorms => _spawnedWorms;

  public enum eGameState
  {
    None,
    Intro,
    Game,
    PostGame
  }

  private eGameState _currentGameState = eGameState.None;
  public eGameState GameState => _currentGameState;

  [SerializeField] private eGameState _initialGameState = eGameState.Game;

  public bool IsPlayerJoined(int playerId)
  {
    for (int i = 0; i < _spawnedPlayers.Count; ++i)
    {
      if (_spawnedPlayers[i].PlayerInput.id == playerId)
        return true;
    }

    return false;
  }

  void Start()
  {
    GameController.Instance = this;
    _lavaController.gameObject.SetActive(false);

    Application.targetFrameRate = 60;

    SetGameState(_initialGameState);
  }

  private void Update()
  {
#if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
    {
      _lavaController.RiseRate = _lavaController.RiseRate + 0.1f;
      _cameraController.RiseRate = _cameraController.RiseRate + 0.1f;
    }

    if (Input.GetKeyDown(KeyCode.Minus))
    {
      _lavaController.RiseRate = Mathf.Max(_lavaController.RiseRate - 0.1f, 0.0f);
      _cameraController.RiseRate = Mathf.Max(_cameraController.RiseRate - 0.1f, 0.0f);
    }
#endif

    // Iterate over existing rewired players and spawn their character if they press a button
    if (_isSpawningAllowed && !MenuFocus.AnyFocusTaken)
    {
      for (int i = 0; i < Rewired.ReInput.players.playerCount; ++i)
      {
        Rewired.Player player = Rewired.ReInput.players.GetPlayer(i);
        if (!IsPlayerJoined(i) && player.GetAnyButtonDown())
        {
          SpawnPlayerAtSpawnPoint(player.id);
        }
      }
    }
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
        AudioManager.Instance.PlaySound(MusicTitle);
        break;
      case eGameState.Game:
        SpawnLevel();
        AudioManager.Instance.PlaySound(MusicGame);
        break;
      case eGameState.PostGame:
        ShowUI<PostGameUI>();
        AudioManager.Instance.PlaySound(MusicEnd);
        break;
    }
  }

  void OnExitState(eGameState oldState)
  {
    switch (oldState)
    {
      case eGameState.Intro:
        HideUI<MainMenuUI>();
        AudioManager.Instance.StopSound(MusicTitle);
        break;
      case eGameState.Game:
        _lavaController.StopRising();
        _cameraController.StopRising();
        AudioManager.Instance.StopSound(MusicGame);
        break;
      case eGameState.PostGame:
        ClearLevel();
        HideUI<PostGameUI>();
        AudioManager.Instance.StopSound(MusicEnd);
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

  void SpawnLevel()
  {
    _isMatchStarted = false;
    _isSpawningAllowed = true;

    // Use the rising game camera
    if (MainCamera.Instance != null)
    {
      MainCamera.Instance.CameraStack.PushController(_cameraController);
    }

    // Enable the lava plane
    _lavaController.gameObject.SetActive(true);

    // Spawn the level sections
    _levelManager.GenerateLevel(false);
  }

  void DespawnPlayers()
  {
    foreach (PlayerActorController player in _spawnedPlayers)
    {
      Destroy(player.gameObject);
    }

    _spawnedPlayers.Clear();
  }

  void DespawnWorms()
  {
    foreach (WormActorController worm in _spawnedWorms)
    {
      Destroy(worm.gameObject);
    }

    _spawnedWorms.Clear();
  }

  void SpawnPlayerAtSpawnPoint(int playerIndex)
  {
    if (_playerPrefab != null)
    {
      Transform spawnTransform = _levelManager.PickSpawnPoint();

      if (spawnTransform != null)
      {
        SpawnPlayerAtLocation(playerIndex, spawnTransform.position, spawnTransform.rotation);
      }
    }
  }

  void SpawnPlayerAtLocation(int playerIndex, Vector3 position, Quaternion rotation)
  {
    var playerGO = Instantiate(_playerPrefab.gameObject, position, rotation);
    var playerController = playerGO.GetComponent<PlayerActorController>();
    playerController.SetPlayerIndex(playerIndex);

    playerController.OnPlayerTouchedLava += this.OnPlayerTouchedLava;
    playerController.OnPlayerSectionChanged += this.OnPlayerSectionChanged;
    _spawnedPlayers.Add(playerController);
  }

  void DespawnPlayer(PlayerActorController playerController)
  {
    playerController.OnPlayerTouchedLava -= this.OnPlayerTouchedLava;
    playerController.OnPlayerSectionChanged -= this.OnPlayerSectionChanged;
    _spawnedPlayers.Remove(playerController);
  }

  void SpawnWorm(int playerIndex, Vector3 position, Quaternion rotation)
  {
    if (_wormPrefab != null)
    {
      var initialPosition = new Vector3(position.x, _lavaController.LavaYPosition, position.z);
      var wormGO = Instantiate(_wormPrefab.gameObject, initialPosition, rotation);
      var wormController = wormGO.GetComponent<WormActorController>();
      wormController.SetPlayerIndex(playerIndex);

      wormController.OnWormTouchedPlayer += OnWormTouchedPlayer;
      wormController.OnWormTransformComplete += OnWormTransformComplete;
      _spawnedWorms.Add(wormController);
    }
  }

  void DespawnWorm(WormActorController wormController)
  {
    wormController.OnWormTouchedPlayer -= OnWormTouchedPlayer;
    _spawnedWorms.Remove(wormController);
  }

  private void OnWormTouchedPlayer(WormActorController worm, PlayerActorController player)
  {
    // Launch the player off into the background
    player.ReceiveLaunch();

    // Start transforming the worm back into a player
    worm.StartPlayerTransformation();
  }

  private void OnWormTransformComplete(WormActorController wormController)
  {
    int playerIndex = wormController.PlayerIndex;
    Vector3 playerPosition = wormController.transform.position;
    Quaternion playerRotation = wormController.transform.rotation;

    // Despawn the worm
    DespawnWorm(wormController);

    // Spawn the player back at the location
    SpawnPlayerAtLocation(playerIndex, playerPosition, playerRotation);
  }

  private void OnPlayerTouchedLava(PlayerActorController playerController)
  {
    int playerIndex = playerController.PlayerIndex;
    Vector3 playerPosition = playerController.transform.position;
    Quaternion playerRotation = playerController.transform.rotation;

    DespawnPlayer(playerController);
    SpawnWorm(playerIndex, playerPosition, playerRotation);

    if (_currentGameState == eGameState.Game)
    {
      if (_spawnedPlayers.Count == 0 && !IsAnyWormTramsforming())
      {
        //WinningPlayerID = _spawnedPlayers[0].PlayerIndex;
        OnLastPlayerKilled();
      }
    }
  }

  private bool IsAnyWormTramsforming()
  {
    foreach (var worm in _spawnedWorms)
    {
      if (worm.IsTransforming)
      {
        return true;
      }
    }

    return false;
  }

  private void OnPlayerSectionChanged(int newSectionIndex, int oldSectionIndex)
  {
    if (newSectionIndex >= 1)
    {
      _isMatchStarted = true;
      _isSpawningAllowed = false;
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
    DespawnPlayers();
    DespawnWorms();
    _lavaController.Reset();
    _lavaController.gameObject.SetActive(false);
    _cameraController.Reset();
    _levelManager.DestroyLevel(false);
    MainCamera.Instance.CameraStack.PopController(_cameraController);
  }
}