#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

public class Path : MonoBehaviour
{
    public GameObject[] Waypoints;

    public Vector3 GetPosition(int index)
    {
        if (Waypoints == null || Waypoints.Length == 0)
        {
            Debug.LogError("Path: Waypoints array is null or empty!");
            return Vector3.zero;
        }

        if (index < 0 || index >= Waypoints.Length)
        {
            Debug.LogError($"Path: Index {index} is out of bounds! Waypoints length: {Waypoints.Length}");
            return Vector3.zero;
        }

        if (Waypoints[index] == null)
        {
            Debug.LogError($"Path: Waypoint at index {index} is null!");
            return Vector3.zero;
        }

        return Waypoints[index].transform.position;
    }

    private void OnDrawGizmos()
    {
        if (Waypoints == null || Waypoints.Length == 0)
            return;

        for (int i = 0; i < Waypoints.Length; i++)
        {
            if (Waypoints[i] == null)
                continue;

            #if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            Handles.Label(Waypoints[i].transform.position + Vector3.up * 0.7f, Waypoints[i].name, style);
            #endif

            if (i < Waypoints.Length - 1 && Waypoints[i + 1] != null)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(Waypoints[i].transform.position, Waypoints[i + 1].transform.position);
            }
        }
    }
}
