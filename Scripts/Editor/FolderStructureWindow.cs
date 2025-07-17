#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

[assembly: InternalsVisibleTo("ThisSome1.ColorfulHierarchy")]
namespace ThisSome1.ColorfulHierarchy
{
    public class FolderStructureWindow : EditorWindow
    {
        #region Events
        private static event Action _afterPaint;
        #endregion

        #region Fields
        private static int _renamingIndex = -1;
        private static string _renameBuffer = "";
        private static float _infoPanelWidth = 400f;
        private static bool _resizingSplitter = false;
        private static readonly List<bool> _folds = new();
        private static string _lastSelectedFolderPath = "";
        private static FolderStructureWindow _window = null;
        private static (FolderData data, string path) _selectedFolder;
        private static (string Name, FolderDesign Design) _beforeChangeDesign;
        #endregion

        #region Methods
        [MenuItem("Window/ThisSome1/ColorfulHierarchy/Folder Structures")]
        internal static void ShowWindow()
        {
            _window = GetWindow<FolderStructureWindow>();
            _window.titleContent = new GUIContent("Folder Structures");
            _window.position = DeselectFolder(_window.position);
            _window.minSize = new Vector2(400, 300);
            _window.Show();
        }
        internal static void ShowAndRepaint()
        {
            if (!_window)
                ShowWindow();
            while (!_window) ;
            _window.Repaint();
        }
        private static (FolderData data, string path) GetFolderFromPath(List<int> path)
        {
            if (path.Count < 2)
                return (null, "");

            FolderStructure structure = ColorfulHierarchyEditorData.Structures[path[0]];
            (FolderData data, string path) res = (null, structure.Title);
            try
            {
                res = (structure.Folders[path[1]], $"{res.path}/{structure.Folders[path[1]].Name}");
                for (int pI = 2; pI < path.Count; pI++)
                    res = (res.data.SubFolders[path[pI]], $"{res.path}/{res.data.SubFolders[path[pI]].Name}");
            }
            catch
            {
                return (null, "");
            }
            return res;
        }

        private void OnEnable()
        {
            _renamingIndex = -1;
            _renameBuffer = "";
            _folds.Clear();
        }
        private void OnDisable()
        {
            _window = null;
            ColorfulHierarchyEditorData.Save();
        }
        private void OnFocus()
        {
            _window ??= GetWindow<FolderStructureWindow>();
        }
        private void OnLostFocus()
        {
            ColorfulHierarchyEditorData.Save();
        }
        private void OnGUI()
        {
            if (ColorfulHierarchyEditorData.SelectedFolderPath.Count > 0 && _window.minSize == new Vector2(400, 300))
                SelectFolder(ColorfulHierarchyEditorData.SelectedFolderPath);
            else if (ColorfulHierarchyEditorData.SelectedFolderPath.Count == 0 && _window.minSize == new Vector2(800, 300))
                position = DeselectFolder(position);

            // Draw structures list
            var leftPanelRect = new Rect(5, 0, (ColorfulHierarchyEditorData.SelectedFolderPath.Count == 0 ? position.width : position.width - _infoPanelWidth) - 10, position.height);
            GUILayout.BeginArea(leftPanelRect);
            DrawStructureList();
            GUILayout.EndArea();

            // Handle deselection by clicking left panel
            Event e = Event.current;
            if (e?.type == EventType.MouseDown && leftPanelRect.Contains(e.mousePosition))
            {
                position = DeselectFolder(position);
                e.Use();
            }
            else if (e?.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (_renamingIndex != -1)
                    _renamingIndex = -1;
                else
                    position = DeselectFolder(position);
                e.Use();
            }

            _infoPanelWidth = Mathf.Clamp(_infoPanelWidth, 400, Mathf.Max(400, position.width - 400));
            if (ColorfulHierarchyEditorData.SelectedFolderPath.Count > 0)
            {
                // Draw splitter
                var splitterRect = new Rect(position.width - _infoPanelWidth, 0, 5, position.height);
                EditorGUI.DrawRect(splitterRect, Color.white * 0.3f);
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

                // Draw selected folder info
                var rightPanelRect = new Rect(position.width - _infoPanelWidth + 10, 10, _infoPanelWidth - 15, position.height);
                GUILayout.BeginArea(rightPanelRect);
                DrawSelectedFolderInfo();
                GUILayout.EndArea();

                // Handle unfocus by clicking right panel
                e = Event.current;
                if (e?.type == EventType.MouseDown && rightPanelRect.Contains(e.mousePosition))
                {
                    GUI.FocusControl(null);
                    e.Use();
                }

                HandleResize(splitterRect);
            }

            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                _afterPaint?.Invoke();
                _afterPaint = null;
            }
        }

