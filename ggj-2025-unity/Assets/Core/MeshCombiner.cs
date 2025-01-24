// The MIT License (MIT)
// Copyright (c) 2024 David Evans @festivevector
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections.Generic;

public static class MeshCombiner
{
  private static List<MeshRenderer> _renderers = new();
  private static List<MeshFilter> _meshFilters = new();
  private static List<Material> _uniqueMaterials = new();
  private static List<CombineInstance> _combineInstances = new();
  private static List<Mesh> _meshesByMaterial = new();
  private static Transform _helperTransform;

  // Given a root object, takes all child MeshRenderers and combines them into a single mesh
  // Any materials found in the hierarchy will be included in the output materials list to be assigned
  // to meshRenderer.sharedMaterials
  public static void MakeCombinedMesh(Transform root, Mesh outputMesh, List<Material> outputMaterials)
  {
    // The helper transform is useful for getting local transform matrices
    if (_helperTransform == null)
    {
      _helperTransform = new GameObject("mesh-combiner-helper").transform;
      _helperTransform.gameObject.hideFlags = HideFlags.DontSave;
    }

    // Get all mesh renderers and materials in root 
    root.GetComponentsInChildren<MeshRenderer>(includeInactive: false, _renderers);
    for (int i = 0; i < _renderers.Count; i++)
    {
      if (!_renderers[i].enabled)
      {
        _renderers.RemoveAt(i);
        --i;
      }
    }

    foreach (var r in _renderers)
    {
      _meshFilters.Add(r.GetComponent<MeshFilter>());
      if (!_uniqueMaterials.Contains(r.sharedMaterial))
        _uniqueMaterials.Add(r.sharedMaterial);
    }

    // First, combine sets of meshes together that share a material
    for (int i = 0; i < _uniqueMaterials.Count; ++i)
    {
      Material mat = _uniqueMaterials[i];
      _combineInstances.Clear();
      for (int rendererIndex = 0; rendererIndex < _renderers.Count; ++rendererIndex)
      {
        Renderer r = _renderers[rendererIndex];
        MeshFilter meshFilter = _meshFilters[rendererIndex];
        if (r.sharedMaterial == mat)
        {
          _helperTransform.position = root.InverseTransformPoint(meshFilter.transform.position);
          _helperTransform.rotation = Quaternion.Inverse(root.rotation) * meshFilter.transform.rotation;
          _helperTransform.localScale = meshFilter.transform.lossyScale;

          CombineInstance combineInstance = new();
          combineInstance.mesh = meshFilter.sharedMesh;
          combineInstance.transform = _helperTransform.localToWorldMatrix;
          _combineInstances.Add(combineInstance);
        }
      }

      var mesh = MeshPool.GetMesh();
      mesh.name = $"combined-submesh-{mat.name}-{root.name}";
      mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      mesh.CombineMeshes(_combineInstances.ToArray(), true, true, false);
      _meshesByMaterial.Add(mesh);
    }

    // Then, combine each of those combined meshes into a single mesh with sub meshes
    if (_meshesByMaterial.Count > 0)
    {
      _combineInstances.Clear();
      for (int i = 0; i < _meshesByMaterial.Count; ++i)
      {
        var mesh = _meshesByMaterial[i];
        CombineInstance combineInstance = new CombineInstance();
        combineInstance.mesh = mesh;
        combineInstance.transform = Matrix4x4.identity;
        _combineInstances.Add(combineInstance);
      }

      outputMesh.Clear();
      outputMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      outputMesh.CombineMeshes(_combineInstances.ToArray(), false, true, false);
      outputMesh.RecalculateBounds();

      outputMaterials.Clear();
      outputMaterials.AddRange(_uniqueMaterials);

      // Clean up temp meshes
      for (int i = 0; i < _meshesByMaterial.Count; ++i)
        MeshPool.FreeMesh(_meshesByMaterial[i]);
      _meshesByMaterial.Clear();
    }

    // Reset static lists
    _renderers.Clear();
    _meshFilters.Clear();
    _uniqueMaterials.Clear();
    _combineInstances.Clear();
    _meshesByMaterial.Clear();
  }
}