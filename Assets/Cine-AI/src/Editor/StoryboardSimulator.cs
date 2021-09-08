using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Linq;
using Unity.EditorCoroutines.Editor;


/// <summary>
/// Responsible for simulating timeline.
/// </summary>
public class StoryboardSimulator
{
    public const int m_iterationTimeout = 10000;

    public bool FillStoryboardNodes(StoryboardData storyboardData, TimelineAsset timeline, string dataDumpPath)
    {

        // Save locked nodes.
        List<StoryboardNode> lockedNodes = new List<StoryboardNode>();
        for (int i = 0; i < storyboardData.m_nodes.Count; i++)
        {
            StoryboardNode currentNode = storyboardData.m_nodes[i];

            if (currentNode.m_isLocked)
                lockedNodes.Add(currentNode);
            else
            {
                for (int j = 0; j < currentNode.m_simulationData.m_snapshots.Count; j++)
                {
                    string path = AssetDatabase.GetAssetPath(currentNode.m_simulationData.m_snapshots[j]);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();

                }
            }

        }

        // First fill the nodes with markers.
        storyboardData.m_nodes.Clear();
        IEnumerable<IMarker> markers = timeline.markerTrack.GetMarkers();
        markers = markers.OrderBy(a => a.time);
        IEnumerator markersIt = markers.GetEnumerator();
        int index = 0;
        while (markersIt.MoveNext())
        {
            if (markersIt.Current is StoryboardMarker marker)
            {
                int foundIndex = lockedNodes.FindIndex(o => o.m_marker == marker);

                if (foundIndex > -1)
                {
                    if (lockedNodes[foundIndex].m_index != index)
                    {
                        for (int j = 0; j < lockedNodes[foundIndex].m_simulationData.m_snapshots.Count; j++)
                        {
                            string assetPath = dataDumpPath + "/" + storyboardData.name + "_node_" + lockedNodes[foundIndex].m_index.ToString() + "_" + j.ToString() + ".texture2d";
                            string newPath = storyboardData.name + "_node_" + index.ToString() + "_" + j.ToString() + ".texture2d";

                            string msg = AssetDatabase.RenameAsset(assetPath, newPath);
                            AssetDatabase.SaveAssets();
                        }
                    }

                    lockedNodes[foundIndex].m_index = index;
                    storyboardData.m_nodes.Add(lockedNodes[foundIndex]);
                }
                else
                {
                    storyboardData.m_nodes.Add(new StoryboardNode());
                    StoryboardNode node = storyboardData.m_nodes.Last();
                    node.m_marker = marker;
                    node.m_index = index;
                }

                index++;
            }
        }

        return storyboardData.m_nodes.Count != 0;
    }



    public bool CheckMarkerValidity(TimelineAsset timeline)
    {
        if (timeline == null || timeline.markerTrack == null) return false;

        IEnumerable<IMarker> markers = timeline.markerTrack.GetMarkers();
        markers = markers.OrderBy(a => a.time);
        IEnumerator markersIt = markers.GetEnumerator();
        bool checkForTimeZero = true;
        bool markerWithoutATarget = false;

        while (markersIt.MoveNext())
        {
            if (markersIt.Current is StoryboardMarker marker)
            {
                if (checkForTimeZero)
                {
                    if (marker.time == 0.0)
                    {
                        checkForTimeZero = false;
                    }
                }

                if (marker.m_targets == null || marker.m_targets.Length == 0)
                {
                    markerWithoutATarget = true;
                    break;
                }
            }
        }
        return !markerWithoutATarget && !checkForTimeZero;
    }

    public int GetTimelineMarkerCount(TimelineAsset timeline)
    {
        IEnumerable<IMarker> markers = timeline.markerTrack.GetMarkers();
        return markers.Where(o => o is StoryboardMarker).ToList().Count;
    }

    public IEnumerator CalculateSceneProxies(StoryboardWindow window, StoryboardData data, TimelineAsset timeline, PlayableDirector playable, Vector3 position, Vector3 sceneProxyBounds)
    {
        data.m_proxySets.Clear();

        IEnumerable<IMarker> markers = timeline.markerTrack.GetMarkers();
        markers = markers.OrderBy(a => a.time);
        IEnumerator markersIt = markers.GetEnumerator();

        while (markersIt.MoveNext())
        {
            if (markersIt.Current is StoryboardMarker marker)
            {
                playable.time = marker.m_jumpsToTime ? marker.m_jumpTime : marker.time;
                playable.Evaluate();

                yield return null;

                ProxySet set = new ProxySet();
                set.m_proxies = new List<Proxy>();



                Collider[] colliders = Physics.OverlapBox(position, sceneProxyBounds / 2.0f);

                for (int i = 0; i < colliders.Length; i++)
                {
                    Proxy proxy = new Proxy();
                    Collider col = colliders[i];
                    proxy.m_collider = colliders[i];
                    proxy.m_gameObjectID = proxy.m_collider.gameObject.GetInstanceID().ToString();
                    set.m_proxies.Add(proxy);

                }

                yield return null;
                set.m_marker = marker;
                data.m_proxySets.Add(set);
            }
        }
        playable.time = 0.0;
        playable.Evaluate();
        window.OnProxyCalculationEnd();

    }

