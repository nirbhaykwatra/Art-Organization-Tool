using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ArtAssetPreprocessor : AssetPostprocessor
{
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
            Debug.LogError("Asset is not in the art folder!");
            return;
        }
        
        bool moveAssetOnImport = EditorPrefs.GetBool("SCP.ArtToolSettings.MoveAssetsOnImport", false);
        if (!moveAssetOnImport) return;
        
        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        string assetExtension = Path.GetExtension(assetPath);
        
        string assetType = typeof(T).Name;
        
        Debug.Log($"Asset Path: {assetPath}");

        switch (typeof(T).Name)
        {
            case "GameObject":
                if (Directory.Exists(artFolder + "/" + assetName))
                {
                    File.Move(assetPath, artFolder + "/" + assetName + "/Meshes/" + assetName + assetExtension);
                }
                else
                {
                    AssetDatabase.CreateFolder(artFolder, assetName);
                    AssetDatabase.CreateFolder(artFolder + "/" + assetName, "Meshes");
                    File.Move(assetPath, artFolder + "/" + assetName + "/Meshes/" + assetName + assetExtension);
                }
                break;
        }
        
    }
    private void OnPostprocessTexture(Texture2D texture)
    {
        string textureName = Path.GetFileNameWithoutExtension(assetPath);
    }

    private void OnPostprocessModel(GameObject model)
    {
        string modelName = Path.GetFileNameWithoutExtension(assetPath);
        
        ProcessAsset(model);
    }

    private void OnPostprocessMaterial(Material material)
    {
        string materialName = Path.GetFileNameWithoutExtension(assetPath);
    }

}
