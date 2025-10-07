using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArtPipeline
{
    public class ArtAssetPreprocessor : AssetPostprocessor
    {
        // Asset type configuration
        private static readonly AssetTypeConfig[] AssetConfigs = {
            new AssetTypeConfig("GameObject", "Meshes", "SCP.ArtToolSettings.MeshPrefix"),
            new AssetTypeConfig("Material", "Materials", "SCP.ArtToolSettings.MaterialPrefix"),
            new AssetTypeConfig("Texture2D", "Textures", "SCP.ArtToolSettings.TexturePrefix")
        };

        private void ProcessAsset<T>(T asset)
        {
            
            // If the model with the corresponding name exists in the project, move this texture into the Textures folder
            // of the model folder structure. If the Textures folder doesn't exist, create it (ModelName/Textures).
            // Do the same with the model and materials as well.
            
            // TODO: Add naming convention support. When an asset is imported, check its name for prefixes defined in the tool.
            //  If the asset doesn't have the defined prefix, assign it.
            
            // TODO: Add support for multiple meshes in the same folder. For example, if SM_handgun_01.fbx has variants named
            //  SM_handgun_01_mag_full.fbx and SM_handgun_01_mag_empty.fbx, add them to the Meshes folder in the handgun_01 folder.
            
            // TODO: Come up with a way to allow the user to import models without automatic sorting, on a per case basis.
            
            string artFolder = EditorPrefs.GetString("SCP.ArtToolSettings.ArtFolderPath", "");

            if (artFolder == "")
            {
                Debug.LogError("ArtOrganizationTool: artFolder not set! Please set artFolder first!");
                return;
            }

            if (!assetPath.Contains(artFolder))
            {
                return;
            }
            
            bool moveAssetOnImport = EditorPrefs.GetBool("SCP.ArtToolSettings.MoveAssetsOnImport", false);
            if (!moveAssetOnImport)
            {
                return;
            }
            
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string assetExtension = Path.GetExtension(assetPath);
            string assetType = typeof(T).Name;
            Debug.Log($"Processing asset {assetName} of type {assetType}");

            // Find the configuration for this asset type
            AssetTypeConfig config = Array.Find(AssetConfigs, c => c.TypeName == assetType);
            if (config == null) return; // Asset type not supported
            
            // Get prefix and separator from EditorPrefs
            string prefix = EditorPrefs.GetString(config.PrefixKey, "");
            string separator = EditorPrefs.GetString("SCP.ArtToolSettings.Separator", "");

            string[] assetNameSplit = assetName.Split(separator);

            AssetData assetData = new AssetData { AssetName = assetName, ArtFolderPath = artFolder, AssetPrefix = prefix, AssetSeparator = separator };
            
            (string finalAssetName, string finalFolderName) = GetAssetNameAndFolderName(assetData, assetNameSplit, config);
            Debug.Log($"After GetAssetNameAndFolderName:");
            Debug.Log($"Final asset name: {finalAssetName}");
            Debug.Log($"Final folder name: {finalFolderName}");
            
            // Build destination path
            string destination = BuildDestinationPath(artFolder, finalFolderName, config.FolderName, prefix, finalAssetName, separator, assetExtension);
            
            // Move asset to destination
            MoveAssetToDestination(artFolder, finalAssetName, config.FolderName, destination);
        }

        private (string finalAssetName, string finalFolderName) GetAssetNameAndFolderName(AssetData assetData, string[] assetNameSplit, AssetTypeConfig config)
        {
            string assetName = assetData.AssetName;
            string artFolder = assetData.ArtFolderPath;
            string prefix = assetData.AssetPrefix;
            string separator = assetData.AssetSeparator;
            
            Debug.Log($"GetAssetNameAndFolderName:");
            Debug.Log($"Asset name: {assetName}");
            Debug.Log($"Prefix: {prefix}");
            Debug.Log($"Separator: {separator}");
            Debug.Log($"Asset name split: {string.Join(", ", assetNameSplit)}");
            
            if (assetNameSplit.Length > 1)
            {
                if (assetNameSplit[0] == prefix)
                {
                    // If asset name starts with the prefix, remove it.
                    assetName = string.Join(separator, assetNameSplit, 1, assetNameSplit.Length - 1);
                    
                    string[] assetNameSubSplit = assetName.Split(separator);
                    if (assetNameSubSplit.Length > 1)
                    {
                        string assetParentName = assetNameSubSplit[0];
                        if (Directory.Exists(Path.Combine(artFolder, assetParentName)))
                        {
                            //return (assetName, assetParentName);
                        }
                    }
                    if (Directory.Exists(Path.Combine(artFolder, assetName)))
                    {
                        string assetParentName = assetNameSplit[1];
                        return (assetName, assetParentName);
                    }
                }
                else if (assetNameSplit[0] != prefix)
                {
                    string assetParentName = assetNameSplit[0];
                    if (Directory.Exists(Path.Combine(artFolder, assetParentName)))
                    {
                        return (assetName, assetParentName);
                    }
                }
            }
            
            return (assetName, assetName);
        }
        
        private string BuildDestinationPath(string artFolder, string assetFolderName, string subFolderName, string prefix, string assetName, string separator, string extension)
        {
            return Path.Combine(artFolder, assetFolderName, subFolderName, prefix + separator + assetName + extension);
        }

        private void MoveAssetToDestination(string artFolder, string assetName, string folderName, string destination)
        {
            string assetFolderPath = Path.Combine(artFolder, assetName);
            
            // Create asset folder if it doesn't exist
            if (!Directory.Exists(assetFolderPath))
            {
                Directory.CreateDirectory(assetFolderPath);
            }
            
            // Create subfolder if it doesn't exist
            string subFolderPath = Path.Combine(assetFolderPath, folderName);
            if (!Directory.Exists(subFolderPath))
            {
                AssetDatabase.CreateFolder(assetFolderPath, folderName);
            }

            // Move the asset
            if (!File.Exists(destination))
            {
                Debug.Log($"Moving asset {assetName} to {destination}");
                File.Move(assetPath, destination);
            }
        }

        private void OnPostprocessTexture(Texture2D texture)
        {
            ProcessAsset(texture);
        }

        private void OnPostprocessModel(GameObject model)
        {
            ProcessAsset(model);
        }

        private void OnPostprocessMaterial(Material material)
        {
            ProcessAsset(material);
        }

        // Helper class to define asset type configurations
        private class AssetTypeConfig
        {
            public string TypeName { get; }
            public string FolderName { get; }
            public string PrefixKey { get; }

            public AssetTypeConfig(string typeName, string folderName, string prefixKey)
            {
                TypeName = typeName;
                FolderName = folderName;
                PrefixKey = prefixKey;
            }
        }

        private struct AssetData
        {
            public string AssetName;
            public string ArtFolderPath;
            public string AssetPrefix;
            public string AssetSeparator;
        }
    }
}
