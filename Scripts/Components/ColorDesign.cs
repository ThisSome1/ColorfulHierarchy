#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("ThisSome1.ColorfulHierarchy")]
namespace ThisSome1.ColorfulHierarchy
{
    [InitializeOnLoad, DisallowMultipleComponent, AddComponentMenu("ThisSome1/ColorfulHierarchy/Color Settings")]
    internal class ColorDesign : MonoBehaviour
    {
        [Header("This GameObject should not contain any component\nand will not remain in the build.\n\nName Format: \\\\ [Title]"), Space(15)]
        [SerializeField] private bool _removeInPlayMode;
        [SerializeField] internal FolderDesign Settings;

        public ColorDesign()
        {
            EditorApplication.delayCall += HideGizmoIcon;
        }

        private static void HideGizmoIcon()
        {
            const int MONO_BEHAVIOR_CLASS_ID = 114; // https://docs.unity3d.com/Manual/ClassIDReference.html
            System.Type annotationType = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.AnnotationUtility");
            var setIconEnabled = annotationType?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);
            setIconEnabled?.Invoke(null, new object[] { MONO_BEHAVIOR_CLASS_ID, typeof(ColorDesign).Name, 0 });
        }

        private void Reset()
        {
            _removeInPlayMode = false;
            Settings = FolderDesign.Default();
        }
        private void OnValidate()
        {
            Settings.fontSize = Mathf.Max(Settings.fontSize, 1);
        }
        private void Awake()
        {
            if (_removeInPlayMode && EditorApplication.isPlaying)
            {
                transform.DetachChildren();
                Destroy(gameObject);
            }
        }
    }
}
#endif