using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

[System.Serializable]
public class PointConstraintInfo
{
    public Transform ConstrainedObj;
    public Vector3 Offset  =Vector3.zero;
}

[ExecuteInEditMode]
public class PointConstraint : MonoBehaviour
{
    public bool RunInEditor = true;
    public List<PointConstraintInfo> ConstraintedObjs = new List<PointConstraintInfo>();

    Vector3 originalLoc = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BuildOffsetList();
    }

    // Update is called once per frame
    void Update()
    {
        bool constraintMoved = false;
        if (transform.position != originalLoc)
        {
            constraintMoved = true;
        }

        if(constraintMoved && (Application.isPlaying || RunInEditor))
        {
            foreach(PointConstraintInfo pc in ConstraintedObjs)
            {
                if(pc.ConstrainedObj != null) pc.ConstrainedObj.position = transform.position - pc.Offset;
            }
            originalLoc = transform.position;
        }
    }

    void BuildOffsetList()
    {
        originalLoc = transform.position;
        foreach(PointConstraintInfo pc in ConstraintedObjs)
        {
            pc.Offset = transform.position - pc.ConstrainedObj.position;
        }
    }
}
