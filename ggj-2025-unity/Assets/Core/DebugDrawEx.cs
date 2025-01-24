using UnityEngine;

public class GizmosEx
{
  public static void DrawCircle(Vector3 center, Vector3 normal, float radius)
  {
    Quaternion circleLookRot = Quaternion.LookRotation(normal);
    Vector3 radiusDir = circleLookRot * Vector3.up;
    const int kVertexCount = 16;
    for (int i = 1; i <= kVertexCount; ++i)
    {
      float t1 = (i - 1) / (float)kVertexCount;
      float t2 = i / (float)kVertexCount;
      float angleA = t1 * 360;
      float angleB = t2 * 360;
      Vector3 posA = center + Quaternion.AngleAxis(angleA, normal) * radiusDir * radius;
      Vector3 posB = center + Quaternion.AngleAxis(angleB, normal) * radiusDir * radius;
      Gizmos.DrawLine(posA, posB);
    }
  }

  public static void DrawCircle(Matrix4x4 transform, float radius)
  {
    Vector3 radiusDir = Vector3.forward;
    const int kVertexCount = 16;
    for (int i = 1; i <= kVertexCount; ++i)
    {
      float t1 = (i - 1) / (float)kVertexCount;
      float t2 = i / (float)kVertexCount;
      float angleA = t1 * 360;
      float angleB = t2 * 360;
      Vector3 posA = transform * (Quaternion.Euler(0, angleA, 0) * radiusDir * radius).WithW(1);
      Vector3 posB = transform * (Quaternion.Euler(0, angleB, 0) * radiusDir * radius).WithW(1);
      Gizmos.DrawLine(posA, posB);
    }
  }
}

public class DebugDrawEx
{
  public static void DrawCircle(Vector3 center, Vector3 normal, float radius, Color color)
  {
    Quaternion circleLookRot = Quaternion.LookRotation(normal);
    Vector3 radiusDir = circleLookRot * Vector3.up;
    const int kVertexCount = 16;
    for (int i = 1; i <= kVertexCount; ++i)
    {
      float t1 = (i - 1) / (float)kVertexCount;
      float t2 = i / (float)kVertexCount;
      float angleA = t1 * 360;
      float angleB = t2 * 360;
      Vector3 posA = center + Quaternion.AngleAxis(angleA, normal) * radiusDir * radius;
      Vector3 posB = center + Quaternion.AngleAxis(angleB, normal) * radiusDir * radius;
      Debug.DrawLine(posA, posB, color);
    }
  }

  public static void DrawCircle(Matrix4x4 transform, float radius, Color color)
  {
    Vector3 radiusDir = Vector3.forward;
    const int kVertexCount = 16;
    for (int i = 1; i <= kVertexCount; ++i)
    {
      float t1 = (i - 1) / (float)kVertexCount;
      float t2 = i / (float)kVertexCount;
      float angleA = t1 * 360;
      float angleB = t2 * 360;
      Vector3 posA = transform * (Quaternion.Euler(0, angleA, 0) * radiusDir * radius).WithW(1);
      Vector3 posB = transform * (Quaternion.Euler(0, angleB, 0) * radiusDir * radius).WithW(1);
      Debug.DrawLine(posA, posB, color);
    }
  }
}