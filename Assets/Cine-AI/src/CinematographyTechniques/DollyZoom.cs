using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class DollyZoom : CinematographyTechniqueImplementation
{
    public float m_targetFOV = 30.0f;
    public float m_moveAmount = 10.0f;
    public float m_duration = 3.0f;
    public float m_rayDistance = 0.5f;
    private Coroutine m_dollyRoutine = null;

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {
        m_dollyRoutine = StoryboardPlayController.Instance.StartCoroutine(DollyRoutine(cam));

    }

    public override void Stop(Camera cam)
    {
        StoryboardPlayController.Instance.StopCoroutine(m_dollyRoutine);
    }

    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        return false;

    }


    private IEnumerator DollyRoutine(Camera cam)
    {
        float i = 0.0f;
        float startFOV = cam.fieldOfView;

        while (i < 1.0f)
        {
            i += Time.deltaTime * 1.0f / m_duration;

            if (!Physics.Raycast(cam.transform.position, -cam.transform.forward, m_rayDistance))
            {
                cam.fieldOfView = Mathf.Lerp(startFOV, m_targetFOV, i * 5f);
                cam.transform.parent.position += cam.transform.parent.forward * m_moveAmount * Time.deltaTime;

            }

            yield return null;
        }
    }
}
