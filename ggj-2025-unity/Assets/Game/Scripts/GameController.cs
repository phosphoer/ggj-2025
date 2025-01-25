using Rewired;
using System.Collections.Generic;
using UnityEngine;

public class GameController : Singleton<GameController>
{
  [SerializeField]
  private LevelGenerator _levelManager;

  [SerializeField]
  private LevelCameraController _cameraController;

  [SerializeField]
  private PlayerActorController _playerPrefab;

  [SerializeField]
  private int _desiredPlayerCount = 1;

  private List<PlayerActorController> _spawnedPlayers = new List<PlayerActorController>();

  void Start()
  {
    SpawnLevel();
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

        _spawnedPlayers.Add(playerController);
      }
    }
  }

  void ClearLevel()
  {
    _cameraController.StopRising();
    _levelManager.DestroyLevel(false);
    MainCamera.Instance.CameraStack.PopController(_cameraController);
  }
}