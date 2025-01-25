using System.Collections.Generic;
using UnityEngine;

public class LevelSection : MonoBehaviour
{
  [SerializeField] private BoxCollider _sectionBounds;
  public BoxCollider SectionBounds => _sectionBounds;
  public Vector3 SectionWorldCenter => transform.TransformPoint(_sectionBounds.center);
  public float SectionWidth => _sectionBounds.size.x * _sectionBounds.transform.localScale.x;
  public float SectionHeight => _sectionBounds.size.y * _sectionBounds.transform.localScale.y;
  public GameObject SectionTemplate { get; set; }

  [SerializeField] private PlayerSpawnPoint[] _playerSpawns;

  private void Awake()
  {
    _sectionBounds.enabled = false;
    _sectionBounds.isTrigger = true;
  }

  public List<PlayerSpawnPoint> GatherAvailablePlayerSpawners()
  {
    int numSpawnPoints = _playerSpawns.Length;
    var result = new List<PlayerSpawnPoint>();
    for (int spawnIndex = 0; spawnIndex < numSpawnPoints; ++spawnIndex)
    {
      result.Add(_playerSpawns[spawnIndex]);
    }

    return result;
  }
}