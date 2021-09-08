using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuickZoom : CinematographyTechniqueImplementation
{
    public float m_targetFOV = 30.0f;
    public float m_duration = 1.0f;
    private Coroutine m_fovRoutine = null;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {
        m_fovRoutine = StoryboardPlayController.Instance.StartCoroutine(FOVRoutine(cam));
    }

    public override void Stop(Camera cam)
    {
        if (m_fovRoutine != null)
            StoryboardPlayController.Instance.StopCoroutine(m_fovRoutine);
    }

    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        return false;
    }

    private IEnumerator FOVRoutine(Camera cam)
    {
        float i = 0.0f;
        float startFOV = cam.fieldOfView;

        while (i < 1.0f)
        {
            i += Time.deltaTime * 1.0f / m_duration;
            cam.fieldOfView = Mathf.Lerp(startFOV, m_targetFOV, i);
            yield return null;
        }
    }
}

