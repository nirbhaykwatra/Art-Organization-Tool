using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using FilePathAttribute = Sirenix.OdinInspector.FilePathAttribute;

namespace ArtPipeline
{
    [Serializable]
    public struct ArtOrganizationSettings
    {
        public bool MoveAssetsOnImport;
        public bool CreateFolderForVariants;
        public string MeshPrefix;
        public string TexturePrefix;
        public string MaterialPrefix;
        public string ShaderPath;
        public string ShaderBaseColorProperty;
        public string ShaderMHERProperty;
        public string ShaderNormalProperty;
        public string ArtFolderPath;
        public string PrefabFolderPath;
        public char Separator;
        
        public override string ToString()
        {
            return
                $"MoveAssetsOnImport: {MoveAssetsOnImport}, " +
                $"CreateFolderForVariants: {CreateFolderForVariants}, " +
                $"MeshPrefix: {MeshPrefix}, TexturePrefix: {TexturePrefix}, " +
                $"MaterialPrefix: {MaterialPrefix}, " +
                $"ArtFolderPath: {ArtFolderPath}, " +
                $"Separator: {Separator}, ";
        }
    }
    
    public class ArtOrganizationWindow :  OdinEditorWindow
    {
        [MenuItem("Tools/Art Organization Tool")]
        private static void OpenWindow() => GetWindow<ArtOrganizationWindow>();

        private readonly Serializer m_serializer = new();
        private ArtOrganizationSettings m_settings;
        
        private const string k_settingsPathKey = "ArtOrganizationSettings.SettingsFilePath";
        private const string k_defaultSettingsFileName = ".artimportsettings.json";
        
        string[] m_meshExtensions = new[] { ".fbx", ".obj", ".dae" };
        string[] m_materialExtensions = new[] { ".mat", ".shader" };
        string[] m_textureExtensions = new[] { ".png", ".jpg", ".jpeg" , ".tga"};

        private string SettingsFilePath
        {
            get => EditorPrefs.GetString(k_settingsPathKey, Path.Combine(Application.dataPath, k_defaultSettingsFileName));
            set => EditorPrefs.SetString(k_settingsPathKey, value);
        }
        
        // TODO: Fix ordering of variables

        [Title("Basic Information")]
        
        [ShowInInspector] 
        [PropertyOrder(-2)]
        [FolderPath(ParentFolder = "Assets", RequireExistingPath = true)]
        private string m_artFolderPath;
        
        [ShowInInspector] 
        [PropertyOrder(-2)]
        [FolderPath(ParentFolder = "Assets", RequireExistingPath = true)]
        private string m_prefabFolderPath;
        
        [TitleGroup("Asset Generation")]
        [ShowInInspector]
        [PropertyOrder(-2)]
        private Shader m_BaseShaderForMaterialGeneration;

        [TitleGroup("Asset Generation")] 
        [ShowInInspector] 
        [PropertyOrder(-2)]
        private string m_BaseColorProperty;
        
        [TitleGroup("Asset Generation")] 
        [ShowInInspector] 
        [PropertyOrder(-2)]
        private string m_MHERProperty;
        
        [TitleGroup("Asset Generation")] 
        [ShowInInspector] 
        [PropertyOrder(-2)]
        private string m_NormalProperty;

        [ShowInInspector]
        [TitleGroup("Import Settings")]
        [PropertyOrder(0)]
        private bool m_assetProcessingActive
        {
            get => SessionState.GetBool("ArtOrganizationSettings.AssetProcessingActive", false);
            set => SessionState.SetBool("ArtOrganizationSettings.AssetProcessingActive", value);
        }
        
        [TitleGroup("Import Settings")]
        [ShowInInspector]
        [PropertyOrder(1)]
        private bool m_moveAssetsOnImport;
        
        [TitleGroup("Import Settings")]
        [ShowInInspector]
        [PropertyOrder(1)]
        private bool m_createFolderForVariants;
        
        [TitleGroup("Import Settings")]
        [ShowInInspector]
        [PropertyOrder(2)]
        private string m_meshPrefix;
        
        [TitleGroup("Import Settings")]
        [ShowInInspector]
        [PropertyOrder(2)]
        private string m_texturePrefix;
        
        [TitleGroup("Import Settings")]
        [ShowInInspector]
        [PropertyOrder(2)]
        private string m_materialPrefix;
        
        [TitleGroup("Import Settings")]
        [ShowInInspector]
        [PropertyOrder(2)]
        private char m_separator;
        
