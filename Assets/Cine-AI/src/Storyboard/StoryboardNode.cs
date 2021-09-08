using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class SimulationData
{
    public Vector3 m_cameraPosition = Vector3.zero;
    public Quaternion m_cameraRotation = Quaternion.identity;
    public float m_cameraFOV = 0.0f;
    public SimulationTargetData m_targetData;
#if UNITY_EDITOR
    public List<Texture2D> m_snapshots = new List<Texture2D>();
#endif
}

[System.Serializable]
public class SimulationTargetData
{
    public Transform m_target = null;
    public Vector3 m_targetPosition = Vector3.zero;
    public Vector3 m_targetForward = Vector3.zero;
    public Vector3 m_targetUp = Vector3.zero;
    public Vector3 m_targetRight = Vector3.zero;
}

[System.Serializable]
public class StoryboardNode
{
    public int m_index = -1;
    public StoryboardMarker m_marker = null;
    public SimulationData m_simulationData;
    public CinematographyTechnique m_positioningTechnique = null;
    public CinematographyTechnique m_lookTechnique = null;
    public CinematographyTechnique m_trackTechnique = null;
    public CinematographyTechnique m_fxTechnique = null;

#if UNITY_EDITOR

    public bool m_isLocked = false;
    public Rect m_rect = new Rect(200, 200, 400, 400);

#endif
}


