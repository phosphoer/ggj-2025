using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private LevelGenerator _levelManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _levelManager.GenerateLevel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
