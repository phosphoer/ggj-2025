using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class PipeGenerator : MonoBehaviour
{
  public Mesh StartMesh = null;
  public Mesh EndMesh = null;
  public Mesh[] SectionMeshes = null;
  public Material Material = null;
  public LayerMask GroundRaycastMask = default;
  public bool GenerateCollider = true;
  public float GroundHeightOffset = 0;

  private bool _isDirty;
  private Mesh _generatedMesh;
  private Vector3 _lastGenPosition;
  private Vector3 _lastGenScale;
  private Quaternion _lastGenRotation;
  private List<CombineInstance> _combineInstances = new();

  private void Awake()
  {
    RebuildMesh();
  }

  private void OnDestroy()
  {
    if (_generatedMesh != null)
      MeshPool.FreeMesh(_generatedMesh);

    _generatedMesh = null;
  }


#if UNITY_EDITOR
  private void Update()
  {
    if (!Application.isPlaying)
    {
      Transform spawnTransform = transform;

      _isDirty |= _lastGenPosition != spawnTransform.position;
      _isDirty |= _lastGenScale != spawnTransform.localScale;
      _isDirty |= _lastGenRotation != spawnTransform.rotation;

      if (_isDirty)
        RebuildMesh();
    }
  }

  private void OnValidate()
  {
    if (!Application.isPlaying)
    {
      _isDirty = true;
    }
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.white;
    for (int i = 0; i < transform.childCount - 1; ++i)
    {
      Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
    }

    for (int i = 0; i < transform.childCount; ++i)
    {
      Gizmos.DrawSphere(transform.GetChild(i).position, 1);
    }
  }
#endif

  private void RebuildMesh()
  {
    if (SectionMeshes == null || SectionMeshes.Length == 0)
      return;

    if (_generatedMesh == null)
    {
      _generatedMesh = MeshPool.GetMesh();
      _generatedMesh.hideFlags = HideFlags.DontSave;
    }

    _lastGenPosition = transform.position;
    _lastGenScale = transform.localScale;
    _lastGenRotation = transform.rotation;

    _combineInstances.Clear();
    for (int i = 0; i < transform.childCount - 1; ++i)
    {
      Mesh selectedMesh = SectionMeshes[SectionMeshes.WrapIndex(i)];
      if (i == 0 && StartMesh != null)
        selectedMesh = StartMesh;
      else if (i == transform.childCount - 2 && EndMesh != null)
        selectedMesh = EndMesh;

      CombineInstance meshInstance = default;
      meshInstance.mesh = Instantiate(selectedMesh);
      meshInstance.transform = Matrix4x4.identity;
      meshInstance.subMeshIndex = 0;
      _combineInstances.Add(meshInstance);
    }

    bool isCombineValid = true;
    for (int i = 1; i < transform.childCount; ++i)
    {
      Transform childA = transform.GetChild(i - 1);
      Transform childB = transform.GetChild(i);
      Matrix4x4 childAMatrix = transform.worldToLocalMatrix * childA.localToWorldMatrix;
      Matrix4x4 childBMatrix = transform.worldToLocalMatrix * childB.localToWorldMatrix;
      CombineInstance meshInstance = _combineInstances[i - 1];
      Mesh sectionMesh = meshInstance.mesh;
      if (sectionMesh != null)
      {
        Bounds sectionBounds = sectionMesh.bounds;
        Vector3[] vertices = sectionMesh.vertices;
        Color[] colors = sectionMesh.colors;
        for (int j = 0; j < vertices.Length; ++j)
        {
          float sectionT = colors.IsIndexValid(j) ? Mathf.Clamp01(colors[j].r) : 0;
          float groundT = colors.IsIndexValid(j) ? Mathf.Clamp01(colors[j].g) : 0;
          Physics.Raycast(childA.position, Vector3.down, out RaycastHit groundHitInfoA, Mathf.Infinity, GroundRaycastMask);
          Physics.Raycast(childB.position, Vector3.down, out RaycastHit groundHitInfoB, Mathf.Infinity, GroundRaycastMask);
          Vector3 groundOffsetA = Vector3.down * (groundHitInfoA.distance * groundT / childA.lossyScale.y + GroundHeightOffset * groundT);
          Vector3 groundOffsetB = Vector3.down * (groundHitInfoB.distance * groundT / childB.lossyScale.y + GroundHeightOffset * groundT);
          Vector3 vertA = childAMatrix * (vertices[j] + groundOffsetA).WithW(1);
          Vector3 vertB = childBMatrix * (vertices[j] + groundOffsetB - Vector3.forward * sectionBounds.size.z).WithW(1);
          vertices[j] = Vector3.Lerp(vertA, vertB, sectionT);
        }

        sectionMesh.SetVertices(vertices);
        sectionMesh.UploadMeshData(markNoLongerReadable: false);
      }
      else
      {
        isCombineValid = false;
      }
    }

    if (isCombineValid)
    {
      _generatedMesh.CombineMeshes(_combineInstances.ToArray(), mergeSubMeshes: true, useMatrices: true, hasLightmapData: false);
      _generatedMesh.RecalculateBounds();
      _generatedMesh.RecalculateNormals();
      _generatedMesh.Optimize();
    }

    for (int i = 0; i < _combineInstances.Count; ++i)
      DestroyImmediate(_combineInstances[i].mesh);

    MeshFilter meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
    meshFilter.sharedMesh = _generatedMesh;
    meshFilter.hideFlags = HideFlags.DontSave;

    MeshRenderer meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
    meshRenderer.sharedMaterial = Material;
    meshRenderer.hideFlags = HideFlags.DontSave;

    if (GenerateCollider)
    {
      MeshCollider meshCollider = gameObject.GetOrAddComponent<MeshCollider>();
      meshCollider.sharedMesh = _generatedMesh;
    }
  }
}