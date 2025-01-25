using UnityEngine;

public class LevelCameraController : CameraControllerDynamic
{
  [SerializeField]
  private float _riseRate = 0.01f;

  public enum CameraState
  {
    Idle,
    Rising
  }
  private CameraState _cameraState = CameraState.Idle;

  void Start()
  {

  }

  public void StartRising()
  {
    _cameraState = CameraState.Rising;
  }

  public void StopRising()
  {
    _cameraState = CameraState.Idle;
  }

  void Update()
  {
    if (_cameraState == CameraState.Rising)
    {
      var newMountPointLocation = MountPoint.position;
      newMountPointLocation.y += _riseRate * Time.deltaTime;

      MountPoint.position = newMountPointLocation;
    }
  }
}