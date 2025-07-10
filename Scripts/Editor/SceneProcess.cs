#if UNITY_EDITOR
using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine;

namespace ThisSome1.ColorfulHierarchy
{
    class SceneProcess
    {
        [PostProcessScene(int.MinValue)]
        static void OnPostProcessScene()
        {
            if (EditorApplication.isPlaying)
                return;

            foreach (ColorDesign nc in Object.FindObjectsByType<ColorDesign>(FindObjectsSortMode.None))
            {
                foreach (GameObject child in nc.transform)
                    child.SetActive(nc.gameObject.activeSelf & child.activeSelf);
                nc.transform.DetachChildren();
                Undo.DestroyObjectImmediate(nc.gameObject);
            }
        }
    }
}
#endif