using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor;

public class StoryboardPlayController : MonoBehaviour, INotificationReceiver
{
    [SerializeField] private StoryboardData m_storyboardData;

    public delegate void LateUpdateActions();
    public static LateUpdateActions OnLateUpdate;

    public Camera m_camera;

    public static StoryboardPlayController Instance { get { return s_playController; } }

    private static StoryboardPlayController s_playController = null;
    private Transform m_cameraTransform = null;
    private int m_markerCount = 0;
    private StoryboardNode m_previousNode = null;
    private Transform m_cameraManipulator;

    private void Awake()
    {
        if (s_playController == null)
            s_playController = this;
       
    }

    private void OnEnable()
    {
        m_markerCount = 0;
        m_cameraTransform = m_camera.transform;

        GameObject camManipulator = new GameObject("Storyboard Camera Manipulator");
        m_cameraManipulator = camManipulator.transform;
        m_cameraManipulator.transform.SetParent(m_cameraTransform.parent);
        m_cameraManipulator.localEulerAngles = m_cameraTransform.localEulerAngles;
        m_cameraManipulator.localPosition = m_cameraTransform.localPosition;
        m_cameraManipulator.localScale = m_cameraTransform.localScale;
        m_cameraTransform.SetParent(m_cameraManipulator);
        m_cameraTransform.localEulerAngles = m_cameraTransform.localPosition = Vector3.zero;
        m_cameraTransform.localScale = Vector3.one;
    }


    private void LateUpdate()
    {
        if(OnLateUpdate != null)
        {
            OnLateUpdate.Invoke();
        }
    }

    public void OnNotify(Playable origin, INotification notification, object context)
    {

        if (m_storyboardData == null) return;

        if (notification is StoryboardMarker marker)
        {

            if(m_previousNode != null)
            {
                m_previousNode.m_lookTechnique.m_implementation.Stop(m_camera);
                m_previousNode.m_trackTechnique.m_implementation.Stop(m_camera);
            }

            StoryboardNode node = m_storyboardData.m_nodes.Find(o => o.m_marker == marker);

            if(node != null)
            {
                SimulationData simData = node.m_simulationData;
                m_cameraManipulator.position = simData.m_cameraPosition;
                m_cameraManipulator.rotation = simData.m_cameraRotation;
                m_camera.fieldOfView = simData.m_cameraFOV;

                node.m_lookTechnique.m_implementation.Play(m_camera, node, m_cameraManipulator);
                node.m_trackTechnique.m_implementation.Play(m_camera, node, m_cameraManipulator);
                node.m_fxTechnique.m_implementation.Play(m_camera, node, m_cameraManipulator);
                m_previousNode = node;

#if UNITY_EDITOR
                m_storyboardData.m_transitionNode = node;
#endif
            }

            m_markerCount++;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GetComponent<PlayableDirector>().Play();
        }
    }
}
