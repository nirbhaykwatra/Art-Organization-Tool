using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;

namespace ArtPipeline
{
    public class ArtOrganizationWindow :  OdinEditorWindow
    {
        [MenuItem("Tools/Art Organization Tool")]
        private static void OpenWindow() => GetWindow<ArtOrganizationWindow>();
        
        [ShowInInspector]
        [AssetsOnly]
        [AssetSelector(Filter = "t:Model")]
        [InfoBox("Click the small three dots next to the selection field to see a list of model files in the project.")]
        private GameObject m_selectedObject;

        [ShowInInspector] 
        [FolderPath(RequireExistingPath = true)]
        private string m_artFolderPath;
        
        [Button(ButtonSizes.Large)]
        private void CreateFolderStructure()
        {
            string assetFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(m_artFolderPath, m_selectedObject.name));
            AssetDatabase.CreateFolder(assetFolder, "Meshes");
            AssetDatabase.CreateFolder(assetFolder, "Materials");
            AssetDatabase.CreateFolder(assetFolder, "Textures");
            AssetDatabase.CreateFolder(assetFolder, "Animations");
            AssetDatabase.CreateFolder(assetFolder, "Audio");
            AssetDatabase.Refresh();
        }
    }
}
