using System.Collections.Generic;
using UnityEngine;

public class LevelSection : MonoBehaviour
{
    [SerializeField]
    private BoxCollider _sectionBounds;
    public BoxCollider SectionBounds => _sectionBounds;
    public float SectionHeight => _sectionBounds.size.y * _sectionBounds.transform.localScale.y;

    [SerializeField]
    private PlayerSpawnPoint[] _playerSpawns;

    public List<PlayerSpawnPoint> GatherAvailablePlayerSpawners()
    {
        int numSpawnPoints = _playerSpawns.Length;
        var result = new List<PlayerSpawnPoint>();
        for (int spawnIndex= 0; spawnIndex < numSpawnPoints; ++spawnIndex)
        {
            result.Add(_playerSpawns[spawnIndex]);
        }

        return result;
    }
}
