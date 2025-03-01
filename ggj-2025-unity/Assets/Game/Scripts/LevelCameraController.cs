using UnityEngine;

public class LevelCameraController : CameraControllerDynamic
{
  [SerializeField]
  private float _riseRate = 0.01f;
  public float RiseRate
  {
    get
    {
      return _riseRate;
    }
    set
    {
      _riseRate = value;
    }
  }

  private Vector3 _initialPosition = Vector3.zero;

  public enum CameraState
  {
    Idle,
    Rising
  }
  private CameraState _cameraState = CameraState.Idle;

  public void Awake()
  {
    _initialPosition = MountPoint.position;
  }

  public void Reset()
  {
    StopRising();
    MountPoint.position = _initialPosition;
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