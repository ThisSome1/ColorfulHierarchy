#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThisSome1.ColorfulHierarchy
{
    internal class SavedStructures : ScriptableSingleton<SavedStructures>
    {
        const string PrefKey = "ThisSome1.ColorfulHierarchy.SavedStructures";
        [SerializeField] private List<FolderStructure> _structures = null;

        internal List<FolderStructure> Structures
        {
            get
            {
                if (_structures == null)
                {
                    _structures = new();
                    if (EditorPrefs.HasKey(PrefKey))
                        _structures.AddRange(JsonUtility.FromJson<StructureList>(EditorPrefs.GetString(PrefKey)).Structures);
                }
                return _structures;
            }
        }

        internal static void RecordUndo(string name) => Undo.RecordObject(instance, "ThisSome1.ColorfulHierarchy " + name);
        internal static void Save() => AssetDatabase.SaveAssetIfDirty(instance);
        internal static void SaveInPrefs()
        {
            Save();
            EditorPrefs.SetString(PrefKey, JsonUtility.ToJson(new StructureList { Structures = instance.Structures.ToArray() }));
        }
    }
}
#endif