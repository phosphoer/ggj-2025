using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlayerColors
{
  public Color MouthColor;
  public Color GumColor;
  public string ColorName;
}

public class GameController : Singleton<GameController>
{
  public event System.Action MatchStarted;

  public eGameState GameState => _currentGameState;
  public LevelGenerator LevelManager => _levelManager;
  public LavaController LavaController => _lavaController;
  public List<PlayerActorController> SpawnedPlayers => _spawnedPlayers;
  public List<WormActorController> SpawnedWorms => _spawnedWorms;

  public int WinningPlayerID { get; set; } = -1;
  public float WinningPlayerCountdownTimer { get; set; } = 0;

  public SoundBank MusicTitle;
  public SoundBank MusicGame;
  public SoundBank MusicEnd;
  public int WinCountDownTime = 10;
  public float StartMatchHeight = 5;
  public float SPTopSpeedupThreshold = 0.25f;

  [SerializeField] private eGameState _initialGameState = eGameState.Game;
  [SerializeField] private LevelGenerator _levelManager;
  [SerializeField] private LevelCameraController _cameraController;
  [SerializeField] private PlayerActorController _playerPrefab;
  [SerializeField] private WormActorController _wormPrefab;
  [SerializeField] private LavaController _lavaController;
  [SerializeField] private PlayerColors[] _playerColors = null;
  [SerializeField] private AnimationCurve _riseRateCurve = default;

  private bool _isMatchStarted;
  private bool _isInCountdown;
  private bool _isSpawningAllowed;
  private float _extraRiseRate;
  private List<PlayerActorController> _spawnedPlayers = new List<PlayerActorController>();
  private List<WormActorController> _spawnedWorms = new List<WormActorController>();
  private eGameState _currentGameState = eGameState.None;

  public enum eGameState
  {
    None,
    Intro,
    Game,
    PostGame
  }

  public string GetPlayerColorName(int playerID)
  {
    foreach (var player in _spawnedPlayers)
    {
      if (player.PlayerIndex == playerID)
      {
        return player.PlayerColorName;
      }
    }

    return "";
  }

  public bool IsPlayerJoined(int playerId)
  {
    for (int i = 0; i < _spawnedPlayers.Count; ++i)
    {
      if (_spawnedPlayers[i].PlayerInput.id == playerId)
        return true;
    }

    return false;
  }

  private void Awake()
  {
    Instance = this;
  }

  private void Start()
  {
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

    if (Input.GetKeyDown(KeyCode.P))
    {
      TriggerPostGame();
    }

    if (Input.GetKeyDown(KeyCode.L))
    {
      StartMatch();
    }
#endif

    bool isAddingExtraRiseRate = false;
    if (SpawnedPlayers.Count == 1)
    {
      var spawnedPlayer = SpawnedPlayers[0];
      Camera mainCamera = MainCamera.Instance.Camera;
      Vector3 topScreenPos = mainCamera.ViewportToScreenPoint(new Vector3(0.5f, 1f - SPTopSpeedupThreshold, 0));
      Vector3 cameraTopPos = mainCamera.ScreenToWorldPoint(topScreenPos.WithZ(mainCamera.transform.position.z));
      Debug.DrawRay(cameraTopPos, Vector3.right * 10);
      if (spawnedPlayer.transform.position.y > cameraTopPos.y - SPTopSpeedupThreshold)
      {
        _extraRiseRate += Time.deltaTime * 0.1f;
        isAddingExtraRiseRate = true;
      }
    }

    if (!isAddingExtraRiseRate)
      _extraRiseRate = Mathfx.Damp(_extraRiseRate, 0, 0.25f, Time.deltaTime);

    // Update rise rate difficulty
    float riseRate = _riseRateCurve.Evaluate(_cameraController.MountPoint.position.y);
    _lavaController.RiseRate = riseRate + _extraRiseRate;
    _cameraController.RiseRate = riseRate + _extraRiseRate;

    // Win count down
    if (_isInCountdown)
    {
      WinningPlayerCountdownTimer -= Time.deltaTime;
      if (WinningPlayerCountdownTimer <= 0)
      {
        _isInCountdown = false;
        TriggerPostGame();
      }
    }

    for (int i = 0; i < _spawnedPlayers.Count; ++i)
    {
      if (!_isMatchStarted)
      {
        if (_spawnedPlayers[i].transform.position.y > StartMatchHeight)
        {
          StartMatch();
        }
      }
    }

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
        HideUI<CountdownTimerUI>();
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
    playerController.SetColors(_playerColors[playerIndex].ColorName, _playerColors[playerIndex].MouthColor, _playerColors[playerIndex].GumColor);

    playerController.OnPlayerTouchedLava += this.OnPlayerTouchedLava;
    playerController.OnPlayerSectionChanged += this.OnPlayerSectionChanged;
    _spawnedPlayers.Add(playerController);
  }

  void DespawnPlayer(PlayerActorController playerController)
  {
    playerController.OnPlayerTouchedLava -= this.OnPlayerTouchedLava;
    playerController.OnPlayerSectionChanged -= this.OnPlayerSectionChanged;
    _spawnedPlayers.Remove(playerController);

    Destroy(playerController.gameObject);
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
    wormController.OnWormTransformComplete -= OnWormTransformComplete;
    _spawnedWorms.Remove(wormController);

    Destroy(wormController.gameObject);
  }

  private void OnWormTouchedPlayer(WormActorController worm, PlayerActorController player)
  {
    // Launch the player off into the background
    float playerGum = player.GumMassTotal;
    player.ReceiveLaunch();

    // Start transforming the worm back into a player
    worm.StartPlayerTransformation(playerGum);

    if (_isInCountdown)
    {
      WinningPlayerCountdownTimer = 10;
    }
  }

  private void OnWormTransformComplete(WormActorController wormController)
  {
    int playerIndex = wormController.PlayerIndex;
    Vector3 playerPosition = wormController.transform.position;
    Quaternion playerRotation = wormController.transform.rotation;

    // Despawn the worm
    float gumAmount = wormController.TransformationGumAmount;
    DespawnWorm(wormController);

    // Spawn the player back at the location
    SpawnPlayerAtLocation(playerIndex, playerPosition, playerRotation);
    _spawnedPlayers[^1].SetGumMass(gumAmount);
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
      if (_spawnedPlayers.Count <= 1 && !IsAnyWormTramsforming())
      {
        if (_spawnedPlayers.Count > 0)
        {
          WinningPlayerID = _spawnedPlayers[0].PlayerIndex;
          TriggerCountDown();
        }
        else
        {
          TriggerPostGame();
        }
      }
    }
  }

  private void TriggerCountDown()
  {
    _isInCountdown = true;
    WinningPlayerCountdownTimer = WinCountDownTime;

    ShowUI<CountdownTimerUI>();
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

  private void StartMatch()
  {
    _isMatchStarted = true;
    _isSpawningAllowed = false;
    _lavaController.StartRising();
    _cameraController.StartRising();
    MatchStarted?.Invoke();
  }

  private void OnPlayerSectionChanged(int newSectionIndex, int oldSectionIndex)
  {
  }

  private void TriggerPostGame()
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
    _isInCountdown = false;
    MainCamera.Instance.CameraStack.PopController(_cameraController);
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.white;
    Gizmos.DrawLine(new Vector3(-100, StartMatchHeight, 0), new Vector3(100, StartMatchHeight, 0));
  }
}