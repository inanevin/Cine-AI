using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Cut : CinematographyTechniqueImplementation
{

    public override void Play(Camera cam, StoryboardNode node, Transform camManipulator)
    {

    }

    public override void Stop(Camera cam)
    {

    }
    public override bool Simulate(StoryboardData data, StoryboardNode currentNode, StoryboardNode nextNode, SimulationTargetData targetData)
    {
        return false;

    }
}

