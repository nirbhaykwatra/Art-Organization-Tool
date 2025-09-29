using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ArtAssetPreprocessor : AssetPostprocessor
{
    private void OnPostprocessTexture(Texture2D texture)
    {
        // If the model with the corresponding name exists in the project, move this texture into the Textures folder
        // of the model folder structure. If the Textures folder doesn't exist, create it (ModelName/Textures).
        // Do the same with the model and materials as well
        string textureName = Path.GetFileNameWithoutExtension(assetPath);
    }

    private void OnPostprocessModel(GameObject model)
    {
        
    }

    private void OnPostprocessMaterial(Material material)
    {
        
    }
}
