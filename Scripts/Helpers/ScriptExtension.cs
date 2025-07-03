#if UNITY_EDITOR
using UnityEditor;
using System;

public static class ScriptExtension
{
    public static string GetScriptPath(this Type type)
    {
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
            asset = guids[0];
        else
            return null;

        return AssetDatabase.GUIDToAssetPath(asset);
    }
}
#endif