using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Mastershot : CinematographyTechniqueImplementation
{
    public float m_radiusLB = 10.0f;
    public float m_radiusUB = 25.0f;

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
            Vector3 onSphere1 = targetData.m_targetPosition + Random.onUnitSphere * m_radiusLB;
            Vector3 onSphere2 = targetData.m_targetPosition + Random.onUnitSphere * m_radiusUB;
            finalPosition = Vector3.Lerp(onSphere1, onSphere2, Random.value);
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