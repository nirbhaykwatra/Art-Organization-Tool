using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArtPipeline
{
    public class ArtAssetPostprocessor : AssetPostprocessor
    {
        private static ArtOrganizationSettings m_settings;
        private string[] m_assetSubFolders = { "Meshes", "Materials", "Textures", "Audio", "Animations" };

        private void OnPostprocessTexture(Texture2D texture)
        {
            m_settings = LoadSettings();
            if (!assetImporter.importSettingsMissing) return;
            if (!SessionState.GetBool("ArtOrganizationSettings.AssetProcessingActive", false)) return;
            if (!m_settings.MoveAssetsOnImport) return;
            (string assetName, string assetCategory, string assetPrefix) = ExtractMetadataFromFileName(assetPath);
            if (assetPrefix != m_settings.TexturePrefix) 
            {
                Debug.LogError($"Asset {assetName} does not have a valid prefix! Skipped processing.");
                return;
            }
            CreateAssetSubfoldersIfMissing(assetName, assetCategory);

            string fileName = Path.GetFileName(assetPath);
            string destination =
                Path.Combine(Application.dataPath, m_settings.ArtFolderPath, assetCategory, assetName, "Textures", fileName);
            
            if (!File.Exists(destination))
            {
                if (assetPath != null) File.Move(assetPath, destination);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnPostprocessModel(GameObject model)
        {
            m_settings = LoadSettings();
            if (!assetImporter.importSettingsMissing) return;
            if (!SessionState.GetBool("ArtOrganizationSettings.AssetProcessingActive", false)) return;
            if (!m_settings.MoveAssetsOnImport) return;
            (string assetName, string assetCategory, string assetPrefix) = ExtractMetadataFromFileName(assetPath);
            if (assetPrefix != m_settings.MeshPrefix) 
            {
                Debug.LogError($"Asset {assetName} does not have a valid prefix! Skipped processing.");
                return;
            }
            CreateAssetSubfoldersIfMissing(assetName, assetCategory);
            
            string fileName = Path.GetFileName(assetPath);
            string destination =
                Path.Combine(Application.dataPath, m_settings.ArtFolderPath, assetCategory, assetName, "Meshes", fileName);
            
            if (!File.Exists(destination))
            {
                if (assetPath != null) File.Move(assetPath, destination);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private void CreateAssetSubfoldersIfMissing(string assetName, string assetCategory)
        {
            string assetFolder = Path.Combine(m_settings.ArtFolderPath, assetCategory, assetName);

            foreach (string subFolder in m_assetSubFolders)
            {
                if (!Directory.Exists(Path.Combine(Application.dataPath, assetFolder, subFolder)))
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, assetFolder, subFolder));
                }
            }
        }
        
        private (string assetName, string assetCategory, string assetPrefix) ExtractMetadataFromFileName(string filePath)
        {
            // File naming convention: Prefix_Name_Category_Variant.ext
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            string[] fileNameSplit = fileName.Split(m_settings.Separator);
            string assetPrefix = fileName.Length > 0 ? fileNameSplit[0] : "";
            string assetName = fileNameSplit.Length > 1 ? fileNameSplit[1] : "";
            string assetCategory = fileNameSplit.Length > 2 ? fileNameSplit[2] : "";
            
            return (assetName, assetCategory, assetPrefix);
        }
        
        private static ArtOrganizationSettings LoadSettings()
        {
            string path = EditorPrefs.GetString(
                "ArtOrganizationSettings.SettingsFilePath",
                Path.Combine(Application.dataPath, ".artimportsettings.json")
            );

            if (!File.Exists(path)) return default;

            return new Serializer().Deserialize<ArtOrganizationSettings>(path);
        }
    }
}
