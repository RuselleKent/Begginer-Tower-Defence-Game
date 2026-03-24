#if UNITY_EDITOR
using UnityEditor; // kailangan para sa Handles.Label (pang-display ng text sa editor)
#endif

using UnityEngine;

public class Path : MonoBehaviour
{
    public GameObject[] Waypoints; // array ng mga waypoint objects (kung saan dadaan yung mga kalaban)

    public Vector3 GetPosition(int index)
    {
        if (Waypoints == null || Waypoints.Length == 0) // kung walang waypoints o walang laman
        {
            Debug.LogError("Path: Waypoints array is null or empty!"); // mag-error
            return Vector3.zero; // return zero
        }

        if (index < 0 || index >= Waypoints.Length) // kung negative yung index o lagpas sa dami ng waypoints
        {
            Debug.LogError($"Path: Index {index} is out of bounds! Waypoints length: {Waypoints.Length}"); // mag-error
            return Vector3.zero; // return zero
        }

        if (Waypoints[index] == null) // kung yung waypoint sa index na yun ay null
        {
            Debug.LogError($"Path: Waypoint at index {index} is null!"); // mag-error
            return Vector3.zero; // return zero
        }

        return Waypoints[index].transform.position; // ibalik yung position ng waypoint
    }

    private void OnDrawGizmos()
    {
        if (Waypoints == null || Waypoints.Length == 0) // kung walang waypoints
            return; // wag mag-drawing

        for (int i = 0; i < Waypoints.Length; i++) // dumaan sa bawat waypoint
        {
            if (Waypoints[i] == null) // kung yung waypoint ay null
                continue; // skip

            #if UNITY_EDITOR
            GUIStyle style = new GUIStyle(); // gumawa ng style para sa label
            style.normal.textColor = Color.white; // kulay puti yung text
            style.alignment = TextAnchor.MiddleCenter; // i-center yung text
            Handles.Label(Waypoints[i].transform.position + Vector3.up * 0.7f, Waypoints[i].name, style); // magpakita ng label sa taas ng waypoint (pangalan nung waypoint)
            #endif

            if (i < Waypoints.Length - 1 && Waypoints[i + 1] != null) // kung hindi pa last waypoint at may next waypoint
            {
                Gizmos.color = Color.gray; // kulay gray yung linya
                Gizmos.DrawLine(Waypoints[i].transform.position, Waypoints[i + 1].transform.position); // mag-drawing ng linya mula current waypoint papuntang next waypoint
            }
        }
    }
}