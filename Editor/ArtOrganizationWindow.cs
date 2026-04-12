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
        public string ArtFolderPath;
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
        [ShowInInspector]
        [Title("Create Folder Structure")]
        [AssetsOnly]
        [AssetSelector(Filter = "t:Model")]
        [InfoBox("Click the small three dots next to the selection field to see a list of model files in the project.")]
        [PropertyOrder(-1)]
        private GameObject m_selectedObject;

        [Title("Basic Information")]
        
        [ShowInInspector] 
        [PropertyOrder(-2)]
        [FolderPath(ParentFolder = "Assets", RequireExistingPath = true)]
        private string m_artFolderPath;
        
        [TitleGroup("Asset Generation")]
        [ShowInInspector]
        [PropertyOrder(-2)]
        private Shader m_BaseShaderForMaterialGeneration;

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
            List<Texture2D> assetTextures = new();
            List<GameObject> assetMeshes = new();
            string assetMaterialFolder = null;
            
            string artFolderFullPath = Path.Combine(Application.dataPath, m_artFolderPath);
            Debug.Log($"Scanning art folder: {artFolderFullPath}");
            
            foreach (string category in Directory.EnumerateDirectories(artFolderFullPath))
            {
                Debug.Log($"Processing category: {category}");
                
                foreach (string asset in Directory.EnumerateDirectories(category))
                {
                    Debug.Log($"Processing asset: {asset}");
                    
                    string texturesPath = Path.Combine(asset, "Textures");
                    string materialsPath = Path.Combine(asset, "Materials");
                    string meshesPath = Path.Combine(asset, "Meshes");

                    if (Directory.Exists(texturesPath))
                    {
                        Debug.Log($"Found textures folder: {texturesPath}");
                        
                        foreach (string texturePath in Directory.EnumerateFiles(texturesPath))
                        {
                            Debug.Log($"Extension for {Path.GetFileName(texturePath)}: {Path.GetExtension(texturePath)}");
                            if (!m_textureExtensions.Contains(Path.GetExtension(texturePath))) continue;
                            string relativePath = "Assets" + texturePath.Replace(Application.dataPath, "").Replace('\\', '/');
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
                            string relativePath = "Assets" + meshPath.Replace(Application.dataPath, "").Replace('\\', '/');
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
                            
                            Material material = new(m_BaseShaderForMaterialGeneration);
            
                            foreach (Texture2D tex in assetTextures)
                            {
                                string texName = tex.name.ToLower();

                                if (texName.Contains("albedo") || texName.Contains("diffuse") || texName.Contains("color"))
                                {
                                    material.SetTexture("_MainTex", tex);
                                    Debug.Log($"Assigned {tex.name} to _MainTex");
                                }
                                else if (texName.Contains("normal") || texName.Contains("nrm"))
                                {
                                    material.SetTexture("_Normal", tex);
                                    Debug.Log($"Assigned {tex.name} to _Normal");
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
                                    material.SetTexture("_Mask", tex);
                                    Debug.Log($"Assigned {tex.name} to _Mask");
                                }
                            }

                            if (assetMaterialFolder != null)
                            {
                                string materialSavePath = Path.Combine(assetMaterialFolder, mesh.name + ".mat");
                                string relativePath = "Assets" + materialSavePath.Replace(Application.dataPath, "").Replace('\\', '/');
                                Debug.Log($"Saving material to: {relativePath}");
                                AssetDatabase.CreateAsset(material, relativePath);
                                AssetDatabase.SaveAssets();    
                                AssetDatabase.Refresh();
                            }
                            else
                            {
                                Debug.LogWarning($"No material folder found for mesh: {mesh.name}");
                            }
                    
                            mesh.GetComponent<MeshRenderer>().material = material;
                            Debug.Log($"Material assigned to mesh renderer: {mesh.name}");
                        }
                    }
                    else
                    {
                        Debug.Log($"No textures found for asset: {asset}");
                    }
                }
                
                assetTextures.Clear();
                assetMeshes.Clear();
                Debug.Log("Cleared asset lists for next category");
            }
            
            Debug.Log("Material generation completed for all assets.");
        }
        
        [TitleGroup("Asset Generation")]
        [PropertyOrder(-1)]
        [Button(ButtonSizes.Large)]
        private void GeneratePrefabsForAllAssets()
        {
            // Loop through the folder structures in the art folder path and generate prefabs using the Meshes, Materials and Audio folders.
        }

        [PropertyOrder(3)]
        [Button(ButtonSizes.Large)]
        private void SaveSettings()
        {
            ArtOrganizationSettings settings = new()
            {
                ArtFolderPath = m_artFolderPath,
                CreateFolderForVariants = m_createFolderForVariants,
                MoveAssetsOnImport = m_moveAssetsOnImport,
                MaterialPrefix = m_materialPrefix,
                TexturePrefix = m_texturePrefix,
                MeshPrefix = m_meshPrefix,
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
            m_createFolderForVariants = m_settings.CreateFolderForVariants;
            m_moveAssetsOnImport = m_settings.MoveAssetsOnImport;
            m_materialPrefix = m_settings.MaterialPrefix;
            m_texturePrefix = m_settings.TexturePrefix;
            m_meshPrefix = m_settings.MeshPrefix;
            m_separator = m_settings.Separator;
        }
    }
}
