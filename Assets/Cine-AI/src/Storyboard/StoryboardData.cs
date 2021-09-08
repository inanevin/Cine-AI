using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum DecisionTechniquePreference
{
    ExponentialDistribution,
    ProbabilityDistribution
}

[CreateAssetMenu(fileName = "StoryboardData", menuName = "Cine-AI/StoryboardData", order = 1)]
[System.Serializable]
public class StoryboardData : ScriptableObject
{
    public List<StoryboardNode> m_nodes = new List<StoryboardNode>();
    public List<ProxySet> m_proxySets = new List<ProxySet>();
    public StoryboardDirectorData m_directorData;
    public int m_simulationFrameRate = 30;
    public float m_defaultFOV = 60.0f;
    public DecisionTechniquePreference m_decisionTechnique;
    public float m_dramatizationThreshold = 1.0f;
    public float m_paceThreshold = 1.0f;
    public bool m_useFX = false;
    public int m_techniqueTimeout = 100;
    public int m_implementationTimeout = 1000;
    public float m_visibilityCapsuleRadius = 0.3f;
    public float m_visibilityContactThreshold = 0.1f;
    public List<CinematographyTechniqueImplementation> m_techniqueImplementations = null;

#if UNITY_EDITOR
    public StoryboardNode m_transitionNode = null;
#endif
}
