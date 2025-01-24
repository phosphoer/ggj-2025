using UnityEngine;
using System.Collections.Generic;

public class BiomeVolume : MonoBehaviour
{
  public enum VolumeType
  {
    Global,
    Sphere,
    Bounds,
    Box
  }

  public static IReadOnlyList<BiomeVolume> Instances => _instances;

  public float Radius => Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
  public Bounds Bounds => new Bounds(transform.position, transform.localScale);

  public BiomeDefinition Biome;
  public VolumeType Type;
  public int Priority;

  private static List<BiomeVolume> _instances = new();

  public bool ContainsPoint(Vector3 worldPos)
  {
    if (Type == VolumeType.Global)
      return true;
    else if (Type == VolumeType.Sphere)
      return Vector3.Distance(worldPos, transform.position) < Radius;
    else if (Type == VolumeType.Bounds)
      return Bounds.Contains(worldPos);
    else if (Type == VolumeType.Box)
    {
      Vector3 localPos = transform.InverseTransformPoint(worldPos);
      return new Bounds(Vector3.zero, Vector3.one).Contains(localPos);
    }

    // This should never be hit
    return false;
  }

  private void OnEnable()
  {
    _instances.Add(this);
  }

  private void OnDisable()
  {
    _instances.Remove(this);
  }

  private void OnDrawGizmos()
  {
    if (Biome != null)
      Gizmos.color = Biome.SkyboxColors.SkyColor;

    if (Type == VolumeType.Bounds)
      Gizmos.DrawWireCube(transform.position, transform.localScale);
    else if (Type == VolumeType.Sphere)
      Gizmos.DrawWireSphere(transform.position, Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z));
    else if (Type == VolumeType.Box)
    {
      Gizmos.matrix = transform.localToWorldMatrix;
      Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
      Gizmos.matrix = Matrix4x4.identity;
    }
  }
}