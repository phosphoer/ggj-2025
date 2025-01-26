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
  public LevelSection[] LevelSections => _levelSections;

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
    _levelSections[0] = SpawnNextSection(_levelSectionDatabase.StartLevelSections, 0);
    _unusedSpawnPoints = _levelSections[0].GatherAvailablePlayerSpawners();

    // Then spawn the rest of the sections
    for (int levelIndex = 1; levelIndex < _numLevels - 1; ++levelIndex)
    {
      _levelSections[levelIndex] = SpawnNextSection(_levelSectionDatabase.LevelSections, levelIndex);
    }

    if (_levelSectionDatabase.EndLevelSection != null)
    {
      int endLevelIndex = _numLevels - 1;

      _levelSections[endLevelIndex] = SpawnNextSection(new LevelSection[] {_levelSectionDatabase.EndLevelSection }, endLevelIndex);
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

  private GameObject SelectNextSectionTemplate(LevelSection[] potentialLevelSections, int currentLevelIndex)
  {
    int availableSectionCount = potentialLevelSections.Length;
    LevelSection previousSection= (currentLevelIndex > 0) ? _levelSections[currentLevelIndex - 1] : null;
    GameObject previousSectionTemplate = (previousSection != null) ? previousSection.SectionTemplate : null;

    GameObject nextSectionTemplate= null;
    for (int i = 0; i < 100 && nextSectionTemplate == null; i++)
    {
      int randomSectionDBIndex = UnityEngine.Random.Range(0, availableSectionCount);
      GameObject potentialSectionTemplate= potentialLevelSections[randomSectionDBIndex].gameObject;

      if (potentialSectionTemplate != previousSectionTemplate)
      {
        nextSectionTemplate= potentialSectionTemplate;
      }
    }

    return nextSectionTemplate;
  }

  private LevelSection SpawnNextSection(LevelSection[] potentialLevelSections, int currentLevelIndex)
  {
    var sectionTemplate = SelectNextSectionTemplate(potentialLevelSections, currentLevelIndex);
    var newSectionGO = GameObject.Instantiate(sectionTemplate);
    var newLevelSection = newSectionGO.GetComponent<LevelSection>();
    newLevelSection.SectionTemplate = sectionTemplate;

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