        [Title("Tool Settings")]
        [PropertyOrder(3)]
        [FilePath(ParentFolder = "Assets", RequireExistingPath = true)]
        [ShowInInspector]
        private string m_settingsFilePath
        {
            get => SettingsFilePath;
            set => SettingsFilePath = value;
        }
        
        
        [PropertyOrder(-1)]
        [Button(ButtonSizes.Large)]
        private void CreateFolderStructureForSelectedAsset()
        {
            string assetFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(m_artFolderPath, Selection.activeObject.name));
            AssetDatabase.CreateFolder(assetFolder, "Meshes");
            AssetDatabase.CreateFolder(assetFolder, "Materials");
            AssetDatabase.CreateFolder(assetFolder, "Textures");
            AssetDatabase.CreateFolder(assetFolder, "Animations");
            AssetDatabase.CreateFolder(assetFolder, "Audio");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [TitleGroup("Asset Generation")]
        [PropertyOrder(-1)]
        [Button(ButtonSizes.Large)]
        private void GenerateMaterialsForSelectedAssetFolder()
        {
            if (m_BaseShaderForMaterialGeneration == null)
            {
                Debug.LogError("Base shader for material generation is not set. Please configure it in settings.");
                return;
            }
            
            string asset = Selection.activeObject != null ? Path.GetFullPath(AssetDatabase.GetAssetPath(Selection.activeObject)) : null;
            if (asset == null)
            {
                Debug.LogError("No asset selected for material generation.");
                return;
            }
            
            GenerateMaterialForAsset(asset);
        }
        
        [TitleGroup("Asset Generation")]
        [PropertyOrder(-1)]
        [Button(ButtonSizes.Large)]
        private void GenerateMaterialsForAllAssets()
        {
            if (m_BaseShaderForMaterialGeneration == null)
            {
                Debug.LogError("Base shader for material generation is not set. Please configure it in settings.");
                return;
            }
            
            Debug.Log("Starting material generation for all assets...");
            
            // Loop through the folder structures in the art folder path and generate Uber shader materials from Textures folder.
            string artFolderFullPath = Path.Combine(Application.dataPath, m_artFolderPath);
            Debug.Log($"Scanning art folder: {artFolderFullPath}");
            
            foreach (string category in Directory.EnumerateDirectories(artFolderFullPath))
            {
                Debug.Log($"Processing category: {category}");
                
                foreach (string asset in Directory.EnumerateDirectories(category))
                {
                    GenerateMaterialForAsset(asset);
                }
                
                Debug.Log("Cleared asset lists for next category");
            }
            
            Debug.Log("Material generation completed for all assets.");
        }

