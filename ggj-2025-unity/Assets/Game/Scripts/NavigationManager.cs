using UnityEngine;

public class NavigationManager : Singleton<NavigationManager>
{
  public LayerMask TerrainMask => _terrainMask;
  public LayerMask SolidMask => _solidMask;

  [SerializeField] private LayerMask _terrainMask = default;

  [SerializeField] private LayerMask _solidMask = default;

  public Vector3 ClampDestinationToTerrain(Vector3 fromPos, Vector3 destination, float minTerrainDist)
  {
    Vector3 toDest = destination - fromPos;
    if (Physics.Raycast(fromPos, toDest, out RaycastHit hitInfo, toDest.magnitude, _terrainMask))
    {
      Vector3 clampedPos = hitInfo.point - toDest.normalized * minTerrainDist;
      return clampedPos;
    }

    return destination;
  }

  public Vector3 SnapPosToTerrain(Vector3 worldPos, Vector3 worldUp, float maxDist = Mathf.Infinity)
  {
    return SnapPosToRaycast(worldPos, worldUp, _terrainMask, maxDist);
  }

  public Vector3 SnapPosToRaycast(Vector3 worldPos, Vector3 worldUp, LayerMask layerMask, float maxDist = Mathf.Infinity)
  {
    if (SnapPosToRaycast(worldPos, worldUp, layerMask, out RaycastHit hitInfo, maxDist))
    {
      return hitInfo.point;
    }

    return worldPos;
  }

  public bool SnapPosToRaycast(Vector3 worldPos, Vector3 worldUp, LayerMask layerMask, out RaycastHit hitInfo, float maxDist = Mathf.Infinity)
  {
    bool backFaceState = Physics.queriesHitBackfaces;
    Physics.queriesHitBackfaces = true;

    bool didHit = false;
    if (Physics.Raycast(worldPos + worldUp, -worldUp, out RaycastHit downHitInfo, maxDist, layerMask))
    {
      hitInfo = downHitInfo;
      didHit = true;
    }
    else if (Physics.Raycast(worldPos - worldUp, worldUp, out RaycastHit upHitInfo, maxDist, layerMask))
    {
      hitInfo = upHitInfo;
      didHit = true;
    }
    else
    {
      hitInfo = default;
    }

    Physics.queriesHitBackfaces = backFaceState;
    return didHit;
  }

  private void Awake()
  {
    Instance = this;
  }
}