using UnityEngine;
using System.Collections.Generic;

public class SoftBoneChain : MonoBehaviour
{
  public Transform HeadRoot => _bones[0].Transform;
  public Vector3[] BonePositions => _bonePositions;

  public float PoseStiffness
  {
    get => _poseStiffness;
    set => _poseStiffness = value;
  }

  public Vector3 HeadOffsetPos = Vector3.zero;
  public Quaternion HeadOffsetRot = Quaternion.identity;

  [SerializeField] private Mathfx.Axis _boneForwardAxis = Mathfx.Axis.Z;
  [SerializeField] private float _positionInterpolateSpeed = 15;
  [SerializeField] private float _rotationInterpolateSpeed = 15;
  [SerializeField, Range(0, 1)] private float _poseStiffness = 1;
  [SerializeField] private Bone[] _bones = null;

  private Vector3[] _bonePositions;
  private Bone _rootBone => _bones[0];

  [System.Serializable]
  private class Bone
  {
    [HideInInspector] public string Name;
    public Transform Transform;

    [Range(0, 1)] public float Influence;

    public bool EnableClampY;
    public float ClampYHeight;

    public Vector3 WorldPos { get; set; }
    public Quaternion WorldRot { get; set; }
    public Vector3 LocalPosInPrevBone { get; set; }
    public Quaternion LocalRotInPrevBone { get; set; }
  }

  public void SnapToRestPose()
  {
    for (int i = 1; i < _bones.Length; ++i)
    {
      Bone bone = _bones[i];
      Bone prevBone = GetPrevBone(i);

      // Get base desired transform to match rest pose
      Quaternion desiredRot = prevBone.Transform.rotation * bone.LocalRotInPrevBone;
      Vector3 desiredPos = prevBone.Transform.TransformPoint(bone.LocalPosInPrevBone);

      bone.WorldPos = desiredPos;
      bone.WorldRot = desiredRot;
      bone.Transform.rotation = desiredRot;
      bone.Transform.position = desiredPos;
    }
  }

  public Vector3 GetInterpolatedBonePos(float normalizedPos)
  {
    int boneCount = _bones.Length;
    float posScaled = (1 - normalizedPos) * boneCount;
    int boneIndexA = Mathf.Clamp(Mathf.FloorToInt(posScaled), 0, boneCount - 1);
    int boneIndexB = Mathf.Clamp(Mathf.CeilToInt(posScaled), 0, boneCount - 1);
    Vector3 posA = _bones[boneIndexA].Transform.position;
    Vector3 posB = _bones[boneIndexB].Transform.position;
    return Vector3.Lerp(posA, posB, posScaled - (int)posScaled);
  }

  private void Awake()
  {
    _bonePositions = new Vector3[_bones.Length];

    Vector3 boneBasisForward = Mathfx.GetAxisVector(_boneForwardAxis);
    for (int i = 0; i < _bones.Length; ++i)
    {
      Bone bone = _bones[i];
      Bone prevBone = GetPrevBone(i);

      bone.WorldPos = bone.Transform.position;
      bone.WorldRot = bone.Transform.rotation;

      bone.LocalPosInPrevBone = prevBone.Transform.InverseTransformPoint(bone.Transform.position);
      bone.LocalRotInPrevBone = Quaternion.Inverse(prevBone.Transform.rotation) * bone.Transform.rotation;

      _bonePositions[i] = bone.Transform.position;
    }
  }

  private void LateUpdate()
  {
    Vector3 boneBasisForward = Mathfx.GetAxisVector(_boneForwardAxis);
    Quaternion boneBasisRot = Quaternion.FromToRotation(boneBasisForward, Vector3.forward);

    _rootBone.WorldPos = HeadOffsetPos + transform.position;
    _rootBone.WorldRot = HeadOffsetRot * transform.rotation * boneBasisRot;

    // Update all bones after the first one, which is the root bone
    // and does not require any logic besides setting its position above
    for (int i = 1; i < _bones.Length; ++i)
    {
      Bone bone = _bones[i];
      Bone prevBone = GetPrevBone(i);

      // Get base desired transform to match rest pose
      Quaternion desiredRot = prevBone.Transform.rotation * bone.LocalRotInPrevBone;
      Vector3 desiredPos = prevBone.Transform.TransformPoint(bone.LocalPosInPrevBone);

      // Adjust bone rotation to point towards the previous bone
      Vector3 toPrevBone = prevBone.WorldPos - bone.WorldPos;
      desiredRot = Quaternion.FromToRotation(desiredRot * boneBasisForward, toPrevBone) * desiredRot;

      // Adjust bone position distance from previous based on the 'stiffness' of the pose
      float maxBoneDist = bone.LocalPosInPrevBone.magnitude;
      Vector3 desiredPosLoose = prevBone.WorldPos - Vector3.ClampMagnitude(toPrevBone, maxBoneDist);
      desiredPos = Vector3.Lerp(desiredPosLoose, desiredPos, _poseStiffness);

      // Clamping Y position of some bones can be useful for an 'upright' pose for the skeleton
      if (bone.EnableClampY)
      {
        float clampedPos = Mathf.Min(desiredPos.y, bone.ClampYHeight);
        desiredPos.y = Mathf.Lerp(desiredPos.y, clampedPos, _poseStiffness);
      }

      // Interpolate towards desired transform
      float rotationSpeed = Time.deltaTime * _rotationInterpolateSpeed;
      float positionSpeed = Time.deltaTime * _positionInterpolateSpeed * bone.Influence;
      bone.WorldRot = Mathfx.Damp(bone.WorldRot, desiredRot, 0.25f, rotationSpeed);
      bone.WorldPos = Mathfx.Damp(bone.WorldPos, desiredPos, 0.25f, positionSpeed);

      // Debug axis display
      Debug.DrawRay(bone.Transform.position, bone.Transform.right * 10, Color.red);
      Debug.DrawRay(bone.Transform.position, bone.Transform.up * 10, Color.green);
      Debug.DrawRay(bone.Transform.position, bone.Transform.forward * 10, Color.blue);
    }

    // Copy world transform to snake bones
    for (int i = 0; i < _bones.Length; ++i)
    {
      _bones[i].Transform.position = _bones[i].WorldPos;
      _bones[i].Transform.rotation = _bones[i].WorldRot;
    }

    // Store positions in an array for convenient public access 
    for (int i = 0; i < _bones.Length; ++i)
      _bonePositions[i] = _bones[i].WorldPos;
  }

  private void OnValidate()
  {
    for (int i = 0; _bones != null && i < _bones.Length; ++i)
    {
      if (_bones[i].Transform != null)
        _bones[i].Name = _bones[i].Transform.name;
      else
        _bones[i].Name = "Unassigned Bone";
    }
  }

  private Bone GetPrevBone(int boneIndex)
  {
    return boneIndex - 1 >= 0 ? _bones[boneIndex - 1] : _rootBone;
  }

  [ContextMenu("Calculate Bone Influences")]
  private void CalculateBoneInfluences()
  {
    for (int i = 0; i < _bones.Length; ++i)
    {
      Bone bone = _bones[i];
      float t = i / (float)(_bones.Length - 1);
      bone.Influence = Mathf.SmoothStep(1f, 0.1f, t);
    }
  }
}