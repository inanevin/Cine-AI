using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class PositionHandheld : CinematographyTechniqueImplementation
{
    public Vector3 m_noiseAmount = new Vector3(1, 1, 0);
    public Vector3 m_noiseSpeed = new Vector3(4, 8, 4);
    private Coroutine m_routine = null;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {
        Transform target = node.m_simulationData.m_targetData.m_target;
        if (target != null)
        {
            Vector3 offset = node.m_simulationData.m_targetData.m_targetPosition - camManipulator.position;
            m_routine = StoryboardPlayController.Instance.StartCoroutine(Routine(cam.transform, target, camManipulator, offset));
        }

    }

    public override void Stop(Camera cam)
    {
        if (m_routine != null)
            StoryboardPlayController.Instance.StopCoroutine(m_routine);
        cam.transform.localPosition = Vector3.zero;
    }

    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        return true;
    }

    private IEnumerator Routine(Transform camera, Transform target, Transform manipulator, Vector3 offset)
    {
        float targetVelocityMag = 0.0f;
        Vector3 targetPreviousPosition = target.position;

        while (true)
        {
            targetVelocityMag = (target.position - targetPreviousPosition).magnitude;

            manipulator.position = target.position - offset;

            camera.localPosition += new Vector3(
                Mathf.Sin(Time.time * m_noiseSpeed.x) * m_noiseAmount.x,
                Mathf.Sin(Time.time * m_noiseSpeed.y) * m_noiseAmount.y,
                Mathf.Sin(Time.time * m_noiseSpeed.z) * m_noiseAmount.z
                ) * targetVelocityMag * 10;


            targetPreviousPosition = target.position;

            yield return null;
        }
    }

}

