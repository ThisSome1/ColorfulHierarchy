#if UNITY_EDITOR && UNITY_6000_6_OR_NEWER
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class HierarchyModuleInstaller
{
    const string k_PromptedKey = "ThisSome1.ColorfulHierarchy.HierarchyModulePrompted";
    const string k_ModuleId = "com.unity.modules.hierarchycore";
    const string k_ModuleId2 = "com.unity.modules.hierarchy";

    private static AddRequest _request, _request2;

    static HierarchyModuleInstaller()
    {
        if (IsModuleAvailable()) return;

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

    static bool IsModuleAvailable() => System.Type.GetType("Unity.Hierarchy.Editor.HierarchyWindow, UnityEngine.HierarchyModule") != null;

    static void Progress()
    {
        if (_request == null || !_request.IsCompleted || _request2 == null || !_request2.IsCompleted)
            return;

        EditorApplication.update -= Progress;

        if (_request.Status == StatusCode.Success)
            Debug.Log("[Colorful Hierarchy] Hierarchy module enabled.");
        else if (_request.Status >= StatusCode.Failure)
            Debug.LogWarning($"[Colorful Hierarchy] Could not enable Hierarchy module: {_request.Error.message}");
    }
}
#endif