using UnityEngine;

[System.Serializable]
public class Volume
{
  public enum VolumeType
  {
    Global,
    Sphere,
    Bounds,
    Box
  }

  public float Radius => Mathf.Max(Transform.localScale.x, Transform.localScale.y, Transform.localScale.z);
  public Bounds Bounds => new Bounds(Transform.position, Transform.localScale);

  public VolumeType Type;
  public Transform Transform;

  public Vector3 GetPositionInVolume()
  {
    if (Type == VolumeType.Bounds)
      return Mathfx.RandomInBounds(Bounds, Quaternion.identity);
    else if (Type == VolumeType.Box)
      return Mathfx.RandomInBounds(Bounds, Transform.rotation);
    else if (Type == VolumeType.Sphere)
      return Transform.position + Random.insideUnitSphere * Radius;

    return Transform.position;
  }

  public bool ContainsPoint(Vector3 worldPos)
  {
    if (Type == VolumeType.Global)
      return true;
    else if (Type == VolumeType.Sphere)
      return Vector3.Distance(worldPos, Transform.position) < Radius;
    else if (Type == VolumeType.Bounds)
      return Bounds.Contains(worldPos);
    else if (Type == VolumeType.Box)
    {
      Vector3 localPos = Transform.InverseTransformPoint(worldPos);
      return new Bounds(Vector3.zero, Vector3.one).Contains(localPos);
    }

    // This should never be hit
    return false;
  }

  public void OnDrawGizmos(Color color)
  {
    if (Transform)
    {
      Gizmos.color = color;

      if (Type == VolumeType.Bounds)
        Gizmos.DrawWireCube(Transform.position, Transform.localScale);
      else if (Type == VolumeType.Sphere)
        Gizmos.DrawWireSphere(Transform.position, Mathf.Max(Transform.localScale.x, Transform.localScale.y, Transform.localScale.z));
      else if (Type == VolumeType.Box)
      {
        Gizmos.matrix = Transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
      }
    }
  }
}