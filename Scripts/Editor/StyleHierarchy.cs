#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
#if UNITY_6000_6_OR_NEWER && TS1_COLORFUL_HIERARCHY_HIERARCHY_PKG_INSTALLED
using Unity.Hierarchy.Editor;
using UnityEngine.UIElements;
using Unity.Hierarchy;
#endif

namespace ThisSome1.ColorfulHierarchy
{
    [InitializeOnLoad]
    internal class StyleHierarchy
    {
        internal readonly static Texture2D CrossMarkTexture;
        internal readonly static Texture2D GradientTexture;
        internal readonly static string PackageDirectory;

        static StyleHierarchy()
        {
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

            PackageDirectory = AssetDatabase.GUIDToAssetPath(asset);
            PackageDirectory = PackageDirectory[..PackageDirectory.LastIndexOf('/')];
            PackageDirectory = PackageDirectory[..PackageDirectory.LastIndexOf('/')];
            PackageDirectory = PackageDirectory[..PackageDirectory.LastIndexOf('/')];

            // Initialize the gradient texture.
            GradientTexture = new(1000, 1, TextureFormat.RGBA32, false)
            {
                name = "[Generated] Gradient Texture",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
            };
            for (int i = 0; i <= 1000; i++)
                GradientTexture.SetPixel(i, 0, new Color(1, 1, 1, Mathf.Lerp(1, 0, Mathf.Pow(Mathf.Clamp01((Mathf.Abs(500 - i) - 200) / 300f), 2))));
            GradientTexture.Apply();

            CrossMarkTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PackageDirectory + "/AdditionalFiles/CrossMark.png");

            // Check if the color palette asset is importing.
#if UNITY_6000_6_OR_NEWER && TS1_COLORFUL_HIERARCHY_HIERARCHY_PKG_INSTALLED
            HierarchyWindow.BindViewItem += OnBindViewItem;
            HierarchyWindow.UnbindViewItem += OnUnbindViewItem;
#endif

#if UNITY_6000_3_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyWindow;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyWindow;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindow;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindow;
#endif

            // Handle undo and redo for saved structures.
            Undo.undoRedoEvent -= ColorfulHierarchyEditorData.UndoRedoHappened;
            Undo.undoRedoEvent += ColorfulHierarchyEditorData.UndoRedoHappened;

            // Create the PalapalHelper if needed.
            EditorApplication.delayCall += CreatePalapalHelper;
        }

#if UNITY_6000_6_OR_NEWER && TS1_COLORFUL_HIERARCHY_HIERARCHY_PKG_INSTALLED
        private static void OnBindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            if (item.Handler is not HierarchyGameObjectHandler handler)
                return;

            GameObject thisGO = handler.GetGameObject(item.Node);
            if (thisGO == null)
                return;

