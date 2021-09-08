using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class MathUtility 
{
    public static int GetCumulativeDistribution(float[] weights)
    {
        float random = Random.Range(0, 1.0f);

        for(int i = 0; i < weights.Length; i++)
        {
            if (random < weights[i])
                return i;

            random -= weights[i];
        }

        return -1;
    }

    public static float MaxComponent(this Vector3 v)
    {
        Vector3 absV = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        return Mathf.Max(absV.x, Mathf.Max(absV.y, absV.z));
    }

    public static Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t, Vector3 outDirection)
    {
        float parabolicT = t * 2 - 1;
        //start and end are not level, gets more complicated
        Vector3 travelDirection = end - start;
        Vector3 levelDirection = end - new Vector3(start.x, end.y, start.z);
        Vector3 right = Vector3.Cross(travelDirection, levelDirection);
        Vector3 up = outDirection;
        Vector3 result = start + t * travelDirection;
        result += ((-parabolicT * parabolicT + 1) * height) * up.normalized;
        return result;
    }
}
