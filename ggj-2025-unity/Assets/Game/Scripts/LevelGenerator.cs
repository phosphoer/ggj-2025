using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField]
    private LevelSectionDatabase _levelSectionDatabase;

	[SerializeField]
    private int _numLevels;

    private LevelSection[] _levelSections;
    private Vector3 _nextSectionOrigin;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateLevel()
    {
        _nextSectionOrigin= Vector3.zero;
        _levelSections = new LevelSection[_numLevels];

        // Spawn the starting section first
        _levelSections[0] = SpawnNextSection(_levelSectionDatabase.StartLevelSections);

        // Then spawn the rest of the sections
        for (int levelIndex= 1; levelIndex < _numLevels; ++levelIndex)
        {
            _levelSections[levelIndex] = SpawnNextSection(_levelSectionDatabase.LevelSections);
		}
	}

    private GameObject SelectNextSectionTemplate(LevelSection[] potentialLevelSections)
    {
		int availableSectionCount = potentialLevelSections.Length;
		int randomSectionDBIndex= Random.Range(0, availableSectionCount);

        return _levelSectionDatabase.LevelSections[randomSectionDBIndex].gameObject;
    }

    private LevelSection SpawnNextSection(LevelSection[] potentialLevelSections)
    {
		var sectionTemplate = SelectNextSectionTemplate(potentialLevelSections);
		var newSectionGO = GameObject.Instantiate(sectionTemplate);
		var newLevelSection = newSectionGO.GetComponent<LevelSection>();

		newSectionGO.transform.parent = this.transform;
        newSectionGO.transform.localPosition = _nextSectionOrigin;

        _nextSectionOrigin.y+= newLevelSection.SectionHeight;

		return newLevelSection;
	}
}
