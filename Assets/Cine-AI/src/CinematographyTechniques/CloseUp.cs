using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CloseUp : CinematographyTechniqueImplementation
{
    public float m_distanceLB = 1.0f;
    public float m_distanceUB = 3.0f;

    [Range(0.0f, 1.0f)]
    public float m_arcHeight = 5.0f;

    [Range(0.0f, 1.0f)]
    public float m_xArc = 1.0f;

    [Range(0.0f, 1.0f)]
    public float m_yArc = 0.5f;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {
        
    }

    public override void Stop(Camera cam)
    {

    }

    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        float distance = 0.0f;
        Vector3 fwPosition = Vector3.zero;
        Vector3 rightPosition = Vector3.zero;
        Vector3 leftPosition = Vector3.zero;
        Vector3 upPosition = Vector3.zero;
        Vector3 downPosition = Vector3.zero;
        Vector3 finalPosition = Vector3.zero;
        Quaternion finalRotation = Quaternion.identity;

        int counter = 0;
        while (counter < data.m_implementationTimeout)
        {
            float t = Random.Range(-m_xArc, m_xArc);
            distance = Random.Range(m_distanceLB, m_distanceUB);
            fwPosition = targetData.m_targetPosition + targetData.m_targetForward * distance;
            rightPosition = targetData.m_targetPosition + targetData.m_targetRight * distance;
            leftPosition = targetData.m_targetPosition + -targetData.m_targetRight * distance;
            upPosition = targetData.m_targetPosition + targetData.m_targetUp * distance;
            downPosition = targetData.m_targetPosition + -targetData.m_targetUp * distance;
            finalPosition = fwPosition;

            if (t > 0)
                finalPosition = MathUtility.SampleParabola(fwPosition, leftPosition, m_arcHeight, t, Quaternion.LookRotation(leftPosition - finalPosition) * Vector3.right);
            else
                finalPosition = MathUtility.SampleParabola(fwPosition, rightPosition, m_arcHeight, -t, Quaternion.LookRotation(rightPosition - finalPosition) * Vector3.left);

            t = Random.Range(-m_yArc, m_yArc);

            if (t > 0)
                finalPosition = MathUtility.SampleParabola(finalPosition, upPosition, m_arcHeight, t, Quaternion.LookRotation(upPosition - finalPosition) * Vector3.up);
            else
                finalPosition = MathUtility.SampleParabola(finalPosition, downPosition, m_arcHeight, -t, Quaternion.LookRotation(downPosition - finalPosition) * Vector3.down);

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
