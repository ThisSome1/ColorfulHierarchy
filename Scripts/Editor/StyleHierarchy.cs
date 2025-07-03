#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ThisSome1.ColorfulHierarchy
{
    [InitializeOnLoad]
    internal class StyleHierarchy
    {
        static StyleHierarchy()
        {
            // Check if the color palette asset is importing.
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindow;

            // Create the PalapalHelper if needed
            EditorApplication.delayCall += CreatePalapalHelper;
        }

        private static void OnHierarchyWindow(int instanceID, Rect selectionRect)
        {
            Object instance = EditorUtility.InstanceIDToObject(instanceID);
            if (instance == null)
                return;

            // Check if the name of each gameObject is begin with keyChar in colorDesigns list.
            if (System.Text.RegularExpressions.Regex.IsMatch(instance.name, @"\\\\ .+"))
            {
                if (instance is not GameObject)
                    return;

                GameObject thisGO = instance as GameObject;

                if (thisGO.transform.localPosition != Vector3.zero || thisGO.transform.localRotation != Quaternion.identity || thisGO.transform.localScale != Vector3.one)
                {
                    Undo.RecordObject(thisGO.transform, "Reset Folder Transform");
                    foreach (Transform child in thisGO.transform)
                    {
                        Undo.RecordObject(child, "Reset Folder Transform");
                        child.localPosition += thisGO.transform.localPosition;
                        child.localRotation *= thisGO.transform.localRotation;
                        child.localScale.Scale(thisGO.transform.localScale);
                    }
                    thisGO.transform.SetLocalPositionAndRotation(new(0, 0, 0), Quaternion.identity);
                    thisGO.transform.localScale = new(1, 1, 1);
                }

                foreach (Component c in thisGO.GetComponents<Component>())
                    if (c is not Transform and not ColorDesign)
                        Object.DestroyImmediate(c);
                if (!thisGO.TryGetComponent(out ColorDesign cd))
                    cd = thisGO.AddComponent<ColorDesign>();
                cd.enabled = true;

                // Get the desired folder design from the palette
                FolderDesign design = cd.Settings;

                // Create a new GUIStyle to match the design in colorDesigns list.
                GUIStyle nameStyle = new GUIStyle()
                {
                    fontSize = design.fontSize,
                    fontStyle = design.fontStyle,
                    alignment = design.textAlignment,
                    normal = new GUIStyleState() { textColor = design.textColor }
                };

                // Draw a rectangle as a background, and set the color.
                design.backgroundColor.a = 1;
                Rect boxRect = new(selectionRect.x, selectionRect.y, selectionRect.height, selectionRect.height);
                Rect nameRect = new(selectionRect.x + selectionRect.height, selectionRect.y, selectionRect.width - selectionRect.height, selectionRect.height);
                if (thisGO && !thisGO.activeInHierarchy)
                {
                    EditorGUI.DrawRect(boxRect, Color.white);
                    EditorGUI.DrawRect(nameRect, design.backgroundColor);
                    EditorGUI.LabelField(nameRect, instance.name[(instance.name.IndexOf(' ') + 1)..], nameStyle);
                    EditorGUI.LabelField(boxRect, "X", new GUIStyle() { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = new GUIStyleState() { textColor = Color.red } });
                }
                else
                {
                    EditorGUI.DrawRect(selectionRect, design.backgroundColor);
                    EditorGUI.LabelField(selectionRect, instance.name[(instance.name.IndexOf(' ') + 1)..], nameStyle);
                }
            }
            else if (instance is GameObject && (instance as GameObject).TryGetComponent(out ColorDesign nc))
                Object.DestroyImmediate(nc);
        }

        [MenuItem("GameObject/ThisSome1/Colorful Hierarchy/Colored Folder", true)]
        private static bool SelectionHaveSameParent()
        {
            if (Selection.count < 2) return true;
            Transform pr = Selection.activeGameObject.transform.parent;
            foreach (var child in Selection.gameObjects)
                if (child.transform.parent != pr) return false;
            return true;
        }
        [MenuItem("GameObject/ThisSome1/Colorful Hierarchy/Colored Folder", false)]
        private static void CreateNewFolder(MenuCommand cmd)
        {
            bool notFound = cmd.context != null;
            if (notFound && Selection.count > 0)
                foreach (Object obj in Selection.objects)
                    if (obj == cmd.context)
                        notFound = false;
            if (notFound)
                return;

            var folder = new GameObject(@"\\ Colored Folder");
            if (Selection.count > 0)
                folder.transform.parent = Selection.activeGameObject.transform.parent;
            foreach (GameObject go in Selection.gameObjects)
            {
                Undo.RegisterFullObjectHierarchyUndo(go, "Moved to folder");
                GameObjectUtility.SetParentAndAlign(go, folder);
            }
            Undo.RegisterCreatedObjectUndo(folder, "new colored folder");
            Selection.activeObject = folder;
        }
        [MenuItem("GameObject/ThisSome1/Colorful Hierarchy/Deploy Folder Structure", true)]
        private static bool NoSelection() => Selection.count < 2;
        [MenuItem("GameObject/ThisSome1/Colorful Hierarchy/Deploy Folder Structure", false)]
        private static void CreateFolderStructure() => SelectStructureWindow.ShowWindow();
        [MenuItem("GameObject/ThisSome1/Colorful Hierarchy/Save Folder Structure", true)]
        private static bool SameParentAndMoreThanOneFolderSelected()
        {
            if (Selection.count == 0)
                return false;

            if (!SelectionHaveSameParent())
                return false;

            var folderCount = new List<ColorDesign>();
            foreach (var obj in Selection.gameObjects)
                if (obj.TryGetComponent(out ColorDesign _))
                    folderCount.AddRange(obj.GetComponentsInChildren<ColorDesign>());
                else
                    return false;
            return folderCount.Count > 1;
        }
        [MenuItem("GameObject/ThisSome1/Colorful Hierarchy/Save Folder Structure", false)]
        private static void SaveFolderStructure()
        {
            if (Selection.count == 0)
                return;

            var folders = new Stack<(ColorDesign, int)>();
            for (int i = Selection.count - 1; i >= 0; i--)
                folders.Push((Selection.gameObjects[i].GetComponent<ColorDesign>(), 0));

            var structure = new List<FolderData>();
            while (folders.Count > 0)
            {
                var (cd, depth) = folders.Pop();
                var folderData = new FolderData() { Name = cd.gameObject.name[3..], Design = new FolderDesign(cd.Settings) };

                if (depth == 0)
                    structure.Add(folderData);
                else
                {
                    FolderData parentFolder = structure[^1];
                    for (int i = 1; i < depth; i++)
                        parentFolder = parentFolder.SubFolders[^1];
                    parentFolder.SubFolders.Add(folderData);
                }

                for (int i = cd.transform.childCount - 1; i >= 0; i--)
                    if (cd.transform.GetChild(i).TryGetComponent(out ColorDesign ccd))
                        folders.Push((ccd, depth + 1));
            }

            FolderStructureSO savedStructures = AssetDatabase.LoadAssetAtPath<FolderStructureSO>(FolderStructureWindow.DataPath);
            savedStructures.Structures.Add(new FolderStructure() { Title = "Saved Structure", Folders = new List<FolderData>(structure) });
            EditorUtility.SetDirty(savedStructures);
            AssetDatabase.SaveAssetIfDirty(savedStructures);
            FolderStructureWindow.ShowWindow();

            Selection.activeGameObject = null;
        }

        private static void CreatePalapalHelper()
        {
            static bool IsPalapalDefined()
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.ToLower().StartsWith("unity") || assembly.FullName.ToLower().StartsWith("system") || assembly.FullName.ToLower().StartsWith("mono")
                        || assembly.FullName.ToLower().StartsWith("bee") || assembly.FullName.ToLower().StartsWith("net") || assembly.FullName.ToLower().StartsWith("mscorlib"))
                        continue;

                    foreach (var type in assembly.GetTypes())
                        if (type.Namespace != null && type.Namespace.StartsWith("Palapal"))
                            return true;
                }
                return false;
            }

            string dir = typeof(FolderStructureWindow).GetScriptPath();
            dir = dir[..dir.LastIndexOf('/')];
            if (IsPalapalDefined() && !File.Exists(dir + "/PalapalHelper.cs"))
            {
                File.WriteAllText(dir + "/PalapalHelper.cs", "#if UNITY_EDITOR\nusing UnityEditor;\n\nnamespace ThisSome1.ColorfulHierarchy\n{\n\tpublic class PalapalHelper\n\t{\n\t\t" +
                                                             "[MenuItem(\"Palapal/ColorfulHierarchy/Folder Structures\")]\n\t\tpublic static void ShowWindow() => FolderStructureWindow.ShowWindow();\n\t}\n}\n#endif");

                var structures = AssetDatabase.LoadAssetAtPath<FolderStructureSO>(FolderStructureWindow.DataPath);
                structures ??= FolderStructureWindow.CreateDataAsset();
                structures.Structures.Add(new FolderStructure()
                {
                    Title = "Palapal",
                    Folders = new List<FolderData>()
                        {
                            new FolderData() { Name = "Debug", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(0.5f, 0, 1) } },
                            new FolderData() { Name = "Managers", Design = new FolderDesign(true) { textColor = Color.black, backgroundColor = new Color(1, 0.5f, 0) } },
                            new FolderData() { Name = "UIs", Design = new FolderDesign(true) { textColor = Color.black, backgroundColor = new Color(0, 1, 1) } },
                            new FolderData() { Name = "Player", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(0, 0, 1) } },
                            new FolderData() { Name = "Lights", Design = new FolderDesign(true) { textColor = Color.black, backgroundColor = new Color(1, 1, 0) } },
                            new FolderData() { Name = "VFXs", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(1, 0, 0.5f) } },
                            new FolderData() { Name = "SFXs", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(0, 0.5f, 1) } },
                            new FolderData() { Name = "Environment", Design = new FolderDesign(true) { textColor = Color.black, backgroundColor = new Color(0, 1, 0) } },
                            new FolderData() { Name = "Gameplay", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(1, 0, 0) } },
                        }
                });
                EditorUtility.SetDirty(structures);
                AssetDatabase.SaveAssetIfDirty(structures);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif