using System.Collections.Generic;
using UnityEngine;
using System;

namespace ThisSome1.ColorfulHierarchy
{
    [Serializable]
    internal class FolderStructure
    {
        [SerializeField] internal string Title = "new structure";
        [SerializeField] internal List<FolderData> Folders = new();
    }

    [Serializable]
    internal class FolderData
    {
        [SerializeField] internal string Name = "new folder";
        [SerializeReference] internal List<FolderData> SubFolders = new();
        [SerializeField] internal FolderDesign Design;
    }

    [Serializable]
    internal struct FolderDesign
    {
        [ColorUsage(false)] public Color textColor, backgroundColor;
        public TextAnchor textAlignment;
        public FontStyle fontStyle;
        public int fontSize;

        public FolderDesign(bool setDefault)
        {
            textColor = Color.white;
            backgroundColor = Color.black;
            textAlignment = TextAnchor.MiddleCenter;
            fontStyle = FontStyle.Bold;
            fontSize = 12;
        }
        public FolderDesign(FolderDesign source)
        {
            textColor = source.textColor;
            backgroundColor = source.backgroundColor;
            textAlignment = source.textAlignment;
            fontStyle = source.fontStyle;
            fontSize = source.fontSize;
        }

        public static FolderDesign Default() => new FolderDesign
        {
            textColor = Color.white,
            backgroundColor = Color.black,
            textAlignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
    }
}