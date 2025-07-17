#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThisSome1.ColorfulHierarchy
{
    internal class ColorfulHierarchyEditorData : ScriptableSingleton<ColorfulHierarchyEditorData>
    {
        const string PrefKey = "ThisSome1.ColorfulHierarchy.Data", UndoPrefix = "ThisSome1.ColorfulHierarchy ";
        [SerializeField] private List<FolderStructure> _structures = null;
        [SerializeField] private List<int> _selectedFolderPath = new();

        internal static List<int> SelectedFolderPath { get => instance._selectedFolderPath; set => instance._selectedFolderPath = value; }
        internal static List<FolderStructure> Structures
        {
            get
            {
                if (instance._structures == null)
                {
                    instance._structures = new();
                    if (EditorPrefs.HasKey(PrefKey))
                        instance._structures.AddRange(JsonUtility.FromJson<StructureList>(EditorPrefs.GetString(PrefKey)).Structures);
                }
                return instance._structures;
            }
        }

        internal static void UndoRedoHappened(in UndoRedoInfo undo)
        {
            if (undo.undoName.StartsWith(UndoPrefix))
                FolderStructureWindow.ShowAndRepaint();
        }
        internal static void RecordUndo(string name) => Undo.RegisterCompleteObjectUndo(instance, UndoPrefix + name);
        internal static void Save() => EditorPrefs.SetString(PrefKey, JsonUtility.ToJson(new StructureList { Structures = Structures.ToArray() }));
    }
}
#endif