    IEnumerator SimRoutine(StoryboardWindow window, Transform cameraTransform, StoryboardData data, TimelineAsset timeline, PlayableDirector playable,
        int simMarkDummy, string debugPositioning, string debugLook, string debugTrack, string debugFX, RenderTexture rt, bool simulateAll,
        bool camPosDebug, int simulatedIndex = -1)
    {
        StoryboardNode previousNode = simulateAll ? null : (simulatedIndex == 0 ? null : data.m_nodes[simulatedIndex - 1]);

        int iStart = simulateAll ? 0 : simulatedIndex;
        int iEnd = simulateAll ? data.m_nodes.Count : simulatedIndex + 1;
        int simulatedMarkerCount = 0;

        for (int i = iStart; i < iEnd; i++)
        {
            StoryboardNode currentNode = data.m_nodes[i];

            if (currentNode.m_isLocked)
            {
                previousNode = currentNode;
                simulatedMarkerCount++;
                continue;
            }

            playable.time = currentNode.m_marker.time;
            playable.Evaluate();

            // Calculate target position
            SimulationTargetData targetData = new SimulationTargetData();
            targetData.m_target = null;
            targetData.m_targetForward = Vector3.forward;
            targetData.m_targetRight = Vector3.right;
            targetData.m_targetUp = Vector3.up;
            targetData.m_targetPosition = Vector3.zero;

            for (int j = 0; j < currentNode.m_marker.m_targets.Length; j++)
            {
                var objects = Resources.FindObjectsOfTypeAll<StoryboardTarget>().Where(obj => obj.gameObject.name == currentNode.m_marker.m_targets[j]);

                if (objects.Count<StoryboardTarget>() > 1)
                {
                    Debug.LogError("Multiple storyboard targets with the same name is found. Please make sure each target has a unique name to avoid any confusions.");
                    currentNode.m_simulationData = null;
                    //data.m_nodes.Clear();
                    yield break;
                }

                GameObject go = GameObject.Find(currentNode.m_marker.m_targets[j]);

                if (go == null)
                {
                    Debug.LogError("The target " + currentNode.m_marker.m_targets[j] + " could not be found, aborting simulation.");
                    currentNode.m_simulationData = null;
                    //data.m_nodes.Clear();
                    yield break;
                }

                targetData.m_targetPosition += go.transform.position;

                if (currentNode.m_marker.m_targets.Length == 1)
                {
                    targetData.m_target = go.transform;
                    targetData.m_targetPosition = targetData.m_target.position;
                    targetData.m_targetForward = targetData.m_target.forward;
                    targetData.m_targetRight = targetData.m_target.right;
                    targetData.m_targetUp = targetData.m_target.up;
                }
            }

            // Finalize target position as mid point.
            targetData.m_targetPosition /= currentNode.m_marker.m_targets.Length;

            StoryboardDirectorData dirData = data.m_directorData;
            DecisionTechniquePreference pref = data.m_decisionTechnique;
            float dramThresh = data.m_dramatizationThreshold;
            float paceThresh = data.m_paceThreshold;
            bool useFX = data.m_useFX;
            bool simulationSuccessful = false;
            int timeoutCounter = 0;
            StoryboardNode nextNode = i < data.m_nodes.Count - 1 ? data.m_nodes[i + 1] : null;

            while (!simulationSuccessful && timeoutCounter < data.m_techniqueTimeout)
            {
                // if (debugPositioning != "")
                //     currentNode.m_positioningTechnique = data.m_directorData.m_categories[0].m_techniques.Find(o => o.m_title.CompareTo(debugPositioning) == 0);
                // else


                currentNode.m_positioningTechnique = GetTechnique("Positioning", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
                currentNode.m_simulationData = new SimulationData();
                simulationSuccessful = currentNode.m_positioningTechnique.m_implementation.Simulate(data, currentNode, nextNode, targetData);
                timeoutCounter++;
            }


            // Set simulation's target data.
            currentNode.m_simulationData.m_targetData = targetData;

            // if (camPosDebug)
            // {
            //     currentNode.m_simulationData.m_cameraPosition = Camera.main.transform.position;
            //     currentNode.m_simulationData.m_cameraRotation = Camera.main.transform.rotation;
            // }
            // currentNode.m_lookTechnique = data.m_directorData.m_categories[1].m_techniques.Find(o => o.m_title.CompareTo("QuickZoom") == 0);
            // currentNode.m_trackTechnique = data.m_directorData.m_categories[2].m_techniques.Find(o => o.m_title.CompareTo("Cut") == 0);

            currentNode.m_lookTechnique = GetTechnique("Look", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
            currentNode.m_trackTechnique = GetTechnique("Track", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
            currentNode.m_fxTechnique = GetTechnique("FX", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);

            // D E B U G

            // if (debugLook != "")
            //     currentNode.m_lookTechnique = data.m_directorData.m_categories[1].m_techniques.Find(o => o.m_title.CompareTo(debugLook) == 0);
            // else
            //     currentNode.m_lookTechnique = GetTechnique("Look", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
            // if (debugTrack != "")
            //     currentNode.m_trackTechnique = data.m_directorData.m_categories[2].m_techniques.Find(o => o.m_title.CompareTo(debugTrack) == 0);
            // else
            //     currentNode.m_trackTechnique = GetTechnique("Track", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
            // if (debugFX != "")
            //     currentNode.m_fxTechnique = data.m_directorData.m_categories[3].m_techniques.Find(o => o.m_title.CompareTo(debugFX) == 0);
            // else
            //     currentNode.m_fxTechnique = GetTechnique("FX", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);

            ApplySimulationPropertiesToCamera(currentNode.m_simulationData, cameraTransform);
            currentNode.m_simulationData.m_snapshots.Add(TakeCameraSnapshot(cameraTransform.GetComponent<Camera>(), rt));
            yield return new WaitForEndOfFrame();

            previousNode = currentNode;
            simulatedMarkerCount++;
            targetData = null;
        }

        // Make sure we reset back the playable.
        playable.time = 0.0;
        playable.Evaluate();
        window.OnSimulationEnd();

    }
    public bool Simulate(StoryboardWindow window, Transform cameraTransform, StoryboardData data, TimelineAsset timeline, PlayableDirector playable,
        ref int simulatedMarkerCount, string debugPositioning, string debugLook, string debugTrack, string debugFX, RenderTexture rt, bool simulateAll,
        bool camPosDebug, int simulatedIndex = -1)
    {

        StoryboardNode previousNode = simulateAll ? null : (simulatedIndex == 0 ? null : data.m_nodes[simulatedIndex - 1]);

        int iStart = simulateAll ? 0 : simulatedIndex;
        int iEnd = simulateAll ? data.m_nodes.Count : simulatedIndex + 1;

        for (int i = iStart; i < iEnd; i++)
        {
            StoryboardNode currentNode = data.m_nodes[i];

            if (currentNode.m_isLocked)
            {
                previousNode = currentNode;
                simulatedMarkerCount++;
                continue;
            }

            playable.time = currentNode.m_marker.time;
            playable.Evaluate();

            // Calculate target position
            SimulationTargetData targetData = new SimulationTargetData();
            targetData.m_target = null;
            targetData.m_targetForward = Vector3.forward;
            targetData.m_targetRight = Vector3.right;
            targetData.m_targetUp = Vector3.up;
            targetData.m_targetPosition = Vector3.zero;

            for (int j = 0; j < currentNode.m_marker.m_targets.Length; j++)
            {
                var objects = Resources.FindObjectsOfTypeAll<StoryboardTarget>().Where(obj => obj.gameObject.name == currentNode.m_marker.m_targets[j]);

                if (objects.Count<StoryboardTarget>() > 1)
                {
                    Debug.LogError("Multiple storyboard targets with the same name is found. Please make sure each target has a unique name to avoid any confusions.");
                    currentNode.m_simulationData = null;
                    //data.m_nodes.Clear();
                    return false;
                }

                GameObject go = GameObject.Find(currentNode.m_marker.m_targets[j]);

                if (go == null)
                {
                    Debug.LogError("The target " + currentNode.m_marker.m_targets[j] + " could not be found, aborting simulation.");
                    currentNode.m_simulationData = null;
                    //data.m_nodes.Clear();
                    return false;
                }

                targetData.m_targetPosition += go.transform.position;

                if (currentNode.m_marker.m_targets.Length == 1)
                {
                    targetData.m_target = go.transform;
                    targetData.m_targetPosition = targetData.m_target.position;
                    targetData.m_targetForward = targetData.m_target.forward;
                    targetData.m_targetRight = targetData.m_target.right;
                    targetData.m_targetUp = targetData.m_target.up;
                }
            }

            // Finalize target position as mid point.
            targetData.m_targetPosition /= currentNode.m_marker.m_targets.Length;

            StoryboardDirectorData dirData = data.m_directorData;
            DecisionTechniquePreference pref = data.m_decisionTechnique;
            float dramThresh = data.m_dramatizationThreshold;
            float paceThresh = data.m_paceThreshold;
            bool useFX = data.m_useFX;
            bool simulationSuccessful = false;
            int timeoutCounter = 0;
            StoryboardNode nextNode = i < data.m_nodes.Count - 1 ? data.m_nodes[i + 1] : null;

            while (!simulationSuccessful && timeoutCounter < data.m_techniqueTimeout)
            {
                // if (debugPositioning != "")
                //     currentNode.m_positioningTechnique = data.m_directorData.m_categories[0].m_techniques.Find(o => o.m_title.CompareTo(debugPositioning) == 0);
                // else


                currentNode.m_positioningTechnique = GetTechnique("Positioning", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
                currentNode.m_simulationData = new SimulationData();
                simulationSuccessful = currentNode.m_positioningTechnique.m_implementation.Simulate(data, currentNode, nextNode, targetData);
                timeoutCounter++;
            }


            // Set simulation's target data.
            currentNode.m_simulationData.m_targetData = targetData;

            // if (camPosDebug)
            // {
            //     currentNode.m_simulationData.m_cameraPosition = Camera.main.transform.position;
            //     currentNode.m_simulationData.m_cameraRotation = Camera.main.transform.rotation;
            // }
            // currentNode.m_lookTechnique = data.m_directorData.m_categories[1].m_techniques.Find(o => o.m_title.CompareTo("QuickZoom") == 0);
            // currentNode.m_trackTechnique = data.m_directorData.m_categories[2].m_techniques.Find(o => o.m_title.CompareTo("Cut") == 0);

            currentNode.m_lookTechnique = GetTechnique("Look", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
            currentNode.m_trackTechnique = GetTechnique("Track", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
            currentNode.m_fxTechnique = GetTechnique("FX", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);

            // D E B U G

            // if (debugLook != "")
            //     currentNode.m_lookTechnique = data.m_directorData.m_categories[1].m_techniques.Find(o => o.m_title.CompareTo(debugLook) == 0);
            // else
            //     currentNode.m_lookTechnique = GetTechnique("Look", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
            // if (debugTrack != "")
            //     currentNode.m_trackTechnique = data.m_directorData.m_categories[2].m_techniques.Find(o => o.m_title.CompareTo(debugTrack) == 0);
            // else
            //     currentNode.m_trackTechnique = GetTechnique("Track", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);
            // if (debugFX != "")
            //     currentNode.m_fxTechnique = data.m_directorData.m_categories[3].m_techniques.Find(o => o.m_title.CompareTo(debugFX) == 0);
            // else
            //     currentNode.m_fxTechnique = GetTechnique("FX", previousNode, currentNode, dirData, pref, dramThresh, paceThresh, useFX);

            ApplySimulationPropertiesToCamera(currentNode.m_simulationData, cameraTransform);
            currentNode.m_simulationData.m_snapshots.Add(TakeCameraSnapshot(cameraTransform.GetComponent<Camera>(), rt));

            previousNode = currentNode;
            simulatedMarkerCount++;
            targetData = null;
        }

        // Make sure we reset back the playable.
        playable.time = 0.0;
        playable.Evaluate();
        window.OnSimulationEnd();
        return true;
    }

    private CinematographyTechnique GetTechnique(string categoryID, StoryboardNode previousNode, StoryboardNode node, StoryboardDirectorData dirData, DecisionTechniquePreference pref, float dramThresh, float paceThresh, bool useFX)
    {
        int selectedIndex = -1;
        int it = 0;

        CinematographyTechniqueCategory category = dirData.m_categories.Find(o => o.m_title.CompareTo(categoryID) == 0);
        List<CinematographyTechnique> dataList = new List<CinematographyTechnique>(category.m_techniques);
        CinematographyTechnique defaultTechnique = category.m_defaultTechnique;

        if (categoryID.CompareTo("FX") == 0 && !useFX)
            return dataList.Find(o => o.m_title.CompareTo("NoFX") == 0);


        // Eliminate some techniques from the data list based on the previous node's technique information if necessary.
        if (previousNode != null)
            ApplyShotBasedRuleset(categoryID, ref dataList, previousNode);

        // Eliminate some techniques based on the current node's categories.
        ApplyNodeBasedRuleset(categoryID, ref dataList, node);

        // After we've selected the closest random technique, compare it's dramatization & pace.
        while (it < m_iterationTimeout)
        {


            // Select technique.
            if (pref == DecisionTechniquePreference.ProbabilityDistribution)
                selectedIndex = MathUtility.GetCumulativeDistribution(dataList.Select(o => o.m_probabilityDistribution).ToArray());
            else if (pref == DecisionTechniquePreference.ExponentialDistribution)
                selectedIndex = MathUtility.GetCumulativeDistribution(dataList.Select(o => o.m_classDistribution).ToArray());

            if (selectedIndex != -1)
            {
                // Check dramatization & pace thresholds for the selected technique.
                CinematographyTechnique techniqueData = dataList[selectedIndex];
                bool dramatizationChecks = dramThresh == 1.0f || (node.m_marker.m_dramatization > techniqueData.m_dramatization && node.m_marker.m_dramatization - techniqueData.m_dramatization < dramThresh);
                bool paceChecks = paceThresh == 1.0f || (node.m_marker.m_pace > techniqueData.m_pace && node.m_marker.m_pace - techniqueData.m_pace < dramThresh);

                if (dramatizationChecks && paceChecks)
                    return techniqueData;

            }

            it++;
        }
        dataList = null;
        return defaultTechnique;
    }

    private void ApplyShotBasedRuleset(string categoryID, ref List<CinematographyTechnique> dataList, StoryboardNode previousNode)
    {
        if (categoryID.CompareTo("Positioning") == 0)
        {

        }
        else if (categoryID.CompareTo("Look") == 0)
        {
            if (previousNode.m_lookTechnique.m_title.CompareTo("DollyZoom") == 0)
            {
                dataList.RemoveAll(o => o.m_title.CompareTo("DollyZoom") == 0);
            }
            else if (previousNode.m_lookTechnique.m_title.CompareTo("QuickZoom") == 0)
            {
                dataList.RemoveAll(o => o.m_title.CompareTo("QuickZoom") == 0);
            }
        }
        else if (categoryID.CompareTo("Track") == 0)
        {

        }
        else if (categoryID.CompareTo("FX") == 0)
        {
            if (previousNode.m_fxTechnique.m_title.CompareTo("SlowMotion") == 0)
            {
                dataList.RemoveAll(o => o.m_title.CompareTo("SlowMotion") == 0);
            }
        }
    }

    private void ApplyNodeBasedRuleset(string categoryID, ref List<CinematographyTechnique> dataList, StoryboardNode currentNode)
    {
        if (categoryID.CompareTo("Positioning") == 0)
        {

        }
        else if (categoryID.CompareTo("Look") == 0)
        {

        }
        else if (categoryID.CompareTo("Track") == 0)
        {
            if (currentNode.m_lookTechnique.m_title.CompareTo("DollyZoom") == 0)
            {
                dataList.RemoveAll(o => o.m_title.CompareTo("PositionHandheld") == 0 || o.m_title.CompareTo("PositionSteadycam") == 0
                || o.m_title.CompareTo("PosRotHandheld") == 0 || o.m_title.CompareTo("PosRotHandheld") == 0);
            }
        }
        else if (categoryID.CompareTo("FX") == 0)
        {

        }
    }

    public static void ApplySimulationPropertiesToCamera(SimulationData simData, Transform cameraTransform)
    {
        cameraTransform.position = simData.m_cameraPosition;
        cameraTransform.rotation = simData.m_cameraRotation;
        cameraTransform.GetComponent<Camera>().fieldOfView = simData.m_cameraFOV;
    }

    public static Texture2D TakeCameraSnapshot(Camera camera, RenderTexture rt)
    {
        camera.Render();

        // Set target texture
        camera.targetTexture = rt;

        // The Render Texture in RenderTexture.active is the one
        // that will be read by ReadPixels.
        var currentRT = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;

        // Render the camera's view.
        camera.Render();

        // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGBA32, true, true);
        image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
        image.Apply();

        // Replace the original active Render Texture.
        RenderTexture.active = currentRT;

        camera.targetTexture = null;

        return image;
    }
}

