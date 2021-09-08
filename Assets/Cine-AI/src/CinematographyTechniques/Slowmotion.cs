using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[System.Serializable]
public class Slowmotion : CinematographyTechniqueImplementation
{
    public float m_duration = 1.0f;
    public float m_timeScale = 0.2f;
    private Coroutine m_routine = null;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {
        m_routine = StoryboardPlayController.Instance.StartCoroutine(Routine());

    }

    public override void Stop(Camera cam)
    {
        StoryboardPlayController.Instance.StopCoroutine(m_routine);
    }

    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        return false;
    }

    private IEnumerator Routine()
    {
        float previousTimescale = Time.timeScale;
        Time.timeScale = m_timeScale;
        yield return new WaitForSecondsRealtime(m_duration);
        Time.timeScale = previousTimescale;
    }
}