        private void GenerateMaterialForAsset(string assetPath)
        {
            Debug.Log($"Processing asset: {assetPath}");
            
            List<Texture2D> assetTextures = new();
            List<GameObject> assetMeshes = new();
            string assetMaterialFolder = null;
            
            Debug.Log("Starting material generation for selected asset folder...");
                    
            string texturesPath = Path.Combine(assetPath, "Textures");
            string materialsPath = Path.Combine(assetPath, "Materials");
            string meshesPath = Path.Combine(assetPath, "Meshes");

            if (Directory.Exists(texturesPath))
            {
                Debug.Log($"Found textures folder: {texturesPath}");
                
                foreach (string texturePath in Directory.EnumerateFiles(texturesPath))
                {
                    Debug.Log($"Processing texture: {texturePath}");
                    Debug.Log($"Extension for {Path.GetFileName(texturePath)}: {Path.GetExtension(texturePath)}");
                    if (!m_textureExtensions.Contains(Path.GetExtension(texturePath))) continue;
                    string relativePath = "Assets" + texturePath.Replace('\\', '/').Replace(Application.dataPath, string.Empty);
                    Debug.Log($"Texture relative path: {relativePath}");
                    if (File.Exists(Path.Combine(Application.dataPath, relativePath)) && (Path.GetFileNameWithoutExtension(relativePath).Contains("normal") 
                        || Path.GetFileNameWithoutExtension(relativePath).Contains("nrm")))
                    {
                        TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(relativePath);
                        texImporter.textureType = TextureImporterType.NormalMap;
                        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                    if (texture != null)
                    {
                        assetTextures.Add(texture);
                        Debug.Log($"Loaded texture: {texture.name}");
                    }
                }
                
                Debug.Log($"Total textures loaded: {assetTextures.Count}");
            }

            if (Directory.Exists(materialsPath))
            {
                assetMaterialFolder = materialsPath;
                Debug.Log($"Found materials folder: {materialsPath}");
            }
            
            if (Directory.Exists(meshesPath))
            {
                Debug.Log($"Found meshes folder: {meshesPath}");

                foreach (string meshPath in Directory.EnumerateFiles(meshesPath))
                {
                    Debug.Log($"Extension for {Path.GetFileName(meshPath)}: {Path.GetExtension(meshPath)}");
                    if (!m_meshExtensions.Contains(Path.GetExtension(meshPath))) continue;
                    string relativePath = "Assets" + meshPath.Replace('\\', '/').Replace(Application.dataPath, string.Empty);
                    GameObject mesh = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                    if (mesh != null)
                    {
                        assetMeshes.Add(mesh);
                        Debug.Log($"Loaded mesh: {mesh.name}");
                    }
                }
            }

            if (assetTextures.Count > 0)
            {
                Debug.Log($"Generating materials for {assetMeshes.Count} meshes...");
                
                foreach (GameObject mesh in assetMeshes)
                {
                    Debug.Log($"Creating material for mesh: {mesh.name}");
                    
                    List<string> materialNames = new();
                    List<Material> materials = new();

                    foreach (Texture2D tex in assetTextures)
                    {
                        string[] textureSplit = tex.name.Split(m_settings.Separator);
                        string materialName = textureSplit[3];
                        
                        if (materialNames.Contains(materialName))
                            continue;
                        
                        materialNames.Add(materialName);
                    }

                    foreach (string materialName in materialNames)
                    {
                        if (string.IsNullOrEmpty(materialName))
                            continue;
                        
                        Material material = new(m_BaseShaderForMaterialGeneration);
                        
                        foreach (Texture2D tex in assetTextures)
                        {
                            string texName = tex.name.ToLower();
                            string[] texSplit = tex.name.Split(m_settings.Separator);
                            string texMaterialName = texSplit[3];
                            
                            if (string.IsNullOrEmpty(texMaterialName))
                                continue;
                            
                            if (texMaterialName != materialName) 
                                continue;
                            
                            if (texName.Contains("albedo") || texName.Contains("diffuse") || texName.Contains("color"))
                            {
                                material.SetTexture(m_BaseColorProperty, tex);
                                Debug.Log($"Assigned {tex.name} to {m_BaseColorProperty} property.");
                            }
                            else if (texName.Contains("normal") || texName.Contains("nrm"))
                            {
                                material.SetTexture(m_NormalProperty, tex);
                                Debug.Log($"Assigned {tex.name} to {m_NormalProperty} property.");
                            }
                            else if (texName.Contains("metallic") 
                                     || texName.Contains("height")
                                     || texName.Contains("emission")
                                     || texName.Contains("roughness")
                                     || texName.Contains("metal")
                                     || texName.Contains("MHER")
                                     || texName.Contains("mher")
                                     || texName.Contains("metallicheightemissionroughness")
                                    )
                            {
                                material.SetTexture(m_MHERProperty, tex);
                                Debug.Log($"Assigned {tex.name} to {m_MHERProperty} property.");
                            }
                        }

                        if (assetMaterialFolder != null || materialName != null)
                        {
                            string[] meshSplit = mesh.name.Split(m_settings.Separator);
                            string meshName = meshSplit[1];
                            string materialSavePath = Path.Combine(
                                assetMaterialFolder, m_settings.MaterialPrefix + m_settings.Separator + materialName + ".mat");
                            string relativePath = "Assets" + materialSavePath.Replace('\\', '/').Replace(Application.dataPath, "");
                            Debug.Log($"Saving material to: {relativePath}");
                            AssetDatabase.CreateAsset(material, relativePath);
                            materials.Add(material);
                        }
                        else
                        {
                            Debug.LogWarning($"No material folder found for mesh: {mesh.name}");
                        }
                    }
                    Debug.Log($"Material assigned to mesh renderer: {mesh.name}");
                    
                    if (mesh.GetComponent<MeshRenderer>() == null)
                    {
                        Debug.LogWarning($"Mesh renderer not found for mesh: {mesh.name}");
                        continue;
                    }

                    Material[] meshMaterials = mesh.GetComponent<MeshRenderer>().sharedMaterials;
                    if (meshMaterials.Length == 1)
                    {
                        mesh.GetComponent<MeshRenderer>().material = materials[0];
                        return;
                    }
                    mesh.GetComponent<MeshRenderer>().SetMaterials(materials);
                }
            }
            else
            {
                Debug.Log($"No textures found for asset: {assetPath}");
            }
        }
        
        [TitleGroup("Asset Generation")]
        [PropertyOrder(-1)]
        [Button(ButtonSizes.Large)]
        private void GeneratePrefabForSelectedAssets()
        {
            UnityEngine.Object[] selectedAssets = Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets);
            foreach (UnityEngine.Object asset in selectedAssets)
            {
                if (asset == null)
                {
                    Debug.LogError($"Selected asset is null!");
                    continue;
                }
                
                GeneratePrefabForAsset(AssetDatabase.GetAssetPath(asset));
            }
        }
        
