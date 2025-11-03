using UnityEngine;
using UnityEditor;

namespace ChickenPathfinding
{
    [CustomEditor(typeof(Grid))]
    public class GridEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Grid grid = (Grid)target;

            if (GUILayout.Button("Regenerate Grid"))
            {
                grid.GenerateGrid();
                EditorUtility.SetDirty(grid);
            }
        }
    }
}
