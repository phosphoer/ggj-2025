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

  private List<PlayerActorController> _spawnedPlayers = new List<PlayerActorController>();
  public List<PlayerActorController> SpawnedPlayers => _spawnedPlayers;

  void Start()
  {
    GameController.Instance= this;

    SpawnLevel();
  }

  private void OnDestroy()
  {
    GameController.Instance = null;
  }

  void SpawnLevel()
  {
    // Use the rising game camera
    if (MainCamera.Instance != null)
    {
      MainCamera.Instance.CameraStack.PushController(_cameraController);
    }

    // Spawn the level sections
    _levelManager.GenerateLevel(false);

    // Spawn the desired number of players
    SpawnPlayers();

    // Start animating the camera
    _cameraController.StartRising();

    // Start raising the Lava
    //_lavaController.StartRising();
  }

  void SpawnPlayers()
  {
    for (int playerIndex = 0; playerIndex < _desiredPlayerCount; playerIndex++)
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
        playerController.SetPlayerInput(playerIndex);

        playerController.OnPlayerKilled+= OnPlayerKilled;
        _spawnedPlayers.Add(playerController);
      }
    }
  }

  private void OnPlayerKilled(PlayerActorController playerController)
  {
    playerController.OnPlayerKilled-= OnPlayerKilled;
    _spawnedPlayers.Remove(playerController);

    if (_spawnedPlayers.Count == 0)
    {
      OnAllPlayersKilled();
    }
  }

  private void OnAllPlayersKilled()
  {
    _lavaController.StopRising();
    _cameraController.StopRising();
  }

  void ClearLevel()
  {
    _cameraController.StopRising();
    _levelManager.DestroyLevel(false);
    MainCamera.Instance.CameraStack.PopController(_cameraController);
  }
}