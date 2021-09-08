using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PosRotSteadycam : CinematographyTechniqueImplementation
{

    private Coroutine m_routine = null;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {
        Transform target = node.m_simulationData.m_targetData.m_target;
        if (target != null)
        {
            Vector3 offset = node.m_simulationData.m_targetData.m_targetPosition - cam.transform.position;
            m_routine = StoryboardPlayController.Instance.StartCoroutine(Routine(camManipulator, target, offset));
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

    private IEnumerator Routine(Transform manipulator, Transform target, Vector3 offset)
    {
        while (true)
        {
            manipulator.transform.position = target.position - offset;
            manipulator.rotation = Quaternion.LookRotation(target.position - manipulator.transform.position);
            yield return null;
        }
    }
}
