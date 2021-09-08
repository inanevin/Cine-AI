using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

[System.Serializable]
public class StoryboardWindow : EditorWindow
{
    private const string m_editorPrefsID = "ie_storyboardWindow";
    private const float m_panelsSpacing = 5.0f;
    private const float m_zoomSensitivity = 3.5f;
    private const float m_minZoom = 0.5f;
    private const float m_maxZoom = 2.0f;
    private const float m_seperatorWidth = 3.0f;
    private const float m_propertyPanelMinWidth = 300.0f;
    private float m_propertyPanelWidth = 500.0f;
    private float m_boardPanelWidth = 0.0f;
    private Vector2 m_zoomCoordsOrigin = Vector2.zero;
    private Rect m_zoomArea;
    private Rect m_seperatorRect;
    private Rect m_seperatorResizeRect;
    private float m_zoomScale = 1.0f;
    private bool m_resizePanelSplit = false;

    private StoryboardWindowResources m_resources = new StoryboardWindowResources();
    private StoryboardSimulator m_simulator = new StoryboardSimulator();
    [SerializeField] private string m_lastStoryboardDataPath;
    [SerializeField] private string m_lastTimelinePath;
    [SerializeField] private string m_lastDirectorThresholdsPath;
    [SerializeField] private string m_lastRenderTexturePath;
    [SerializeField] private string m_implementationResourcesPath;
    [SerializeField] private string m_dataDumpPath;
    [SerializeField] private bool m_camDebug;
    [SerializeField] private StoryboardData m_storyboardData;
    [SerializeField] private TimelineAsset m_timeline;
    [SerializeField] private StoryboardSceneProxy m_sceneProxy;
    [SerializeField] private PlayableDirector m_playableDirector;
    [SerializeField] private Camera m_camera;
    [SerializeField] private RenderTexture m_renderTexture;
    [SerializeField] private StoryboardPlayController m_playController;
    [SerializeField] private bool m_visualizeProxyCollisions = false;
    [SerializeField] private bool m_visualizeProxyBoundaries = false;
    [SerializeField] private bool m_visualizeEvaluateTimeline = false;
    [SerializeField] private int m_proxySetToDraw = 0;
    [SerializeField] private Vector3 m_proxyBounds = new Vector3(10, 10, 10);
    [SerializeField] private bool m_markersValid = false;
    [SerializeField] private bool m_showAdvancedSimSettings = false;
    [SerializeField] private int m_selectedTab = 0;
    [SerializeField] private int m_simulatedMarkers = 0;
    [SerializeField] private bool m_directorDataValid = false;
    [SerializeField] private bool m_shouldDisableButtons = false;
    [SerializeField] private string m_debugPositioning = "";
    [SerializeField] private string m_debugLook = "";
    [SerializeField] private string m_debugTrack = "";
    [SerializeField] private string m_debugFX = "";
    [SerializeField] private bool m_proxiesValid = false;
    [SerializeField] private int m_markerCount = 0;
    [SerializeField] private float s_transitionTimer = 0.0f;
    [SerializeField] private bool m_allNodesLocked = false;

    private TextAsset m_directorDataTextAsset;
    private bool m_displayProxyProgressBar = false;
    private bool m_displaySimulationProgressBar = false;
    private int m_proxyProgressID = -1;
    private int m_simulationProgressID = -1;
    private EditorCoroutine m_proxyCalculationRoutine = null;
    private Vector2 m_propertiesScrollPos;
    Rect window1;

    [MenuItem("Cine-AI/Storyboard Window")]
    public static void ShowWindow()
    {
        StoryboardWindow window = EditorWindow.GetWindow<StoryboardWindow>();
        EditorWindow.FocusWindowIfItsOpen(typeof(StoryboardWindow));
    }

    protected void OnEnable()
    {
        wantsMouseMove = true;
        window1 = new Rect(10, 10, 100, 100);
        // Here we retrieve the data if it exists or we save the default field initialisers we set above
        var data = EditorPrefs.GetString(m_editorPrefsID, JsonUtility.ToJson(this, false));

        // Then we apply them to this window
        JsonUtility.FromJsonOverwrite(data, this);

        FindReferences();
        CreateEditorsForImplementations();

        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.hierarchyChanged += OnHierarchyWindowChanged;
    }

    protected void OnDisable()
    {
        // We get the Json data
        var data = JsonUtility.ToJson(this, false);

        // And we save it
        EditorPrefs.SetString(m_editorPrefsID, data);

        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.hierarchyChanged -= OnHierarchyWindowChanged;

    }

    private void OnGUI()
    {
        if (m_resources.m_smallLabel == null)
        {
            m_resources = new StoryboardWindowResources();
            m_resources.SetupResources();
        }
        // Setup width & height of panels.
        m_boardPanelWidth = EditorGUIUtility.currentViewWidth - m_propertyPanelWidth;
        m_seperatorRect = new Rect(m_propertyPanelWidth, 0, m_seperatorWidth, Screen.height);
        m_seperatorResizeRect = new Rect(m_seperatorRect.x, m_seperatorRect.y, m_seperatorRect.width + 5, m_seperatorRect.height);
        ProcessEvents(Event.current);

        // Properties panel.
        GUILayout.BeginHorizontal();
        m_propertiesScrollPos = GUILayout.BeginScrollView(m_propertiesScrollPos, GUILayout.Width(m_propertyPanelWidth - m_panelsSpacing));
        DrawProperties();
        GUILayout.EndScrollView();

        // Actual board.
        GUILayout.BeginVertical(GUILayout.Width(m_boardPanelWidth + m_panelsSpacing));
        DrawBoard();
        GUILayout.EndVertical();

        // Seperator.
        EditorGUI.DrawRect(m_seperatorRect, Color.black);
        EditorGUIUtility.AddCursorRect(m_seperatorResizeRect, MouseCursor.ResizeHorizontal);

        GUILayout.EndHorizontal();

        Handles.color = Handles.xAxisColor;
        Handles.DrawLine(Vector3.zero, new Vector3(0, 15, 0));

    }

