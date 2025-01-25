using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
  [SerializeField]
  private LevelSectionDatabase _levelSectionDatabase;

  [SerializeField]
  private int _numLevels;

  private LevelSection[] _levelSections;
  private List<PlayerSpawnPoint> _unusedSpawnPoints;
  private Vector3 _nextSectionOrigin;

  public void DestroyLevel(bool inEditor)
  {
    if (_levelSections != null)
    {
      foreach (var level in _levelSections)
      {
        if (level != null)
        {
          if (inEditor)
          {
            DestroyImmediate(level.gameObject);
          }
          else
          {
            Destroy(level.gameObject);
          }
        }
      }
    }

    _levelSections = new LevelSection[0];
  }

  public void GenerateLevel(bool inEditor)
  {
    DestroyLevel(inEditor);

    _nextSectionOrigin = Vector3.zero;
    _levelSections = new LevelSection[_numLevels];

    // Spawn the starting section first
    _levelSections[0] = SpawnNextSection(_levelSectionDatabase.StartLevelSections);
    _unusedSpawnPoints = _levelSections[0].GatherAvailablePlayerSpawners();

    // Then spawn the rest of the sections
    for (int levelIndex = 1; levelIndex < _numLevels; ++levelIndex)
    {
      _levelSections[levelIndex] = SpawnNextSection(_levelSectionDatabase.LevelSections);
    }
  }

  public Transform PickSpawnPoint()
  {
    if (_unusedSpawnPoints != null && _unusedSpawnPoints.Count > 0)
    {
      int randIndex = UnityEngine.Random.Range(0, _unusedSpawnPoints.Count);
      Transform result = _unusedSpawnPoints[randIndex].transform;

      _unusedSpawnPoints.RemoveAt(randIndex);
      return result;
    }

    return null;
  }

  private GameObject SelectNextSectionTemplate(LevelSection[] potentialLevelSections)
  {
    int availableSectionCount = potentialLevelSections.Length;
    int randomSectionDBIndex = UnityEngine.Random.Range(0, availableSectionCount);

    return potentialLevelSections[randomSectionDBIndex].gameObject;
  }

  private LevelSection SpawnNextSection(LevelSection[] potentialLevelSections)
  {
    var sectionTemplate = SelectNextSectionTemplate(potentialLevelSections);
    var newSectionGO = GameObject.Instantiate(sectionTemplate);
    var newLevelSection = newSectionGO.GetComponent<LevelSection>();

    newSectionGO.transform.parent = this.transform;
    newSectionGO.transform.localPosition = _nextSectionOrigin;

    _nextSectionOrigin.y += newLevelSection.SectionHeight;

    return newLevelSection;
  }

  [ContextMenu("Regenerate Layout")]
  private void EditorGenerateLayout()
  {
    GenerateLevel(true);
  }
}
