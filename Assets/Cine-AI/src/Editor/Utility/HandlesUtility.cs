using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class HandlesUtility
{
    public static void DrawCubeOutlined(Vector3 position, Vector3 size, Color faceColor, Color outlineColor)
    {
        Vector3[] down = new Vector3[] {
                                 position + new Vector3(-size.x / 2.0f, -size.y / 2.0f, -size.z / 2.0f),
                                 position + new Vector3(-size.x / 2.0f, -size.y / 2.0f, size.z / 2.0f),
                                 position + new Vector3(size.x / 2.0f, -size.y / 2.0f, size.z / 2.0f),
                                 position + new Vector3(size.x / 2.0f, -size.y / 2.0f, -size.z / 2.0f)
                                 };

        Vector3[] up = new Vector3[4] {
                                 position + new Vector3(-size.x / 2.0f, size.y / 2.0f, -size.z / 2.0f),
                                 position + new Vector3(-size.x / 2.0f, size.y / 2.0f, size.z / 2.0f),
                                 position + new Vector3(size.x / 2.0f, size.y / 2.0f, size.z / 2.0f),
                                 position + new Vector3(size.x / 2.0f, size.y / 2.0f, -size.z / 2.0f),
                                 };

        Vector3[] left = new Vector3[4] {
                                 position + new Vector3(-size.x / 2.0f, size.y / 2.0f, -size.z / 2.0f),
                                 position + new Vector3(-size.x / 2.0f, size.y / 2.0f, size.z / 2.0f),
                                   position + new Vector3(-size.x / 2.0f, -size.y / 2.0f, size.z / 2.0f),
                                 position + new Vector3(-size.x / 2.0f, -size.y / 2.0f, -size.z / 2.0f)
                                 };

        Vector3[] right = new Vector3[4] {
                                 position + new Vector3(size.x / 2.0f, size.y / 2.0f, -size.z / 2.0f),
                                 position + new Vector3(size.x / 2.0f, size.y / 2.0f, size.z / 2.0f),
                                   position + new Vector3(size.x / 2.0f, -size.y / 2.0f, size.z / 2.0f),
                                 position + new Vector3(size.x / 2.0f, -size.y / 2.0f, -size.z / 2.0f)
                                 };

        Vector3[] front = new Vector3[4] {
                                 position + new Vector3(-size.x / 2.0f, size.y / 2.0f, size.z / 2.0f),
                                 position + new Vector3(size.x / 2.0f, size.y / 2.0f, size.z / 2.0f),
                                   position + new Vector3(size.x / 2.0f, -size.y / 2.0f, size.z / 2.0f),
                                 position + new Vector3(-size.x / 2.0f, -size.y / 2.0f, size.z / 2.0f)
                                 };

        Vector3[] back = new Vector3[4] {
                                 position + new Vector3(-size.x / 2.0f, size.y / 2.0f, -size.z / 2.0f),
                                 position + new Vector3(size.x / 2.0f, size.y / 2.0f, -size.z / 2.0f),
                                   position + new Vector3(size.x / 2.0f, -size.y / 2.0f, -size.z / 2.0f),
                                 position + new Vector3(-size.x / 2.0f, -size.y / 2.0f, -size.z / 2.0f)
                                 };
        Handles.DrawSolidRectangleWithOutline(down, faceColor, outlineColor);
        Handles.DrawSolidRectangleWithOutline(up, faceColor, outlineColor);
        Handles.DrawSolidRectangleWithOutline(left, faceColor, outlineColor);
        Handles.DrawSolidRectangleWithOutline(right, faceColor, outlineColor);
        Handles.DrawSolidRectangleWithOutline(front, faceColor, outlineColor);
        Handles.DrawSolidRectangleWithOutline(back, faceColor, outlineColor);
    }

}
