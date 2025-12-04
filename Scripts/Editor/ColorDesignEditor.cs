#if UNITY_EDITOR
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