        [TitleGroup("Asset Generation")]
        [PropertyOrder(-1)]
        [Button(ButtonSizes.Large)]
        private void GeneratePrefabsForAllAssets()
        {
            string[] meshes = AssetDatabase.FindAssets("t:GameObject", new[] { m_artFolderPath });
            foreach (string meshGuid in meshes)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(meshGuid);
                GeneratePrefabForAsset(assetPath);
            }
        }
        
        private void GeneratePrefabForAsset(string assetPath)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            
            if (asset == null)
            {
                Debug.LogError($"Failed to load asset from path: {assetPath}");
                return;
            }

            string[] assetSplit = asset.name.Split(m_settings.Separator);
            string assetName = assetSplit[1]; 
            
            string prefabPath = Path.Combine(m_prefabFolderPath, assetName + ".prefab").Replace("\\", "/");
            string fullPath = Path.Combine(Application.dataPath, prefabPath);
            PrefabUtility.SaveAsPrefabAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Prefab generated for asset: {assetPath} at {prefabPath}");
        }

        [PropertyOrder(3)]
        [Button(ButtonSizes.Large)]
        private void SaveSettings()
        {
            ArtOrganizationSettings settings = new()
            {
                ArtFolderPath = m_artFolderPath,
                PrefabFolderPath = m_prefabFolderPath,
                CreateFolderForVariants = m_createFolderForVariants,
                MoveAssetsOnImport = m_moveAssetsOnImport,
                MaterialPrefix = m_materialPrefix,
                TexturePrefix = m_texturePrefix,
                MeshPrefix = m_meshPrefix,
                ShaderPath = AssetDatabase.GetAssetPath(m_BaseShaderForMaterialGeneration),
                ShaderBaseColorProperty = m_BaseColorProperty,
                ShaderMHERProperty = m_MHERProperty,
                ShaderNormalProperty = m_NormalProperty,
                Separator = m_separator,
            };

            m_serializer.Serialize(settings, SettingsFilePath);
            m_settings = settings;
        }
        
        [PropertyOrder(3)]
        [Button(ButtonSizes.Large)]
        private void ClearPrefs()
        {
            EditorPrefs.DeleteKey(k_settingsPathKey);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (EditorPrefs.GetString(k_settingsPathKey) == null ||
                !File.Exists(SettingsFilePath))
            {
                ArtOrganizationSettings settings = new()
                {
                    ArtFolderPath = m_artFolderPath,
                    PrefabFolderPath = m_prefabFolderPath,
                    CreateFolderForVariants = m_createFolderForVariants,
                    MoveAssetsOnImport = m_moveAssetsOnImport,
                    MaterialPrefix = m_materialPrefix,
                    TexturePrefix = m_texturePrefix,
                    MeshPrefix = m_meshPrefix,
                    ShaderPath = AssetDatabase.GetAssetPath(m_BaseShaderForMaterialGeneration),
                    ShaderBaseColorProperty = m_BaseColorProperty,
                    ShaderMHERProperty = m_MHERProperty,
                    ShaderNormalProperty = m_NormalProperty,
                    Separator = m_separator,
                };

                m_serializer.Serialize(settings, Path.Combine(Application.dataPath, k_defaultSettingsFileName));
                m_settings = settings;
                
                EditorPrefs.SetString(k_settingsPathKey, Path.Combine(Application.dataPath, k_defaultSettingsFileName));
                EditorPrefs.SetString(k_defaultSettingsFileName, k_defaultSettingsFileName);
            }
            
            LoadSettings();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (!string.IsNullOrEmpty(m_settingsFilePath)) SaveSettings();
        }
        
        private void LoadSettings()
        {
            string path = SettingsFilePath;

            if (!File.Exists(path))
            {
                // First run: save defaults and record the path in EditorPrefs
                SettingsFilePath = path; // persist the default
                SaveSettings();
                return;
            }

            m_settings = m_serializer.Deserialize<ArtOrganizationSettings>(path);

            m_artFolderPath = m_settings.ArtFolderPath;
            m_prefabFolderPath = m_settings.PrefabFolderPath;
            m_createFolderForVariants = m_settings.CreateFolderForVariants;
            m_moveAssetsOnImport = m_settings.MoveAssetsOnImport;
            m_materialPrefix = m_settings.MaterialPrefix;
            m_texturePrefix = m_settings.TexturePrefix;
            m_meshPrefix = m_settings.MeshPrefix;
            m_BaseShaderForMaterialGeneration = AssetDatabase.LoadAssetAtPath<Shader>(m_settings.ShaderPath);
            m_BaseColorProperty = m_settings.ShaderBaseColorProperty;
            m_MHERProperty = m_settings.ShaderMHERProperty;
            m_NormalProperty = m_settings.ShaderNormalProperty;
            m_separator = m_settings.Separator;
        }
    }
}
