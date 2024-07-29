/* 
 * Copyright (C) 2021 Victor Soupday
 * This file is part of CC_Unity_Tools <https://github.com/soupday/CC_Unity_Tools>
 * 
 * CC_Unity_Tools is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * CC_Unity_Tools is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with CC_Unity_Tools.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reallusion.Import
{
    public class Importer
    {
        private readonly GameObject fbx;
        private readonly QuickJSON jsonData;        
        private readonly QuickJSON jsonPhysicsData;
        private readonly string fbxPath;
        private readonly string fbxFolder;
        private readonly string fbmFolder;
        private readonly string texFolder;
        private readonly string materialsFolder;
        private readonly string characterName;
        private readonly int id;
        private readonly List<string> textureFolders;
        private readonly ModelImporter importer;
        private readonly List<string> importAssets = new List<string>();
        private CharacterInfo characterInfo;
        private List<string> processedBuildMaterials;
        private List<string> doneTextureGUIDS = new List<string>();
        private Dictionary<Material, Texture2D> bakedDetailMaps;
        private Dictionary<Material, Texture2D> bakedThicknessMaps;
        private Dictionary<Material, Texture2D> bakedHDRPMaps;
        private readonly BaseGeneration generation;
        private readonly bool blenderProject;

        public const string MATERIALS_FOLDER = "Materials";
        public const string PREFABS_FOLDER = "Prefabs";
        public const string BAKE_SUFFIX = "_Baked";
        
        public const float MIPMAP_BIAS_HAIR_ID_MAP = -1f;
        public const float MIPMAP_ALPHA_CLIP_HAIR = 0.6f;
        public const float MIPMAP_ALPHA_CLIP_HAIR_BAKED = 0.8f;

        public const int FLAG_SRGB = 1;
        public const int FLAG_NORMAL = 2;
        public const int FLAG_FOR_BAKE = 4;
        public const int FLAG_ALPHA_CLIP = 8;
        public const int FLAG_HAIR = 16;
        public const int FLAG_ALPHA_DATA = 32;
        public const int FLAG_HAIR_ID = 64;
        public const int FLAG_WRAP_CLAMP = 1024;
        public const int FLAG_READ_WRITE = 2048;
        public const int FLAG_UNCOMPRESSED = 2048;

        public const float MAX_SMOOTHNESS = 0.897f;        
        public const float TRA_SPECULAR_SCALE = 0.2f;

        public static float MIPMAP_BIAS
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Importer_Mipmap_Bias"))
                    return EditorPrefs.GetFloat("RL_Importer_Mipmap_Bias");
                return 0f;
            }

            set
            {
                EditorPrefs.SetFloat("RL_Importer_Mipmap_Bias", value);
            }
        }

        public static bool USE_AMPLIFY_SHADER
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Importer_Use_Amplify_Shaders"))
                    return EditorPrefs.GetBool("RL_Importer_Use_Amplify_Shaders");
                return true;
            }

            set
            {
                EditorPrefs.SetBool("RL_Importer_Use_Amplify_Shaders", value);
            }
        }

        public static bool USE_DIGITAL_HUMAN_SHADER
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Importer_Use_Digital_Human_Shaders"))
                    return EditorPrefs.GetBool("RL_Importer_Use_Digital_Human_Shaders");
                return false;
            }

            set
            {
                EditorPrefs.SetBool("RL_Importer_Use_Digital_Human_Shaders", value);
            }
        }

        public static bool ANIMPLAYER_ON_BY_DEFAULT
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Importer_Animation_Player_On"))
                    return EditorPrefs.GetBool("RL_Importer_Animation_Player_On");
                return false;
            }

            set
            {
                EditorPrefs.SetBool("RL_Importer_Animation_Player_On", value);
            }
        }

        public static bool USE_SELF_COLLISION
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Importer_Use_Self_Collision"))
                    return EditorPrefs.GetBool("RL_Importer_Use_Self_Collision");
                return false;
            }

            set
            {
                EditorPrefs.SetBool("RL_Importer_Use_Self_Collision", value);
            }
        }

        public static bool RECONSTRUCT_FLOW_NORMALS
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Importer_Reconstruct_Flow_Normals"))
                    return EditorPrefs.GetBool("RL_Importer_Reconstruct_Flow_Normals");
                return false;
            }

            set
            {
                EditorPrefs.SetBool("RL_Importer_Reconstruct_Flow_Normals", value);
            }
        }

        public static bool REBAKE_BLENDER_UNITY_MAPS
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Rebake_Blender_Unity_Maps"))
                    return EditorPrefs.GetBool("RL_Rebake_Blender_Unity_Maps");
                return false;
            }

            set
            {
                EditorPrefs.SetBool("RL_Rebake_Blender_Unity_Maps", value);
            }
        }

        private RenderPipeline RP => Pipeline.GetRenderPipeline();

        public Importer(CharacterInfo info)
        {
            Util.LogInfo("Initializing character import.");

            // fetch all the asset details for this character fbx object.
            characterInfo = info;
            fbx = info.Fbx;
            id = fbx.GetInstanceID();
            fbxPath = info.path;
            importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            characterName = info.name;
            fbxFolder = info.folder;

            // construct the texture folder list for the character.
            fbmFolder = Path.Combine(fbxFolder, characterName + ".fbm");
            texFolder = Path.Combine(fbxFolder, "textures", characterName);
            textureFolders = new List<string>() { fbmFolder, texFolder };

            Util.LogInfo("Using texture folders:");
            Util.LogInfo("    " + fbmFolder);
            Util.LogInfo("    " + texFolder);

            // find or create the materials folder for the character import.
            string parentMaterialsFolder = Util.CreateFolder(fbxFolder, MATERIALS_FOLDER);
            materialsFolder = Util.CreateFolder(parentMaterialsFolder, characterName);
            Util.LogInfo("Using material folder: " + materialsFolder);

            // fetch the character json export data.            
            jsonData = info.JsonData;            
            if (jsonData == null) Util.LogError("Unable to find Json data!");
            
            jsonPhysicsData = info.PhysicsJsonData;
            if (jsonPhysicsData == null)
                Util.LogWarn("Unable to find Json physics data!");

            string jsonVersion = jsonData?.GetStringValue(characterName + "/Version");
            if (!string.IsNullOrEmpty(jsonVersion))
                Util.LogInfo("JSON version: " + jsonVersion);

            generation = info.Generation;
            blenderProject = info.IsBlenderProject;

            // initialise the import path cache.        
            // this is used to re-import everything in one batch after it has all been setup.
            // (calling a re-import on sub-materials or sub-objects will trigger a re-import of the entire fbx each time...)
            importAssets = new List<string>(); // { fbxPath };
            processedBuildMaterials = new List<string>();

            bakedDetailMaps = new Dictionary<Material, Texture2D>();
            bakedThicknessMaps = new Dictionary<Material, Texture2D>();
            bakedHDRPMaps = new Dictionary<Material, Texture2D>();
        }

        public GameObject Import(bool batchMode = false)
        {
            // make sure custom diffusion profiles are installed
            Pipeline.AddDiffusionProfilesHDRP();

            // firstly make sure the fbx is in:
            //      material creation mode: import via materialDescription
            //      location: use emmbedded materials
            //      extract any embedded textures
            bool reimport = false;
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                reimport = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            }
            if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
            {
                reimport = true;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            }

            // only if we need to...
            if (!AssetDatabase.IsValidFolder(fbmFolder))
            {
                Util.LogInfo("Extracting embedded textures to: " + fbmFolder);
                Util.CreateFolder(fbxPath, characterName + ".fbm");
                importer.ExtractTextures(fbmFolder);
            }

            // clean up missing material remaps:
            Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> remaps = importer.GetExternalObjectMap();
            List<AssetImporter.SourceAssetIdentifier> remapsToCleanUp = new List<AssetImporter.SourceAssetIdentifier>();
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> pair in remaps)
            {
                if (pair.Value == null)
                {
                    remapsToCleanUp.Add(pair.Key);
                    reimport = true;
                }
            }
            foreach (AssetImporter.SourceAssetIdentifier key in remapsToCleanUp) importer.RemoveRemap(key);

            // if nescessary write changes and reimport so that the fbx is populated with mappable material names:
            if (reimport)
            {
                Util.LogInfo("Resetting import settings for correct material generation and reimporting.");
                AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
            }

            Util.LogInfo("Processing Materials.");

            // Note: the material setup is split into three passes because,
            //       Unity's asset dependencies can cause re-import loops on *every* texure re-import.
            //       To minimise this, the import is done in three passes, and
            //       any import changes are applied and re-imported after each pass.
            //       So the *worst case* scenario is that the fbx/blend file is only re-imported three
            //       times, rather than potentially hundreds if caught in a recursive import loop.            

            // set up import settings in preparation for baking default and/or blender textures.            
            ProcessObjectTreePrepass(fbx);

            // bake additional default or unity packed textures from blender export.            
            ProcessObjectTreeBakePass(fbx);

            // create / apply materials and shaders with supplied or baked texures.            
            ProcessObjectTreeBuildPass(fbx);

            characterInfo.tempHairBake = false;

            Util.LogInfo("Writing changes to asset database.");

            // set humanoid animation type
            RL.HumanoidImportSettings(fbx, importer, characterInfo);
            if (blenderProject)
            {
                importer.importCameras = false;
                importer.importLights = false;
                importer.importVisibility = true;
                importer.useFileUnits = true;
            }

            // setup initial animations (only do this once)
            if (!characterInfo.animationSetup)
            {
                RL.SetupAnimation(importer, characterInfo, false);
            }

            // save all the changes and refresh the asset database.
            AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
            importAssets.Add(fbxPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // create prefab.
            string prefabAssetPath = RL.InitCharacterPrefab(characterInfo);
            GameObject prefabInstance = RL.InstantiateModelFromSource(characterInfo, fbx, prefabAssetPath);

            // setup 2 pass hair in the prefab.
            if (characterInfo.DualMaterialHair)
            {
                Util.LogInfo("Extracting 2 Pass hair meshes.");
                MeshUtil.Extract2PassHairMeshes(characterInfo, prefabInstance);
                ImporterWindow.TrySetMultiPass(true);
            }

            bool clothPhysics = (characterInfo.ShaderFlags & CharacterInfo.ShaderFeatureFlags.ClothPhysics) > 0;
            bool hairPhysics = (characterInfo.ShaderFlags & CharacterInfo.ShaderFeatureFlags.HairPhysics) > 0;
            bool springBoneHair = (characterInfo.ShaderFlags & CharacterInfo.ShaderFeatureFlags.SpringBoneHair) > 0;
            if ((clothPhysics || hairPhysics || springBoneHair) && jsonPhysicsData != null)
            {
                Physics physics = new Physics(characterInfo, prefabInstance);
                physics.AddPhysics(true);
            }

            if (blenderProject)
            {
                MeshUtil.FixSkinnedMeshBounds(prefabInstance);
            }

            // apply post setup to prefab instance
            ProcessObjectTreePostPass(prefabInstance);

            Util.LogAlways("Done building materials for character " + characterName + "!");

            // extract and retarget animations if needed.
            int animationRetargeted = characterInfo.DualMaterialHair ? 2 : 1;
            bool replace = characterInfo.animationRetargeted != animationRetargeted;
            if (replace) Util.LogInfo("Retargeting all imported animations.");
            AnimRetargetGUI.GenerateCharacterTargetedAnimations(fbxPath, prefabInstance, replace);            

            // create default animator if there isn't one:
            //  commenting out due to a unity bug in 2022+,
            //  adding any animator controller to a skinned mesh renderer prefab
            //  generates a memory leak warning.
            //RL.AddDefaultAnimatorController(characterInfo, prefabInstance);

            List<string> motionGuids = characterInfo.GetMotionGuids();
            if (motionGuids.Count > 0)
            {
                Avatar sourceAvatar = characterInfo.GetCharacterAvatar();
                if (sourceAvatar)
                {
                    foreach (string guid in motionGuids)
                    {
                        ProcessMotionFbx(guid, sourceAvatar, prefabInstance);
                    }
                }
            }

            characterInfo.animationRetargeted = animationRetargeted;

            // save final prefab instance and remove from scene
            GameObject prefabAsset = RL.SaveAndRemovePrefabInstance(prefabInstance, prefabAssetPath);

            if (!batchMode) Selection.activeObject = prefabAsset;
            else Selection.activeObject = null;            

            return prefabAsset;
        }        

        void ProcessObjectTreeBuildPass(GameObject obj)
        {
            doneTextureGUIDS.Clear();
            processedBuildMaterials.Clear();

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                ProcessObjectBuildPass(renderer);
            }            
        }

        private void ProcessObjectBuildPass(Renderer renderer)
        {
            GameObject obj = renderer.gameObject;

            if (renderer)
            {
                Util.LogInfo("Processing sub-object: " + obj.name);

                foreach (Material sharedMat in renderer.sharedMaterials)
                {
                    if (!sharedMat) continue;

                    // in case any of the materials have been renamed after a previous import, get the source name.
                    string sourceName = Util.GetSourceMaterialName(fbxPath, sharedMat);

                    // if the material has already been processed, it is already in the remap list and should be connected automatically.
                    if (!processedBuildMaterials.Contains(sourceName))
                    {
                        // fetch the json parent for this material.
                        // the json data for the material contains custom shader names, parameters and texture paths.
                        QuickJSON matJson = characterInfo.GetMatJson(obj, sourceName);

                        // determine the material type, this dictates the shader and template material.
                        MaterialType materialType = GetMaterialType(obj, sharedMat, sourceName, matJson);

                        Util.LogInfo("    Material name: " + sourceName + ", type:" + materialType.ToString());

                        // re-use or create the material.
                        Material mat = CreateRemapMaterial(materialType, sharedMat, sourceName);

                        // connect the textures.
                        if (mat) ProcessTextures(obj, sourceName, sharedMat, mat, materialType, matJson);

                        processedBuildMaterials.Add(sourceName);
                    }
                    else
                    {
                        Util.LogInfo("    Material name: " + sourceName + " already processed.");
                    }
                }
            }
        }

        void ProcessObjectTreePrepass(GameObject obj)
        {
            doneTextureGUIDS.Clear();
            processedBuildMaterials.Clear();

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                ProcessObjectPrepass(renderer);
            }
            
            AssetDatabase.WriteImportSettingsIfDirty(fbxPath);            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ProcessObjectPrepass(Renderer renderer)
        {
            GameObject obj = renderer.gameObject;

            if (renderer)
            {
                foreach (Material sharedMat in renderer.sharedMaterials)
                {
                    if (!sharedMat) continue;

                    string sourceName = Util.GetSourceMaterialName(fbxPath, sharedMat);
                    if (!processedBuildMaterials.Contains(sourceName))
                    {
                        QuickJSON matJson = characterInfo.GetMatJson(obj, sourceName);
                        MaterialType materialType = GetMaterialType(obj, sharedMat, sourceName, matJson);                        

                        if (matJson != null)
                        {
                            if (blenderProject)
                                PrepBlenderTextures(sourceName, matJson);

                            if ((materialType == MaterialType.SSS || characterInfo.BasicMaterials) && Pipeline.isHDRP)
                            {
                                if (materialType == MaterialType.Skin || 
                                    materialType == MaterialType.Head ||
                                    materialType == MaterialType.SSS)
                                {
                                    PrepDefaultMap(sourceName, "TransMap", matJson, "Custom Shader/Image/Transmission Map");
                                    PrepDefaultMap(sourceName, "MicroN", matJson, "Custom Shader/Image/MicroNormal", FLAG_NORMAL);
                                }
                            }
                            
                            if (materialType == MaterialType.Skin || materialType == MaterialType.Head)
                            {
                                FixHDRPMap(sharedMat, sourceName, matJson);
                            }
                        }
                    }
                }
            }
        }

        void ProcessObjectTreeBakePass(GameObject obj)
        {
            doneTextureGUIDS.Clear();
            processedBuildMaterials.Clear();

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                ProcessObjectBakePass(renderer);
            }
            
            AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ProcessObjectBakePass(Renderer renderer)
        {
            GameObject obj = renderer.gameObject;

            if (renderer)
            {
                foreach (Material sharedMat in renderer.sharedMaterials)
                {
                    if (!sharedMat) continue;

                    string sourceName = Util.GetSourceMaterialName(fbxPath, sharedMat);
                    if (!processedBuildMaterials.Contains(sourceName))
                    {
                        QuickJSON matJson = characterInfo.GetMatJson(obj, sourceName);
                        MaterialType materialType = GetMaterialType(obj, sharedMat, sourceName, matJson);                        

                        if (matJson != null)
                        {
                            if (blenderProject)
                                BakeBlenderTextures(sourceName, matJson);

                            if (materialType == MaterialType.SSS || characterInfo.BasicMaterials)
                            {
                                if (materialType == MaterialType.Skin || 
                                    materialType == MaterialType.Head || 
                                    materialType == MaterialType.SSS)
                                {
                                    BakeDefaultMap(sharedMat, sourceName, "_ThicknessMap", "TransMap",
                                        matJson, "Custom Shader/Image/Transmission Map");

                                    if (RP == RenderPipeline.HDRP) // only HDRP uses the packed detail map
                                    {
                                        BakeDefaultMap(sharedMat, sourceName, "_DetailMap", "MicroN",
                                            matJson, "Custom Shader/Image/MicroNormal");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void ProcessObjectTreePostPass(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                ProcessObjectPostPass(renderer);
            }
        }

        private void ProcessObjectPostPass(Renderer renderer)
        {
            GameObject obj = renderer.gameObject;

            if (renderer)
            {
                Util.LogInfo("Post Processing sub-object: " + obj.name);

                foreach (Material sharedMat in renderer.sharedMaterials)
                {
                    if (!sharedMat) continue;

                    // in case any of the materials have been renamed after a previous import, get the source name.
                    string sourceName = Util.GetSourceMaterialName(fbxPath, sharedMat);

                    // fetch the json parent for this material.
                    // the json data for the material contains custom shader names, parameters and texture paths.
                    QuickJSON matJson = characterInfo.GetMatJson(obj, sourceName);

                    // determine the material type, this dictates the shader and template material.
                    MaterialType materialType = GetMaterialType(obj, sharedMat, sourceName, matJson);                        

                    // Fix ray tracing and shadow casting
                    FixRayTracing(obj, sharedMat, materialType);

                    if (materialType == MaterialType.Head && characterInfo.FeatureUseWrinkleMaps)
                    {
                        if (renderer.GetType() == typeof(SkinnedMeshRenderer))
                        {                            
                            AddWrinkleManager(obj, (SkinnedMeshRenderer)renderer, sharedMat, matJson);
                        }
                    }
                }
            }
        }        

        private MaterialType GetMaterialType(GameObject obj, Material mat, string sourceName, QuickJSON matJson)
        {            
            if (matJson != null)
            {
                bool hasOpacity = false;
                bool blendOpacity = false;
                if (Util.NameContainsKeywords(sourceName, "Transparency", "Alpha", "Opacity", "Lenses", "Lens", "Glass", "Glasses", "Blend"))
                {
                    hasOpacity = true;
                    blendOpacity = true;
                }

                if (Util.NameContainsKeywords(sourceName, "Base", "Scalp", "Eyelash"))
                {
                    hasOpacity = true;
                    blendOpacity = true;
                }

                if (matJson != null)
                {
                    string texturePath = matJson.GetStringValue("Textures/Opacity/Texture Path");
                    if (!string.IsNullOrEmpty(texturePath)) hasOpacity = true;
                    float opacity = matJson.GetFloatValue("Opacity");
                    if (opacity < 1.0f)
                    {
                        hasOpacity = true;
                        blendOpacity = true;
                    }
                }                

                if (Util.NameContainsKeywords(sourceName, "Std_Eye_L", "Std_Eye_R"))
                {
                    return MaterialType.Eye;
                }

                // actor build materials that are opaque, but detected as transparent.
                if (characterInfo.Generation == BaseGeneration.ActorBuild)
                {
                    if (Util.NameContainsKeywords(sourceName, "Cornea_L", "Cornea_R",
                            "Upper_Teeth", "Lower_Teeth", "Tongue"))
                    {
                        return MaterialType.DefaultOpaque;
                    }
                }

                if (hasOpacity)
                {
                    if (Util.NameContainsKeywords(sourceName, "Eyelash"))
                        return MaterialType.Eyelash;
                    if (Util.NameContainsKeywords(sourceName, "Scalp", "Base"))
                        return MaterialType.Scalp;
                }

                string customShader = matJson?.GetStringValue("Custom Shader/Shader Name");
                string defaultType = matJson?.GetStringValue("Material Type");

                switch (customShader)
                {
                    case "RLEyeOcclusion": return MaterialType.EyeOcclusion;
                    case "RLEyeTearline": return MaterialType.Tearline;
                    case "RLHair": return MaterialType.Hair;
                    case "RLSkin": return MaterialType.Skin;
                    case "RLHead": return MaterialType.Head;
                    case "RLTongue": return MaterialType.Tongue;
                    case "RLTeethGum": return MaterialType.Teeth;
                    case "RLEye": return MaterialType.Cornea;
                    case "RLSSS": return MaterialType.SSS;
                    default:
                        if (blendOpacity) return MaterialType.BlendAlpha;
                        else if (hasOpacity) return MaterialType.DefaultAlpha;                        
                        else return MaterialType.DefaultOpaque;
                }
            }
            else
            {
                // if there is no JSON, try to determine the material types from the names.

                if (Util.NameContainsKeywords(sourceName, "Std_Eye_L", "Std_Eye_R"))
                    return MaterialType.Eye;

                if (Util.NameContainsKeywords(sourceName, "Std_Cornea_L", "Std_Cornea_R"))
                    return MaterialType.Cornea;

                if (Util.NameContainsKeywords(sourceName, "Std_Eye_Occlusion_L", "Std_Eye_Occlusion_R"))
                    return MaterialType.EyeOcclusion;

                if (Util.NameContainsKeywords(sourceName, "Std_Tearline_L", "Std_Tearline_R"))
                    return MaterialType.Tearline;

                if (Util.NameContainsKeywords(sourceName, "Std_Upper_Teeth", "Std_Lowe_Teeth"))
                    return MaterialType.Teeth;

                if (Util.NameContainsKeywords(sourceName, "Std_Tongue"))
                    return MaterialType.Tongue;

                if (Util.NameContainsKeywords(sourceName, "Std_Skin_Head"))
                    return MaterialType.Head;

                if (Util.NameContainsKeywords(sourceName, "Std_Skin_"))
                    return MaterialType.Skin;

                if (Util.NameContainsKeywords(sourceName, "Std_Nails"))
                    return MaterialType.Skin;

                if (Util.NameContainsKeywords(sourceName, "Eyelash"))
                    return MaterialType.Eyelash;

                // Detecting the hair is harder to do...

                return MaterialType.DefaultOpaque;
            }
        }        

        private Material CreateRemapMaterial(MaterialType materialType, Material sharedMaterial, string sourceName)
        {            
            // get the template material.
            Material templateMaterial = Pipeline.GetTemplateMaterial(sourceName, materialType, 
                characterInfo.BuildQuality, 
                characterInfo, USE_AMPLIFY_SHADER, 
                characterInfo.FeatureUseTessellation, 
                characterInfo.FeatureUseWrinkleMaps,
                USE_DIGITAL_HUMAN_SHADER);

            // get the appropriate shader to use            
            Shader shader;
            if (templateMaterial && templateMaterial.shader != null)
                shader = templateMaterial.shader;
            else
                shader = Pipeline.GetDefaultShader();

            // check that shader exists.
            if (!shader)
            {
                Util.LogError("No shader found for material: " + sourceName);
                return null;
            }

            Material remapMaterial = sharedMaterial;            

            // if the material is missing or it is embedded in the fbx, create a new unique material:
            if (!remapMaterial || AssetDatabase.GetAssetPath(remapMaterial) == fbxPath)
            {
                // create the remapped material and save it as an asset.
                string matPath = AssetDatabase.GenerateUniqueAssetPath(
                        Path.Combine(materialsFolder, sourceName + ".mat")
                    );

                remapMaterial = new Material(shader);

                // save the material to the asset database.
                AssetDatabase.CreateAsset(remapMaterial, matPath);

                Util.LogInfo("    Created new material: " + remapMaterial.name);

                // add the new remapped material to the importer remaps.
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName), remapMaterial);
            }

            // copy the template material properties to the remapped material.
            if (templateMaterial)
            {
                Util.LogInfo("    Using template material: " + templateMaterial.name);
                if (templateMaterial.shader && templateMaterial.shader != remapMaterial.shader)
                    remapMaterial.shader = templateMaterial.shader;
                remapMaterial.CopyPropertiesFromMaterial(templateMaterial);
            }
            else
            {
                // if the material shader doesn't match, update the shader.            
                if (remapMaterial.shader != shader)
                    remapMaterial.shader = shader;
            }

            // add the path of the remapped material for later re-import.
            string remapPath = AssetDatabase.GetAssetPath(remapMaterial);
            if (remapPath == fbxPath) Util.LogError("remapPath: " + remapPath + " is fbxPath (shouldn't happen)!");
            if (remapPath != fbxPath && AssetDatabase.WriteImportSettingsIfDirty(remapPath))
                importAssets.Add(AssetDatabase.GetAssetPath(remapMaterial));

            return remapMaterial;
        }        

        private void FixRayTracing(GameObject obj, Material mat, MaterialType materialType)
        {
            SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();

            if (smr)
            {
                if (materialType == MaterialType.EyeOcclusion ||
                    materialType == MaterialType.Tearline)
                {
                    Pipeline.DisableRayTracing(smr);
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                else if (materialType == MaterialType.Scalp)
                {
                    if (smr.sharedMaterials.Length == 1)
                    {
                        Pipeline.DisableRayTracing(smr);
                        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }
        }

        private void ProcessTextures(GameObject obj, string sourceName, Material sharedMat, Material mat, 
            MaterialType materialType, QuickJSON matJson)
        {
            string shaderName = mat.shader.name;

            if (shaderName.iContains(Pipeline.SHADER_DEFAULT))
            {
                ConnectDefaultMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_HQ_SSS))
            {
                ConnectSSSMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_DEFAULT_HAIR))
            {
                ConnectDefaultHairMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_HQ_SKIN) ||
                     shaderName.iContains(Pipeline.SHADER_HQ_HEAD))
            {
                ConnectHQSkinMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_HQ_TEETH))
            {
                ConnectHQTeethMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_HQ_TONGUE))
            {
                ConnectHQTongueMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_HQ_EYE_REFRACTIVE) ||
                     shaderName.iContains(Pipeline.SHADER_HQ_CORNEA) ||
                     shaderName.iContains(Pipeline.SHADER_HQ_CORNEA_PARALLAX) ||
                     shaderName.iContains(Pipeline.SHADER_HQ_CORNEA_REFRACTIVE))
            {
                ConnectHQEyeMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_HQ_HAIR) ||
                     shaderName.iContains(Pipeline.SHADER_HQ_HAIR_COVERAGE))
            {
                ConnectHQHairMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_HQ_EYE_OCCLUSION))
            {
                ConnectHQEyeOcclusionMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.iContains(Pipeline.SHADER_HQ_TEARLINE))
            {
                ConnectHQTearlineMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else
            {
                ConnectDefaultMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            Pipeline.ResetMaterial(mat);
        }

        private bool BakeBlenderTextures(string sourceName, QuickJSON matJson)
        {
            if (!blenderProject) return false;

            bool baked = false;
            bool search = true;

            Texture2D diffuseAlpha = null;
            Texture2D HDRPMask = null;
            Texture2D metallicGloss = null;                        

            if (!REBAKE_BLENDER_UNITY_MAPS)
            {
                diffuseAlpha = GetTexture(sourceName, "BDiffuseAlpha", matJson, "Textures/NOTEX", true);
                HDRPMask = GetTexture(sourceName, "BHDRP", matJson, "Textures/NOTEX", true);
                metallicGloss = GetTexture(sourceName, "BMetallicAlpha", matJson, "Textures/NOTEX", true);
            }            
            
            if (!HDRPMask || !metallicGloss)
            {
                ComputeBake baker = new ComputeBake(characterInfo.Fbx, characterInfo);
                string folder;

                Texture2D diffuse = GetTexture(sourceName, "Diffuse", matJson, "Textures/Base Color", search);
                Texture2D opacity = GetTexture(sourceName, "Opacity", matJson, "Textures/Opacity", search);
                Texture2D metallic = GetTexture(sourceName, "Metallic", matJson, "Textures/Metallic", search);
                Texture2D roughness = GetTexture(sourceName, "Roughness", matJson, "Textures/Roughness", search);
                Texture2D occlusion = GetTexture(sourceName, "ao", matJson, "Textures/AO", search);
                Texture2D microNormalMask = GetTexture(sourceName, "MicroNMask", matJson, "Custom Shader/Image/MicroNormalMask", search);
                Texture2D smoothnessLUT = (Texture2D)Util.FindAsset("RL_RoughnessToSmoothness");

                if (!diffuseAlpha)
                {
                    if ((diffuse || opacity) && diffuse != opacity)
                    {
                        Util.LogInfo("Baking DiffuseAlpha texture for " + sourceName);
                        folder = Util.GetAssetFolder(diffuse, opacity);
                        diffuseAlpha = baker.BakeBlenderDiffuseAlphaMap(diffuse, opacity, folder, sourceName + "_BDiffuseAlpha");                        
                        baked = true;
                    }
                }

                if (!HDRPMask)
                {                                        
                    if (metallic || roughness || occlusion || microNormalMask)
                    {
                        Util.LogInfo("Baking HDRP Mask texture for " + sourceName);
                        folder = Util.GetAssetFolder(metallic, roughness, occlusion, microNormalMask);
                        HDRPMask = baker.BakeBlenderHDRPMaskMap(metallic, occlusion, microNormalMask, roughness, smoothnessLUT, folder, sourceName + "_BHDRP");
                        baked = true;
                    }                    
                }
            
                if (!metallicGloss)
                {                    
                    if (metallic || roughness)
                    {
                        Util.LogInfo("Baking MetallicAlpha texture for " + sourceName);
                        folder = Util.GetAssetFolder(metallic, roughness);
                        metallicGloss = baker.BakeBlenderMetallicGlossMap(metallic, roughness, smoothnessLUT, folder, sourceName + "_BMetallicAlpha");                        
                        baked = true;
                    }
                }
            }

            return baked;
        }

        private void ConnectBlenderTextures(string sourceName, Material mat, QuickJSON matJson, 
            string diffuseRef, string HDRPRef, string metallicAlphaRef)
        {
            if (!blenderProject) return;
            
            // The blender export ensures that all materials have unique names, so searching for the textures
            // by material name should fetch the correct ones for the materials.
            Texture2D diffuseAlpha = GetTexture(sourceName, "BDiffuseAlpha", matJson, "Textures/NOTEX", true);
            Texture2D HDRPMask = GetTexture(sourceName, "BHDRP", matJson, "Textures/NOTEX", true);
            Texture2D metallicGloss = GetTexture(sourceName, "BMetallicAlpha", matJson, "Textures/NOTEX", true);
            if (diffuseAlpha) mat.SetTextureIf(diffuseRef, diffuseAlpha);
            if (HDRPMask) mat.SetTextureIf(HDRPRef, HDRPMask);
            if (metallicGloss) mat.SetTextureIf(metallicAlphaRef, metallicGloss);
        }

        private void PrepBlenderTextures(string sourceName, QuickJSON matJson)
        {
            if (!blenderProject) return;

            Texture2D diffuse = GetTexture(sourceName, "Diffuse", matJson, "Textures/Base Color", true);
            Texture2D opacity = GetTexture(sourceName, "Opacity", matJson, "Textures/Opacity", true);
            Texture2D metallic = GetTexture(sourceName, "Metallic", matJson, "Textures/Metallic", true);
            Texture2D roughness = GetTexture(sourceName, "Roughness", matJson, "Textures/Roughness", true);
            Texture2D occlusion = GetTexture(sourceName, "ao", matJson, "Textures/AO", true);
            Texture2D microNormalMask = GetTexture(sourceName, "MicroNMask", matJson, "Custom Shader/Image/MicroNormalMask", true);
            if (!DoneTexture(diffuse)) SetTextureImport(diffuse, "", FLAG_FOR_BAKE + FLAG_SRGB);
            // sometimes the opacity texture is the alpha channel of the diffuse...
            if (opacity != diffuse && !DoneTexture(opacity)) SetTextureImport(opacity, "", FLAG_FOR_BAKE);
            if (!DoneTexture(metallic)) SetTextureImport(metallic, "", FLAG_FOR_BAKE);
            if (!DoneTexture(roughness)) SetTextureImport(roughness, "", FLAG_FOR_BAKE);
            if (!DoneTexture(occlusion)) SetTextureImport(occlusion, "", FLAG_FOR_BAKE);
            if (!DoneTexture(microNormalMask)) SetTextureImport(microNormalMask, "", FLAG_FOR_BAKE);
        }

        private void ConnectDefaultMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            string customShader = matJson?.GetStringValue("Custom Shader/Shader Name");
            string jsonMaterialType = matJson?.GetStringValue("Material Type");
            bool isGameBaseSkin = sourceName.iContains("Ga_Skin_");
            int numGameBaseSkinMaterials = CountMaterials(obj, "Ga_Skin_");

            if (jsonMaterialType == "Tra")
            {
                if (RP == RenderPipeline.HDRP)
                {
                    mat.SetFloatIf("_MaterialID", 4f);
                    mat.EnableKeyword("_MATERIAL_FEATURE_SPECULAR_COLOR");
                }
                else if (RP == RenderPipeline.URP)
                {
                    mat.SetFloatIf("_WorkflowMode", 0f);
                    mat.EnableKeyword("_SPECULAR_SETUP");
                }
                else
                {
                    Shader specShader = Shader.Find("Standard (Specular setup)");
                    int renderQueue = mat.renderQueue;
                    mat.shader = specShader;
                    mat.renderQueue = renderQueue;                    
                }
            }

            // these default materials should *not* attach any textures:
            if (customShader == "RLEyeTearline" || customShader == "RLEyeOcclusion") return;

            if (RP == RenderPipeline.HDRP)
            {
                if (!ConnectTextureTo(sourceName, mat, "_BaseColorMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB))
                {
                    ConnectTextureTo(sourceName, mat, "_BaseColorMap", "Opacity",
                        matJson, "Textures/Opacity",
                        FLAG_SRGB);
                }

                ConnectTextureTo(sourceName, mat, "_SpecularColorMap", "Specular",
                    matJson, "Textures/Specular");                

                ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                    matJson, "Textures/HDRP");                

                if (ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                    matJson, "Textures/Normal",
                    FLAG_NORMAL))
                {
                    mat.EnableKeyword("_NORMALMAP");
                    mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                }
                else
                {
                    mat.DisableKeyword("_NORMALMAP");
                    mat.DisableKeyword("_NORMALMAP_TANGENT_SPACE");
                }                

                ConnectTextureTo(sourceName, mat, "_EmissiveColorMap", "Glow",
                    matJson, "Textures/Glow");                

                if (matJson.GetBoolValue("Two Side"))
                {
                    mat.SetFloatIf("_DoubleSidedEnable", 1f);
                    mat.EnableKeyword("_DOUBLESIDED_ON");                    
                }                
            }
            else
            {
                if (RP == RenderPipeline.URP)
                {
                    if (!ConnectTextureTo(sourceName, mat, "_BaseMap", "Diffuse",
                        matJson, "Textures/Base Color",
                        FLAG_SRGB))
                    {
                        ConnectTextureTo(sourceName, mat, "_BaseMap", "Opacity",
                            matJson, "Textures/Opacity",
                            FLAG_SRGB);
                    }

                    if (matJson != null && matJson.GetBoolValue("Two Side"))
                    {
                        mat.SetFloatIf("_Cull", 0f); // 1f - cull front, 2f cull back
                    }
                }
                else
                {
                    if (!ConnectTextureTo(sourceName, mat, "_MainTex", "Diffuse",
                        matJson, "Textures/Base Color",
                        FLAG_SRGB))
                    {
                        ConnectTextureTo(sourceName, mat, "_MainTex", "Opacity",
                            matJson, "Textures/Opacity",
                            FLAG_SRGB);
                    }
                }

                ConnectTextureTo(sourceName, mat, "_SpecGlossMap", "Specular",
                        matJson, "Textures/Specular");

                if (ConnectTextureTo(sourceName, mat, "_MetallicGlossMap", "MetallicAlpha",
                        matJson, "Textures/MetallicAlpha"))
                {
                    mat.SetFloatIf("_Metallic", 1f);
                }

                ConnectTextureTo(sourceName, mat, "_OcclusionMap", "ao",
                    matJson, "Textures/AO");

                ConnectTextureTo(sourceName, mat, "_BumpMap", "Normal",
                    matJson, "Textures/Normal", FLAG_NORMAL);                

                if (ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                    matJson, "Textures/Glow"))
                {
                    mat.globalIlluminationFlags = mat.globalIlluminationFlags | MaterialGlobalIlluminationFlags.AnyEmissive;
                    mat.EnableKeyword("_EMISSION");
                }                
            }

            // reconstruct any missing packed texture maps from Blender source maps.
            if (RP == RenderPipeline.HDRP)
                ConnectBlenderTextures(sourceName, mat, matJson, "_BaseColorMap", "_MaskMap", "");
            else if (RP == RenderPipeline.URP)
                ConnectBlenderTextures(sourceName, mat, matJson, "_BaseMap", "", "_MetallicGlossMap");
            else
                ConnectBlenderTextures(sourceName, mat, matJson, "_MainTex", "", "_MetallicGlossMap");

            if (!Pipeline.isHDRP)
            {
                KeywordsOnTexture(mat, "_SpecGlossMap", "_METALLICSPECGLOSSMAP", "_SPECGLOSSMAP");
                KeywordsOnTexture(mat, "_MetallicGlossMap", "_METALLICSPECGLOSSMAP");
                KeywordsOnTexture(mat, "_OcclusionMap", "_OCCLUSIONMAP");
                KeywordsOnTexture(mat, "_BumpMap", "_NORMALMAP");
            }
            
            // All
            if (matJson != null)
            {                
                if (matJson.PathExists("Roughness_Value"))
                {
                    // Roughness_Value from Blender pipeline (instead of baking a small value texture)
                    mat.SetFloatIf("_Smoothness", 1f - matJson.GetFloatValue("Roughness_Value"));                    
                    mat.SetFloatIf("_GlossMapScale", 1f - matJson.GetFloatValue("Roughness_Value"));
                }
                
                if (matJson.PathExists("Metallic_Value"))
                {
                    // Metallic_Value from Blender pipeline (instead of baking a small value texture)
                    mat.SetFloatIf("_Metallic", matJson.GetFloatValue("Metallic_Value"));
                }

                // Diffuse tint
                Color diffuseColor = Util.LinearTosRGB(matJson.GetColorValue("Diffuse Color"));
                float opacity = matJson.GetFloatValue("Opacity");
                diffuseColor.a *= opacity;
                if (RP != RenderPipeline.Builtin)
                    mat.SetColorIf("_BaseColor", diffuseColor);
                else
                    mat.SetColorIf("_Color", diffuseColor);                

                // Emission
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                {
                    if (RP == RenderPipeline.HDRP)
                        mat.SetColorIf("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                    else
                        mat.SetColorIf("_EmissionColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                }

                // Normal map strength
                if (matJson.PathExists("Textures/Normal/Strength"))
                {
                    if (RP == RenderPipeline.HDRP)
                        mat.SetFloatIf("_NormalScale", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
                    else
                        mat.SetFloatIf("_BumpScale", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
                }
            }

            // Subsurface overrides
            if (matJson != null && (matJson.PathExists("Subsurface Scatter") || isGameBaseSkin))
            {
                string[] folders = new string[] { "Assets", "Packages" };                

                Texture2D sssTex = null;
                if (sourceName.iStartsWith("Ga_Skin_Body"))
                {
                    if (numGameBaseSkinMaterials > 1)
                    {
                        sssTex = Util.FindTexture(folders, "RL_GameBaseMulti_Body_SSS_Mask");
                        if (sssTex) mat.SetTextureIf("_SubsurfaceMaskMap", sssTex);
                    }
                    else
                    {
                        sssTex = Util.FindTexture(folders, "RL_GameBaseSingle_Body_SSS_Mask");
                        if (sssTex) mat.SetTextureIf("_SubsurfaceMaskMap", sssTex);
                    }
                }
                else if (sourceName.iStartsWith("Ga_Skin_Arm"))
                {
                    sssTex = Util.FindTexture(folders, "RL_GameBaseMulti_Arm_SSS_Mask");
                    if (sssTex) mat.SetTextureIf("_SubsurfaceMaskMap", sssTex);
                }
                else if (sourceName.iStartsWith("Ga_Skin_Leg"))
                {
                    sssTex = Util.FindTexture(folders, "RL_GameBaseMulti_Leg_SSS_Mask");
                    if (sssTex) mat.SetTextureIf("_SubsurfaceMaskMap", sssTex);
                }
                else if (sourceName.iStartsWith("Ga_Skin_Head"))
                {
                    sssTex = Util.FindTexture(folders, "RL_GameBaseMulti_Head_SSS_Mask");
                    if (sssTex) mat.SetTextureIf("_SubsurfaceMaskMap", sssTex);
                }
                if (!sssTex)
                {
                    ConnectTextureTo(sourceName, mat, "_SubsurfaceMaskMap", "SSSMap",
                        matJson, "Custom Shader/Image/SSS Map");
                }

                float microNormalTiling = 20f;
                float microNormalStrength = 0.5f;
                Color sssFalloff = Color.white;
                float subsurfaceScale = 0.85f;

                if (matJson.PathExists("Subsurface Scatter/Falloff"))
                    sssFalloff = matJson.GetColorValue("Subsurface Scatter/Falloff");

                if (matJson.PathExists("Subsurface Scatter/Lerp"))
                    subsurfaceScale = matJson.GetFloatValue("Subsurface Scatter/Lerp");

                if (matJson.PathExists("Custom Shader/Variable/MicroNormal Tiling"))
                    microNormalTiling = matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Tiling");

                if (matJson.PathExists("Custom Shader/Variable/MicroNormal Strength"))
                    microNormalStrength = matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Strength");                

                if (RP == RenderPipeline.HDRP)
                {
                    // HDRP uses the baked thickness and packed detail map
                    Texture2D thicknessTex = null;
                    if (sourceName.iStartsWith("Ga_Skin_Body"))
                    {
                        if (numGameBaseSkinMaterials > 1)
                        {
                            thicknessTex = Util.FindTexture(folders, "RL_GameBaseMulti_Body_Thickness");
                            if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                        }
                        else
                        {
                            thicknessTex = Util.FindTexture(folders, "RL_GameBaseSingle_Body_Thickness");
                            if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                        }                        
                    }
                    else if (sourceName.iStartsWith("Ga_Skin_Arm"))
                    {
                        thicknessTex = Util.FindTexture(folders, "RL_GameBaseMulti_Arm_Thickness");
                        if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                    }
                    else if (sourceName.iStartsWith("Ga_Skin_Leg"))
                    {
                        thicknessTex = Util.FindTexture(folders, "RL_GameBaseMulti_Leg_Thickness");
                        if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                    }
                    else if (sourceName.iStartsWith("Ga_Skin_Head"))
                    {
                        thicknessTex = Util.FindTexture(folders, "RL_GameBaseMulti_Head_Thickness");
                        if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                    }
                    if (!thicknessTex)
                    {
                        mat.SetTextureIf("_ThicknessMap", GetCachedBakedMap(sharedMat, "_ThicknessMap"));
                    }                    
                    mat.SetTextureIf("_DetailMap", GetCachedBakedMap(sharedMat, "_DetailMap"));
                    mat.SetTextureScaleIf("_DetailMap", new Vector2(microNormalTiling, microNormalTiling));
                    mat.SetFloatIf("_DetailNormalScale", microNormalStrength);
                }
                else
                {
                    Texture2D thicknessTex = null;
                    if (sourceName.iStartsWith("Ga_Skin_Body"))
                    {
                        if (numGameBaseSkinMaterials > 1)
                        {
                            thicknessTex = Util.FindTexture(folders, "RL_GameBaseMulti_Body_Transmission");
                            if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                        }
                        else
                        {
                            thicknessTex = Util.FindTexture(folders, "RL_GameBaseSingle_Body_Transmission");
                            if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                        }
                    }
                    else if (sourceName.iStartsWith("Ga_Skin_Arm"))
                    {
                        thicknessTex = Util.FindTexture(folders, "RL_GameBaseMulti_Arm_Transmission");
                        if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                    }
                    else if (sourceName.iStartsWith("Ga_Skin_Leg"))
                    {
                        thicknessTex = Util.FindTexture(folders, "RL_GameBaseMulti_Leg_Transmission");
                        if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                    }
                    else if (sourceName.iStartsWith("Ga_Skin_Head"))
                    {
                        thicknessTex = Util.FindTexture(folders, "RL_GameBaseMulti_Head_Transmission");
                        if (thicknessTex) mat.SetTextureIf("_ThicknessMap", thicknessTex);
                    }
                    if (!thicknessTex)
                    {
                        ConnectTextureTo(sourceName, mat, "_ThicknessMap", "TransMap",
                           matJson, "Custom Shader/Image/Transmission Map");
                    }                    

                    // 3D & URP use the micro normal mask and map directly
                    ConnectTextureTo(sourceName, mat, "_DetailMask", "MicroNMask",
                        matJson, "Custom Shader/Image/MicroNormalMask");

                    ConnectTextureTo(sourceName, mat, "_DetailNormalMap", "MicroN",
                        matJson, "Custom Shader/Image/MicroNormal", FLAG_NORMAL);

                    mat.SetTextureScaleIf("_DetailNormalMap", new Vector2(microNormalTiling, microNormalTiling));
                    mat.SetTextureScaleIf("_DetailAlbedoMap", new Vector2(microNormalTiling, microNormalTiling));
                    mat.SetFloatIf("_DetailNormalMapScale", microNormalStrength);
                    mat.SetColorIf("_SubsurfaceFalloff", sssFalloff);
                }

                if (isGameBaseSkin)
                {
                    // game base skin has blank white transmission maps so needs to be turned down
                    if (RP == RenderPipeline.HDRP)
                    {
                        mat.SetRemapRange("_ThicknessRemap", 0.25f, 0.9f);
                        mat.SetFloatIf("_SubsurfaceMask", subsurfaceScale);
                    }
                    else if (RP == RenderPipeline.URP)
                    {
                        // URP has particularly sensitive transmission
                        mat.SetFloatIf("_Thickness", 0.1f);
                        // and the differences in subsurface wrapping need to be accounted for
                        mat.SetFloatIf("_SubsurfaceMask", subsurfaceScale * 0.4f);
                    }
                    else
                    {
                        mat.SetFloatIf("_Thickness", 0.1f);
                        // the differences in subsurface wrapping need to be accounted for
                        mat.SetFloatIf("_SubsurfaceMask", subsurfaceScale * 0.4f);
                    }

                }
                else //if (customShader == "RLSSS")
                {
                    mat.SetRemapRange("_ThicknessRemap", 0.0f, 1f);
                    mat.SetFloatIf("_Thickness", 1f);
                    mat.SetFloatIf("_SubsurfaceMask", subsurfaceScale);
                }
            }

            // Smoothness/Glossiness and Specular
            if (matJson != null)
            {
                if (matJson.PathExists("Custom Shader/Variable/Micro Roughness Scale"))
                {                    
                    float microRoughnessMod = 0.0f;
                    float specular = 0.5f;

                    if (matJson.PathExists("Custom Shader/Variable/Micro Roughness Scale"))
                        microRoughnessMod = matJson.GetFloatValue("Custom Shader/Variable/Micro Roughness Scale");
                    if (matJson.PathExists("Custom Shader/Variable/_Specular"))
                        specular = matJson.GetFloatValue("Custom Shader/Variable/_Specular");                    

                    if (RP == RenderPipeline.HDRP)
                    {
                        float smoothness = Util.CombineSpecularToSmoothness(specular, mat.GetFloatIf("_Smoothness", 0.5f));
                        float smoothnessMin = Util.CombineSpecularToSmoothness(specular, mat.GetFloatIf("_SmoothnessRemapMin", 0f));
                        float smoothnessMax = Util.CombineSpecularToSmoothness(specular, mat.GetFloatIf("_SmoothnessRemapMax", MAX_SMOOTHNESS));
                        mat.SetMinMaxRange("_SmoothnessRemap", smoothnessMin - microRoughnessMod, smoothnessMax - microRoughnessMod);
                        mat.SetFloatIf("_Smoothness", smoothness - microRoughnessMod);
                    }
                    else if (RP == RenderPipeline.URP)
                    {
                        float smoothness = Util.CombineSpecularToSmoothness(specular, mat.GetFloatIf("_Smoothness", 0.5f));
                        mat.SetFloatIf("_Smoothness", smoothness - microRoughnessMod);
                    }
                    else
                    {
                        float smoothness = Util.CombineSpecularToSmoothness(specular, mat.GetFloatIf("_GlossMapScale", 0.5f));
                        mat.SetFloatIf("_GlossMapScale", smoothness - microRoughnessMod);
                    }
                }
                else if (jsonMaterialType == "Tra")
                {
                    float glossiness = 0.5f;
                    float specular = 1f;
                    Color specularColor = Color.white * TRA_SPECULAR_SCALE;

                    if (matJson.PathExists("Glossiness"))
                        glossiness = matJson.GetFloatValue("Glossiness");
                    if (matJson.PathExists("Specular"))
                        specular = matJson.GetFloatValue("Specular");
                    if (matJson.PathExists("Specular Color"))
                        specularColor = TRA_SPECULAR_SCALE * Util.LinearTosRGB(matJson.GetColorValue("Specular Color"));

                    glossiness = Util.CombineSpecularToSmoothness(specularColor.grayscale * specular, glossiness);
                    mat.SetFloatIf("_Smoothness", glossiness);
                    mat.SetFloatIf("_GlossMapScale", glossiness);
                    mat.SetFloatIf("_Glossiness", glossiness);
                    mat.SetMinMaxRange("_SmoothnessRemap", 0f, glossiness);

                    if (RP == RenderPipeline.HDRP)
                        mat.SetColorIf("_SpecularColor", specularColor);
                    else
                        mat.SetColorIf("_SpecColor", specularColor);
                }  
                else if (isGameBaseSkin)
                {
                    if (RP == RenderPipeline.HDRP)
                    {
                        mat.SetMinMaxRange("_SmoothnessRemap", 0f, 0.7f);
                        mat.SetFloatIf("_Smoothness", 0.7f);
                    }
                    else if (RP == RenderPipeline.URP)
                    {                        
                        mat.SetFloatIf("_Smoothness", 0.625f);
                    }
                    else
                    {                     
                        mat.SetFloatIf("_GlossMapScale", 0.7f);
                    }
                }
            }
        }

        private void ConnectSSSMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            // for now the SSS implementation is incomplete, treat as default material with extra steps
            ConnectDefaultMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);            
        }

        // HDRP only
        private void ConnectDefaultHairMaterial(GameObject obj, string sourceName, Material sharedMat,
        Material mat, MaterialType materialType, QuickJSON matJson)
        {
            //bool isFacialHair = FacialProfileMapper.MeshHasFacialBlendShapes(obj) != FacialProfile.None;

            if (!ConnectTextureTo(sourceName, mat, "_BaseColorMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB + FLAG_HAIR))
            {
                ConnectTextureTo(sourceName, mat, "_BaseColorMap", "Opacity",
                    matJson, "Textures/Opacity",
                    FLAG_SRGB + FLAG_HAIR);
            }

            ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_MaskMap", "ao",
                matJson, "Textures/AO");

            // reconstruct any missing packed texture maps from Blender source maps.
            if (RP == RenderPipeline.HDRP)
                ConnectBlenderTextures(sourceName, mat, matJson, "_BaseColorMap", "_MaskMap", "");

            if (matJson != null)
            {
                float diffuseStrength = matJson.GetFloatValue("Custom Shader/Variable/Diffuse Strength");
                mat.SetColor("_BaseColor", Util.LinearTosRGB(matJson.GetColorValue("Diffuse Color")).ScaleRGB(diffuseStrength));

                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloat("_NormalScale", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
            }
        }        

        private void ConnectHQSkinMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            ConnectTextureTo(sourceName, mat, "_DiffuseMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB);

            ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL);

            // try to use corrected HDRP mask mask
            if (bakedHDRPMaps.TryGetValue(sharedMat, out Texture2D bakedHDRP))
            {
                mat.SetTextureIf("_MaskMap", bakedHDRP);
            }
            else
            {
                ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                    matJson, "Textures/HDRP");
            }

            ConnectTextureTo(sourceName, mat, "_MetallicAlphaMap", "MetallicAlpha",
                matJson, "Textures/MetallicAlpha");

            ConnectTextureTo(sourceName, mat, "_AOMap", "ao",
                matJson, "Textures/AO");

            ConnectTextureTo(sourceName, mat, "_SSSMap", "SSSMap",
                matJson, "Custom Shader/Image/SSS Map");            

            ConnectTextureTo(sourceName, mat, "_ThicknessMap", "TransMap",
                matJson, "Custom Shader/Image/Transmission Map");

            ConnectTextureTo(sourceName, mat, "_SpecularMask", "SpecMask",
                matJson, "Custom Shader/Image/Specular Mask");

            ConnectTextureTo(sourceName, mat, "_MicroNormalMap", "MicroN",
                matJson, "Custom Shader/Image/MicroNormal", 
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_MicroNormalMaskMap", "MicroNMask",
                matJson, "Custom Shader/Image/MicroNormalMask");

            ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                matJson, "Textures/Glow");

            if (materialType == MaterialType.Head)
            {
                ConnectTextureTo(sourceName, mat, "_ColorBlendMap", "BCBMap",
                    matJson, "Custom Shader/Image/BaseColor Blend2");

                ConnectTextureTo(sourceName, mat, "_MNAOMap", "MNAOMask",
                    matJson, "Custom Shader/Image/Mouth Cavity Mask and AO");

                ConnectTextureTo(sourceName, mat, "_RGBAMask", "NMUILMask",
                    matJson, "Custom Shader/Image/Nose Mouth UpperInnerLid Mask");

                ConnectTextureTo(sourceName, mat, "_CFULCMask", "CFULCMask",
                    matJson, "Custom Shader/Image/Cheek Fore UpperLip Chin Mask");

                ConnectTextureTo(sourceName, mat, "_EarNeckMask", "ENMask",
                    matJson, "Custom Shader/Image/Ear Neck Mask");

                ConnectTextureTo(sourceName, mat, "_NormalBlendMap", "NBMap",
                    matJson, "Custom Shader/Image/NormalMap Blend",
                    FLAG_NORMAL);
                 
                if (characterInfo.FeatureUseWrinkleMaps && matJson.PathExists("Wrinkle"))
                {
                    ConnectTextureTo(sourceName, mat, "_WrinkleDiffuseBlend1", "Wrinkle_Diffuse1",
                        matJson, "Wrinkle/Textures/Diffuse_1",
                        FLAG_SRGB);

                    ConnectTextureTo(sourceName, mat, "_WrinkleDiffuseBlend2", "Wrinkle_Diffuse2",
                        matJson, "Wrinkle/Textures/Diffuse_2",
                        FLAG_SRGB);

                    ConnectTextureTo(sourceName, mat, "_WrinkleDiffuseBlend3", "Wrinkle_Diffuse3",
                        matJson, "Wrinkle/Textures/Diffuse_3",
                        FLAG_SRGB);

                    ConnectTextureTo(sourceName, mat, "_WrinkleNormalBlend1", "Wrinkle_Normal1",
                        matJson, "Wrinkle/Textures/Normal_1",
                        FLAG_NORMAL);

                    ConnectTextureTo(sourceName, mat, "_WrinkleNormalBlend2", "Wrinkle_Normal2",
                        matJson, "Wrinkle/Textures/Normal_2",
                        FLAG_NORMAL);

                    ConnectTextureTo(sourceName, mat, "_WrinkleNormalBlend3", "Wrinkle_Normal3",
                        matJson, "Wrinkle/Textures/Normal_3",
                        FLAG_NORMAL);

                    ConnectTextureTo(sourceName, mat, "_WrinkleRoughnessBlend1", "Wrinkle_Roughness1",
                        matJson, "Wrinkle/Textures/Roughness_1");

                    ConnectTextureTo(sourceName, mat, "_WrinkleRoughnessBlend2", "Wrinkle_Roughness2",
                        matJson, "Wrinkle/Textures/Roughness_2");

                    ConnectTextureTo(sourceName, mat, "_WrinkleRoughnessBlend3", "Wrinkle_Roughness3",
                        matJson, "Wrinkle/Textures/Roughness_3");

                    ConnectTextureTo(sourceName, mat, "_WrinkleFlowMap1", "Wrinkle_Flow1",
                        matJson, "Wrinkle/Textures/Flow_1");

                    ConnectTextureTo(sourceName, mat, "_WrinkleFlowMap2", "Wrinkle_Flow2",
                        matJson, "Wrinkle/Textures/Flow_2");

                    ConnectTextureTo(sourceName, mat, "_WrinkleFlowMap3", "Wrinkle_Flow3",
                        matJson, "Wrinkle/Textures/Flow_3");

                    ApplyWrinkleMasks(mat);                    
                }

                mat.EnableKeyword("BOOLEAN_IS_HEAD_ON");
            }
            else
            {
                ConnectTextureTo(sourceName, mat, "_RGBAMask", "RGBAMask",
                    matJson, "Custom Shader/Image/RGBA Area Mask");
            }

            // reconstruct any missing packed texture maps from Blender source maps.
            ConnectBlenderTextures(sourceName, mat, matJson, "_DiffuseMap", "_MaskMap", "_MetallicAlphaMap");

            if (matJson != null)
            {
                float specular = matJson.GetFloatValue("Custom Shader/Variable/_Specular");
                bool specularBakeZero = false;

                // work around CC4 Head specular export bug, when exporting with bake skin option
                if (specular == 0.0f && materialType == MaterialType.Head)
                {
                    float skinSpecular = 0.0f;
                    QuickJSON materialsJson = jsonData.FindParentOf(matJson);
                    if (materialsJson != null)
                    {
                        QuickJSON skinJson = null;
                        if (materialsJson.PathExists("Std_Skin_Body"))
                            skinJson = materialsJson.GetObjectAtPath("Std_Skin_Body");
                        else if (materialsJson.PathExists("Std_Skin_Arm"))
                            skinJson = materialsJson.GetObjectAtPath("Std_Skin_Arm");
                        else if (materialsJson.PathExists("Std_Skin_Leg"))
                            skinJson = materialsJson.GetObjectAtPath("Std_Skin_Leg");
                        if (skinJson != null)
                            skinSpecular = skinJson.GetFloatValue("Custom Shader/Variable/_Specular");
                    }

                    if (skinSpecular != 0.0f)
                    {
                        Util.LogWarn("Specular export bug in skin material, setting head specular to: " + skinSpecular);
                        specular = skinSpecular;
                        specularBakeZero = true;
                    }
                }

                mat.SetFloatIf("_AOStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColorIf("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloatIf("_NormalStrength", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
                mat.SetFloatIf("_MicroNormalTiling", matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Tiling"));
                mat.SetFloatIf("_MicroNormalStrength", matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Strength"));                                
                float smoothnessMax = Util.CombineSpecularToSmoothness(specular, ValueByPipeline(1f, 0.9f, 1f));
                mat.SetFloatIf("_SmoothnessMax", smoothnessMax);
                //float secondarySmoothness = 0.85f * smoothnessMax;
                //float smoothnessMix = Mathf.Clamp(0.15f * ((1f / Mathf.Pow(secondarySmoothness, 4f)) - 1f), 0.05f, 0.9f);
                //mat.SetFloatIf("_Smoothness2", secondarySmoothness);
                //mat.SetFloatIf("_SmoothnessMix", smoothnessMix);
                // URP's lights affect the AMP SSS more than 3D or HDRP
                mat.SetFloatIf("_SubsurfaceScale", matJson.GetFloatValue("Subsurface Scatter/Lerp"));                
                mat.SetFloatIf("_MicroSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Micro Roughness Scale"));
                mat.SetFloatIf("_UnmaskedSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Unmasked Roughness Scale"));
                mat.SetFloatIf("_UnmaskedScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Unmasked Scatter Scale"));
                mat.SetColorIf("_DiffuseColor", Util.LinearTosRGB(matJson.GetColorValue("Diffuse Color")));                

                if (materialType == MaterialType.Head)
                {
                    // specular bake bug bakes color blend into diffuse
                    float colorBlenderStrength = matJson.GetFloatValue("Custom Shader/Variable/BaseColor Blend2 Strength");
                    if (specularBakeZero) colorBlenderStrength = 0.0f;
                    mat.SetFloatIf("_ColorBlendStrength", colorBlenderStrength);
                    mat.SetFloatIf("_NormalBlendStrength", matJson.GetFloatValue("Custom Shader/Variable/NormalMap Blend Strength"));
                    mat.SetFloatIf("_MouthCavityAO", matJson.GetFloatValue("Custom Shader/Variable/Inner Mouth Ao"));
                    mat.SetFloatIf("_NostrilCavityAO", matJson.GetFloatValue("Custom Shader/Variable/Nostril Ao"));
                    mat.SetFloatIf("_LipsCavityAO", matJson.GetFloatValue("Custom Shader/Variable/Lips Gap Ao"));

                    mat.SetFloatIf("_RSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Nose Roughness Scale"));
                    mat.SetFloatIf("_GSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Mouth Roughness Scale"));
                    mat.SetFloatIf("_BSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/UpperLid Roughness Scale"));
                    mat.SetFloatIf("_ASmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/InnerLid Roughness Scale"));
                    mat.SetFloatIf("_EarSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Ear Roughness Scale"));
                    mat.SetFloatIf("_NeckSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Neck Roughness Scale"));
                    mat.SetFloatIf("_CheekSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Cheek Roughness Scale"));
                    mat.SetFloatIf("_ForeheadSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Forehead Roughness Scale"));
                    mat.SetFloatIf("_UpperLipSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/UpperLip Roughness Scale"));
                    mat.SetFloatIf("_ChinSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Chin Roughness Scale"));

                    mat.SetFloatIf("_RScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Nose Scatter Scale"));
                    mat.SetFloatIf("_GScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Mouth Scatter Scale"));
                    mat.SetFloatIf("_BScatterScale", matJson.GetFloatValue("Custom Shader/Variable/UpperLid Scatter Scale"));
                    mat.SetFloatIf("_AScatterScale", matJson.GetFloatValue("Custom Shader/Variable/InnerLid Scatter Scale"));
                    mat.SetFloatIf("_EarScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Ear Scatter Scale"));
                    mat.SetFloatIf("_NeckScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Neck Scatter Scale"));
                    mat.SetFloatIf("_CheekScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Cheek Scatter Scale"));
                    mat.SetFloatIf("_ForeheadScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Forehead Scatter Scale"));
                    mat.SetFloatIf("_UpperLipScatterScale", matJson.GetFloatValue("Custom Shader/Variable/UpperLip Scatter Scale"));
                    mat.SetFloatIf("_ChinScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Chin Scatter Scale"));
                }
                else
                {
                    mat.SetFloatIf("_RSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/R Channel Roughness Scale"));
                    mat.SetFloatIf("_GSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/G Channel Roughness Scale"));
                    mat.SetFloatIf("_BSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/B Channel Roughness Scale"));
                    mat.SetFloatIf("_ASmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/A Channel Roughness Scale"));

                    mat.SetFloatIf("_RScatterScale", matJson.GetFloatValue("Custom Shader/Variable/R Channel Scatter Scale"));
                    mat.SetFloatIf("_GScatterScale", matJson.GetFloatValue("Custom Shader/Variable/G Channel Scatter Scale"));
                    mat.SetFloatIf("_BScatterScale", matJson.GetFloatValue("Custom Shader/Variable/B Channel Scatter Scale"));
                    mat.SetFloatIf("_AScatterScale", matJson.GetFloatValue("Custom Shader/Variable/A Channel Scatter Scale"));
                }
            }
        }

        private void ConnectHQTeethMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            ConnectTextureTo(sourceName, mat, "_DiffuseMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB);

            ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                matJson, "Textures/HDRP");

            ConnectTextureTo(sourceName, mat, "_MetallicAlphaMap", "MetallicAlpha",
                matJson, "Textures/MetallicAlpha");

            ConnectTextureTo(sourceName, mat, "_AOMap", "ao",
                matJson, "Textures/AO");

            ConnectTextureTo(sourceName, mat, "_MicroNormalMap", "MicroN",
                matJson, "Custom Shader/Image/MicroNormal",
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_GumsMaskMap", "GumsMask",
                matJson, "Custom Shader/Image/Gums Mask");

            ConnectTextureTo(sourceName, mat, "_GradientAOMap", "GradAO",
                matJson, "Custom Shader/Image/Gradient AO");

            ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                matJson, "Textures/Glow");

            // reconstruct any missing packed texture maps from Blender source maps.
            ConnectBlenderTextures(sourceName, mat, matJson, "_DiffuseMap", "_MaskMap", "_MetallicAlphaMap");

            if (matJson != null)
            {
                mat.SetFloat("_IsUpperTeeth", matJson.GetFloatValue("Custom Shader/Variable/Is Upper Teeth"));
                mat.SetFloat("_AOStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColor("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloat("_NormalStrength", 0.5f * matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
                mat.SetFloat("_MicroNormalTiling", matJson.GetFloatValue("Custom Shader/Variable/Teeth MicroNormal Tiling"));
                mat.SetFloat("_MicroNormalStrength", matJson.GetFloatValue("Custom Shader/Variable/Teeth MicroNormal Strength"));
                /*float specular = matJson.GetFloatValue("Custom Shader/Variable/Front Specular");
                float specularT = Mathf.InverseLerp(0f, 0.5f, specular);
                float roughness = 1f - matJson.GetFloatValue("Custom Shader/Variable/Front Roughness");
                mat.SetFloat("_SmoothnessMin", roughness * 0.9f);
                mat.SetFloat("_SmoothnessMax", Mathf.Lerp(0.9f, 1f, specularT));
                mat.SetFloat("_SmoothnessPower", 0.5f);
                */
                float frontSpecular = matJson.GetFloatValue("Custom Shader/Variable/Front Specular");
                float rearSpecular = matJson.GetFloatValue("Custom Shader/Variable/Back Specular");
                float frontSmoothness = Util.CombineSpecularToSmoothness(frontSpecular,
                                            (1f - matJson.GetFloatValue("Custom Shader/Variable/Front Roughness")));
                float rearSmoothness = Util.CombineSpecularToSmoothness(rearSpecular,
                                            (1f - matJson.GetFloatValue("Custom Shader/Variable/Back Roughness")));                
                mat.SetFloat("_SmoothnessFront", frontSmoothness);
                mat.SetFloat("_SmoothnessRear", rearSmoothness);
                mat.SetFloat("_TeethSSS", matJson.GetFloatValue("Custom Shader/Variable/Teeth Scatter"));
                mat.SetFloat("_GumsSSS", matJson.GetFloatValue("Custom Shader/Variable/Gums Scatter"));
                mat.SetFloat("_TeethThickness", Mathf.Clamp01(matJson.GetFloatValue("Subsurface Scatter/Radius") * 1.0f / 5f));
                mat.SetFloat("_GumsThickness", Mathf.Clamp01(matJson.GetFloatValue("Subsurface Scatter/Radius") * 1.0f / 5f));
                mat.SetFloat("_FrontAO", matJson.GetFloatValue("Custom Shader/Variable/Front AO"));
                mat.SetFloat("_RearAO", matJson.GetFloatValue("Custom Shader/Variable/Back AO"));
                mat.SetFloat("_GumsSaturation", Mathf.Clamp01(1f - matJson.GetFloatValue("Custom Shader/Variable/Gums Desaturation")));
                mat.SetFloat("_GumsBrightness", matJson.GetFloatValue("Custom Shader/Variable/Gums Brightness"));
                mat.SetFloat("_TeethSaturation", Mathf.Clamp01(1f - matJson.GetFloatValue("Custom Shader/Variable/Teeth Desaturation")));
                mat.SetFloat("_TeethBrightness", matJson.GetFloatValue("Custom Shader/Variable/Teeth Brightness"));
            }
        }

        private void ConnectHQTongueMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            ConnectTextureTo(sourceName, mat, "_DiffuseMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB);

            ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                matJson, "Textures/HDRP");

            ConnectTextureTo(sourceName, mat, "_MetallicAlphaMap", "MetallicAlpha",
                matJson, "Textures/MetallicAlpha");

            ConnectTextureTo(sourceName, mat, "_AOMap", "ao",
                matJson, "Textures/AO");

            ConnectTextureTo(sourceName, mat, "_MicroNormalMap", "MicroN",
                matJson, "Custom Shader/Image/MicroNormal",
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_GradientAOMap", "GradAO",
                matJson, "Custom Shader/Image/Gradient AO");

            ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                matJson, "Textures/Glow");

            // reconstruct any missing packed texture maps from Blender source maps.
            ConnectBlenderTextures(sourceName, mat, matJson, "_DiffuseMap", "_MaskMap", "_MetallicAlphaMap");

            if (matJson != null)
            {                
                mat.SetFloat("_AOStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColor("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloat("_NormalStrength", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
                mat.SetFloat("_MicroNormalTiling", matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Tiling"));
                mat.SetFloat("_MicroNormalStrength", matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Strength"));
                float frontSpecular = matJson.GetFloatValue("Custom Shader/Variable/Front Specular");
                float rearSpecular = matJson.GetFloatValue("Custom Shader/Variable/Back Specular");
                float frontSmoothness = Util.CombineSpecularToSmoothness(frontSpecular,
                                            (1f - matJson.GetFloatValue("Custom Shader/Variable/Front Roughness")));
                float rearSmoothness = Util.CombineSpecularToSmoothness(rearSpecular,
                                            (1f - matJson.GetFloatValue("Custom Shader/Variable/Back Roughness")));                
                mat.SetFloat("_SmoothnessFront", frontSmoothness);
                mat.SetFloat("_SmoothnessRear", rearSmoothness);
                mat.SetFloat("_TongueSSS", matJson.GetFloatValue("Custom Shader/Variable/_Scatter"));
                //mat.SetFloat("_TongueThickness", Mathf.Clamp01(matJson.GetFloatValue("Subsurface Scatter/Radius") / 2f));
                mat.SetFloat("_FrontAO", matJson.GetFloatValue("Custom Shader/Variable/Front AO"));
                mat.SetFloat("_RearAO", matJson.GetFloatValue("Custom Shader/Variable/Back AO"));
                mat.SetFloat("_TongueSaturation", Mathf.Clamp01(1f - matJson.GetFloatValue("Custom Shader/Variable/_Desaturation")));
                mat.SetFloat("_TongueBrightness", matJson.GetFloatValue("Custom Shader/Variable/_Brightness"));                
            }
        }

        private void ConnectHQEyeMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            bool isCornea = mat.GetFloat("BOOLEAN_ISCORNEA") > 0f;
            bool isLeftEye = sourceName.iContains("Eye_L");            
            string customShader = matJson?.GetStringValue("Custom Shader/Shader Name");            

            // if there is no custom shader, then this is the PBR eye material, 
            // we need to find the cornea material json object with the RLEye shader data:
            if (string.IsNullOrEmpty(customShader) && matJson != null)
            {
                QuickJSON parentJson = jsonData.FindParentOf(matJson);
                if (sourceName.iContains("Eye_L"))
                {
                    matJson = parentJson.FindObjectWithKey("Cornea_L");
                    sourceName.Replace("Eye_L", "Cornea_L");
                }
                else if (sourceName.iContains("Eye_R")) 
                {
                    matJson = parentJson.FindObjectWithKey("Cornea_R");
                    sourceName.Replace("Eye_R", "Cornea_R");
                }
            }

            if (matJson != null) isLeftEye = matJson.GetFloatValue("Custom Shader/Variable/Is Left Eye") > 0f ? true : false;

            ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                matJson, "Textures/Glow");

            if (isCornea)
            {
                ConnectTextureTo(sourceName, mat, "_ScleraDiffuseMap", "Sclera",
                matJson, "Custom Shader/Image/Sclera",
                FLAG_SRGB + FLAG_WRAP_CLAMP);

                ConnectTextureTo(sourceName, mat, "_CorneaDiffuseMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB);

                ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                    matJson, "Textures/HDRP");

                ConnectTextureTo(sourceName, mat, "_MetallicAlphaMap", "MetallicAlpha",
                    matJson, "Textures/MetallicAlpha");

                ConnectTextureTo(sourceName, mat, "_AOMap", "ao",
                    matJson, "Textures/AO");

                ConnectTextureTo(sourceName, mat, "_ColorBlendMap", "BCBMap",
                    matJson, "Custom Shader/Image/EyeBlendMap2");

                ConnectTextureTo(sourceName, mat, "_ScleraNormalMap", "MicroN",
                    matJson, "Custom Shader/Image/Sclera Normal",
                    FLAG_NORMAL);
            }
            else
            {
                ConnectTextureTo(sourceName, mat, "_CorneaDiffuseMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB);

                ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                    matJson, "Textures/HDRP");

                ConnectTextureTo(sourceName, mat, "_ColorBlendMap", "BCBMap",
                    matJson, "Custom Shader/Image/EyeBlendMap2");
            }

            // reconstruct any missing packed texture maps from Blender source maps.
            ConnectBlenderTextures(sourceName, mat, matJson, "_CorneaDiffuseMap", "_MaskMap", "_MetallicAlphaMap");

            if (matJson != null)
            {
                // both the cornea and the eye materials need the same settings:
                mat.SetFloatIf("_AOStrength", 0.5f * Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColorIf("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                mat.SetFloatIf("_ColorBlendStrength", 0.5f * matJson.GetFloatValue("Custom Shader/Variable/BlendMap2 Strength"));
                mat.SetFloatIf("_ShadowRadius", matJson.GetFloatValue("Custom Shader/Variable/Shadow Radius"));
                mat.SetFloatIf("_ShadowHardness", Mathf.Clamp01(matJson.GetFloatValue("Custom Shader/Variable/Shadow Hardness")));
                float specularScale = matJson.GetFloatValue("Custom Shader/Variable/Specular Scale");
                mat.SetColorIf("_CornerShadowColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/Eye Corner Darkness Color")));
                mat.SetColorIf("_IrisColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/Iris Color")));
                mat.SetColorIf("_IrisCloudyColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/Iris Cloudy Color")));

                if (characterInfo.RefractiveEyes)
                {
                    mat.SetFloatIf("_IrisDepth", 0.004f * matJson.GetFloatValue("Custom Shader/Variable/Iris Depth Scale"));
                    mat.SetFloatIf("_PupilScale", 1f * matJson.GetFloatValue("Custom Shader/Variable/Pupil Scale"));
                }
                else if (characterInfo.ParallaxEyes)
                {
                    float depth = Mathf.Clamp(0.333f * matJson.GetFloatValue("Custom Shader/Variable/Iris Depth Scale"), 0.1f, 1.0f);                    
                    //float pupilScale = Mathf.Clamp(1f / Mathf.Pow((depth * 2f + 1f), 2f), 0.1f, 2.0f);                    
                    mat.SetFloatIf("_IrisDepth", depth);
                    //mat.SetFloat("_PupilScale", pupilScale);
                    mat.SetFloatIf("_PupilScale", 0.75f * matJson.GetFloatValue("Custom Shader/Variable/Pupil Scale"));
                }
                else
                {                    
                    mat.SetFloatIf("_PupilScale", 0.75f);
                }

                mat.SetFloatIf("_IrisSmoothness", 0f); // 1f - matJson.GetFloatValue("Custom Shader/Variable/_Iris Roughness"));
                mat.SetFloatIf("_IrisBrightness", 1.5f * matJson.GetFloatValue("Custom Shader/Variable/Iris Color Brightness"));
                mat.SetFloatIf("_IOR", matJson.GetFloatValue("Custom Shader/Variable/_IoR"));
                float irisScale = matJson.GetFloatValue("Custom Shader/Variable/Iris UV Radius") / 0.16f;
                mat.SetFloatIf("_IrisScale", irisScale);
                mat.SetFloatIf("_IrisRadius", 0.15f * irisScale);                
                mat.SetFloatIf("_LimbusWidth", matJson.GetFloatValue("Custom Shader/Variable/Limbus UV Width Color"));
                float limbusDarkScale = matJson.GetFloatValue("Custom Shader/Variable/Limbus Dark Scale");
                float ds = Mathf.Pow(0.01f, 0.2f) / limbusDarkScale;
                float dm = Mathf.Pow(0.5f, 0.2f) / limbusDarkScale;
                mat.SetFloatIf("_LimbusDarkRadius", ds);
                mat.SetFloatIf("_LimbusDarkWidth", 2f * (dm - ds));
                //mat.SetFloatIf("_LimbusDarkWidth", 0.035f);
                float scleraBrightnessPower = 0.65f;
                if (Pipeline.isHDRP) scleraBrightnessPower = 0.75f;
                mat.SetFloatIf("_ScleraBrightness", Mathf.Pow(matJson.GetFloatValue("Custom Shader/Variable/ScleraBrightness"), scleraBrightnessPower));
                mat.SetFloatIf("_ScleraSaturation", 1f);
                mat.SetFloatIf("_ScleraHue", 0.51f);
                mat.SetFloatIf("_ScleraSmoothness", 1f - matJson.GetFloatValue("Custom Shader/Variable/Sclera Roughness"));
                mat.SetFloatIf("_ScleraScale", matJson.GetFloatValue("Custom Shader/Variable/Sclera UV Radius"));
                mat.SetFloatIf("_ScleraNormalStrength", 1f - matJson.GetFloatValue("Custom Shader/Variable/Sclera Flatten Normal"));
                mat.SetFloatIf("_ScleraNormalTiling", Mathf.Clamp(1f / matJson.GetFloatValue("Custom Shader/Variable/Sclera Normal UV Scale"), 0.1f, 10f));
                mat.SetFloatIf("_IsLeftEye", isLeftEye ? 1f : 0f);
            }
        }

        private void ConnectHQHairMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {                        
            if (!ConnectTextureTo(sourceName, mat, "_DiffuseMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB + FLAG_HAIR))
            {
                ConnectTextureTo(sourceName, mat, "_DiffuseMap", "Opacity",
                    matJson, "Textures/Opacity",
                    FLAG_SRGB + FLAG_HAIR);
            }

            ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                matJson, "Textures/HDRP");

            ConnectTextureTo(sourceName, mat, "_MetallicAlphaMap", "MetallicAlpha",
                matJson, "Textures/MetallicAlpha");

            ConnectTextureTo(sourceName, mat, "_AOMap", "ao",
                matJson, "Textures/AO");

            if (!ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL))
            {
                if (RECONSTRUCT_FLOW_NORMALS)
                {
                    BakeHairFlowToNormalMap(mat, sourceName, matJson);
                }
            }    

            ConnectTextureTo(sourceName, mat, "_BlendMap", "blend_multiply",
                matJson, "Textures/Blend");

            ConnectTextureTo(sourceName, mat, "_FlowMap", "Hair Flow Map",
                matJson, "Custom Shader/Image/Hair Flow Map");

            ConnectTextureTo(sourceName, mat, "_IDMap", "Hair ID Map",
                matJson, "Custom Shader/Image/Hair ID Map", FLAG_HAIR_ID);

            ConnectTextureTo(sourceName, mat, "_RootMap", "Hair Root Map",
                matJson, "Custom Shader/Image/Hair Root Map");

            ConnectTextureTo(sourceName, mat, "_SpecularMap", "HSpecMap",
                matJson, "Custom Shader/Image/Hair Specular Mask Map");

            ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                matJson, "Textures/Glow");

            // reconstruct any missing packed texture maps from Blender source maps.
            ConnectBlenderTextures(sourceName, mat, matJson, "_DiffuseMap", "_MaskMap", "_MetallicAlphaMap");

            //if (RP == RenderPipeline.URP && !isHair)
            //{                
            //    mat.SetFloatIf("_AlphaRemap", 0.5f);
            //}

            float smoothnessPowerMod = ValueByPipeline(1f, 1f, 1f);
            float specularPowerMod = ValueByPipeline(0.5f, 0.5f, 0.33f);
            float specularMin = ValueByPipeline(0.05f, 0f, 0f);
            float specularMax = ValueByPipeline(0.5f, 0.4f, 0.65f);

            bool isFacialHair = MeshUtil.MeshIsFacialHair(obj);
            if (isFacialHair)
            {
                // make facial hair thinner and rougher  
                smoothnessPowerMod = ValueByPipeline(1.5f, 1.5f, 1.5f);
                specularPowerMod = ValueByPipeline(1f, 1f, 1f);
                mat.SetFloatIf("_DepthPrepass", 0.75f);                
                mat.SetFloatIf("_AlphaPower", 1.5f);
                mat.SetFloatIf("_AlphaRemap", 1.0f);
                mat.SetFloatIf("_SmoothnessPower", smoothnessPowerMod);
            }

            Color diffuseColor = Color.white;

            if (matJson != null)
            {
                mat.SetFloatIf("_AOStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColorIf("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloatIf("_NormalStrength", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
                mat.SetFloatIf("_AOOccludeAll", (RP == RenderPipeline.HDRP ? 0.5f : 1f) * matJson.GetFloatValue("Custom Shader/Variable/AO Map Occlude All Lighting"));
                mat.SetFloatIf("_BlendStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/Blend/Strength") / 100f));
                mat.SetColorIf("_VertexBaseColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/VertexGrayToColor")));                
                mat.SetFloatIf("_VertexColorStrength", 1f * matJson.GetFloatValue("Custom Shader/Variable/VertexColorStrength"));
                mat.SetFloatIf("_BaseColorStrength", 1f * matJson.GetFloatValue("Custom Shader/Variable/BaseColorMapStrength"));                
                mat.SetFloatIf("_DiffuseStrength", 1f * matJson.GetFloatValue("Custom Shader/Variable/Diffuse Strength"));

                diffuseColor = Util.LinearTosRGB(matJson.GetColorValue("Diffuse Color"));
                mat.SetColorIf("_DiffuseColor", diffuseColor);

                // Hair Specular Map Strength = Custom Shader/Variable/Hair Specular Map Strength
                // == Overall specular multiplier
                //
                // Specular Strength = Custom Shader/Variable/Specular Strength
                // == Light based Anisotropic Highlight strength
                //
                // Indirect Specular Strength = Custom Shader/Variable/Secondary Specular Strength
                // == Specular from the GI? (Ignore this in favour of dual lobe anisotropic?)
                // see Indirect Specular Lighting node in ASE.
                //
                // Transmission Strength = Custom Shader/Variable/Transmission Strength
                // == Rim lighting/rim translucency strength

                float specMapStrength = matJson.GetFloatValue("Custom Shader/Variable/Hair Specular Map Strength");                
                float specStrength = matJson.GetFloatValue("Custom Shader/Variable/Specular Strength");
                float specStrength2 = matJson.GetFloatValue("Custom Shader/Variable/Secondary Specular Strength");
                float rimTransmission = matJson.GetFloatValue("Custom Shader/Variable/Transmission Strength");
                float roughnessStrength = matJson.GetFloatValue("Custom Shader/Variable/Hair Roughness Map Strength");
                float smoothnessStrength = 1f - Mathf.Pow(roughnessStrength, 1f);
                float smoothnessMax = mat.GetFloatIf("_SmoothnessMax", MAX_SMOOTHNESS);

                if (RP == RenderPipeline.HDRP)
                {
                    float secondarySpecStrength = matJson.GetFloatValue("Custom Shader/Variable/Secondary Specular Strength");
                    SetFloatPowerRange(mat, "_SmoothnessMin", smoothnessStrength, 0f, smoothnessMax, smoothnessPowerMod);
                    SetFloatPowerRange(mat, "_SpecularMultiplier", specMapStrength * specStrength, specularMin, specularMax, specularPowerMod);
                    SetFloatPowerRange(mat, "_SecondarySpecularMultiplier", specMapStrength * specStrength2, 0.0125f, 0.125f, specularPowerMod);
                    // set by template
                    //mat.SetFloatIf("_SecondarySmoothness", 0.5f);
                    mat.SetFloatIf("_RimTransmissionIntensity", 0.75f * specMapStrength * Mathf.Pow(rimTransmission, 0.5f));
                    mat.SetFloatIf("_FlowMapFlipGreen", 1f -
                        matJson.GetFloatValue("Custom Shader/Variable/TangentMapFlipGreen"));
                    mat.SetFloatIf("_SpecularShiftMin",
                        matJson.GetFloatValue("Custom Shader/Variable/BlackColor Reflection Offset Z"));
                    mat.SetFloatIf("_SpecularShiftMax",
                        matJson.GetFloatValue("Custom Shader/Variable/WhiteColor Reflection Offset Z"));
                }
                else if (RP == RenderPipeline.URP && !USE_AMPLIFY_SHADER)
                {
                    mat.SetFloatIf("_DiffuseStrength", 1.15f * matJson.GetFloatValue("Custom Shader/Variable/Diffuse Strength"));
                    mat.SetFloatIf("_SmoothnessMin", 0f);
                    mat.SetFloatIf("_SpecularMultiplier", Mathf.Lerp(0.1f, 0.5f, specMapStrength * specStrength));
                    mat.SetFloatIf("_FlowMapFlipGreen", 1f - matJson.GetFloatValue("Custom Shader/Variable/TangentMapFlipGreen"));
                    mat.SetFloatIf("_SpecularShiftMin",
                        matJson.GetFloatValue("Custom Shader/Variable/BlackColor Reflection Offset Z"));
                    mat.SetFloatIf("_SpecularShiftMax",
                        matJson.GetFloatValue("Custom Shader/Variable/WhiteColor Reflection Offset Z"));
                }
                else
                {
                    if (USE_AMPLIFY_SHADER)
                    {
                        SetFloatPowerRange(mat, "_SmoothnessMin", smoothnessStrength, 0f, smoothnessMax, smoothnessPowerMod);
                        SetFloatPowerRange(mat, "_SpecularMultiplier", specMapStrength * specStrength, specularMin, specularMax, specularPowerMod);
                        mat.SetFloatIf("_RimTransmissionIntensity", ValueByPipeline(1f, 75f, 75f) * specMapStrength * Mathf.Pow(rimTransmission, 0.5f));
                        mat.SetFloatIf("_FlowMapFlipGreen", 1f -
                            matJson.GetFloatValue("Custom Shader/Variable/TangentMapFlipGreen"));
                        mat.SetFloatIf("_SpecularShiftMin", -0.25f +
                            matJson.GetFloatValue("Custom Shader/Variable/BlackColor Reflection Offset Z"));
                        mat.SetFloatIf("_SpecularShiftMax", -0.25f +
                            matJson.GetFloatValue("Custom Shader/Variable/WhiteColor Reflection Offset Z"));
                    }
                    else
                    {
                        mat.SetFloatIf("_SmoothnessMin", Util.CombineSpecularToSmoothness(specMapStrength * specStrength, smoothnessStrength));
                    }
                }

                Color rootColor = Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/RootColor"));
                Color tipColor = Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/TipColor"));
                Color hairColor = diffuseColor * ((rootColor + tipColor) * 0.5f);
                Color.RGBToHSV(hairColor, out float H, out float S, out float V);                
                Color specTint = Color.HSVToRGB(H, S * 0.333f, 1f);
                mat.SetColorIf("_RootColor", rootColor);
                mat.SetColorIf("_EndColor", tipColor);
                mat.SetFloatIf("_GlobalStrength", matJson.GetFloatValue("Custom Shader/Variable/UseRootTipColor"));
                mat.SetFloatIf("_RootColorStrength", matJson.GetFloatValue("Custom Shader/Variable/RootColorStrength"));
                mat.SetFloatIf("_EndColorStrength", matJson.GetFloatValue("Custom Shader/Variable/TipColorStrength"));
                mat.SetFloatIf("_InvertRootMap", matJson.GetFloatValue("Custom Shader/Variable/InvertRootTip"));
                mat.SetColorIf("_SpecularTint", specTint);

                if (matJson.GetFloatValue("Custom Shader/Variable/ActiveChangeHairColor") > 0f)
                {
                    mat.EnableKeyword("BOOLEAN_ENABLECOLOR_ON");
                    mat.SetFloat("BOOLEAN_ENABLECOLOR", 1f);
                }
                else
                {
                    mat.DisableKeyword("BOOLEAN_ENABLECOLOR_ON");
                    mat.SetFloat("BOOLEAN_ENABLECOLOR", 0f);
                }

                mat.SetColorIf("_HighlightAColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/_1st Dye Color")));
                mat.SetFloatIf("_HighlightAStrength", matJson.GetFloatValue("Custom Shader/Variable/_1st Dye Strength"));
                mat.SetVectorIf("_HighlightADistribution", (1f / 255f) * matJson.GetVector3Value("Custom Shader/Variable/_1st Dye Distribution from Grayscale"));
                mat.SetFloatIf("_HighlightAOverlapEnd", matJson.GetFloatValue("Custom Shader/Variable/Mask 1st Dye by RootMap"));
                mat.SetFloatIf("_HighlightAOverlapInvert", matJson.GetFloatValue("Custom Shader/Variable/Invert 1st Dye RootMap Mask"));

                mat.SetColorIf("_HighlightBColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/_2nd Dye Color")));
                mat.SetFloatIf("_HighlightBStrength", matJson.GetFloatValue("Custom Shader/Variable/_2nd Dye Strength"));
                mat.SetVectorIf("_HighlightBDistribution", (1f / 255f) * matJson.GetVector3Value("Custom Shader/Variable/_2nd Dye Distribution from Grayscale"));
                mat.SetFloatIf("_HighlightBOverlapEnd", matJson.GetFloatValue("Custom Shader/Variable/Mask 2nd Dye by RootMap"));
                mat.SetFloatIf("_HighlightBOverlapInvert", matJson.GetFloatValue("Custom Shader/Variable/Invert 2nd Dye RootMap Mask"));                
            }            
        }

        private void ConnectHQEyeOcclusionMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            if (matJson != null)
            {
                mat.SetFloatIf("_ExpandOut", 0.0001f * matJson.GetFloatValue("Custom Shader/Variable/Depth Offset"));
                mat.SetFloatIf("_ExpandUpper", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Top Offset"));
                mat.SetFloatIf("_ExpandLower", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Bottom Offset"));
                mat.SetFloatIf("_ExpandInner", 0.0001f + 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Inner Corner Offset"));
                mat.SetFloatIf("_ExpandOuter", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Outer Corner Offset"));
                mat.SetFloatIf("_TearDuctPosition", matJson.GetFloatValue("Custom Shader/Variable/Tear Duct Position"));
                //mat.SetFloat("_TearDuctWidth", 0.05f);
                mat.SetColorIf("_OcclusionColor", Color.Lerp(
                    Util.sRGBToLinear(matJson.GetColorValue("Custom Shader/Variable/Shadow Color")), Color.black, 0.5f));

                float os1 = matJson.GetFloatValue("Custom Shader/Variable/Shadow Strength");
                float os2 = matJson.GetFloatValue("Custom Shader/Variable/Shadow2 Strength");

                mat.SetFloatIf("_OcclusionStrength", Mathf.Pow(os1, 1f / 3f));
                mat.SetFloatIf("_OcclusionStrength2", Mathf.Pow(os2, 1f / 3f));
                mat.SetFloatIf("_OcclusionPower", 2.0f);
                //mat.SetFloat("_OcclusionPower", 2f);

                float top = matJson.GetFloatValue("Custom Shader/Variable/Shadow Top");                
                float bottom = matJson.GetFloatValue("Custom Shader/Variable/Shadow Bottom");
                float inner = matJson.GetFloatValue("Custom Shader/Variable/Shadow Inner Corner");
                float outer = matJson.GetFloatValue("Custom Shader/Variable/Shadow Outer Corner");
                float top2 = matJson.GetFloatValue("Custom Shader/Variable/Shadow2 Top");

                
                float topMax = Mathf.Lerp(top, 1f, matJson.GetFloatValue("Custom Shader/Variable/Shadow Top Range"));
                float bottomMax = Mathf.Lerp(bottom, 1f, matJson.GetFloatValue("Custom Shader/Variable/Shadow Bottom Range"));
                float innerMax = Mathf.Lerp(inner, 1f, matJson.GetFloatValue("Custom Shader/Variable/Shadow Inner Corner Range"));
                float outerMax = Mathf.Lerp(outer, 1f, matJson.GetFloatValue("Custom Shader/Variable/Shadow Outer Corner Range"));
                float top2Max = Mathf.Lerp(top2, 1f, matJson.GetFloatValue("Custom Shader/Variable/Shadow2 Top Range"));
                
                /*
                float topMax = top + matJson.GetFloatValue("Custom Shader/Variable/Shadow Top Range");
                float bottomMax = bottom + matJson.GetFloatValue("Custom Shader/Variable/Shadow Bottom Range");
                float innerMax = inner + matJson.GetFloatValue("Custom Shader/Variable/Shadow Inner Corner Range");
                float outerMax = outer + matJson.GetFloatValue("Custom Shader/Variable/Shadow Outer Corner Range");
                float top2Max = top2 + matJson.GetFloatValue("Custom Shader/Variable/Shadow2 Top Range");
                */

                float scale = 1.0f;
                mat.SetFloatIf("_TopMin", scale * top);
                mat.SetFloatIf("_TopMax", topMax);
                mat.SetFloatIf("_TopCurve", matJson.GetFloatValue("Custom Shader/Variable/Shadow Top Arc"));
                mat.SetFloatIf("_BottomMin", scale * bottom);
                mat.SetFloatIf("_BottomMax", bottomMax);
                mat.SetFloatIf("_BottomCurve", matJson.GetFloatValue("Custom Shader/Variable/Shadow Bottom Arc"));
                mat.SetFloatIf("_InnerMin", inner);
                mat.SetFloatIf("_InnerMax", innerMax);
                mat.SetFloatIf("_OuterMin", scale * outer);
                mat.SetFloatIf("_OuterMax", outerMax);
                mat.SetFloatIf("_Top2Min", scale * top2);
                mat.SetFloatIf("_Top2Max", top2Max);                
            }

            float modelScale = (obj.transform.localScale.x +
                                obj.transform.localScale.y +
                                obj.transform.localScale.z) / 3.0f;            
            mat.SetFloatIf("_ExpandScale", 1.0f / modelScale);


        }

        private void ConnectHQTearlineMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {            
            if (matJson != null)
            {
                mat.SetFloatIf("_DepthOffset", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Depth Offset"));                
                mat.SetFloatIf("_InnerOffset", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Depth Offset"));

                mat.SetFloatIf("_Smoothness", MAX_SMOOTHNESS - MAX_SMOOTHNESS * matJson.GetFloatValue("Custom Shader/Variable/Roughness"));
            }

            if (blenderProject)
            {
                mat.SetFloatIf("BOOLEAN_ZUP", 1f);
                mat.EnableKeyword("BOOLEAN_ZUP_ON");
            }
            else
            {
                mat.SetFloatIf("BOOLEAN_ZUP", 0f);
                mat.DisableKeyword("BOOLEAN_ZUP_ON");
            }
        }        

        private Texture2D GetCachedBakedMap(Material sharedMaterial, string shaderRef)
        {
            switch (shaderRef)
            {
                case "_ThicknessMap":
                    if (bakedThicknessMaps.ContainsKey(sharedMaterial)) return bakedThicknessMaps[sharedMaterial];
                    break;

                case "_DetailMap":
                    if (bakedDetailMaps.ContainsKey(sharedMaterial)) return bakedDetailMaps[sharedMaterial];
                    break;
            }

            return null;
        }

        private void PrepDefaultMap(string sourceName, string suffix, QuickJSON jsonData, string jsonPath, int flags = 0)
        {
            string jsonTexturePath = null;
            if (jsonData != null)
            {
                jsonTexturePath = jsonData.GetStringValue(jsonPath + "/Texture Path");
            }

            Texture2D tex = GetTextureFrom(jsonTexturePath, sourceName, suffix, out string name, true);
            // make sure to set the correct import settings for 
            // these textures before using them for baking...                        
            if (!DoneTexture(tex)) SetTextureImport(tex, name, FLAG_FOR_BAKE + flags);
        }

        private void FixHDRPMap(Material sharedMat, string sourceName, QuickJSON jsonData, 
            string maskJsonPath = "Textures/HDRP", string maskSuffix = "HDRP",
            string detailJsonPath = "Custom Shader/Image/MicroNormalMask", string detailSuffix = "MicroNMask")
        {            
            int flags = 0;
            string maskJsonTexturePath = jsonData?.GetStringValue(maskJsonPath + "/Texture Path");
            string detailJsonTexturePath = jsonData?.GetStringValue(detailJsonPath + "/Texture Path");            

            Texture2D mask = GetTextureFrom(maskJsonTexturePath, sourceName, maskSuffix, out string maskName, true);
            Texture2D detail = GetTextureFrom(detailJsonTexturePath, sourceName, detailSuffix, out string detailName, true);
            
            // make sure to set the correct import settings for 
            // these textures before using them for baking...                        
            if (!DoneTexture(mask)) SetTextureImport(mask, maskName, FLAG_FOR_BAKE + flags);
            if (!DoneTexture(detail)) SetTextureImport(detail, detailName, FLAG_FOR_BAKE + flags);

            ComputeBake baker = new ComputeBake(fbx, characterInfo);
            
            Texture2D bakedTex = null;

            if (mask && detail)
            {
                bakedTex = baker.BakeCorrectedHDRPMap(mask, detail, maskName);
                if (bakedTex)
                {
                    bakedHDRPMaps.Add(sharedMat, bakedTex);
                }
            }
        }

        private void BakeDefaultMap(Material sharedMat, string sourceName, string shaderRef, string suffix, QuickJSON jsonData, string jsonPath)
        {
            ComputeBake baker = new ComputeBake(fbx, characterInfo);

            string jsonTexturePath = null;
            if (jsonData != null)
            {
                jsonTexturePath = jsonData.GetStringValue(jsonPath + "/Texture Path");
            }

            Texture2D tex = GetTextureFrom(jsonTexturePath, sourceName, suffix, out string name, true);
            Texture2D bakedTex = null;

            if (tex)
            {
                switch (shaderRef)
                {
                    case "_ThicknessMap":                        
                        bakedTex = baker.BakeDefaultSkinThicknessMap(tex, name);
                        if (bakedTex)
                        {
                            bakedThicknessMaps.Add(sharedMat, bakedTex);
                        }
                        break;

                    case "_DetailMap":                        
                        bakedTex = baker.BakeDefaultDetailMap(tex, name);
                        if (bakedTex)
                        {
                            bakedDetailMaps.Add(sharedMat, bakedTex);
                        }
                        break;
                }
            }
        }

        private void BakeHairFlowToNormalMap(Material mat, string sourceName, QuickJSON matJson)
        {
            ComputeBake baker = new ComputeBake(fbx, characterInfo);
            
            Vector3 tangentVector = new Vector3(1, 0, 0);
            bool flipY = false;

            string jsonFlowTexturePath = null;
            string jsonNormalTexturePath = null;
            if (matJson != null)
            {
                jsonFlowTexturePath = matJson.GetStringValue("Custom Shader/Image/Hair Flow Map/Texture Path");
                jsonNormalTexturePath = matJson.GetStringValue("Textures/Normal/Texture Path");
                tangentVector = (1f / 255f) * matJson.GetVector3Value("Custom Shader/Variable/TangentVectorColor");
                flipY = matJson.GetFloatValue("Custom Shader/Variable/TangentMapFlipGreen") > 0f ? true : false;
            }
            Texture2D flowMap = GetTextureFrom(jsonFlowTexturePath, sourceName, "Hair Flow Map", out string name, true);
            Texture2D normalMap = GetTextureFrom(jsonNormalTexturePath, sourceName, "Normal", out name, true);

            if (flowMap && !normalMap)
            {
                normalMap = baker.BakeFlowMapToNormalMap(flowMap, tangentVector, flipY, sourceName + "_Normal");                
                mat.SetTextureIf("_NormalMap", normalMap);
            }
        }

        private Texture2D GetTextureFrom(string jsonTexturePath, string materialName, string suffix, out string name, bool search)
        {
            Texture2D tex = null;
            name = "";

            // try to find the texture from the supplied texture path (usually from the json data).
            if (!string.IsNullOrEmpty(jsonTexturePath))
            {             
                // try to load the texture asset directly from the json path.
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Util.CombineJsonTexPath(fbxFolder, jsonTexturePath));
                name = Path.GetFileNameWithoutExtension(jsonTexturePath);

                // if that fails, try to find the texture by name in the texture folders.
                if (!tex && search)
                {                    
                    tex = Util.FindTexture(textureFolders.ToArray(), name);
                }
            }

            // as a final fallback try to find the texture from the material name and suffix.
            if (!tex && search)
            {
                name = materialName + "_" + suffix;
                tex = Util.FindTexture(textureFolders.ToArray(), name);
            }

            return tex;
        }

        private void SetTextureImport(Texture2D tex, string name, int flags = 0)
        {
            if (!tex) return;

            // now fix the import settings for the texture.
            string path = AssetDatabase.GetAssetPath(tex);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = 4096;

            // apply the sRGB and alpha settings for re-import.
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.mipMapBias = Importer.MIPMAP_BIAS;
            if ((flags & FLAG_SRGB) > 0)
            {
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;                
                importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;                
                if ((flags & FLAG_HAIR) > 0)
                {
                    importer.mipMapsPreserveCoverage = true;
                    importer.alphaTestReferenceValue = Importer.MIPMAP_ALPHA_CLIP_HAIR;                    
                }
                else if ((flags & FLAG_ALPHA_CLIP) > 0)
                {
                    importer.mipMapsPreserveCoverage = true;
                    importer.alphaTestReferenceValue = 0.5f;
                }
                else
                {
                    importer.mipMapsPreserveCoverage = false;
                }
            }
            else
            {
                importer.sRGBTexture = false;
                importer.alphaIsTransparency = false;
                importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                importer.mipMapBias = Importer.MIPMAP_BIAS;
                importer.mipMapsPreserveCoverage = false;
            }

            if ((flags & FLAG_HAIR_ID) > 0)
            {
                importer.mipMapBias = Importer.MIPMAP_BIAS_HAIR_ID_MAP;
                importer.mipmapEnabled = true;
            }

            // apply the texture type for re-import.
            if ((flags & FLAG_NORMAL) > 0)
            {
                importer.textureType = TextureImporterType.NormalMap;
                if (name.iEndsWith("Bump"))
                {
                    importer.convertToNormalmap = true;
                    importer.heightmapScale = 0.025f;
                    importer.normalmapFilter = TextureImporterNormalFilter.Standard;
                }
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
            }

            if ((flags & FLAG_FOR_BAKE) > 0)
            {
                // turn off texture compression and unlock max size to 4k, for the best possible quality bake
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.compressionQuality = 0;
                importer.maxTextureSize = 4096;
            }
            else
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.compressionQuality = 50;
                importer.maxTextureSize = 4096;
            }

            if ((flags & FLAG_WRAP_CLAMP) > 0)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
            }

            // add the texure path to the re-import paths.
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
            {
                if (!importAssets.Contains(path)) importAssets.Add(path);
            }
        }    
        
        private bool DoneTexture(Texture2D tex)
        {
            string texGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tex));
            if (!doneTextureGUIDS.Contains(texGUID))
            {                
                doneTextureGUIDS.Add(texGUID);
                return false;
            }            
            return true;
        } 

        private Dictionary<string, WrinkleProp> BuildWrinkleProps(QuickJSON matJson)
        {
            if (matJson != null)
            {
                Dictionary<string, WrinkleProp> wrinkleProps = new Dictionary<string, WrinkleProp>();

                QuickJSON wrinkleRulesJson = matJson.GetObjectAtPath("Wrinkle/WrinkleRules");
                QuickJSON wrinkleEaseJson = matJson.GetObjectAtPath("Wrinkle/WrinkleEaseStrength");
                QuickJSON wrinkleWeightJson = matJson.GetObjectAtPath("Wrinkle/WrinkleRuleWeights");                

                if (wrinkleRulesJson != null && wrinkleEaseJson != null && wrinkleWeightJson != null)
                {
                    for (int i = 0; i < wrinkleRulesJson.values.Count; i++)
                    {
                        string ruleName = wrinkleRulesJson.values[i].StringValue;
                        float easeStrength = wrinkleEaseJson.values[i].FloatValue;
                        float weight = wrinkleWeightJson.values[i].FloatValue;

                        wrinkleProps.Add(ruleName, new WrinkleProp() { ease = easeStrength, weight = weight });
                    }
                }

                return wrinkleProps;
            }

            return null;
        }
          
        private void AddWrinkleManager(GameObject obj, SkinnedMeshRenderer smr, Material mat, QuickJSON matJson)
        {
            WrinkleManager wm = obj.AddComponent<WrinkleManager>();
            wm.headMaterial = mat;
            wm.skinnedMeshRenderer = smr;
            float overallWeight = 1;
            if (matJson.PathExists("Wrinkle/WrinkleOverallWeight"))
            {
                overallWeight = matJson.GetFloatValue("Wrinkle/WrinkleOverallWeight");
            }
            wm.BuildConfig(BuildWrinkleProps(matJson), overallWeight);
        }

        private void CopyWrinkleMasks(string folder)
        {
            string[] packageFolders = new string[] { "Packages" };
            string[] characterFolders = new string[] { folder };

            string[] maskNames = new string[] { "RL_Wrinkle_Set 1-1", "RL_Wrinkle_Set 1-2", "RL_Wrinkle_Set 2", "RL_Wrinkle_Set 3" };

            List<Texture2D> maskTextures = new List<Texture2D>();

            foreach (string maskName in maskNames)
            {
                Texture2D tex = Util.FindTexture(characterFolders, maskName);
                if (!tex)
                {
                    tex = Util.FindTexture(packageFolders, maskName);
                    if (tex)
                    {
                        // TODO
                    }
                }
            }
        }


        private void ApplyWrinkleMasks(Material mat)
        {
            string[] folders = new string[] { "Packages", fbmFolder, texFolder };

            string[] maskNames = new string[] { "RL_WrinkleMask_Set1A", "RL_WrinkleMask_Set1B", "RL_WrinkleMask_Set2", "RL_WrinkleMask_Set3", "RL_WrinkleMask_Set123" };
            string[] refNames = new string[] { "_WrinkleMaskSet1A", "_WrinkleMaskSet1B", "_WrinkleMaskSet2", "_WrinkleMaskSet3", "_WrinkleMaskSet123" };
            
            for (int i = 0; i < maskNames.Length; i++)
            {
                string maskName = maskNames[i];
                string refName = refNames[i];
                Texture2D tex = Util.FindTexture(folders, maskName);
                if (tex)
                {
                    mat.SetTextureIf(refName, tex);
                }
            }            
        }

        private bool ConnectTextureTo(string materialName, Material mat, string shaderRef, string suffix, 
                                      QuickJSON jsonData, string jsonPath, int flags = 0)
        {
            Texture2D tex = null;

            if (mat.HasProperty(shaderRef))
            {
                Vector2 offset = Vector2.zero;
                Vector2 tiling = Vector2.one;
                string jsonTexturePath = null;

                if (jsonData != null)
                {                    
                    if (jsonData.PathExists(jsonPath + "/Texture Path"))
                        jsonTexturePath = jsonData.GetStringValue(jsonPath + "/Texture Path");
                    if (jsonData.PathExists(jsonPath + "/Offset"))
                        offset = jsonData.GetVector2Value(jsonPath + "/Offset");
                    if (jsonData.PathExists(jsonPath + "/Tiling"))
                        tiling = jsonData.GetVector2Value(jsonPath + "/Tiling");
                }

                tex = GetTextureFrom(jsonTexturePath, materialName, suffix, out string name, true);

                if (tex)
                {
                    // set the texture ref in the material.
                    mat.SetTexture(shaderRef, tex);
                    mat.SetTextureOffset(shaderRef, offset);
                    mat.SetTextureScale(shaderRef, tiling);

                    Util.LogInfo("        Connecting texture: " + tex.name);

                    if (!DoneTexture(tex)) SetTextureImport(tex, name, flags);
                }
                else
                {
                    if (!string.IsNullOrEmpty(jsonTexturePath))
                    {
                        Util.LogError("Unable to locate texture defined in Json: " + jsonTexturePath + "\nMaterial: " + materialName);
                    }

                    mat.SetTexture(shaderRef, null);
                }
            }

            return tex != null;
        }

        private Texture2D GetTexture(string materialName, string suffix, QuickJSON jsonData, string jsonPath, bool search)
        {
            Texture2D tex = null;

            string jsonTexturePath = null;

            if (jsonData != null)
            {
                if (jsonData.PathExists(jsonPath + "/Texture Path"))
                    jsonTexturePath = jsonData.GetStringValue(jsonPath + "/Texture Path");                
            }

            tex = GetTextureFrom(jsonTexturePath, materialName, suffix, out string name, search);

            return tex;
        }

        private bool HasTextureIf(Material mat, string shaderRef)
        {
            if (mat.HasProperty(shaderRef))
            {
                return mat.GetTexture(shaderRef) != null;
            }
            return false;
        }

        private int CountMaterials(GameObject obj, string match)
        {
            int count = 0;
            SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
            if (smr)
            {
                foreach (Material mat in smr.sharedMaterials)
                {
                    if (mat && mat.name.iContains(match)) count++;
                }
            }
            return count;
        }

        private void KeywordsOnTexture(Material mat, string shaderRef, params string[] keywords)
        {
            if (mat.HasProperty(shaderRef))
            {
                if (mat.GetTexture(shaderRef) != null)
                {
                    foreach (string keyword in keywords)
                    {
                        mat.EnableKeyword(keyword);                        
                    }
                }
                else
                {
                    foreach (string keyword in keywords)
                    {
                        mat.DisableKeyword(keyword);
                    }
                }
            }            
        }

        private T ValueByPipeline<T>(T hdrp, T urp, T builtin)
        {
            if (RP == RenderPipeline.HDRP) return hdrp;
            else if (RP == RenderPipeline.URP) return urp;
            else return builtin;
        }        

        private void SetFloatPowerRange(Material mat, string shaderRef, float value, float min, float max, float power = 1f)
        {
            mat.SetFloatIf(shaderRef, Mathf.Lerp(min, max, Mathf.Pow(value, power)));
        }

        public void ProcessMotionFbx(string guid, Avatar sourceAvatar, GameObject targetCharacterModel)
        {
            string motionAssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(motionAssetPath))
            {
                Util.LogInfo("Processing motion Fbx: " + motionAssetPath);
                RL.DoMotionImport(characterInfo, sourceAvatar, motionAssetPath);                

                // extract and retarget animations if needed.                
                int animationRetargeted = characterInfo.DualMaterialHair ? 2 : 1;
                bool replace = characterInfo.animationRetargeted != animationRetargeted;
                if (replace) Util.LogInfo("Retargeting all imported animations: " + motionAssetPath);
                AnimRetargetGUI.GenerateCharacterTargetedAnimations(motionAssetPath, targetCharacterModel, replace);
            }            
        }
    }
}
