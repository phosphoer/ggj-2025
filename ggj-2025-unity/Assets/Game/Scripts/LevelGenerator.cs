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

        for (int levelIndex= 0; levelIndex < _numLevels; ++levelIndex)
        {
            var newLevelSection = SpawnNextSection();

            _levelSections[levelIndex]= newLevelSection;
		}
	}

    private GameObject SelectNextSectionTemplate()
    {
		int availableSectionCount = _levelSectionDatabase.LevelSections.Length;
		int randomSectionDBIndex= Random.Range(0, availableSectionCount);

        return _levelSectionDatabase.LevelSections[randomSectionDBIndex].gameObject;
    }

    private LevelSection SpawnNextSection()
    {
		var sectionTemplate = SelectNextSectionTemplate();
		var newSectionGO = GameObject.Instantiate(sectionTemplate);
		var newLevelSection = newSectionGO.GetComponent<LevelSection>();

		newSectionGO.transform.parent = this.transform;
        newSectionGO.transform.localPosition = _nextSectionOrigin;

        _nextSectionOrigin.y+= newLevelSection.SectionHeight;

		return newLevelSection;
	}
}
