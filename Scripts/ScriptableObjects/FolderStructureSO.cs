#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

namespace ThisSome1.ColorfulHierarchy
{
    internal class FolderStructureSO : ScriptableObject
    {
        [SerializeField] internal List<FolderStructure> Structures = new();
        public static string GetDataPath
        {
            get
            {
                Type type = typeof(FolderStructureSO);
                var asset = "";
                var guids = AssetDatabase.FindAssets($"{type.Name} t:Script");

                if (guids.Length > 1)
                {
                    foreach (var guid in guids)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        var filename = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                        if (filename == type.Name)
                        {
                            asset = guid;
                            break;
                        }
                    }
                }
                else if (guids.Length == 1)
                {
                    asset = guids[0];
                }
                else
                {
                    Debug.LogError($"Unable to locate {type.Name}");
                    return null;
                }

                string path = AssetDatabase.GUIDToAssetPath(asset);
                path = path[..path.LastIndexOf('/')];
                path = path[..path.LastIndexOf('/')];
                path = path[..path.LastIndexOf('/')] + "/Data/FolderStructures.asset";
                return path;
            }
        }
    }
}
#endif