        private void DrawStructureList()
        {
            if (_folds.Count < ColorfulHierarchyEditorData.Structures.Count)
            {
                var count = _folds.Count;
                for (int i = 0; i < ColorfulHierarchyEditorData.Structures.Count - count; i++)
                    _folds.Add(false);
            }
            else if (_folds.Count > ColorfulHierarchyEditorData.Structures.Count)
                for (int i = ColorfulHierarchyEditorData.Structures.Count; i < _folds.Count; i++)
                    _folds[i] = false;

            // Unfold the selected structure
            if (ColorfulHierarchyEditorData.SelectedFolderPath.Count > 0)
                _folds[ColorfulHierarchyEditorData.SelectedFolderPath[0]] = true;

            EditorGUILayout.LabelField("Structures:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (ColorfulHierarchyEditorData.Structures.Count > 0)
                for (int i = 0; i < ColorfulHierarchyEditorData.Structures.Count; i++)
                {
                    int structIndex = i;

                    // Draw structure
                    Color originalColor = GUI.backgroundColor;
                    GUILayout.BeginHorizontal();
                    _folds[structIndex] = EditorGUILayout.Foldout(_folds[structIndex], ColorfulHierarchyEditorData.Structures[structIndex].Title, true);

                    var buttonStyle = new GUIStyle(GUI.skin.GetStyle("Button"))
                    {
                        padding = new RectOffset(),
                        fontSize = 15
                    };

                    // Rename button
                    if (_renamingIndex != structIndex)
                    {
                        GUI.backgroundColor = new Color(0, 0, 0, 0.3f);
                        buttonStyle.fontSize = 12;
                        if (GUILayout.Button("Rename ✏️", buttonStyle, GUILayout.Width(buttonStyle.CalcSize(new GUIContent("Rename ✏️")).x + 10), GUILayout.Height(18)))
                        {
                            GUI.FocusControl(null);
                            _renamingIndex = structIndex;
                            _renameBuffer = ColorfulHierarchyEditorData.Structures[structIndex].Title;
                        }
                        buttonStyle.fontSize = 15;
                    }
                    // Remove structure button
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("-", buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                        _afterPaint += () =>
                            {
                                ColorfulHierarchyEditorData.RecordUndo("Remove Structure");
                                ColorfulHierarchyEditorData.Structures.RemoveAt(structIndex);
                                position = DeselectFolder(position);
                            };
                    // Add folder button
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("+", buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                        _afterPaint += () =>
                            {
                                ColorfulHierarchyEditorData.RecordUndo("Add Folder");
                                ColorfulHierarchyEditorData.Structures[structIndex].Folders.Add(new FolderData() { Name = "New Folder", Design = new FolderDesign(ColorfulHierarchyEditorData.Structures[structIndex].Folders[^1].Design) });
                            };
                    GUILayout.EndHorizontal();

                    // Handle renaming structure
                    if (_renamingIndex == structIndex)
                    {
                        GUI.backgroundColor = originalColor;
                        if (GUI.GetNameOfFocusedControl() == "RenameTextField")
                        {
                            Event e = Event.current;
                            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                            {
                                RenameStructure(structIndex);
                                e.Use();
                            }
                        }

                        EditorGUILayout.BeginHorizontal();
                        GUI.SetNextControlName("RenameTextField");
                        _renameBuffer = EditorGUILayout.TextField(_renameBuffer);
                        Color regularBG = GUI.backgroundColor;
                        GUI.backgroundColor = Color.green;
                        if (GUILayout.Button("OK", GUILayout.Width(100)))
                            RenameStructure(structIndex);
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("Cancel", GUILayout.Width(100)))
                            _renamingIndex = -1;
                        EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = regularBG;
                    }

                    //  Draw structure folders
                    if (_folds[structIndex])
                    {
                        for (int fI = 0; fI < ColorfulHierarchyEditorData.Structures[structIndex].Folders.Count; fI++)
                            ViewFolderRecursive(ColorfulHierarchyEditorData.Structures[structIndex].Folders[fI], ColorfulHierarchyEditorData.Structures[structIndex].Folders, new() { structIndex, fI });
                        // foreach (FolderData folder in SavedStructures.Structures[structIndex].Folders)
                        //     ViewFolderRecursive(folder, 2, SavedStructures.Structures[structIndex].Folders, SavedStructures.Structures[structIndex].Title);
                    }
                    GUI.backgroundColor = originalColor;
                }
            else
            {
                EditorGUILayout.LabelField("No Structures Found.");
                EditorGUILayout.LabelField("Press create button to add a structure.");
            }

            EditorGUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create New Structure", GUILayout.Width(200)))
                _afterPaint += () =>
                    {
                        ColorfulHierarchyEditorData.RecordUndo("Add Structure");
                        ColorfulHierarchyEditorData.Structures.Add(new FolderStructure()
                        {
                            Title = "New Structure",
                            Folders = new List<FolderData>() { new FolderData()
                                        {
                                            Name = "New Folder",
                                            Design = FolderDesign.Default()
                                        }
                                    }
                        });
                    };
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            RemoveEmptyStructures();
        }
        private void DrawSelectedFolderInfo()
        {
            if (ColorfulHierarchyEditorData.SelectedFolderPath.Count == 0)
                return;

            EditorGUI.BeginChangeCheck();
            if (_lastSelectedFolderPath != string.Join(',', ColorfulHierarchyEditorData.SelectedFolderPath))
            {
                _lastSelectedFolderPath = string.Join(',', ColorfulHierarchyEditorData.SelectedFolderPath);
                _selectedFolder = GetFolderFromPath(ColorfulHierarchyEditorData.SelectedFolderPath);
            }
            _beforeChangeDesign = (_selectedFolder.data.Name, new(_selectedFolder.data.Design));

            EditorGUILayout.LabelField(_selectedFolder.path, new GUIStyle() { fontStyle = FontStyle.Bold, normal = new() { textColor = Color.white } });

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:");
            _selectedFolder.data.Name = EditorGUILayout.TextField(_selectedFolder.data.Name);
            EditorGUILayout.EndHorizontal();

            _selectedFolder.data.Design.textColor = EditorGUILayout.ColorField(new GUIContent("Text Color:"), _selectedFolder.data.Design.textColor, true, false, false);
            _selectedFolder.data.Design.backgroundColor = EditorGUILayout.ColorField(new GUIContent("Background Color:"), _selectedFolder.data.Design.backgroundColor, true, false, false);
            _selectedFolder.data.Design.textAlignment = (TextAnchor)EditorGUILayout.EnumPopup(new GUIContent("Text Alignment:"), _selectedFolder.data.Design.textAlignment);
            _selectedFolder.data.Design.fontStyle = (FontStyle)EditorGUILayout.EnumPopup(new GUIContent("Font Style:"), _selectedFolder.data.Design.fontStyle);
            _selectedFolder.data.Design.fontSize = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Font Size:"), _selectedFolder.data.Design.fontSize));

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset"))
                _selectedFolder.data.Design = FolderDesign.Default();
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                var (Name, Design) = (_selectedFolder.data.Name, _selectedFolder.data.Design);
                _selectedFolder.data.Design = _beforeChangeDesign.Design;
                _selectedFolder.data.Name = _beforeChangeDesign.Name;
                ColorfulHierarchyEditorData.RecordUndo("Change Folder Design");
                _selectedFolder.data.Design = Design;
                _selectedFolder.data.Name = Name;
            }
        }
        private static void RemoveEmptyStructures()
        {
            for (int i = ColorfulHierarchyEditorData.Structures.ToArray().Length - 1; i >= 0; i--)
                if (ColorfulHierarchyEditorData.Structures[i].Folders.Count == 0)
                {
                    ColorfulHierarchyEditorData.RecordUndo("Remove Folder");
                    ColorfulHierarchyEditorData.Structures.RemoveAt(i);
                }
        }
        private static void RenameStructure(int structureIndex)
        {
            ColorfulHierarchyEditorData.RecordUndo("Rename Folder Structure");
            ColorfulHierarchyEditorData.Structures[structureIndex].Title = _renameBuffer;
            _renamingIndex = -1;
        }
        private void SelectFolder(List<int> path)
        {
            var selected = GetFolderFromPath(path);
            if (selected.path == _selectedFolder.path)
                return;

            if (_window.minSize == new Vector2(400, 300))
            {
                position = new Rect(position.x, position.y, position.width + 400, position.height);
                _window.minSize = new Vector2(800, 300);
                _infoPanelWidth = 400;
            }

            ColorfulHierarchyEditorData.RecordUndo($"Select Folder: {selected.path}");

            ColorfulHierarchyEditorData.SelectedFolderPath = path;
            _selectedFolder = selected;
            GUI.FocusControl(null);
        }
        private static Rect DeselectFolder(Rect position)
        {
            if (string.IsNullOrEmpty(_lastSelectedFolderPath))
                return position;

            ColorfulHierarchyEditorData.RecordUndo($"Deselect Folder: {_selectedFolder.path}");

            ColorfulHierarchyEditorData.SelectedFolderPath.Clear();
            _window.minSize = new Vector2(400, 300);
            _lastSelectedFolderPath = "";
            _selectedFolder = default;
            GUI.FocusControl(null);
            return new Rect(position.x, position.y, position.width - _infoPanelWidth, position.height);
        }
        private void HandleResize(Rect splitterRect)
        {
            Event e = Event.current;

            switch (e.rawType)
            {
                case EventType.MouseDown:
                    if (splitterRect.Contains(e.mousePosition))
                    {
                        _resizingSplitter = true;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_resizingSplitter)
                    {
                        _infoPanelWidth = Mathf.Clamp(position.width - e.mousePosition.x, 400, Mathf.Max(400, position.width - 400));
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    _resizingSplitter = false;
                    break;
            }
        }
        private void ViewFolderRecursive(FolderData folderData, List<FolderData> container, List<int> path)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(path.Count * 15);
            GUI.backgroundColor = folderData.Design.backgroundColor;
            if (GUILayout.Button(folderData.Name, new GUIStyle()
            {
                fontSize = folderData.Design.fontSize,
                fontStyle = folderData.Design.fontStyle,
                alignment = folderData.Design.textAlignment,
                normal = new GUIStyleState() { textColor = folderData.Design.textColor, background = Texture2D.whiteTexture }
            }, GUILayout.MinHeight(20)))
                SelectFolder(path);
            var buttonStyle = new GUIStyle(GUI.skin.GetStyle("Button"))
            {
                padding = new RectOffset(),
                fontSize = 15
            };
            if (container != null)
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("-", buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                    _afterPaint += () =>
                        {
                            ColorfulHierarchyEditorData.RecordUndo("Remove Folder");
                            if (_selectedFolder.data == folderData)
                                position = DeselectFolder(position);
                            container.Remove(folderData);
                        };
            }
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+", buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                _afterPaint += () =>
                    {
                        ColorfulHierarchyEditorData.RecordUndo("Add Folder");
                        folderData.SubFolders.Add(new FolderData() { Name = "New Folder", Design = new FolderDesign(folderData.SubFolders.Count > 0 ? folderData.SubFolders[^1].Design : folderData.Design) });
                    };
            GUILayout.EndHorizontal();

            if (folderData.SubFolders.Count > 0)
                for (int i = 0; i < folderData.SubFolders.Count; i++)
                    ViewFolderRecursive(folderData.SubFolders[i], folderData.SubFolders, new(path) { i });
        }
        #endregion
    }

