using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThisSome1.ColorfulHierarchy
{
    internal class SavedStructures : ScriptableSingleton<SavedStructures>
    {
        const string PrefKey = "ThisSome1.ColorfulHierarchy.SavedStructures";
        private List<FolderStructure> _structures = null;

        internal List<FolderStructure> Structures
        {
            get
            {
                if (_structures == null)
                    if (EditorPrefs.HasKey(PrefKey))
                        _structures = JsonUtility.FromJson<List<FolderStructure>>(EditorPrefs.GetString(PrefKey));
                    else
                        _structures = new();
                return _structures;
            }
        }

        internal static void Save() => EditorPrefs.SetString(PrefKey, JsonUtility.ToJson(instance.Structures));
    }
}