using UnityEngine;
using UnityEditor;
using Scripts.Diagnostics;

[CustomEditor(typeof(SceneDiagnotics))]
class SceneDiagnosticsEditor : Editor
{
    // SceneView sv = //get your scene view as usual;
    // GUIStyle st = (GUIStyle)"GV Gizmo DropDown";  
    // Vector2 ribbon = st.CalcSize(sv.titleContent);

    // Vector2 sv_correctSize = sv.position.size;
    // sv_correctSize.y -= ribbon.y; //exclude this nasty ribbon

    // //flip the position:
    // Vector2 mousePosFlipped = Event.current.mousePosition;
    // mousePosFlipped.y = sv_correctSize.y - mousePosFlipped.y;

    // ----------------------------------------------

    // //gives coordinate inside SceneView context.
    // // WorldToViewportPoint() returns 0-to-1 value, where 0 means 0% and 1.0 means 100% of the dimension
    // Vector3 pointInSceneView = sv.camera.WorldToViewportPoint() * sv_correctSize;

    void OnSceneGUI()
    {
        SceneDiagnotics sceneiagObj = target as SceneDiagnotics;
        Camera cam = SceneView.lastActiveSceneView.camera;
        Vector3 mousepos = Event.current.mousePosition;
        
        mousepos.z = -cam.worldToCameraMatrix.MultiplyPoint(sceneiagObj.transform.position).z;
        mousepos.y = Screen.height - mousepos.y - 40.0f; // Offset by file menu? or toolbar? height
        mousepos = cam.ScreenToWorldPoint(mousepos);
        sceneiagObj.mousePositionWorld = mousepos;
    }
}