            if (System.Text.RegularExpressions.Regex.IsMatch(thisGO.name, @"\\\\ .+"))
            {
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
                bool ignoreColorDesign = true;
                foreach (Component c in thisGO.GetComponents<Component>())
                {
                    if (ignoreColorDesign && c is ColorDesign)
                    {
                        ignoreColorDesign = false;
                        continue;
                    }
                    if (c is Transform)
                        continue;

                    Object.DestroyImmediate(c);
                }

                if (!thisGO.TryGetComponent(out ColorDesign cd))
                    cd = thisGO.AddComponent<ColorDesign>();
                cd.enabled = true;
                FolderDesign design = cd.Settings;

                item.style.backgroundImage = new StyleBackground(GradientTexture);
                item.style.unityBackgroundImageTintColor = design.backgroundColor;
                item.style.opacity = thisGO.activeInHierarchy ? 1 : 0.6f;

                var leftContainer = item.Q<VisualElement>(className: "hierarchy-item__left-container");
                leftContainer.style.flexGrow = 1;

                var icon = item.Q<VisualElement>(className: "hierarchy-item__icon");
                icon.style.backgroundImage = thisGO.activeSelf ? null : CrossMarkTexture;
                icon.style.backgroundColor = Color.clear;
                var overlayIcon = item.Q<VisualElement>(className: "hierarchy-item__overlay-icon");
                overlayIcon.style.backgroundColor = Color.clear;
                overlayIcon.style.backgroundImage = null;

                var nameContainer = item.Q<VisualElement>(className: "hierarchy-item__name");
                nameContainer.style.flexGrow = 1;

                var name = nameContainer.Q<Label>();
                name.style.display = DisplayStyle.None;

                nameContainer.Q<TextField>().RegisterCallback<FocusInEvent>(OnRenameStarted);
                nameContainer.Q<TextField>().RegisterCallback<FocusOutEvent>(OnRenameFinished);

                var folderName = item.Q<Label>("colorful-hierarchy__folder-name") ?? new Label(thisGO.name[3..]) { name = "colorful-hierarchy__folder-name" };
                folderName.style.unityFontStyleAndWeight = design.fontStyle;
                folderName.style.unityTextAlign = design.textAlignment;
                folderName.style.fontSize = design.fontSize;
                folderName.style.color = design.textColor;
                folderName.style.flexGrow = 1;

                if (folderName.parent == null)
                    nameContainer.Insert(0, folderName);
            }
            else if (thisGO.TryGetComponent(out ColorDesign nc))
                Object.DestroyImmediate(nc);
        }
        private static void OnUnbindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            if (item.Handler is not HierarchyGameObjectHandler)
                return;

            item.style.unityBackgroundImageTintColor = StyleKeyword.Null;
            item.style.backgroundImage = StyleKeyword.Null;
            item.style.opacity = StyleKeyword.Null;

            var leftContainer = item.Q<VisualElement>(className: "hierarchy-item__left-container");
            leftContainer.style.flexGrow = StyleKeyword.Null;

            var icon = item.Q<VisualElement>(className: "hierarchy-item__icon");
            icon.style.backgroundColor = StyleKeyword.Null;
            icon.style.backgroundImage = StyleKeyword.Null;
            var overlayIcon = item.Q<VisualElement>(className: "hierarchy-item__overlay-icon");
            overlayIcon.style.backgroundColor = StyleKeyword.Null;
            overlayIcon.style.backgroundImage = StyleKeyword.Null;

            var nameContainer = item.Q<VisualElement>(className: "hierarchy-item__name");
            nameContainer.style.flexGrow = StyleKeyword.Null;

            var name = nameContainer.Q<Label>();
            name.style.display = StyleKeyword.Null;

            var folderName = item.Q<Label>("colorful-hierarchy__folder-name");
            folderName?.parent?.Remove(folderName);
        }

        private static void OnRenameStarted(FocusInEvent evt)
        {
            var folder = (evt.currentTarget as VisualElement).parent.Q<Label>("colorful-hierarchy__folder-name");
            if (folder != null)
                folder.style.display = DisplayStyle.None;
        }
        private static void OnRenameFinished(FocusOutEvent evt)
        {
            var folder = (evt.currentTarget as VisualElement).parent.Q<Label>("colorful-hierarchy__folder-name");
            if (folder != null)
                folder.style.display = DisplayStyle.Flex;
        }
#endif

#if UNITY_6000_3_OR_NEWER
        private static void OnHierarchyWindow(EntityId instanceID, Rect selectionRect)
        {
            Object instance = EditorUtility.EntityIdToObject(instanceID);
#else
        private static void OnHierarchyWindow(int instanceID, Rect selectionRect)
        {
            Object instance = EditorUtility.InstanceIDToObject(instanceID);
#endif
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

                bool ignoreColorDesign = true;
                foreach (Component c in thisGO.GetComponents<Component>())
                {
                    if (ignoreColorDesign && c is ColorDesign)
                    {
                        ignoreColorDesign = false;
                        continue;
                    }
                    if (c is Transform)
                        continue;

                    Object.DestroyImmediate(c);
                }

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
                    GUI.DrawTexture(rect, GradientTexture, ScaleMode.StretchToFill, true, rect.width / rect.height, design.backgroundColor, 0, 0);
                    EditorGUI.LabelField(rect, instance.name[(instance.name.IndexOf(' ') + 1)..], nameStyle);
                    EditorGUI.LabelField(boxRect, "×", new GUIStyle() { fontSize = (int)rect.height, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = new GUIStyleState() { textColor = Color.red } });
                }
                else
                {
                    GUI.DrawTexture(selectionRect, GradientTexture, ScaleMode.StretchToFill, true, rect.width / rect.height, design.backgroundColor, 0, 0);
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
#if UNITY_6000_3_OR_NEWER
                foreach (var assembly in UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies())
#else
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
#endif
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

            string dir = PackageDirectory + "/Scripts/Editor";
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