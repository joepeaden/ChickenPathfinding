using UnityEngine;
using UnityEditor;

namespace ChickenPathfinding
{
    [CustomEditor(typeof(MyGrid))]
    public class GridEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MyGrid grid = (MyGrid)target;

            if (GUILayout.Button("Regenerate Grid"))
            {
                grid.GenerateGrid();
                EditorUtility.SetDirty(grid);
            }
        }
    }
}
