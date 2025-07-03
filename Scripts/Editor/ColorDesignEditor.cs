#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace ThisSome1.ColorfulHierarchy
{
    [CanEditMultipleObjects, CustomEditor(typeof(ColorDesign))]
    public class ColorDesignEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ColorDesign[] colorDesigns = new ColorDesign[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                colorDesigns[i] = targets[i] as ColorDesign;
            serializedObject.Update();

            var iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);

            EditorGUI.BeginChangeCheck();
            do
            {
                switch (iterator.name)
                {
                    case "m_Script":
                        break;
                    // case "Settings":
                    //     EditorGUILayout.LabelField("Settings:");
                    //     EditorGUI.indentLevel++;
                    //     var folderDesigns = new List<FolderDesignSO>();
                    //     foreach (var tgt in targets)
                    //         folderDesigns.Add((tgt as ColorDesign).Settings);
                    //     Editor editor = null;
                    //     CreateCachedEditor(folderDesigns.ToArray(), null, ref editor);
                    //     editor.OnInspectorGUI();
                    //     EditorGUI.indentLevel--;
                    //     break;
                    default:
                        EditorGUILayout.PropertyField(iterator);
                        break;
                }
            } while (iterator.NextVisible(false));

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif