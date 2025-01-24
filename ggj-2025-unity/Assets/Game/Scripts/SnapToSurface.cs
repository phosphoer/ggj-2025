using UnityEngine;
using System.Collections.Generic;

public class SnapToSurface : MonoBehaviour
{
  [SerializeField]
  private List<Transform> _transforms = new();

  [SerializeField]
  private bool _addChildren = false;

  [SerializeField]
  private LayerMask _raycastMask = default;

  [SerializeField]
  private Vector3 _surfaceOffset = default;

  [SerializeField]
  [Range(0, 1)]
  private float _rotationNormalAlignment = 0;

  private void Start()
  {
    if (_addChildren)
    {
      for (int i = 0; i < transform.childCount; ++i)
      {
        _transforms.Add(transform.GetChild(i));
      }
    }

    for (int i = 0; i < _transforms.Count; ++i)
    {
      Transform t = _transforms[i];
      if (NavigationManager.Instance.SnapPosToRaycast(t.position, Vector3.up, _raycastMask, out RaycastHit hitInfo))
      {
        t.position = hitInfo.point + _surfaceOffset;
        t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(t.forward, hitInfo.normal), _rotationNormalAlignment);
      }
    }
  }
}