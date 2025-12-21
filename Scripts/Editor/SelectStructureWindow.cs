#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("ThisSome1.ColorfulHierarchy")]
namespace ThisSome1.ColorfulHierarchy
{
    internal class SelectStructureWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        internal static void ShowWindow()
        {
            var window = GetWindow<SelectStructureWindow>();
            window.titleContent = new GUIContent("Select Structure");
            window.minSize = new Vector2(200, 100);
            window.Show();
        }

        private void OnGUI()
        {
            if (ColorfulHierarchyEditorData.instance == null)
                ShowWindow();
            else if (ColorfulHierarchyEditorData.Structures.Count == 0)
            {
                EditorGUILayout.LabelField("No Structures Found.");
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Edit Structures", GUILayout.Width(200)))
                {
                    GetWindow<SelectStructureWindow>().Close();
                    GetWindow<FolderStructureWindow>().Show();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                var color = GUI.color;
                GUI.color = Color.white;
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);
                foreach (FolderStructure structure in ColorfulHierarchyEditorData.Structures)
                    if (GUILayout.Button(structure.Title))
                    {
                        foreach (FolderData folder in structure.Folders)
                            CreateFolderRecursive(folder, Selection.activeGameObject);
                        GetWindow<SelectStructureWindow>().Close();
                    }
                GUILayout.EndScrollView();
                GUI.color = color;
            }
        }

        private void CreateFolderRecursive(FolderData folderData, GameObject parent = null)
        {
            var folder = new GameObject(@"\\ " + folderData.Name);
            Undo.RegisterCreatedObjectUndo(folder, "Load Structure");
            ColorDesign design = folder.AddComponent<ColorDesign>();
            design.Settings = new FolderDesign(folderData.Design);

            if (parent)
                folder.transform.SetParent(parent.transform);
            foreach (FolderData subFolder in folderData.SubFolders)
                CreateFolderRecursive(subFolder, folder);
        }
    }
}
#endif