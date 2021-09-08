using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class PosRotHandheld : CinematographyTechniqueImplementation
{
    public Vector3 m_posNoiseAmount = new Vector3(1, 1, 0);
    public Vector3 m_posNoiseSpeed = new Vector3(4, 8, 4);

    public Vector3 m_rotNoiseAmount = new Vector3(1, 1, 1);
    public Vector3 m_rotNoiseSpeed = new Vector3(4, 8, 4);

    private Coroutine m_routine = null;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {
        Transform target = node.m_simulationData.m_targetData.m_target;
        if (target != null)
        {
            Vector3 offset = node.m_simulationData.m_targetData.m_targetPosition - camManipulator.position;
            m_routine = StoryboardPlayController.Instance.StartCoroutine(Routine(cam.transform, camManipulator, target, offset));
        }

    }

    public override void Stop(Camera cam)
    {
        if (m_routine != null)
            StoryboardPlayController.Instance.StopCoroutine(m_routine);
    }

    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        return true;
    }

    private IEnumerator Routine(Transform camera, Transform manipulator, Transform target, Vector3 offset)
    {
        float targetVelocityMag = 0.0f;
        Vector3 targetPreviousPosition = target.position;

        while (true)
        {
            targetVelocityMag = (target.position - targetPreviousPosition).magnitude;

            manipulator.position = target.position - offset;
            manipulator.rotation = Quaternion.LookRotation(target.position - manipulator.position);

            camera.localPosition += new Vector3(
                Mathf.Sin(Time.time * m_posNoiseSpeed.x) * m_posNoiseAmount.x,
                Mathf.Sin(Time.time * m_posNoiseSpeed.y) * m_posNoiseAmount.y,
                Mathf.Sin(Time.time * m_posNoiseSpeed.z) * m_posNoiseAmount.z
                ) * targetVelocityMag * 10;

            camera.localEulerAngles += new Vector3(
            Mathf.Sin(Time.time * m_rotNoiseSpeed.x) * m_rotNoiseAmount.x,
            Mathf.Sin(Time.time * m_rotNoiseSpeed.y) * m_rotNoiseAmount.y,
            Mathf.Sin(Time.time * m_rotNoiseSpeed.z) * m_rotNoiseAmount.z
            ) * targetVelocityMag * 10;

            targetPreviousPosition = target.position;

            yield return null;
        }
    }
}
