using UnityEngine;
using UnityEditor;

namespace ChickenPathfinding
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(MyGrid))]
    public class GridEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MyGrid grid = (MyGrid)target;

            if (GUILayout.Button("Regenerate Grid"))
            {
                grid.CreateGrid();
                EditorUtility.SetDirty(grid);
            }
        }
    }
    #endif
}
