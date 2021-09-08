using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the attached game object's transform as a possible target for storyboard shots,
/// which then can be selected via storyboard markers.
/// </summary>
public class StoryboardTarget : MonoBehaviour
{

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawIcon(transform.position, "StoryboardTarget.png", true);
    }
#endif
}
