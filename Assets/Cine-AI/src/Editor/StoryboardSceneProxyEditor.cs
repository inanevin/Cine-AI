using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StoryboardSceneProxy))]
public class StoryboardSceneProxyEditor : Editor
{
    private StoryboardSceneProxy data;

    private void OnEnable()
    {
        data = (StoryboardSceneProxy)target;
    }

    public override void OnInspectorGUI()
    {

        StoryboardSceneProxy[] proxies = FindObjectsOfType<StoryboardSceneProxy>();
        if(proxies.Length > 1)
        {
            EditorGUILayout.HelpBox("There can only be a single scene proxy object per scene! Please remove others.", MessageType.Error);
            return;
        }

        serializedObject.Update();
        serializedObject.ApplyModifiedProperties();
    }

}