    internal class SelectStructureWindow : EditorWindow
    {
        #region Methods
        internal static void ShowWindow()
        {
            var window = GetWindow<SelectStructureWindow>();
            window.titleContent = new GUIContent("Select Structure");
            window.minSize = new Vector2(200, 100);
            window.Show();
        }

        private void OnGUI()
        {
            if (ColorfulHierarchyEditorData.instance == null)
                ShowWindow();
            else if (ColorfulHierarchyEditorData.Structures.Count == 0)
            {
                EditorGUILayout.LabelField("No Structures Found.");
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Edit Structures", GUILayout.Width(200)))
                {
                    GetWindow<SelectStructureWindow>().Close();
                    GetWindow<FolderStructureWindow>().Show();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                foreach (FolderStructure structure in ColorfulHierarchyEditorData.Structures)
                    if (GUILayout.Button(structure.Title))
                    {
                        foreach (FolderData folder in structure.Folders)
                            CreateFolderRecursive(folder, Selection.activeGameObject);
                        GetWindow<SelectStructureWindow>().Close();
                    }
            }
        }

        private void CreateFolderRecursive(FolderData folderData, GameObject parent = null)
        {
            var folder = new GameObject(@"\\ " + folderData.Name);
            Undo.RegisterCreatedObjectUndo(folder, "Load Structure");
            ColorDesign design = folder.AddComponent<ColorDesign>();
            design.Settings = new FolderDesign(folderData.Design);

            if (parent)
                folder.transform.SetParent(parent.transform);
            foreach (FolderData subFolder in folderData.SubFolders)
                CreateFolderRecursive(subFolder, folder);
        }
    }
    #endregion
}

#endif