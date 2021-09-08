using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class Pan : CinematographyTechniqueImplementation
{
    public float m_distanceLB = 3.0f;
    public float m_distanceUB = 6.0f;
    public bool m_fixedDirection = false;
    public bool m_isRight = false;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {

    }

    public override void Stop(Camera cam)
    {

    }
    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        Vector3 finalPosition = Vector3.zero;
        Quaternion finalRotation = Quaternion.identity;

        int counter = 0;
        while (counter < data.m_implementationTimeout)
        {
            float distance = Random.Range(m_distanceLB, m_distanceUB);
            float directionMultiplier = m_fixedDirection ? (m_isRight ? 1.0f : -1.0f) : ((Random.Range(0.0f, 1.0f) < 0.5f) ? -1.0f : 1.0f);
            finalPosition = targetData.m_targetPosition + targetData.m_targetRight * directionMultiplier * distance;
            finalRotation = Quaternion.LookRotation(targetData.m_targetPosition - finalPosition);

            if (CheckVisibility(data, currentNode, finalPosition, finalRotation, targetData))
            {
                SetSimulationData(currentNode, finalPosition, finalRotation, data.m_defaultFOV);
                return true;
            }
            counter++;
        }

        SetSimulationData(currentNode, finalPosition, finalRotation, data.m_defaultFOV);
        return false;

    }
}

