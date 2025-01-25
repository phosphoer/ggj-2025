using UnityEngine;

public class LevelSection : MonoBehaviour
{
    [SerializeField]
    private BoxCollider _sectionBounds;
    public BoxCollider SectionBounds => _sectionBounds;
    public float SectionHeight => _sectionBounds.size.y * _sectionBounds.transform.localScale.y;

    [SerializeField]
    private PlayerSpawnPoint[] _playerSpawns;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