    private void OnSceneGUI(SceneView obj)
    {
        if (m_sceneProxy == null || m_storyboardData == null) return;

        if (Event.current.type == EventType.Repaint)
        {
            Color outerBoundsColor = Color.blue;
            Color proxyColor = Color.red;
            Color outline = Color.white;
            outerBoundsColor.a = 0.05f;
            proxyColor.a = 0.3f;

            if (m_visualizeProxyBoundaries)
                HandlesUtility.DrawCubeOutlined(m_sceneProxy.transform.position, m_proxyBounds, outerBoundsColor, outline);

            if (m_visualizeProxyCollisions)
            {
                if (m_storyboardData.m_proxySets.Count > 0)
                {

                    for (int i = 0; i < m_storyboardData.m_proxySets[m_proxySetToDraw].m_proxies.Count; i++)
                    {
                        Proxy proxy = m_storyboardData.m_proxySets[m_proxySetToDraw].m_proxies[i];

                        if (proxy.m_collider == null) continue;


                        if (proxy.m_collider.GetType() == typeof(BoxCollider))
                        {
                            BoxCollider bc = proxy.m_collider.transform.GetComponent<BoxCollider>();
                            Handles.matrix = Matrix4x4.TRS(proxy.m_collider.transform.TransformPoint(bc.center), proxy.m_collider.transform.rotation, proxy.m_collider.transform.lossyScale);
                            HandlesUtility.DrawCubeOutlined(Vector3.zero, bc.size, proxyColor, outline);

                        }
                        else if (proxy.m_collider.GetType() == typeof(SphereCollider))
                        {
                            SphereCollider cc = proxy.m_collider.transform.GetComponent<SphereCollider>();
                            Handles.color = proxyColor;
                            Handles.matrix = Matrix4x4.TRS(proxy.m_collider.transform.TransformPoint(cc.center), proxy.m_collider.transform.rotation, Vector3.one);
                            //  Handles.matrix = Matrix4x4.identity;
                            Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, cc.radius * proxy.m_collider.transform.localScale.MaxComponent() * 2, EventType.Repaint);
                        }
                        else if (proxy.m_collider.GetType() == typeof(CapsuleCollider))
                        {
                            CapsuleCollider cc = proxy.m_collider.transform.GetComponent<CapsuleCollider>();
                            Handles.color = proxyColor;
                            Handles.matrix = Matrix4x4.TRS(proxy.m_collider.transform.TransformPoint(cc.center), proxy.m_collider.transform.rotation, proxy.m_collider.transform.lossyScale);
                            HandlesUtility.DrawCubeOutlined(Vector3.zero, new Vector3(1, cc.height, 1), proxyColor, outline);
                        }
                        else if (proxy.m_collider.GetType() == typeof(MeshCollider))
                        {
                            // TODO
                            MeshCollider cc = proxy.m_collider.transform.GetComponent<MeshCollider>();
                            //
                            Handles.color = proxyColor;
                            Handles.matrix = Matrix4x4.identity;
                            // Handles.matrix = Matrix4x4.TRS(proxy.m_collider.transform.TransformPoint(proxy.m_collider.transform.pos), proxy.m_collider.transform.rotation, proxy.m_collider.transform.lossyScale);
                            Handles.SphereHandleCap(0, proxy.m_collider.transform.position, Quaternion.identity, .5f, EventType.Repaint);
                            //  HandlesUtility.DrawCubeOutlined(Vector3.zero, new Vector3(1, cc.bounds.size.y, 1), proxyColor, outline);
                        }
                        else if (proxy.m_collider.GetType() == typeof(TerrainCollider))
                        {
                            // TOOD
                        }

                    }

                }
            }
        }
    }

    private void DrawProperties()
    {

        EditorGUILayout.BeginVertical("GroupBox");
        EditorGUILayout.BeginHorizontal();
        m_selectedTab = GUILayout.Toolbar(m_selectedTab, new string[] { "Director", "Techniques" });

        if (GUILayout.Button("Refresh", GUILayout.MaxWidth(75)))
            FindReferences();

        if (GUILayout.Button("Reset Defaults", GUILayout.MaxWidth(100)))
            ResetDefaults();

        EditorGUILayout.EndHorizontal();

        if (m_timeline != null)
            m_markersValid = m_simulator.CheckMarkerValidity(m_timeline);



        m_shouldDisableButtons = !m_markersValid || !m_directorDataValid;

        if (!m_markersValid)
        {
            EditorGUILayout.Space(7);
            EditorGUILayout.HelpBox("Markers are not valid. Make sure you have a marker right at the beginning of the timeline (0.0) also all markers should have valid targets.", MessageType.Error);
        }

        if (!m_directorDataValid)
        {
            EditorGUILayout.Space(7);
            EditorGUILayout.HelpBox("Director data is not valid, please calculate it.", MessageType.Error);
        }

        EditorGUILayout.EndVertical();

        if (DrawGeneralProperties())
        {
            if (m_selectedTab == 0)
            {
                DrawDirectorProperties();
                DrawSceneProxyProperties();
                DrawSimulationProperties();
            }
            else
            {
                DrawImplementationProperties();
            }

        }

        if (m_storyboardData)
        {
            EditorUtility.SetDirty(m_storyboardData);

            if (m_storyboardData.m_techniqueImplementations != null)
            {
                for (int i = 0; i < m_storyboardData.m_techniqueImplementations.Count; i++)
                {
                    if (m_storyboardData.m_techniqueImplementations[i] != null)
                        EditorUtility.SetDirty(m_storyboardData.m_techniqueImplementations[i]);
                }
            }
        }
    }

    private bool DrawGeneralProperties()
    {
        float controlWidth = m_propertyPanelWidth - m_panelsSpacing * 2 - 10;

        EditorGUILayout.BeginVertical("GroupBox", GUILayout.MaxWidth(controlWidth));

        EditorGUILayout.LabelField("General", m_resources.m_smallLabel);
        EditorGUILayout.Space(5);

        m_storyboardData = EditorGUILayout.ObjectField("Target Data", m_storyboardData, typeof(StoryboardData), true) as StoryboardData;

        if (m_storyboardData == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No storyboard data is assigned, please assign one to continue operations.", MessageType.Error, true);
            return false;
        }
        m_lastStoryboardDataPath = AssetDatabase.GetAssetPath(m_storyboardData);

        m_timeline = EditorGUILayout.ObjectField("Timeline", m_timeline, typeof(TimelineAsset), true) as TimelineAsset;

        if (m_timeline == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No timeline asset is assigned, please assign one to continue operations.", MessageType.Error);
            return false;
        }
        m_lastTimelinePath = AssetDatabase.GetAssetPath(m_timeline);

        m_playableDirector = EditorGUILayout.ObjectField("Playable Director", m_playableDirector, typeof(PlayableDirector), true) as PlayableDirector;

        if (m_playableDirector == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No playable director is referenced, please assign one to continue operations.", MessageType.Error);
            return false;
        }

        m_playController = EditorGUILayout.ObjectField("Play Controller", m_playController, typeof(StoryboardPlayController), true) as StoryboardPlayController;

        if (m_playController == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No play controller is referenced, please assign one to continue operations.", MessageType.Error);
            return false;
        }

        m_camera = EditorGUILayout.ObjectField("Camera", m_camera, typeof(Camera), true) as Camera;
        m_playController.m_camera = m_camera;

        if (m_camera == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No camera transform is referenced, please assign one to continue operations.", MessageType.Error);
            return false;
        }

        m_sceneProxy = EditorGUILayout.ObjectField("Scene Proxy", m_sceneProxy, typeof(StoryboardSceneProxy), true) as StoryboardSceneProxy;
        if (m_sceneProxy == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No scene proxy is referenced, please assign one to continue operations.", MessageType.Error);
            return false;
        }


        m_directorDataTextAsset = EditorGUILayout.ObjectField("Director Data", m_directorDataTextAsset, typeof(TextAsset), true) as TextAsset;

        if (m_directorDataTextAsset == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No director threshold data is referenced, please assign one to continue operations.", MessageType.Error);
            return false;
        }
        m_lastDirectorThresholdsPath = AssetDatabase.GetAssetPath(m_directorDataTextAsset);


        m_renderTexture = EditorGUILayout.ObjectField("Screenshot RT", m_renderTexture, typeof(RenderTexture), true, GUILayout.MaxHeight(16)) as RenderTexture;

        if (m_renderTexture == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No render texture is referenced, please assign one to continue operations.", MessageType.Error);
            return false;
        }
        m_lastRenderTexturePath = AssetDatabase.GetAssetPath(m_renderTexture);

        m_dataDumpPath = EditorGUILayout.TextField("Data Dump Path", m_dataDumpPath);

        if (!AssetDatabase.IsValidFolder(m_dataDumpPath))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Data dump path does not exist, please provide a valid path.", MessageType.Error);
            return false;
        }

        m_implementationResourcesPath = EditorGUILayout.TextField("Implementations Path", m_implementationResourcesPath);

        if (!AssetDatabase.IsValidFolder(m_implementationResourcesPath))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Implementations path does not exist, please provide a valid path.", MessageType.Error);
            return false;
        }

        //m_debugPositioning = EditorGUILayout.TextField("Debug Positioning", m_debugPositioning);
        //m_debugLook = EditorGUILayout.TextField("Debug Look", m_debugLook);
        //m_debugTrack = EditorGUILayout.TextField("Debug Track", m_debugTrack);
        //m_debugFX = EditorGUILayout.TextField("Debug FX", m_debugFX);
        //m_camDebug = EditorGUILayout.Toggle("Cam Debug", m_camDebug);

        EditorGUILayout.Space(5);

        EditorGUILayout.EndVertical();
        return true;
    }

    private void DrawDirectorProperties()
    {
        EditorGUILayout.BeginVertical("GroupBox");
        EditorGUILayout.LabelField("Director Data", m_resources.m_smallLabel);
        EditorGUILayout.Space(5);


        if (GUILayout.Button("Calculate Distributions"))
            CalculateDirectorDistributions();

        if (m_storyboardData.m_directorData != null && m_storyboardData.m_directorData != null)
        {
            StoryboardDirectorData dirData = m_storyboardData.m_directorData;
            EditorGUILayout.BeginVertical("GroupBox");
            GUILayout.Label("Calculated Data", m_resources.m_miniBoldLabel);

            EditorGUILayout.Space();
            GUIStyle labelstyle = new GUIStyle();
            labelstyle.alignment = TextAnchor.MiddleLeft;
            labelstyle.normal.textColor = new Color(1, 1, 0, 1);

            for (int i = 0; i < m_storyboardData.m_directorData.m_categories.Count; i++)
            {
                CinematographyTechniqueCategory category = m_storyboardData.m_directorData.m_categories[i];

                category.m_foldout = EditorGUILayout.Foldout(category.m_foldout, category.m_title);

                if (category.m_foldout)
                {
                    for (int j = 0; j < category.m_techniques.Count; j++)
                    {
                        CinematographyTechnique techniqueData = category.m_techniques[j];
                        EditorGUI.indentLevel++;
                        GUILayout.Label(" " + techniqueData.m_title, labelstyle);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("PX: " + techniqueData.m_probabilityDistribution.ToString("F2"));
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("Pace: " + techniqueData.m_pace.ToString());
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("EPX: " + techniqueData.m_classDistribution.ToString("F10"));
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("Dram: " + techniqueData.m_dramatization.ToString());
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel--;

                    }
                }
            }

            EditorGUILayout.EndVertical();

        }


        EditorGUILayout.EndVertical();
    }

    private void DrawSceneProxyProperties()
    {
        EditorGUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Scene Proxy", m_resources.m_smallLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox("Proxy set calculations are session based, meaning they won't be serialized and will not survive scene changes or editor restarts.", MessageType.Info);

        m_proxyBounds = EditorGUILayout.Vector3Field("Bounds", m_proxyBounds);
        EditorGUILayout.BeginHorizontal();

        if (m_shouldDisableButtons)
            GUI.enabled = false;

        if (GUILayout.Button("Calculate Proxy"))
        {
#if UNITY_EDITOR

            if (Progress.Exists(m_proxyProgressID))
            {
                Progress.Cancel(m_proxyProgressID);
                EditorCoroutineUtility.StopCoroutine(m_proxyCalculationRoutine);
            }
            m_proxyCalculationRoutine = EditorCoroutineUtility.StartCoroutineOwnerless(m_simulator.CalculateSceneProxies(this, m_storyboardData, m_timeline, m_playableDirector, m_sceneProxy.transform.position, m_proxyBounds));
            m_displayProxyProgressBar = true;
            m_proxyProgressID = Progress.Start("Calculating proxy sets...", "proxy");
#endif
        }

        if (m_shouldDisableButtons)
            GUI.enabled = true;

        if (GUILayout.Button("Clear Proxy Sets"))
        {
#if UNITY_EDITOR

            m_proxySetToDraw = 0;

            if (Progress.Exists(m_proxyProgressID))
            {
                Progress.Cancel(m_proxyProgressID);
                EditorCoroutineUtility.StopCoroutine(m_proxyCalculationRoutine);
            }

            m_storyboardData.m_proxySets.Clear();
            m_playableDirector.time = 0;
            m_playableDirector.Evaluate();
#endif
        }

        m_markerCount = m_simulator.GetTimelineMarkerCount(m_timeline);

        m_proxiesValid = m_storyboardData.m_proxySets.Count == m_markerCount;
#if UNITY_EDITOR

        if (m_displayProxyProgressBar)
            Progress.Report(m_proxyProgressID, (float)m_storyboardData.m_proxySets.Count / (float)m_markerCount, "Calculating proxy sets...");
#endif
        EditorGUILayout.EndHorizontal();

        Handles.color = Color.white;
        Handles.DrawWireCube(m_sceneProxy.transform.position, m_proxyBounds);

        // Invalidate sets if colliders are not found.
        for (int i = 0; i < m_storyboardData.m_proxySets.Count; i++)
        {
            ProxySet set = m_storyboardData.m_proxySets[i];

            for (int j = 0; j < set.m_proxies.Count; j++)
            {
                Proxy proxy = set.m_proxies[j];

                if (proxy.m_collider == null)
                    set.m_proxies.RemoveAt(j);
            }

            if (set.m_proxies.Count == 0)
                m_storyboardData.m_proxySets.RemoveAt(i);
        }

        m_visualizeProxyBoundaries = EditorGUILayout.Toggle("Visualize Boundaries", m_visualizeProxyBoundaries);

        if (m_storyboardData.m_proxySets.Count > 0)
        {

            m_visualizeProxyCollisions = EditorGUILayout.Toggle("Visualize Collisions", m_visualizeProxyCollisions);

            if (m_visualizeProxyCollisions)
            {
                m_proxySetToDraw = EditorGUILayout.IntSlider("Set to Draw", m_proxySetToDraw, 0, m_storyboardData.m_proxySets.Count - 1);
                m_visualizeEvaluateTimeline = EditorGUILayout.Toggle("Evaluate Timeline", m_visualizeEvaluateTimeline);

                if (m_visualizeEvaluateTimeline)
                {
                    StoryboardMarker marker = m_storyboardData.m_proxySets[m_proxySetToDraw].m_marker;
                    m_playableDirector.time = marker.m_jumpsToTime ? marker.m_jumpTime : marker.time;
                    m_playableDirector.Evaluate();
                }
            }
        }

        GUIStyle labelStyle = new GUIStyle();
        if (m_storyboardData.m_proxySets.Count != m_markerCount)
            labelStyle.normal.textColor = Color.yellow;
        else
            labelStyle.normal.textColor = Color.green;

        GUILayout.Label(m_storyboardData.m_proxySets.Count + " proxy sets are calculated out of " + m_markerCount + " markers.", labelStyle);
        EditorGUILayout.EndVertical();

    }

    private void DrawSimulationProperties()
    {
        EditorGUILayout.BeginVertical("GroupBox");

        EditorGUILayout.LabelField("Simulation", m_resources.m_smallLabel);
        EditorGUILayout.Space(5);

        m_storyboardData.m_simulationFrameRate = EditorGUILayout.IntSlider("Frame Rate", m_storyboardData.m_simulationFrameRate, 30, 120);
        m_storyboardData.m_techniqueTimeout = EditorGUILayout.IntField("Technique Timeout", m_storyboardData.m_techniqueTimeout);
        m_storyboardData.m_implementationTimeout = EditorGUILayout.IntField("Implementation Timeout", m_storyboardData.m_implementationTimeout);
        m_storyboardData.m_defaultFOV = EditorGUILayout.FloatField("Default FOV", m_storyboardData.m_defaultFOV);
        m_storyboardData.m_visibilityCapsuleRadius = EditorGUILayout.Slider("Visibility Capsule Radius", m_storyboardData.m_visibilityCapsuleRadius, 0.01f, 10.0f);
        m_storyboardData.m_visibilityContactThreshold = EditorGUILayout.Slider("Visibility Contact Threshold", m_storyboardData.m_visibilityContactThreshold, 0.01f, 5.0f);
        m_storyboardData.m_useFX = EditorGUILayout.Toggle("Use FX", m_storyboardData.m_useFX);

        m_storyboardData.m_decisionTechnique = (DecisionTechniquePreference)EditorGUILayout.EnumPopup("Decision Technique", m_storyboardData.m_decisionTechnique);

        m_showAdvancedSimSettings = EditorGUILayout.Foldout(m_showAdvancedSimSettings, "Advanced");

        if (m_showAdvancedSimSettings)
        {
            EditorGUI.indentLevel++;
            m_storyboardData.m_dramatizationThreshold = EditorGUILayout.Slider(new GUIContent("Dramatization Threshold", "Defines how much the dramatization affects the cinematography technique desicions."), m_storyboardData.m_dramatizationThreshold, 0.0f, 1.0f);
            m_storyboardData.m_paceThreshold = EditorGUILayout.Slider(new GUIContent("Pace Threshold", "Defines how much the pace affects the cinematography technique desicions."), m_storyboardData.m_paceThreshold, 0.0f, 1.0f);

            if (m_storyboardData.m_dramatizationThreshold == 0.0f || m_storyboardData.m_paceThreshold == 0.0f)
            {
                EditorGUILayout.HelpBox("Setting dramatization or pace thresholds to zero means that the marker's values must exactly match the thresholds in the director data." +
                   " This will most likely result in selecting the default cinematography technique for all the markers. Recommended values are: 1.0f (ignore thresholds), 0.2~", MessageType.Warning);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.BeginHorizontal();

        if (m_shouldDisableButtons || !m_proxiesValid)
            GUI.enabled = false;

        if (GUILayout.Button("Simulate All"))
        {
            //EditorCoroutineUtility.StartCoroutineOwnerless(DisplaySimulationProgress());

            if (m_simulator.FillStoryboardNodes(m_storyboardData, m_timeline, m_dataDumpPath))
            {
                m_displaySimulationProgressBar = true;

                m_simulatedMarkers = 0;

                if (m_storyboardData.m_directorData.m_categories[0].m_defaultTechnique.m_implementation == null)
                    CalculateDirectorDistributions();

                m_simulator.Simulate(this, m_camera.transform, m_storyboardData, m_timeline, m_playableDirector,
                 ref m_simulatedMarkers, m_debugPositioning, m_debugLook, m_debugTrack, m_debugFX, m_renderTexture, true, m_camDebug);

                SaveSimulationTextures(true);


            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Problem occured with filling the marker nodes.", "OK");
            }


        }


        GUI.enabled = true;

        if (GUILayout.Button("Clear All"))
        {
            m_storyboardData.m_nodes.Clear();
        }

        GUI.enabled = m_storyboardData.m_nodes.Count > 0;

        string lockText = m_allNodesLocked ? "Unlock All" : "Lock All";
        if (GUILayout.Button(lockText))
        {
            for (int i = 0; i < m_storyboardData.m_nodes.Count; i++)
                m_storyboardData.m_nodes[i].m_isLocked = !m_allNodesLocked;

            m_allNodesLocked = !m_allNodesLocked;
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Find Targets"))
        {
            for (int i = 0; i < m_storyboardData.m_nodes.Count; i++)
            {
                if (m_storyboardData.m_nodes[i].m_marker.m_targets.Length == 1)
                {
                    string str = m_storyboardData.m_nodes[i].m_marker.m_targets[0];
                    GameObject go = GameObject.Find(str);

                    if (go == null)
                        Debug.Log(str);

                    Transform target = go.transform;
                    m_storyboardData.m_nodes[i].m_simulationData.m_targetData.m_target = target;
                }
            }
        }

        if (!m_proxiesValid)
        {
            EditorGUILayout.HelpBox("Proxy calculation is not valid, please calculate the proxies before simulating.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawImplementationProperties()
    {

        for (int i = 0; i < m_storyboardData.m_directorData.m_categories.Count; i++)
        {
            CinematographyTechniqueCategory category = m_storyboardData.m_directorData.m_categories[i];

            for (int j = 0; j < category.m_techniques.Count; j++)
            {
                Editor editor = category.m_techniques[j].m_implementation.m_editor;

                EditorGUILayout.BeginVertical("GroupBox");

                EditorGUILayout.LabelField(category.m_techniques[j].m_implementation.GetType().ToString(), m_resources.m_smallLabel);

                editor.serializedObject.Update();
                SerializedProperty iterator = editor.serializedObject.GetIterator();
                iterator.NextVisible(true);

                while (iterator.NextVisible(false))
                    EditorGUILayout.PropertyField(iterator, true);

                editor.serializedObject.ApplyModifiedProperties();

                EditorGUILayout.EndVertical();
            }
        }

    }

    private void DrawBoard()
    {
        // Prepare zoomable area.
        m_zoomArea = new Rect(m_propertyPanelWidth, 0, m_boardPanelWidth, Screen.height);

        // Draw background.
        EditorGUI.DrawRect(new Rect(m_propertyPanelWidth, 0, m_boardPanelWidth, Screen.height), new Color(0.1f, 0.1f, 0.1f));

        if (m_storyboardData == null) return;

        // Information.
        if (m_storyboardData && m_storyboardData.m_nodes.Count == 0)
            EditorGUI.LabelField(new Rect(m_propertyPanelWidth + 20, 20, 100, 50), "No nodes found, please generate.", m_resources.m_mediumLabel);

        // Begin zoom area.
        EditorZoomArea.Begin(m_zoomScale, m_zoomArea);

        Vector2 windowStartPosition = new Vector2(50, 50);
        Vector2 windowPosition = windowStartPosition;
        int horizontalCounter = 0;
        Vector2 textureSize = new Vector2(300, 300);
        Vector2 windowSize = new Vector2(350, 510);

        float xAddition = 150;
        float yAddition = 100;
        float totalXAddition = windowSize.x + xAddition;
        float totalYAddition = windowSize.y + yAddition;

        Color windowColor = new Color(0.15f, 0.15f, 0.15f, 1.0f);
        Color windowBorderColorUnlocked = new Color(0.05f, 0.05f, 0.05f, 1.0f);
        Color windowBorderColorLocked = new Color(0.05f, 0.35f, 0.05f, 1.0f);

        Color textureBorderColor = new Color(0.05f, 0.05f, 0.05f, 1.0f);
        ColorUtility.TryParseHtmlString("#53505A", out textureBorderColor);
        ColorUtility.TryParseHtmlString("#A58961", out windowBorderColorLocked);

        for (int i = 0; i < m_storyboardData.m_nodes.Count; i++)
        {
            StoryboardNode node = m_storyboardData.m_nodes[i];

            // m_storyboardData.m_nodes[i].m_rect = GUI.Window(i, m_storyboardData.m_nodes[i].m_rect, DrawNodeWindow,"");
            node.m_rect = new Rect(windowPosition.x - m_zoomCoordsOrigin.x, windowPosition.y - m_zoomCoordsOrigin.y, windowSize.x, windowSize.y);

            if (i != 0 && horizontalCounter != 0)
            {
                StoryboardNode previousNode = m_storyboardData.m_nodes[i - 1];
                DrawHorizontalNodeTransition(previousNode, node);
            }
            else if (i != 0 && horizontalCounter == 0)
            {
                StoryboardNode previousNode = m_storyboardData.m_nodes[i - 1];
                DrawVerticalNodeTransition(previousNode, node, yAddition);
            }


            if (horizontalCounter < 3)
            {
                windowPosition.x += totalXAddition;
                horizontalCounter++;
            }
            else
            {
                horizontalCounter = 0;
                windowPosition.y += totalYAddition;
                windowPosition.x = windowStartPosition.x;
            }

        }

        // Draw each node.
        for (int i = 0; i < m_storyboardData.m_nodes.Count; i++)
        {
            StoryboardNode node = m_storyboardData.m_nodes[i];
            Color windowBorderColor = node.m_isLocked ? windowBorderColorLocked : windowBorderColorUnlocked;
            Rect nodeRect = node.m_rect;

            // Outline 
            EditorGUI.DrawRect(new Rect(nodeRect.x - 2, nodeRect.y - 2, nodeRect.width + 4, nodeRect.height + 4), windowBorderColor);

            // Window.
            EditorGUI.DrawRect(new Rect(nodeRect.x, nodeRect.y, nodeRect.width, nodeRect.height), windowColor);

            // Header
            GUI.Label(new Rect(nodeRect.x + 10, nodeRect.y + 10, 100, 50), "MARKER " + i.ToString(), m_resources.m_nodeTitleLabel);

            GUI.enabled = !node.m_isLocked;

            // Header buttons.
            if (GUI.Button(new Rect(nodeRect.x + windowSize.x - 160, nodeRect.y + 5, 75, 30), "Resimulate"))
            {
                m_simulator.Simulate(this, m_camera.transform, m_storyboardData, m_timeline, m_playableDirector,
                    ref m_simulatedMarkers, m_debugPositioning, m_debugLook, m_debugTrack, m_debugFX, m_renderTexture, false, m_camDebug, i);

                SaveSimulationTextures(false, i);
            }

            GUI.enabled = true;

            string lockButtonText = node.m_isLocked ? "Unlock" : "Lock";
            if (GUI.Button(new Rect(nodeRect.x + windowSize.x - 80, nodeRect.y + 5, 75, 30), lockButtonText, node.m_isLocked ? m_resources.m_lockButtonLockedStyle : m_resources.m_lockButtonUnlockedStyle))
            {
                node.m_isLocked = !node.m_isLocked;
            }

            // Header separator.
            for (int j = 0; j < 10; j++)
                EditorGUI.DrawRect(new Rect(nodeRect.x, nodeRect.y + 40 + ((float)j * 1), windowSize.x, 1), new Color(0.0f, 0.0f, 0.0f, 0.5f - ((j + 1) * 0.05f)));


            // Information.
            Rect fieldsRect = nodeRect;

            fieldsRect.x += 10;
            fieldsRect.y += 55;
            GUI.Label(fieldsRect, "Time: " + node.m_marker.time.ToString("F2"), m_resources.m_nodeFieldLabel);

            fieldsRect.y += 17.5f;

            if (node.m_marker.m_targets.Length > 1)
                GUI.Label(fieldsRect, "Target: Multiple", m_resources.m_nodeFieldLabel);
            else if (node.m_marker.m_targets.Length > 0)
                GUI.Label(fieldsRect, "Target: Single", m_resources.m_nodeFieldLabel);

            else
                GUI.Label(fieldsRect, "Target: None", m_resources.m_nodeFieldLabel);

            fieldsRect.y -= 17.5f;
            fieldsRect.x += windowSize.x - 170;
            GUI.Label(fieldsRect, "Dramatization: " + node.m_marker.m_dramatization.ToString("F2"), m_resources.m_nodeFieldLabel);

            fieldsRect.y += 17.5f;
            GUI.Label(fieldsRect, "Pace: " + node.m_marker.m_pace.ToString("F2"), m_resources.m_nodeFieldLabel);

            fieldsRect.y += 25;
            EditorGUI.DrawRect(new Rect(nodeRect.x, fieldsRect.y, windowSize.x, 1), new Color(0.0f, 0.0f, 0.0f, 0.5f));

            // Separator - Techniques
            fieldsRect.x = nodeRect.x + 10;
            fieldsRect.y += 10;
            GUI.Label(fieldsRect, "Positioning: " + node.m_positioningTechnique.m_implementation.GetType().ToString(), m_resources.m_nodeFieldLabel);

            fieldsRect.y += 17.5f;
            GUI.Label(fieldsRect, "Look: " + node.m_lookTechnique.m_implementation.GetType().ToString(), m_resources.m_nodeFieldLabel);

            fieldsRect.y -= 17.5f;
            fieldsRect.x += windowSize.x - 170;
            GUI.Label(fieldsRect, "Track: " + node.m_trackTechnique.m_implementation.GetType().ToString(), m_resources.m_nodeFieldLabel);

            fieldsRect.y += 17.5f;
            GUI.Label(fieldsRect, "FX: " + node.m_fxTechnique.m_implementation.GetType().ToString(), m_resources.m_nodeFieldLabel);

            // Seperator - Preview
            fieldsRect.x = nodeRect.x + 10;
            fieldsRect.y += 25;
            EditorGUI.DrawRect(new Rect(nodeRect.x, fieldsRect.y, windowSize.x, 1), new Color(0.0f, 0.0f, 0.0f, 0.5f));

            fieldsRect.y += 12;
            fieldsRect.x = nodeRect.x + windowSize.x / 2.0f - fieldsRect.width / 8.0f;
            GUI.Label(fieldsRect, "Shot Preview", m_resources.m_nodeFieldLabel);

            // Texture border.
            fieldsRect.y += 25;
            fieldsRect.x = nodeRect.x + 25;
            fieldsRect.width = textureSize.x;
            fieldsRect.height = textureSize.y;
            EditorGUI.DrawRect(fieldsRect, textureBorderColor);

            // Texture
            fieldsRect.x += 2.5f;
            fieldsRect.y += 2.5f;
            fieldsRect.width -= 5;
            fieldsRect.height -= 5;
            if (m_storyboardData.m_nodes[i].m_simulationData.m_snapshots[0] != null)
                GUI.DrawTexture(fieldsRect, m_storyboardData.m_nodes[i].m_simulationData.m_snapshots[0]);

            GUI.changed = true;
        }

        EditorZoomArea.End();

    }

    private void ProcessEvents(Event e)
    {
        if (e.type == EventType.MouseDown && m_seperatorResizeRect.Contains(e.mousePosition))
        {
            m_resizePanelSplit = true;
        }
        if (m_resizePanelSplit)
        {
            m_propertyPanelWidth = e.mousePosition.x;

            if (m_propertyPanelWidth < m_propertyPanelMinWidth)
                m_propertyPanelWidth = m_propertyPanelMinWidth;

            GUI.changed = true;
            //m_seperatorRect.Set(m_seperatorRect.x, e.mousePosition.x , m_seperatorRect.width, m_seperatorRect.height);
            //currentScrollViewHeight = Event.current.mousePosition.y;
            //cursorChangeRect.Set(cursorChangeRect.x, currentScrollViewHeight, cursorChangeRect.width, cursorChangeRect.height);
        }
        if (e.type == EventType.MouseUp)
            m_resizePanelSplit = false;

        // Allow adjusting the zoom with the mouse wheel as well. In this case, use the mouse coordinates
        // as the zoom center instead of the top left corner of the zoom area. This is achieved by
        // maintaining an origin that is used as offset when drawing any GUI elements in the zoom area.
        if (e.type == EventType.ScrollWheel)
        {
            Vector2 delta = e.delta;
            Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(e.mousePosition);
            float zoomDelta = -delta.y / 150.0f;
            float oldZoom = m_zoomScale;
            m_zoomScale += zoomDelta * m_zoomSensitivity;
            m_zoomScale = Mathf.Clamp(m_zoomScale, m_minZoom, m_maxZoom);
            m_zoomCoordsOrigin += (zoomCoordsMousePos - m_zoomCoordsOrigin) - (oldZoom / m_zoomScale) * (zoomCoordsMousePos - m_zoomCoordsOrigin);
            e.Use();
            GUI.changed = true;

        }

        if (e.type == EventType.MouseDrag)
        {
            if (Event.current.button == 1)
            {
                Vector2 delta = Event.current.delta;
                delta /= m_zoomScale;
                m_zoomCoordsOrigin -= delta;
                Event.current.Use();
            }
        }

    }

    private void CalculateDirectorDistributions()
    {
        if (m_directorDataTextAsset != null)
        {
            m_storyboardData.m_directorData = JsonUtility.FromJson<StoryboardDirectorData>(m_directorDataTextAsset.text);

            bool success = false;

            if (m_storyboardData.m_directorData != null)
            {
                m_storyboardData.m_directorData.CreateImplementationDetails(m_implementationResourcesPath, ref m_storyboardData.m_techniqueImplementations);
                success = m_storyboardData.m_directorData.CalculateProbabilityAndClassDistribution(ref m_storyboardData.m_techniqueImplementations);
            }

            m_directorDataValid = success && m_storyboardData.m_directorData != null && m_storyboardData.m_directorData.m_categories != null
                && m_storyboardData.m_directorData.m_categories.FindIndex(o => o.m_title.CompareTo("Positioning") == 0) > -1
                && m_storyboardData.m_directorData.m_categories.FindIndex(o => o.m_title.CompareTo("Look") == 0) > -1
                && m_storyboardData.m_directorData.m_categories.FindIndex(o => o.m_title.CompareTo("Track") == 0) > -1
                && m_storyboardData.m_directorData.m_categories.FindIndex(o => o.m_title.CompareTo("FX") == 0) > -1;

            CreateEditorsForImplementations();
        }
    }

    private Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
    {
        return (screenCoords - m_zoomArea.TopLeft()) / m_zoomScale + m_zoomCoordsOrigin;
    }

    public void OnProxyCalculationEnd()
    {
        m_displayProxyProgressBar = false;
#if UNITY_EDITOR

        if (Progress.Exists(m_proxyProgressID))
            Progress.Finish(m_proxyProgressID);
#endif
    }

    public void OnSimulationEnd()
    {

    }

    private void OnHierarchyWindowChanged()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (m_timeline == null)
            m_timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(m_lastTimelinePath);

        if (m_storyboardData == null)
            m_storyboardData = AssetDatabase.LoadAssetAtPath<StoryboardData>(m_lastStoryboardDataPath);

        if (m_sceneProxy == null)
            m_sceneProxy = GameObject.FindObjectOfType<StoryboardSceneProxy>();

        if (m_directorDataTextAsset == null)
            m_directorDataTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(m_lastDirectorThresholdsPath);

        if (m_renderTexture == null)
            m_renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(m_lastRenderTexturePath);

        if (m_playController == null)
            m_playController = FindObjectOfType<StoryboardPlayController>();

        if (m_playableDirector == null)
        {
            PlayableDirector[] directors = FindObjectsOfType<PlayableDirector>();

            for (int i = 0; i < directors.Length; i++)
            {
                if (directors[i].playableAsset == m_timeline)
                {
                    m_playableDirector = directors[i];
                    break;
                }
            }
        }

        if (m_camera == null)
        {
            StoryboardTargetCamera targetCam = FindObjectOfType<StoryboardTargetCamera>();

            if (targetCam != null && m_playController != null)
            {
                m_camera = targetCam.GetComponent<Camera>();
                m_playController.m_camera = m_camera;
            }
        }
        else if (m_camera.GetComponent<Camera>() == null)
            m_camera = null;

        if (m_storyboardData.m_techniqueImplementations == null)
        {
            m_storyboardData.m_directorData.CreateImplementationDetails(m_implementationResourcesPath, ref m_storyboardData.m_techniqueImplementations);
        }
    }

    private void CreateEditorsForImplementations()
    {
        if (m_storyboardData == null) return;

        for (int i = 0; i < m_storyboardData.m_techniqueImplementations.Count; i++)
        {
            if (m_storyboardData.m_techniqueImplementations[i] == null) continue;

            if (m_storyboardData.m_techniqueImplementations[i].m_editor == null)
            {
                m_storyboardData.m_techniqueImplementations[i].m_editor = Editor.CreateEditor(m_storyboardData.m_techniqueImplementations[i]);
            }
        }
    }

    private void ResetDefaults()
    {
        m_visualizeProxyCollisions = false;
        m_visualizeProxyBoundaries = false;
        m_visualizeEvaluateTimeline = false;
        m_proxySetToDraw = 0;
        m_proxyBounds = new Vector3(10, 10, 10);
        m_markersValid = false;
        m_showAdvancedSimSettings = false;
        m_selectedTab = 0;
        m_simulatedMarkers = 0;
        if (m_storyboardData != null && m_storyboardData.m_techniqueImplementations != null)
        {
            m_storyboardData.m_techniqueImplementations.Clear();
            m_storyboardData.m_techniqueImplementations = null;
        }
        m_directorDataValid = false;
        m_shouldDisableButtons = false;
        m_implementationResourcesPath = "";
    }

    private void SaveSimulationTextures(bool saveAll, int saveIndex = -1)
    {
        int iStart = saveAll ? 0 : saveIndex;
        int iEnd = saveAll ? m_storyboardData.m_nodes.Count : saveIndex + 1;

        for (int i = iStart; i < iEnd; i++)
        {
            if (m_storyboardData.m_nodes[i].m_isLocked) continue;

            for (int j = 0; j < m_storyboardData.m_nodes[i].m_simulationData.m_snapshots.Count; j++)
            {
                Texture2D snapshot = m_storyboardData.m_nodes[i].m_simulationData.m_snapshots[j];
                string assetPath = m_dataDumpPath + "/" + m_storyboardData.name + "_node_" + i.ToString() + "_" + j.ToString() + ".texture2d";


                if (System.IO.File.Exists(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.CreateAsset(snapshot, assetPath);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void DrawHorizontalNodeTransition(StoryboardNode node1, StoryboardNode node2)
    {
        Vector3[] points = new Vector3[2] {
           new Vector3(node1.m_rect.x + node1.m_rect.width, node1.m_rect.y + node1.m_rect.height / 2.0f, 0),
           new Vector3(node2.m_rect.x - 5.0f, node2.m_rect.y + node2.m_rect.height / 2.0f, 0),
        };

        Vector3[] pointsArrow1 = new Vector3[2] {
           points[1],
           new Vector3(points[1].x - 10, points[1].y - 10, 0),
        };


        Vector3[] pointsArrow2 = new Vector3[2] {
           points[1],
           new Vector3(points[1].x - 10, points[1].y + 10, 0),
        };

        Color lineColor = Color.white;
        ColorUtility.TryParseHtmlString("#A58961", out lineColor);
        Handles.color = lineColor;
        Handles.DrawAAPolyLine(3.0f, points);
        Handles.DrawAAPolyLine(3.0f, pointsArrow1);
        Handles.DrawAAPolyLine(3.0f, pointsArrow2);

        if (Application.isPlaying && m_storyboardData.m_transitionNode != null && m_storyboardData.m_transitionNode == node1)
        {
            Color transitionColor = Color.white;
            ColorUtility.TryParseHtmlString("#F5EBE0", out transitionColor);
            Handles.color = transitionColor;
            Vector3 target = Vector3.Lerp(points[0], points[1], s_transitionTimer);
            Handles.DrawLine(points[0], target);
            s_transitionTimer += Time.unscaledDeltaTime * 0.2f;
            if (s_transitionTimer > 1.0f)
            {
                s_transitionTimer = 0.0f;
            }
        }
    }

    private void DrawVerticalNodeTransition(StoryboardNode node1, StoryboardNode node2, float yAddition)
    {
        Vector3[] points = new Vector3[4]
        {
            new Vector3(node1.m_rect.x + node1.m_rect.width / 2.0f, node1.m_rect.y + node1.m_rect.height, 0),
            new Vector3(node1.m_rect.x + node1.m_rect.width / 2.0f, node1.m_rect.y + node1.m_rect.height + yAddition/2.0f, 0),
            new Vector3(node2.m_rect.x + node2.m_rect.width / 2.0f, node1.m_rect.y + node1.m_rect.height + yAddition/2.0f, 0),
            new Vector3(node2.m_rect.x + node2.m_rect.width / 2.0f, node2.m_rect.y, 0),
        };

        Vector3[] pointsArrow1 = new Vector3[2]
        {
            new Vector3(node2.m_rect.x + node2.m_rect.width / 2.0f, node2.m_rect.y, 0),
            new Vector3(node2.m_rect.x + node2.m_rect.width / 2.0f - 10, node2.m_rect.y - 10, 0),
        };


        Vector3[] pointsArrow2 = new Vector3[2]
        {
            new Vector3(node2.m_rect.x + node2.m_rect.width / 2.0f, node2.m_rect.y, 0),
            new Vector3(node2.m_rect.x + node2.m_rect.width / 2.0f + 10, node2.m_rect.y - 10, 0),
        };

        Color lineColor = Color.white;
        ColorUtility.TryParseHtmlString("#A58961", out lineColor);
        Handles.color = lineColor;
        Handles.DrawAAPolyLine(3.0f, points);
        Handles.DrawAAPolyLine(3.0f, pointsArrow1);
        Handles.DrawAAPolyLine(3.0f, pointsArrow2);
    }

    private IEnumerator DisplaySimulationProgress()
    {
#if UNITY_EDITOR

        m_simulationProgressID = Progress.Start("Simulating...", "Simulation", Progress.Options.None);
        float progress = 0.0f;

        while (progress < 1.0f)
        {
            progress = (float)m_simulatedMarkers / (float)m_markerCount;
            Progress.Report(m_simulationProgressID, progress);
            yield return null;
        }

        Progress.Remove(m_simulationProgressID);

#endif

        yield return null;
    }

}
