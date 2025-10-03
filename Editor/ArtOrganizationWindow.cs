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
        [Title("Create Folder Structure")]
        [AssetsOnly]
        [AssetSelector(Filter = "t:Model")]
        [InfoBox("Click the small three dots next to the selection field to see a list of model files in the project.")]
        [PropertyOrder(-1)]
        private GameObject m_selectedObject;

        [ShowInInspector] 
        [FolderPath(RequireExistingPath = true)]
        [PropertyOrder(-2)]
        [Title("Basic Information")]
        private string m_artFolderPath;
        
        [ShowInInspector]
        [Title("Import Settings")]
        [PropertyOrder(0)]
        private bool m_moveAssetsOnImport;
        
        [ShowInInspector]
        [PropertyOrder(2)]
        private string m_meshPrefix;
        
        [ShowInInspector]
        [PropertyOrder(2)]
        private string m_texturePrefix;
        
        [ShowInInspector]
        [PropertyOrder(2)]
        private string m_materialPrefix;
        
        [PropertyOrder(-1)]
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

        [Button(ButtonSizes.Large)]
        private void SaveSettings()
        {
            EditorPrefs.SetString("SCP.ArtToolSettings.ArtFolderPath", m_artFolderPath);
            EditorPrefs.SetBool("SCP.ArtToolSettings.MoveAssetsOnImport", m_moveAssetsOnImport);
            EditorPrefs.SetString("SCP.ArtToolSettings.MeshPrefix", m_meshPrefix);
            EditorPrefs.SetString("SCP.ArtToolSettings.TexturePrefix", m_texturePrefix);
            EditorPrefs.SetString("SCP.ArtToolSettings.MaterialPrefix", m_materialPrefix);
            Debug.Log($"Set art organization preferences!");
        }

        protected override void OnEnable()
        {
            m_artFolderPath = EditorPrefs.GetString("SCP.ArtToolSettings.ArtFolderPath", "");
            m_moveAssetsOnImport =  EditorPrefs.GetBool("SCP.ArtToolSettings.MoveAssetsOnImport", false);
            m_meshPrefix = EditorPrefs.GetString("SCP.ArtToolSettings.MeshPrefix", "");
            m_texturePrefix = EditorPrefs.GetString("SCP.ArtToolSettings.TexturePrefix", "");
            m_materialPrefix = EditorPrefs.GetString("SCP.ArtToolSettings.MaterialPrefix", "");
        }

        protected override void OnBeginDrawEditors()
        {
            base.OnBeginDrawEditors();
            EditorPrefs.SetString("SCP.ArtToolSettings.ArtFolderPath", m_artFolderPath);
            EditorPrefs.SetBool("SCP.ArtToolSettings.MoveAssetsOnImport", m_moveAssetsOnImport);
            EditorPrefs.SetString("SCP.ArtToolSettings.MeshPrefix", m_meshPrefix);
            EditorPrefs.SetString("SCP.ArtToolSettings.TexturePrefix", m_texturePrefix);
            EditorPrefs.SetString("SCP.ArtToolSettings.MaterialPrefix", m_materialPrefix);
        }
    }
}
