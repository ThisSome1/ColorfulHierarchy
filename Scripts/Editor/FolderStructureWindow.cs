#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

[assembly: InternalsVisibleTo("ThisSome1.ColorfulHierarchy")]
namespace ThisSome1.ColorfulHierarchy
{
    public class FolderStructureWindow : EditorWindow
    {
        private static event Action _afterPaint;

        #region Fields
        private static FolderStructureWindow _window;
        private static string _structuresPath = "";
        private static string _renameBuffer = "";
        private static int _renamingIndex = -1;
        private static float _infoPanelWidth = 400f;
        private static bool _resizingSplitter = false;
        private static FolderData _selectedFolder = null;
        private static FolderStructureSO _savedStructures = null;
        private static readonly List<bool> _folds = new List<bool>();
        #endregion

        #region Properties
        internal static string DataPath
        {
            get
            {
                if (_structuresPath == "")
                    _structuresPath = FolderStructureSO.GetDataPath;
                return _structuresPath;
            }
            set => _structuresPath = value;
        }
        #endregion

        #region Methods
        [MenuItem("Window/ThisSome1/ColorfulHierarchy/Folder Structures")]
        internal static void ShowWindow()
        {
            _savedStructures = AssetDatabase.LoadAssetAtPath<FolderStructureSO>(DataPath);
            if (_savedStructures == null)
                CreateDataAsset();
            _renamingIndex = -1;
            _renameBuffer = "";
            _folds.Clear();

            _window = GetWindow<FolderStructureWindow>();
            _window.titleContent = new GUIContent("Folder Structures");
            _window.position = DeselectFolder(_window.position);
            _window.minSize = new Vector2(400, 300);
            _window.Show();
        }
        internal static FolderStructureSO CreateDataAsset()
        {
            if (!Directory.Exists(DataPath[..DataPath.LastIndexOf('/')]))
                Directory.CreateDirectory(DataPath[..DataPath.LastIndexOf('/')]);
            _savedStructures = CreateInstance<FolderStructureSO>();
            AssetDatabase.CreateAsset(_savedStructures, DataPath);
            AssetDatabase.Refresh();
            return _savedStructures;
        }

