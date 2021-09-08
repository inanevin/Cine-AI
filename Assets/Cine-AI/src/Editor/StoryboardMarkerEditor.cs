using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;



[CustomEditor(typeof(StoryboardMarker))]
[CanEditMultipleObjects]
public class StoryboardMarkerEditor : Editor
{
    private StoryboardMarker data;
 

    private void OnEnable()
    {
        data = (StoryboardMarker)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Time: ", data.time.ToString());

        StoryboardTarget[] targets = FindObjectsOfType<StoryboardTarget>();

        string[] options = new string[targets.Length];

        for (int i = 0; i < targets.Length; i++)
            options[i] = targets[i].gameObject.name;

        if(options.Length > 0)
        data.m_targetFlag = EditorGUILayout.MaskField("Targets", data.m_targetFlag, options);

        List<string> targetsToAdd = new List<string>();
        for(int i = 0; i < options.Length; i++)
        {
            if((data.m_targetFlag & (1 << i)) != 0)
            {
               targetsToAdd.Add(targets[i].gameObject.name);
            }
        }

        data.m_targets = new string[targetsToAdd.Count];
        data.m_targets = targetsToAdd.ToArray();

        data.m_jumpsToTime = EditorGUILayout.Toggle("Jumps To Time?", data.m_jumpsToTime);

        if(data.m_jumpsToTime)
        {
            data.m_jumpTime = EditorGUILayout.DoubleField("Jump Time", data.m_jumpTime);
        }

        data.m_dramatization = EditorGUILayout.Slider(new GUIContent("Dramatization", "Defines how dramatic this point in the cut-scene is. Used to compare with director thresholds"), data.m_dramatization, 0.0f, 1.0f);
        data.m_pace = EditorGUILayout.Slider(new GUIContent("Pace", "Defines how fast this point in the cut-scene is. Used to compare with director thresholds"), data.m_pace, 0.0f, 1.0f);

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(data);
    }
}