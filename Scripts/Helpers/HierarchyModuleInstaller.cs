#if UNITY_EDITOR && UNITY_6000_6_OR_NEWER
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class HierarchyModuleInstaller
{
    const string k_PromptedKey = "ThisSome1.ColorfulHierarchy.HierarchyModulePrompted";
    const string k_Symbol = "TS1_COLORFUL_HIERARCHY_HIERARCHY_PKG_INSTALLED";
    const string k_ModuleId = "com.unity.modules.hierarchycore";
    const string k_ModuleId2 = "com.unity.modules.hierarchy";

    private static AddRequest _request, _request2;

    static HierarchyModuleInstaller()
    {
        if (ArePackagesInstalled(new[] { k_ModuleId, k_ModuleId2 }))
        {
            DefineSymbol();
            return;
        }

        // Only ask once per project, not on every domain reload.
        if (SessionState.GetBool(k_PromptedKey, false))
            return;

        SessionState.SetBool(k_PromptedKey, true);

        if (EditorUtility.DisplayDialog(
            "Colorful Hierarchy",
            "This Unity version supports the new Hierarchy window, but the built-in " +
            "\"Hierarchy\" module isn't enabled in this project, so colored folders won't " +
            "render there. Enable it now? (This adds one line to Packages/manifest.json " +
            "and triggers a recompile.)",
            "Enable", "Not now"))
        {
            _request = Client.Add(k_ModuleId);
            _request2 = Client.Add(k_ModuleId2);
            EditorApplication.update += Progress;
        }
    }

    private static bool ArePackagesInstalled(string[] packageNames)
    {
        var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
        var list = new List<string>(packageNames);
        foreach (var pkg in packages)
            if (list.Contains(pkg.name))
            {
                list.Remove(pkg.name);
                if (list.Count == 0)
                    return true;
            }
        return false;
    }
    private static void Progress()
    {
        if (_request == null || !_request.IsCompleted || _request2 == null || !_request2.IsCompleted)
            return;

        EditorApplication.update -= Progress;

        if (_request.Status == StatusCode.Success)
            DefineSymbol();
        else if (_request.Status >= StatusCode.Failure)
            Debug.LogWarning($"[Colorful Hierarchy] Could not enable Hierarchy module: {_request.Error.message}");
    }
    private static void DefineSymbol()
    {
        PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone, out var defines);
        var symbols = new List<string>(defines);
        if (!symbols.Contains(k_Symbol))
        {
            symbols.Add(k_Symbol);
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone, symbols.ToArray());
        }
    }
}
#endif