        private void OnGUI()
        {
            if (_savedStructures == null)
                _afterPaint += () => ShowWindow();
            else
            {
                // Draw structures list
                var leftPanelRect = new Rect(5, 0, (_selectedFolder == null ? position.width : position.width - _infoPanelWidth) - 10, position.height);
                GUILayout.BeginArea(leftPanelRect);
                DrawStructureList();
                GUILayout.EndArea();

                // Handle deselection by clicking left panel
                Event e = Event.current;
                if (e?.type == EventType.MouseDown && e.keyCode == KeyCode.Mouse0 && leftPanelRect.Contains(e.mousePosition))
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

                if (_selectedFolder != null)
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
                    if ((e?.type == EventType.MouseDown && e.keyCode == KeyCode.Mouse0 && rightPanelRect.Contains(e.mousePosition)) || (e?.type == EventType.KeyDown && e.keyCode == KeyCode.Escape))
                    {
                        GUI.FocusControl(null);
                        e.Use();
                    }

                    HandleResize(splitterRect);
                    _infoPanelWidth = Mathf.Clamp(_infoPanelWidth, 400, position.width - 400);
                }
            }

            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                _afterPaint?.Invoke();
                _afterPaint = null;
            }
        }

        private void DrawStructureList()
        {
            if (_folds.Count < _savedStructures.Structures.Count)
            {
                var count = _folds.Count;
                for (int i = 0; i < _savedStructures.Structures.Count - count; i++)
                    _folds.Add(false);
            }
            else if (_folds.Count > _savedStructures.Structures.Count)
                for (int i = _savedStructures.Structures.Count; i < _folds.Count; i++)
                    _folds[i] = false;

            EditorGUILayout.LabelField("Structures:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_savedStructures.Structures.Count > 0)
                for (int i = 0; i < _savedStructures.Structures.Count; i++)
                {
                    int structIndex = i;

                    // Draw structure
                    Color originalColor = GUI.backgroundColor;
                    GUILayout.BeginHorizontal();
                    _folds[structIndex] = EditorGUILayout.Foldout(_folds[structIndex], _savedStructures.Structures[structIndex].Title, true);

                    var buttonStyle = new GUIStyle(GUI.skin.GetStyle("Button"))
                    {
                        padding = new RectOffset(),
                        fontSize = 15
                    };

                    // Rename button
                    if (_renamingIndex != structIndex)
                    {
                        GUI.backgroundColor = new Color(0, 0, 0, 0);
                        if (GUILayout.Button("✏️", new GUIStyle(GUI.skin.GetStyle("Label")) { fontSize = 12 }, GUILayout.Width(20), GUILayout.Height(18)))
                        {
                            _renamingIndex = structIndex;
                            _renameBuffer = _savedStructures.Structures[structIndex].Title;
                        }
                    }
                    // Remove structure button
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("-", buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                        _afterPaint += () =>
                            {
                                Undo.RecordObject(_savedStructures, "Remove Structure");
                                _savedStructures.Structures.RemoveAt(structIndex);
                                position = DeselectFolder(position);
                                EditorUtility.SetDirty(_savedStructures);
                                AssetDatabase.SaveAssetIfDirty(_savedStructures);
                            };
                    // Add folder button
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("+", buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                        _afterPaint += () =>
                            {
                                _savedStructures.Structures[structIndex].Folders.Add(new FolderData() { Name = "New Folder", Design = new FolderDesign(_savedStructures.Structures[structIndex].Folders[^1].Design) });
                                EditorUtility.SetDirty(_savedStructures);
                                AssetDatabase.SaveAssetIfDirty(_savedStructures);
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
                        foreach (FolderData folder in _savedStructures.Structures[structIndex].Folders)
                            ViewFolderRecursive(folder, 2, _savedStructures.Structures[structIndex].Folders);
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
            {
                _savedStructures.Structures.Add(new FolderStructure()
                {
                    Title = "New Structure",
                    Folders = new List<FolderData>() { new FolderData()
                                    {
                                        Name = "New Folder",
                                        Design = FolderDesign.Default()
                                    }
                                }
                });
                EditorUtility.SetDirty(_savedStructures);
                AssetDatabase.SaveAssetIfDirty(_savedStructures);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            RemoveEmptyStructures();
        }
        private void DrawSelectedFolderInfo()
        {
            if (_selectedFolder == null)
                return;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset"))
                _selectedFolder.Design = FolderDesign.Default();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:");
            _selectedFolder.Name = EditorGUILayout.TextField(_selectedFolder.Name);
            EditorGUILayout.EndHorizontal();

            _selectedFolder.Design.textColor = EditorGUILayout.ColorField(new GUIContent("Text Color:"), _selectedFolder.Design.textColor, true, false, false);
            _selectedFolder.Design.backgroundColor = EditorGUILayout.ColorField(new GUIContent("Background Color:"), _selectedFolder.Design.backgroundColor, true, false, false);
            _selectedFolder.Design.textAlignment = (TextAnchor)EditorGUILayout.EnumPopup(new GUIContent("Text Alignment:"), _selectedFolder.Design.textAlignment);
            _selectedFolder.Design.fontStyle = (FontStyle)EditorGUILayout.EnumPopup(new GUIContent("Font Style:"), _selectedFolder.Design.fontStyle);
            _selectedFolder.Design.fontSize = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Font Size:"), _selectedFolder.Design.fontSize));

            if (EditorGUI.EndChangeCheck())
                _afterPaint += () =>
                    {
                        EditorUtility.SetDirty(_savedStructures);
                        AssetDatabase.SaveAssetIfDirty(_savedStructures);
                    };
        }
        private static void RemoveEmptyStructures()
        {
            Undo.RecordObject(_savedStructures, "Remove Folder");
            for (int i = _savedStructures.Structures.ToArray().Length - 1; i >= 0; i--)
                if (_savedStructures.Structures[i].Folders.Count == 0)
                {
                    _savedStructures.Structures.RemoveAt(i);
                    EditorUtility.SetDirty(_savedStructures);
                }
            AssetDatabase.SaveAssetIfDirty(_savedStructures);
        }
        private static void RenameStructure(int structureIndex)
        {
            Undo.RecordObject(_savedStructures, "Rename Folder Structure");
            _savedStructures.Structures[structureIndex].Title = _renameBuffer;
            EditorUtility.SetDirty(_savedStructures);
            AssetDatabase.SaveAssetIfDirty(_savedStructures);
            _renamingIndex = -1;
        }
        private void SelectFolder(FolderData selected)
        {
            if (_selectedFolder == selected)
                return;

            if (_selectedFolder == null)
            {
                position = new Rect(position.x, position.y, position.width + 400, position.height);
                _window.minSize = new Vector2(800, 300);
                _infoPanelWidth = 400;
            }

            _selectedFolder = selected;
            GUI.FocusControl(null);
        }
        private static Rect DeselectFolder(Rect position)
        {
            if (_selectedFolder == null)
                return position;

            _selectedFolder = null;
            _window.minSize = new Vector2(400, 300);
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
                        _infoPanelWidth = Mathf.Clamp(position.width - e.mousePosition.x, 400, position.width - 400);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    _resizingSplitter = false;
                    break;
            }
        }

        private void ViewFolderRecursive(FolderData folderData, int indentLevel, List<FolderData> container)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indentLevel * 15);
            GUI.backgroundColor = folderData.Design.backgroundColor;
            if (GUILayout.Button(folderData.Name, new GUIStyle()
            {
                fontSize = folderData.Design.fontSize,
                fontStyle = folderData.Design.fontStyle,
                alignment = folderData.Design.textAlignment,
                normal = new GUIStyleState() { textColor = folderData.Design.textColor, background = Texture2D.whiteTexture }
            }, GUILayout.MinHeight(20)))
            {
                SelectFolder(folderData);
            }
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
                            Undo.RecordObject(_savedStructures, "Remove Folder");
                            if (_selectedFolder == folderData)
                                position = DeselectFolder(position);
                            container.Remove(folderData);
                            EditorUtility.SetDirty(_savedStructures);
                            AssetDatabase.SaveAssetIfDirty(_savedStructures);
                        };
            }
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+", buttonStyle, GUILayout.Width(20), GUILayout.Height(18)))
                _afterPaint += () =>
                    {
                        folderData.SubFolders.Add(new FolderData() { Name = "New Folder", Design = new FolderDesign(folderData.SubFolders.Count > 0 ? folderData.SubFolders[^1].Design : folderData.Design) });
                        EditorUtility.SetDirty(_savedStructures);
                        AssetDatabase.SaveAssetIfDirty(_savedStructures);
                    };
            GUILayout.EndHorizontal();

            if (folderData.SubFolders.Count > 0)
                foreach (FolderData subFolder in folderData.SubFolders)
                    ViewFolderRecursive(subFolder, indentLevel + 1, folderData.SubFolders);
        }
        #endregion
    }

    internal class SelectStructureWindow : EditorWindow
    {
        #region Fields
        private static FolderStructureSO _savedStructures = null;
        #endregion

        #region Methods
        internal static void ShowWindow()
        {
            _savedStructures = AssetDatabase.LoadAssetAtPath<FolderStructureSO>(FolderStructureWindow.DataPath);
            if (_savedStructures == null)
                CreateDataAsset();

            var window = GetWindow<SelectStructureWindow>();
            window.titleContent = new GUIContent("Select Structure");
            window.minSize = new Vector2(200, 100);
            window.Show();
        }
        private static void CreateDataAsset()
        {
            if (!Directory.Exists(FolderStructureWindow.DataPath[..FolderStructureWindow.DataPath.LastIndexOf('/')]))
                Directory.CreateDirectory(FolderStructureWindow.DataPath[..FolderStructureWindow.DataPath.LastIndexOf('/')]);
            _savedStructures = CreateInstance<FolderStructureSO>();
            AssetDatabase.CreateAsset(_savedStructures, FolderStructureWindow.DataPath);
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            if (_savedStructures == null)
                ShowWindow();
            else if (_savedStructures.Structures.Count == 0)
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
                foreach (FolderStructure structure in _savedStructures.Structures)
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