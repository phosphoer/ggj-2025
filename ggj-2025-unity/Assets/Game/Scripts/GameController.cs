using UnityEngine;

public class GameController : Singleton<GameController>
{
    [SerializeField]
    private LevelGenerator _levelManager;

    [SerializeField]
    private LevelCameraController _cameraController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MainCamera.Instance.CameraStack.PushController(_cameraController);

        _levelManager.GenerateLevel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}