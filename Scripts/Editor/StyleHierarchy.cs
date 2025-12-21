#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ThisSome1.ColorfulHierarchy
{
    [InitializeOnLoad]
    internal class StyleHierarchy
    {
        internal static Texture2D gradientTexture;

        static StyleHierarchy()
        {
            // Initialize the gradient texture.
            gradientTexture = new(1000, 1, TextureFormat.RGBA32, false)
            {
                name = "[Generated] Gradient Texture",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
            };
            for (int i = 0; i <= 1000; i++)
                gradientTexture.SetPixel(i, 0, new Color(1, 1, 1, Mathf.Lerp(1, 0, Mathf.Pow(Mathf.Clamp01((Mathf.Abs(500 - i) - 200) / 300f), 2))));
            gradientTexture.Apply();

            // Check if the color palette asset is importing.
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindow;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindow;

            // Handle undo and redo for saved structures.
            Undo.undoRedoEvent -= ColorfulHierarchyEditorData.UndoRedoHappened;
            Undo.undoRedoEvent += ColorfulHierarchyEditorData.UndoRedoHappened;

            // Create the PalapalHelper if needed.
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
                    List<(Vector3 pos, Quaternion rot)> targetTransform = new();
                    foreach (Transform child in thisGO.transform)
                        targetTransform.Add((child.position, child.rotation));

                    Vector3 prevScale = thisGO.transform.localScale;
                    Undo.RecordObject(thisGO.transform, "Reset Folder Transform");
                    thisGO.transform.SetLocalPositionAndRotation(new(0, 0, 0), Quaternion.identity);
                    thisGO.transform.localScale = new(1, 1, 1);

                    foreach (Transform child in thisGO.transform)
                    {
                        Undo.RecordObject(child, "Reset Folder Transform");
                        child.SetPositionAndRotation(targetTransform[0].pos, targetTransform[0].rot);
                        child.localScale = new Vector3(child.localScale.x * prevScale.x, child.localScale.y * prevScale.y, child.localScale.z * prevScale.z);
                        targetTransform.RemoveAt(0);
                    }
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
                var nameStyle = new GUIStyle()
                {
                    fontSize = design.fontSize,
                    clipping = TextClipping.Clip,
                    fontStyle = design.fontStyle,
                    alignment = design.textAlignment,
                    normal = new GUIStyleState() { textColor = design.textColor }
                };

                // Draw a rectangle as a background, and set the color.
                selectionRect.width += 15;
                design.backgroundColor.a = 1;
                EditorGUI.DrawRect(selectionRect, (Color)typeof(EditorGUIUtility).GetMethod("GetDefaultBackgroundColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null));
                Rect boxRect = new(selectionRect.x, selectionRect.y, selectionRect.height, selectionRect.height);
                Rect rect = thisGO && !thisGO.activeInHierarchy ? new(selectionRect.x + selectionRect.height, selectionRect.y, selectionRect.width - selectionRect.height, selectionRect.height) : selectionRect;

                if (thisGO && !thisGO.activeInHierarchy)
                {
                    GUI.DrawTexture(boxRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1, Color.white, 0, rect.height);
                    GUI.DrawTexture(rect, gradientTexture, ScaleMode.StretchToFill, true, rect.width / rect.height, design.backgroundColor, 0, 0);
                    EditorGUI.LabelField(rect, instance.name[(instance.name.IndexOf(' ') + 1)..], nameStyle);
                    EditorGUI.LabelField(boxRect, "×", new GUIStyle() { fontSize = (int)rect.height, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = new GUIStyleState() { textColor = Color.red } });
                }
                else
                {
                    GUI.DrawTexture(selectionRect, gradientTexture, ScaleMode.StretchToFill, true, rect.width / rect.height, design.backgroundColor, 0, 0);
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

            ColorfulHierarchyEditorData.RecordUndo("Saved Folder Structure");
            ColorfulHierarchyEditorData.Structures.Add(new FolderStructure() { Title = "Saved Structure", Folders = new List<FolderData>(structure) });
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

            var asset = "";
            var guids = AssetDatabase.FindAssets($"{typeof(FolderStructureWindow).Name} t:Script");
            if (guids.Length > 1)
            {
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var filename = Path.GetFileNameWithoutExtension(assetPath);
                    if (filename == typeof(FolderStructureWindow).Name)
                    {
                        asset = guid;
                        break;
                    }
                }
            }
            else if (guids.Length == 1)
                asset = guids[0];

            string dir = AssetDatabase.GUIDToAssetPath(asset);
            dir = dir[..dir.LastIndexOf('/')];
            if (IsPalapalDefined() && !File.Exists(dir + "/PalapalHelper.cs"))
            {
                File.WriteAllText(dir + "/PalapalHelper.cs", "#if UNITY_EDITOR\nusing UnityEditor;\n\nnamespace ThisSome1.ColorfulHierarchy\n{\n\tpublic class PalapalHelper\n\t{" +
                                                            "\n\t\t[MenuItem(\"Palapal/ColorfulHierarchy/Folder Structures\")]\n\t\tpublic static void ShowWindow() => FolderStructureWindow.ShowWindow();" +
                                                            "\n\t\t[MenuItem(\"GameObject/Palapal/Colorful Hierarchy/Deploy Folder Structure\", true)]\n\t\tprivate static bool NoSelection() => Selection.count < 2;" +
                                                            "\n\t\t[MenuItem(\"GameObject/Palapal/Colorful Hierarchy/Deploy Folder Structure\", false)]" +
                                                            "\n\t\tprivate static void CreateFolderStructure() => SelectStructureWindow.ShowWindow();\n\t}\n}\n#endif");
                File.WriteAllText(dir + "/PalapalHelper.cs.meta", $"fileFormatVersion: 2\nguid: {GUID.Generate()}");

                if (!ColorfulHierarchyEditorData.Structures.Any((structure) => structure.Title == "Palapal"))
                {
                    ColorfulHierarchyEditorData.Structures.Add(new FolderStructure()
                    {
                        Title = "Palapal",
                        Folders = new List<FolderData>()
                        {
                            new() { Name = "Debug", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(0.5f, 0, 1) } },
                            new() { Name = "Managers", Design = new FolderDesign(true) { textColor = Color.black, backgroundColor = new Color(1, 0.5f, 0) } },
                            new() { Name = "UIs", Design = new FolderDesign(true) { textColor = Color.black, backgroundColor = new Color(0, 1, 1) } },
                            new() { Name = "Player", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(0, 0, 1) } },
                            new() { Name = "Lights", Design = new FolderDesign(true) { textColor = Color.black, backgroundColor = new Color(1, 1, 0) } },
                            new() { Name = "VFXs", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(1, 0, 0.5f) } },
                            new() { Name = "SFXs", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(0, 0.5f, 1) } },
                            new() { Name = "Environment", Design = new FolderDesign(true) { textColor = Color.black, backgroundColor = new Color(0, 1, 0) } },
                            new() { Name = "Gameplay", Design = new FolderDesign(true) { textColor = Color.white, backgroundColor = new Color(1, 0, 0) } },
                        }
                    });
                    ColorfulHierarchyEditorData.Save();
                }
            }
        }
    }
}
#endif