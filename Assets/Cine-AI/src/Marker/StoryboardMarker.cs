using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Defines the interface for markers used to mark camera shots in the timeline.
/// This is only used for defining the shots on the timeline, the storyboard receives the
/// marker data & handles everything else by itself on simulation.
/// </summary>
public class StoryboardMarker : Marker, IMarker, INotification, INotificationOptionProvider
{
    [SerializeField] private bool retroactive = false;
    [SerializeField] private bool emitOnce = false;

    public string[] m_targets;
    public bool m_jumpsToTime = false;
    public double m_jumpTime = 0.0;
    public int m_targetFlag = 0;
    public float m_dramatization = 0.5f;
    public float m_pace = 0.5f;

    public PropertyName id => new PropertyName();

    public NotificationFlags flags => (retroactive ? NotificationFlags.Retroactive : default) | (emitOnce ? NotificationFlags.TriggerOnce : default);

}
