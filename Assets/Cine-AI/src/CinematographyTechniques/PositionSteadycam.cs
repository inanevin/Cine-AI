using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PositionSteadycam : CinematographyTechniqueImplementation
{
    private Coroutine m_routine = null;

    private Transform m_manipulator = null;
    private Transform m_target = null;
    private Vector3 m_desiredPosition = Vector3.zero;
    private Vector3 m_offset = Vector3.zero;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {
        Transform target = node.m_simulationData.m_targetData.m_target;
        if (target != null)
        {
            Vector3 offset = node.m_simulationData.m_targetData.m_targetPosition - camManipulator.position;
            m_routine = StoryboardPlayController.Instance.StartCoroutine(Routine(camManipulator, target, offset));

            m_manipulator = camManipulator;
            m_offset = offset;
            m_target = target;
            StoryboardPlayController.OnLateUpdate += OnLateUpdate;
        }      
    }

    public override void Stop(Camera cam)
    {
        if (m_routine != null)
        {
            StoryboardPlayController.Instance.StopCoroutine(m_routine);
            StoryboardPlayController.OnLateUpdate -= OnLateUpdate;
        }

        m_manipulator = null;
        m_target = null;
        m_desiredPosition = Vector3.zero;
        m_offset = Vector3.zero;
    }

    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        return false;
    }

    Vector3 vel;
    private void OnLateUpdate()
    {
        m_manipulator.transform.position = m_target.position - m_offset;
        //m_manipulator.transform.position = Vector3.SmoothDamp(m_manipulator.transform.position, m_desiredPosition, ref vel, 0.02f);
    }

    private IEnumerator Routine(Transform manipulator, Transform target, Vector3 offset)
    {
        while (true)
        {
            m_desiredPosition = target.position - offset;
            yield return null;
            //Vector3 targetPos = target.position - offset;
            //yield return new WaitForEndOfFrame();
            //manipulator.transform.position = targetPos;
        }
    }
}
