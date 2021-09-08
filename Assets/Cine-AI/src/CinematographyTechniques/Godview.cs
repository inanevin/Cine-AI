using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Godview : CinematographyTechniqueImplementation
{
    public float m_distanceLB = 3.0f;
    public float m_distanceUB = 10.0f;
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
            finalPosition = targetData.m_targetPosition + Vector3.up * distance;
            finalRotation = Quaternion.LookRotation(targetData.m_targetPosition - finalPosition);

            if (CheckVisibility(data, currentNode, finalPosition, finalRotation, targetData))
            {

                SetSimulationData(currentNode, finalPosition, finalRotation, data.m_defaultFOV);
                return true;
            }

            counter++;
        }
        Debug.LogError("Sim Failed");

        SetSimulationData(currentNode, finalPosition, finalRotation, data.m_defaultFOV);
        return false;

    }
}
