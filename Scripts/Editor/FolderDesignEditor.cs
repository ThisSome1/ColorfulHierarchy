#if UNITY_EDITOR
using UnityEditor;

namespace ThisSome1.ColorfulHierarchy
{
    [CanEditMultipleObjects, CustomEditor(typeof(FolderDesign))]
    public class FolderDesignEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);

            EditorGUI.BeginChangeCheck();
            do
                if (iterator.name != "m_Script")
                    EditorGUILayout.PropertyField(iterator);
            while (iterator.NextVisible(false));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
            }
        }
    }
}
#endif