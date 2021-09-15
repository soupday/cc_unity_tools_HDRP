/* 
 * Copyright (C) 2021 Victor Soupday
 * This file is part of CC3_Unity_Tools <https://github.com/soupday/cc3_unity_tools>
 * 
 * CC3_Unity_Tools is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * CC3_Unity_Tools is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with CC3_Unity_Tools.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;

namespace Reallusion.Import
{
    public class Importer
    {
        private readonly GameObject fbx;
        private readonly QuickJSON jsonData;
        private readonly QuickJSON jsonMeshData;
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
        private MaterialQuality quality = MaterialQuality.High;
        private CharacterInfo characterInfo;
        private List<string> processedSourceMaterials;
        private Dictionary<Material, Texture2D> bakedDetailMaps;
        private Dictionary<Material, Texture2D> bakedThicknessMaps;
        private readonly BaseGeneration generation;

        public const string MATERIALS_FOLDER = "Materials";
        public const string PREFABS_FOLDER = "Prefabs";

        public const float MIPMAP_BIAS = -0.2f;        
        public const float MIPMAP_ALPHA_CLIP_HAIR = 0.6f;
        public const float MIPMAP_ALPHA_CLIP_HAIR_BAKED = 0.8f;

        public const int FLAG_SRGB = 1;
        public const int FLAG_NORMAL = 2;
        public const int FLAG_FOR_BAKE = 4;
        public const int FLAG_ALPHA_CLIP = 8;
        public const int FLAG_HAIR = 16;

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
            string jsonPath = characterName + "/Object/" + characterName + "/Meshes";
            jsonMeshData = null;
            if (jsonData.PathExists(jsonPath))
                jsonMeshData = jsonData.GetObjectAtPath(jsonPath);
            else
                Debug.LogError("Unable to find Json mesh data: " + jsonPath);            

            string jsonVersion = jsonData?.GetStringValue(characterName + "/Version");
            if (!string.IsNullOrEmpty(jsonVersion))
                Util.LogInfo("JSON version: " + jsonVersion);

            generation = info.Generation;

            // initialise the import path cache.        
            // this is used to re-import everything in one batch after it has all been setup.
            // (calling a re-import on sub-materials or sub-objects will trigger a re-import of the entire fbx each time...)
            importAssets = new List<string>(); // { fbxPath };
            processedSourceMaterials = new List<string>();
        }        

        public void SetQuality(MaterialQuality qual)
        {
            quality = qual;
        }

        public GameObject Import()
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

            // before we do anything else, if we are connecting default materials we need to bake a few maps first...
            if (quality == MaterialQuality.Default)
            {
                CacheBakedMaps();
            }

            ProcessObjectTree(fbx);

            Util.LogInfo("Writing changes to asset database.");

            // set humanoid animation type
            RL.HumanoidImportSettings(fbx, importer, characterName, generation, jsonData);

            // save all the changes and refresh the asset database.
            AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
            importAssets.Add(fbxPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // create prefab
            GameObject prefab = RL.CreatePrefabFromFbx(characterInfo, fbx);

            Util.LogInfo("Done!");

            Selection.activeObject = fbx;

            //System.Media.SystemSounds.Asterisk.Play();

            return prefab;
        }

        void ProcessObjectTree(GameObject obj)
        {
            int childCount = obj.transform.childCount;
            for (int i = 0; i < childCount; i++) 
            {
                GameObject child = obj.transform.GetChild(i).gameObject;

                if (child.GetComponent<Renderer>() != null)
                {
                    ProcessObject(child);
                }
                else if (child.name.iContains("_LOD0"))
                {
                    ProcessObjectTree(child);
                }
            }
        }

        private void ProcessObject(GameObject obj)
        {
            Renderer renderer = obj.GetComponent<Renderer>();

            if (renderer)
            {
                Util.LogInfo("Processing sub-object: " + obj.name);

                foreach (Material sharedMat in renderer.sharedMaterials)
                {
                    // in case any of the materials have been renamed after a previous import, get the source name.
                    string sourceName = Util.GetSourceMaterialName(fbxPath, sharedMat);

                    // if the material has already been processed, it is already in the remap list and should be connected automatically.
                    if (!processedSourceMaterials.Contains(sourceName))
                    {
                        // fetch the json parent for this material.
                        // the json data for the material contains custom shader names, parameters and texture paths.
                        QuickJSON matJson = null;
                        string jsonPath = obj.name + "/Materials/" + sourceName;
                        if (jsonMeshData != null && jsonMeshData.PathExists(jsonPath))
                            matJson = jsonMeshData.GetObjectAtPath(jsonPath);
                        else
                            Debug.LogError("Unable to find json material data: " + jsonPath);

                        // determine the material type, this dictates the shader and template material.
                        MaterialType materialType = GetMaterialType(obj, sharedMat, sourceName, matJson);

                        Util.LogInfo("    Material name: " + sourceName + ", type:" + materialType.ToString());

                        // re-use or create the material.
                        Material mat = CreateRemapMaterial(materialType, sharedMat, sourceName);

                        // connect the textures.
                        if (mat) ProcessTextures(obj, sourceName, sharedMat, mat, materialType, matJson);

                        processedSourceMaterials.Add(sourceName);
                    }
                    else
                    {
                        Util.LogInfo("    Material name: " + sourceName + " already processed.");
                    }
                }
            }
        }        

        private MaterialType GetMaterialType(GameObject obj, Material mat, string sourceName, QuickJSON matJson)
        {
            if (matJson != null)
            {
                bool hasOpacity = false;
                if (matJson != null && matJson.PathExists("Textures/Opacity/Texture Path"))
                {
                    hasOpacity = true;
                }

                if (sourceName.StartsWith("Std_Eye_L", System.StringComparison.InvariantCultureIgnoreCase) ||
                    sourceName.StartsWith("Std_Eye_R", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    return MaterialType.Eye;
                }

                if (hasOpacity)
                {
                    if (sourceName.StartsWith("Std_Eyelash", System.StringComparison.InvariantCultureIgnoreCase))
                        return MaterialType.Eyelash;
                    if (sourceName.StartsWith("Ga_Eyelash", System.StringComparison.InvariantCultureIgnoreCase))
                        return MaterialType.Eyelash;
                    if (sourceName.ToLowerInvariant().Contains("_base_") || sourceName.ToLowerInvariant().Contains("scalp_"))
                        return MaterialType.Scalp;
                }

                string customShader = matJson?.GetStringValue("Custom Shader/Shader Name");
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
                    default:
                        if (string.IsNullOrEmpty(matJson?.GetStringValue("Textures/Opacity/Texture Path")))
                            return MaterialType.DefaultOpaque;
                        else
                            return MaterialType.DefaultAlpha;
                }
            }
            else
            {
                // if there is no JSON, try to determine the material types from the names.

                if (sourceName.StartsWith("Std_Eye_L", System.StringComparison.InvariantCultureIgnoreCase) ||
                    sourceName.StartsWith("Std_Eye_R", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Eye;

                if (sourceName.StartsWith("Std_Cornea_L", System.StringComparison.InvariantCultureIgnoreCase) ||
                    sourceName.StartsWith("Std_Cornea_R", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Cornea;

                if (sourceName.StartsWith("Std_Eye_Occlusion_", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.EyeOcclusion;

                if (sourceName.StartsWith("Std_Tearline_", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Tearline;

                if (sourceName.StartsWith("Std_Upper_Teeth", System.StringComparison.InvariantCultureIgnoreCase) ||
                    sourceName.StartsWith("Std_Lower_Teeth", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Teeth;

                if (sourceName.StartsWith("Std_Tongue", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Tongue;

                if (sourceName.StartsWith("Std_Skin_Head", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Head;

                if (sourceName.StartsWith("Std_Skin_", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Skin;

                if (sourceName.StartsWith("Std_Nails", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Skin;

                if (sourceName.StartsWith("Std_Eyelash", System.StringComparison.InvariantCultureIgnoreCase))
                    return MaterialType.Eyelash;

                // Detecting the hair is harder to do...

                return MaterialType.DefaultOpaque;
            }
        }        

        private Material CreateRemapMaterial(MaterialType materialType, Material sharedMaterial, string sourceName)
        {
            // get the template material.
            Material templateMaterial = Pipeline.GetTemplateMaterial(materialType, quality, characterInfo);

            // get the appropriate shader to use            
            Shader shader;
            if (templateMaterial && templateMaterial.shader != null)
                shader = templateMaterial.shader;
            else
                shader = Pipeline.GetDefaultShader();

            // check that shader exists.
            if (!shader)
            {
                Debug.LogError("No shader found for material: " + sourceName);
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
                //Debug.Log("Copying from Material template: " + templateMaterial.name);
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
            if (remapPath == fbxPath) Debug.LogError("remapPath: " + remapPath + " is fbxPath (shouldn't happen)!");
            if (remapPath != fbxPath && AssetDatabase.WriteImportSettingsIfDirty(remapPath))
                importAssets.Add(AssetDatabase.GetAssetPath(remapMaterial));

            return remapMaterial;
        }        

        private void ProcessTextures(GameObject obj, string sourceName, Material sharedMat, Material mat, 
            MaterialType materialType, QuickJSON matJson)
        {
            string shaderName = mat.shader.name;            

            if (shaderName.iEndsWith(Pipeline.SHADER_DEFAULT))
            {
                ConnectDefaultMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }
            
            if (shaderName.iEndsWith(Pipeline.SHADER_DEFAULT_HAIR))
            {
                ConnectDefaultHairMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.EndsWith(Pipeline.SHADER_HQ_SKIN) || 
                     shaderName.EndsWith(Pipeline.SHADER_HQ_HEAD))
            {
                ConnectHQSkinMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.EndsWith(Pipeline.SHADER_HQ_TEETH))
            {
                ConnectHQTeethMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.EndsWith(Pipeline.SHADER_HQ_TONGUE))
            {
                ConnectHQTongueMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.EndsWith(Pipeline.SHADER_HQ_EYE) || 
                     shaderName.EndsWith(Pipeline.SHADER_HQ_CORNEA))
            {
                ConnectHQEyeMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.EndsWith(Pipeline.SHADER_HQ_HAIR))
            {
                ConnectHQHairMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.EndsWith(Pipeline.SHADER_HQ_EYE_OCCLUSION))
            {
                ConnectHQEyeOcclusionMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            else if (shaderName.EndsWith(Pipeline.SHADER_HQ_TEARLINE))
            {
                ConnectHQTearlineMaterial(obj, sourceName, sharedMat, mat, materialType, matJson);
            }

            HDShaderUtils.ResetMaterialKeywords(mat);
        }        

        private void ConnectDefaultMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            string customShader = matJson?.GetStringValue("Custom Shader/Shader Name");

            // these default materials should *not* attach any textures as I don't use them for these:
            if (customShader == "RLEyeTearline" || customShader == "RLEyeOcclusion") return;

            // HDRP
            ConnectTextureTo(sourceName, mat, "_BaseColorMap", "Diffuse",
                matJson, "Textures/Base Color",
                FLAG_SRGB);

            ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                matJson, "Textures/HDRP");

            ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_EmissiveColorMap", "Glow",
                matJson, "Textures/Glow");

            // URP/3D
            ConnectTextureTo(sourceName, mat, "_BaseMap", "Diffuse",
                matJson, "Textures/Base Color",
                FLAG_SRGB);

            ConnectTextureTo(sourceName, mat, "_MetallicGlossMap", "MetallicAlpha",
                matJson, "Textures/MetallicAlpha");

            ConnectTextureTo(sourceName, mat, "_OcclusionMap", "ao",
                matJson, "Textures/AO");

            ConnectTextureTo(sourceName, mat, "_BumpMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL);

            // All
            if (matJson != null)
            {
                mat.SetColor("_BaseColor", matJson.GetColorValue("Diffuse Color"));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColor("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength")/100f));
                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloat("_NormalScale", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
            }

            //if (materialType == MaterialType.Scalp)
            //{
            //    mat.SetColor("_BaseColor", matJson.GetColorValue("Diffuse Color").ScaleRGB(0.2f));
            //}

            // connecting default HDRP materials:
            if (Pipeline.GetRenderPipeline() == RenderPipeline.HDRP && !string.IsNullOrEmpty(customShader))
            {
                // for skin and head materials:
                if (customShader == "RLHead" || customShader == "RLSkin")
                {
                    ConnectTextureTo(sourceName, mat, "_SubsurfaceMaskMap", "SSSMap",
                        matJson, "Custom Shader/Image/SSS Map");

                    // use the baked thickness and details maps...
                    mat.SetTexture("_ThicknessMap", GetCachedBakedMap(sharedMat, "_ThicknessMap"));
                    mat.SetTexture("_DetailMap", GetCachedBakedMap(sharedMat, "_DetailMap"));

                    float microNormalTiling = 20f;
                    float microNormalStrength = 0.5f;
                    if (matJson != null)
                    {
                        microNormalTiling = matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Tiling");
                        microNormalStrength = matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Strength");
                    }
                    mat.SetTextureScale("_DetailMap", new Vector2(microNormalTiling, microNormalTiling));
                    mat.SetFloat("_DetailNormalScale", microNormalStrength);
                    mat.SetFloat("_Thickness", 0.4f);
                    mat.SetRemapRange("_ThicknessRemap", 0.4f, 1f);
                }
            }
        }

        private void ConnectDefaultHairMaterial(GameObject obj, string sourceName, Material sharedMat, 
            Material mat, MaterialType materialType, QuickJSON matJson)
        {
            bool isHair = sourceName.iContains("hair");

            ConnectTextureTo(sourceName, mat, "_BaseColorMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB + (isHair ? FLAG_HAIR : FLAG_ALPHA_CLIP));

            ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_MaskMap", "ao",
                matJson, "Textures/AO");            

            if (matJson != null)
            {
                float diffuseStrength = matJson.GetFloatValue("Custom Shader/Variable/Diffuse Strength");
                mat.SetColor("_BaseColor", matJson.GetColorValue("Diffuse Color").ScaleRGB(diffuseStrength));

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

            ConnectTextureTo(sourceName, mat, "_MetallicAlphaMap", "MetallicAlpha",
                matJson, "Textures/MetallicAlpha");

            ConnectTextureTo(sourceName, mat, "_AOMap", "ao",
                matJson, "Textures/AO");

            ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                matJson, "Textures/HDRP");

            ConnectTextureTo(sourceName, mat, "_SSSMap", "SSSMap",
                matJson, "Custom Shader/Image/SSS Map");

            ConnectTextureTo(sourceName, mat, "_ThicknessMap", "TransMap",
                matJson, "Custom Shader/Image/Transmission Map");

            ConnectTextureTo(sourceName, mat, "_MicroNormalMap", "MicroN",
                matJson, "Custom Shader/Image/MicroNormal");

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

                mat.EnableKeyword("BOOLEAN_IS_HEAD_ON");
            }
            else
            {
                ConnectTextureTo(sourceName, mat, "_RGBAMask", "RGBAMask",
                    matJson, "Custom Shader/Image/RGBA Area Mask");
            }

            if (matJson != null)
            {
                mat.SetFloat("_AOStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColor("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloat("_NormalStrength", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
                mat.SetFloat("_MicroNormalTiling", matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Tiling"));
                mat.SetFloat("_MicroNormalStrength", matJson.GetFloatValue("Custom Shader/Variable/MicroNormal Strength"));                
                float specular = matJson.GetFloatValue("Custom Shader/Variable/_Specular");                
                float smoothnessMax = Util.CombineSpecularToSmoothness(specular, 1.0f);

                mat.SetFloat("_SmoothnessMin", 0f);
                mat.SetFloat("_SmoothnessMax", smoothnessMax);
                mat.SetFloat("_SmoothnessPower", 1f);

                mat.SetFloat("_SubsurfaceScale", matJson.GetFloatValue("Custom Shader/Variable/Unmasked Scatter Scale"));
                mat.SetFloat("_ThicknessScale", Mathf.Clamp01(matJson.GetFloatValue("Subsurface Scatter/Radius") / 5f));
                mat.SetFloat("_MicroSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Micro Roughness Scale"));
                mat.SetFloat("_UnmaskedSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Unmasked Roughness Scale"));
                mat.SetFloat("_UnmaskedScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Unmasked Scatter Scale"));

                if (materialType == MaterialType.Head)
                {
                    mat.SetFloat("_ColorBlendStrength", matJson.GetFloatValue("Custom Shader/Variable/BaseColor Blend2 Strength"));
                    mat.SetFloat("_NormalBlendStrength", matJson.GetFloatValue("Custom Shader/Variable/NormalMap Blend Strength"));
                    mat.SetFloat("_MouthCavityAO", matJson.GetFloatValue("Custom Shader/Variable/Inner Mouth Ao"));
                    mat.SetFloat("_NostrilCavityAO", matJson.GetFloatValue("Custom Shader/Variable/Nostril Ao"));
                    mat.SetFloat("_LipsCavityAO", matJson.GetFloatValue("Custom Shader/Variable/Lips Gap Ao"));

                    mat.SetFloat("_RSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Nose Roughness Scale"));
                    mat.SetFloat("_GSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Mouth Roughness Scale"));
                    mat.SetFloat("_BSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/UpperLid Roughness Scale"));
                    mat.SetFloat("_ASmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/InnerLid Roughness Scale"));
                    mat.SetFloat("_EarSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Ear Roughness Scale"));
                    mat.SetFloat("_NeckSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Neck Roughness Scale"));
                    mat.SetFloat("_CheekSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Cheek Roughness Scale"));
                    mat.SetFloat("_ForeheadSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Forehead Roughness Scale"));
                    mat.SetFloat("_UpperLipSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/UpperLip Roughness Scale"));
                    mat.SetFloat("_ChinSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/Chin Roughness Scale"));

                    mat.SetFloat("_RScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Nose Scatter Scale"));
                    mat.SetFloat("_GScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Mouth Scatter Scale"));
                    mat.SetFloat("_BScatterScale", matJson.GetFloatValue("Custom Shader/Variable/UpperLid Scatter Scale"));
                    mat.SetFloat("_AScatterScale", matJson.GetFloatValue("Custom Shader/Variable/InnerLid Scatter Scale"));
                    mat.SetFloat("_EarScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Ear Scatter Scale"));
                    mat.SetFloat("_NeckScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Neck Scatter Scale"));
                    mat.SetFloat("_CheekScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Cheek Scatter Scale"));
                    mat.SetFloat("_ForeheadScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Forehead Scatter Scale"));
                    mat.SetFloat("_UpperLipScatterScale", matJson.GetFloatValue("Custom Shader/Variable/UpperLip Scatter Scale"));
                    mat.SetFloat("_ChinScatterScale", matJson.GetFloatValue("Custom Shader/Variable/Chin Scatter Scale"));
                }
                else
                {
                    mat.SetFloat("_RSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/R Channel Roughness Scale"));
                    mat.SetFloat("_GSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/G Channel Roughness Scale"));
                    mat.SetFloat("_BSmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/B Channel Roughness Scale"));
                    mat.SetFloat("_ASmoothnessMod", -matJson.GetFloatValue("Custom Shader/Variable/A Channel Roughness Scale"));

                    mat.SetFloat("_RScatterScale", matJson.GetFloatValue("Custom Shader/Variable/R Channel Scatter Scale"));
                    mat.SetFloat("_GScatterScale", matJson.GetFloatValue("Custom Shader/Variable/G Channel Scatter Scale"));
                    mat.SetFloat("_BScatterScale", matJson.GetFloatValue("Custom Shader/Variable/B Channel Scatter Scale"));
                    mat.SetFloat("_AScatterScale", matJson.GetFloatValue("Custom Shader/Variable/A Channel Scatter Scale"));
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

            ConnectTextureTo(sourceName, mat, "_MicroNormalMap", "MicroN",
                matJson, "Custom Shader/Image/MicroNormal");

            ConnectTextureTo(sourceName, mat, "_GumsMaskMap", "GumsMask",
                matJson, "Custom Shader/Image/Gums Mask");

            ConnectTextureTo(sourceName, mat, "_GradientAOMap", "GradAO",
                matJson, "Custom Shader/Image/Gradient AO");

            ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                matJson, "Textures/Glow");

            if (matJson != null)
            {
                mat.SetFloat("_IsUpperTeeth", matJson.GetFloatValue("Custom Shader/Variable/Is Upper Teeth"));
                mat.SetFloat("_AOStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColor("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloat("_NormalStrength", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
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
                mat.SetFloat("_SmoothnessMax", 0.88f);
                mat.SetFloat("_SmoothnessPower", 0.5f);
                mat.SetFloat("_TeethSSS", matJson.GetFloatValue("Custom Shader/Variable/Teeth Scatter"));
                mat.SetFloat("_GumsSSS", matJson.GetFloatValue("Custom Shader/Variable/Gums Scatter"));
                mat.SetFloat("_TeethThickness", Mathf.Clamp01(matJson.GetFloatValue("Subsurface Scatter/Radius") / 5f));
                mat.SetFloat("_GumsThickness", Mathf.Clamp01(matJson.GetFloatValue("Subsurface Scatter/Radius") / 5f));
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

            ConnectTextureTo(sourceName, mat, "_MicroNormalMap", "MicroN",
                matJson, "Custom Shader/Image/MicroNormal");

            ConnectTextureTo(sourceName, mat, "_GradientAOMap", "GradAO",
                matJson, "Custom Shader/Image/Gradient AO");

            ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                matJson, "Textures/Glow");

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
                mat.SetFloat("_SmoothnessMax", 0.88f);
                mat.SetFloat("_SmoothnessPower", 0.5f);
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
                FLAG_SRGB);

                ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                    matJson, "Textures/HDRP");

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

            if (matJson != null)
            {
                // both the cornea and the eye materials need the same settings:
                mat.SetFloat("_AOStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColor("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                mat.SetFloat("_ColorBlendStrength", matJson.GetFloatValue("Custom Shader/Variable/BlendMap2 Strength"));
                mat.SetFloat("_ShadowRadius", matJson.GetFloatValue("Custom Shader/Variable/Shadow Radius"));
                mat.SetFloat("_ShadowHardness", matJson.GetFloatValue("Custom Shader/Variable/Shadow Hardness"));
                float specularScale = matJson.GetFloatValue("Custom Shader/Variable/Specular Scale");
                mat.SetColor("_CornerShadowColor", matJson.GetColorValue("Custom Shader/Variable/Eye Corner Darkness Color"));
                mat.SetFloat("_IrisDepth", 0.004f * matJson.GetFloatValue("Custom Shader/Variable/Iris Depth Scale"));
                mat.SetFloat("_IrisSmoothness", 0f); // 1f - matJson.GetFloatValue("Custom Shader/Variable/_Iris Roughness"));
                mat.SetFloat("_IrisBrightness", matJson.GetFloatValue("Custom Shader/Variable/Iris Color Brightness"));
                mat.SetFloat("_PupilScale", 0.8f * matJson.GetFloatValue("Custom Shader/Variable/Pupil Scale"));
                mat.SetFloat("_IOR", matJson.GetFloatValue("Custom Shader/Variable/_IoR"));
                float irisScale = matJson.GetFloatValue("Custom Shader/Variable/Iris UV Radius") / 0.16f;
                mat.SetFloat("_IrisScale", irisScale);
                mat.SetFloat("_IrisRadius", 0.15f * irisScale);                
                mat.SetFloat("_LimbusWidth", matJson.GetFloatValue("Custom Shader/Variable/Limbus UV Width Color"));                
                float limbusDarkScale = matJson.GetFloatValue("Custom Shader/Variable/Limbus Dark Scale");
                float limbusDarkT = Mathf.InverseLerp(0f, 10f, limbusDarkScale);
                mat.SetFloat("_LimbusDarkRadius", Mathf.Lerp(0.145f, 0.075f, limbusDarkT));
                //mat.SetFloat("_LimbusDarkWidth", 0.035f);
                mat.SetFloat("_ScleraBrightness", 1f * matJson.GetFloatValue("Custom Shader/Variable/ScleraBrightness"));
                mat.SetFloat("_ScleraSmoothness", 1f - matJson.GetFloatValue("Custom Shader/Variable/Sclera Roughness"));
                mat.SetFloat("_ScleraScale", matJson.GetFloatValue("Custom Shader/Variable/Sclera UV Radius"));
                mat.SetFloat("_ScleraNormalStrength", 1f - matJson.GetFloatValue("Custom Shader/Variable/Sclera Flatten Normal"));
                mat.SetFloat("_ScleraNormalTiling", Mathf.Clamp(1f / matJson.GetFloatValue("Custom Shader/Variable/Sclera Normal UV Scale"), 0.1f, 10f));
                mat.SetFloat("_IsLeftEye", isLeftEye ? 1f : 0f);
            }
        }

        private void ConnectHQHairMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            bool isHair = sourceName.iContains("Hair");

            ConnectTextureTo(sourceName, mat, "_DiffuseMap", "Diffuse",
                    matJson, "Textures/Base Color",
                    FLAG_SRGB + (isHair ? FLAG_HAIR : FLAG_ALPHA_CLIP));

            ConnectTextureTo(sourceName, mat, "_MaskMap", "HDRP",
                matJson, "Textures/HDRP");

            ConnectTextureTo(sourceName, mat, "_NormalMap", "Normal",
                matJson, "Textures/Normal",
                FLAG_NORMAL);

            ConnectTextureTo(sourceName, mat, "_BlendMap", "blend_multiply",
                matJson, "Textures/Blend");

            ConnectTextureTo(sourceName, mat, "_FlowMap", "Hair Flow Map",
                matJson, "Custom Shader/Image/Hair Flow Map");

            ConnectTextureTo(sourceName, mat, "_IDMap", "Hair ID Map",
                matJson, "Custom Shader/Image/Hair ID Map");

            ConnectTextureTo(sourceName, mat, "_RootMap", "Hair Root Map",
                matJson, "Custom Shader/Image/Hair Root Map");

            ConnectTextureTo(sourceName, mat, "_SpecularMap", "HSpecMap",
                matJson, "Custom Shader/Image/Hair Specular Mask Map");

            ConnectTextureTo(sourceName, mat, "_EmissionMap", "Glow",
                matJson, "Textures/Glow");

            if (isHair)
            {
                mat.SetFloat("_AlphaPower", 1.5f);
                mat.SetFloat("_AlphaRemap", 0.5f);
                mat.SetFloat("_DepthPrepass", 0.95f);
            }
            else
            {
                mat.SetFloat("_AlphaPower", 1.0f);
                mat.SetFloat("_AlphaRemap", 1.0f);
                mat.SetFloat("_DepthPrepass", 0.95f);
            }            

            if (matJson != null)
            {
                mat.SetFloat("_AOStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/AO/Strength") / 100f));
                if (matJson.PathExists("Textures/Glow/Texture Path"))
                    mat.SetColor("_EmissiveColor", Color.white * (matJson.GetFloatValue("Textures/Glow/Strength") / 100f));
                if (matJson.PathExists("Textures/Normal/Strength"))
                    mat.SetFloat("_NormalStrength", matJson.GetFloatValue("Textures/Normal/Strength") / 100f);
                mat.SetFloat("_AOOccludeAll", matJson.GetFloatValue("Custom Shader/Variable/AO Map Occlude All Lighting"));
                mat.SetFloat("_BlendStrength", Mathf.Clamp01(matJson.GetFloatValue("Textures/Blend/Strength") / 100f));
                mat.SetColor("_VertexBaseColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/VertexGrayToColor")));
                // set the transmission colour to 1/4 between vertex base and white.
                // mat.SetColor("_TransmissionColor", Color.Lerp(matJson.GetColorValue("Custom Shader/Variable/VertexGrayToColor"), Color.white, 0.25f));
                mat.SetFloat("_VertexColorStrength", 1f * matJson.GetFloatValue("Custom Shader/Variable/VertexColorStrength"));
                mat.SetFloat("_BaseColorStrength", 1f * matJson.GetFloatValue("Custom Shader/Variable/BaseColorMapStrength"));
                mat.SetFloat("_RimTransmissionIntensity", 0.2f * matJson.GetFloatValue("Custom Shader/Variable/Transmission Strength"));

                float specMapStrength = matJson.GetFloatValue("Custom Shader/Variable/Hair Specular Map Strength");
                mat.SetFloat("_DiffuseStrength", 1f * matJson.GetFloatValue("Custom Shader/Variable/Diffuse Strength"));
                mat.SetFloat("_SmoothnessMax", 1f - matJson.GetFloatValue("Custom Shader/Variable/Hair Roughness Map Strength"));
                // Unity does not have a specular-f0 channel so the specular map is being used to mask the 
                // specular multipliers in the hair shader instead.
                mat.SetFloat("_SpecularMultiplier", specMapStrength * matJson.GetFloatValue("Custom Shader/Variable/Specular Strength"));
                mat.SetFloat("_SecondarySpecularMultiplier", specMapStrength * 0.5f * matJson.GetFloatValue("Custom Shader/Variable/Secondary Specular Strength"));

                mat.SetColor("_RootColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/RootColor")));
                mat.SetColor("_EndColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/TipColor")));
                mat.SetFloat("_GlobalStrength", matJson.GetFloatValue("Custom Shader/Variable/UseRootTipColor"));
                mat.SetFloat("_RootColorStrength", matJson.GetFloatValue("Custom Shader/Variable/RootColorStrength"));
                mat.SetFloat("_EndColorStrength", matJson.GetFloatValue("Custom Shader/Variable/TipColorStrength"));
                mat.SetFloat("_InvertRootMap", matJson.GetFloatValue("Custom Shader/Variable/InvertRootTip"));
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

                mat.SetColor("_HighlightAColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/_1st Dye Color")));
                mat.SetFloat("_HighlightAStrength", matJson.GetFloatValue("Custom Shader/Variable/_1st Dye Strength"));
                mat.SetVector("_HighlightADistribution", (1f / 255f) * matJson.GetVector3Value("Custom Shader/Variable/_1st Dye Distribution from Grayscale"));
                mat.SetFloat("_HighlightAOverlapEnd", matJson.GetFloatValue("Custom Shader/Variable/Mask 1st Dye by RootMap"));
                mat.SetFloat("_HighlightAOverlapInvert", matJson.GetFloatValue("Custom Shader/Variable/Invert 1st Dye RootMap Mask"));

                mat.SetColor("_HighlightBColor", Util.LinearTosRGB(matJson.GetColorValue("Custom Shader/Variable/_2nd Dye Color")));
                mat.SetFloat("_HighlightBStrength", matJson.GetFloatValue("Custom Shader/Variable/_2nd Dye Strength"));
                mat.SetVector("_HighlightBDistribution", (1f / 255f) * matJson.GetVector3Value("Custom Shader/Variable/_2nd Dye Distribution from Grayscale"));
                mat.SetFloat("_HighlightBOverlapEnd", matJson.GetFloatValue("Custom Shader/Variable/Mask 2nd Dye by RootMap"));
                mat.SetFloat("_HighlightBOverlapInvert", matJson.GetFloatValue("Custom Shader/Variable/Invert 2nd Dye RootMap Mask"));
            }
        }

        private void ConnectHQEyeOcclusionMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            if (matJson != null)
            {
                mat.SetFloat("_ExpandOut", 0.0001f * matJson.GetFloatValue("Custom Shader/Variable/Depth Offset"));
                mat.SetFloat("_ExpandUpper", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Top Offset"));
                mat.SetFloat("_ExpandLower", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Bottom Offset"));
                mat.SetFloat("_ExpandInner", 0.0001f + 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Inner Corner Offset"));
                mat.SetFloat("_ExpandOuter", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Outer Corner Offset"));
                mat.SetFloat("_TearDuctPosition", matJson.GetFloatValue("Custom Shader/Variable/Tear Duct Position"));
                //mat.SetFloat("_TearDuctWidth", 0.05f);
                mat.SetColor("_OcclusionColor", Color.Lerp(matJson.GetColorValue("Custom Shader/Variable/Shadow Color"), Color.black, 0.2f));

                float os1 = matJson.GetFloatValue("Custom Shader/Variable/Shadow Strength");
                float os2 = matJson.GetFloatValue("Custom Shader/Variable/Shadow2 Strength");

                mat.SetFloat("_OcclusionStrength", Mathf.Pow(os1, 1f / 3f));
                mat.SetFloat("_OcclusionStrength2", Mathf.Pow(os2, 1f / 3f));
                mat.SetFloat("_OcclusionPower", 1.75f);
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
                mat.SetFloat("_TopMin", scale * top);
                mat.SetFloat("_TopMax", topMax);
                mat.SetFloat("_TopCurve", matJson.GetFloatValue("Custom Shader/Variable/Shadow Top Arc"));
                mat.SetFloat("_BottomMin", scale * bottom);
                mat.SetFloat("_BottomMax", bottomMax);
                mat.SetFloat("_BottomCurve", matJson.GetFloatValue("Custom Shader/Variable/Shadow Bottom Arc"));
                mat.SetFloat("_InnerMin", inner);
                mat.SetFloat("_InnerMax", innerMax);
                mat.SetFloat("_OuterMin", scale * outer);
                mat.SetFloat("_OuterMax", outerMax);
                mat.SetFloat("_Top2Min", scale * top2);
                mat.SetFloat("_Top2Max", top2Max);
            }
        }

        private void ConnectHQTearlineMaterial(GameObject obj, string sourceName, Material sharedMat, Material mat,
            MaterialType materialType, QuickJSON matJson)
        {
            if (matJson != null)
            {
                mat.SetFloat("_DepthOffset", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Depth Offset"));                
                mat.SetFloat("_InnerOffset", 0.005f * matJson.GetFloatValue("Custom Shader/Variable/Depth Offset"));                
                mat.SetFloat("_Smoothness", 1f - matJson.GetFloatValue("Custom Shader/Variable/Roughness"));                
            }
        }

        private void CacheBakedMaps()
        {
            bakedDetailMaps = new Dictionary<Material, Texture2D>();
            bakedThicknessMaps = new Dictionary<Material, Texture2D>();

            if (jsonMeshData == null)
            {
                Debug.LogError("Unable to bake default maps without valid Json data!");
            }

            int childCount = fbx.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject obj = fbx.transform.GetChild(i).gameObject;
                Renderer renderer = obj.GetComponent<Renderer>();

                if (jsonMeshData != null && renderer)
                {                    
                    foreach (Material sharedMat in renderer.sharedMaterials)
                    {
                        string sourceName = Util.GetSourceMaterialName(fbxPath, sharedMat);
                        QuickJSON matJson = jsonMeshData.GetObjectAtPath(obj.name + "/Materials/" + sourceName);
                        MaterialType materialType = GetMaterialType(obj, sharedMat, sourceName, matJson);
                        if (materialType == MaterialType.Skin || materialType == MaterialType.Head)
                        {
                            BakeDefaultMap(sharedMat, sourceName, "_ThicknessMap", "TransMap",
                                matJson, "Custom Shader/Image/Transmission Map");

                            BakeDefaultMap(sharedMat, sourceName, "_DetailMap", "MicroN",
                                matJson, "Custom Shader/Image/MicroNormal");
                        }
                    }
                }
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

        private void BakeDefaultMap(Material sharedMat, string sourceName, string shaderRef, string suffix, QuickJSON jsonData, string jsonPath)
        {
            ComputeBake baker = new ComputeBake(fbx, characterInfo);

            string texturePath = null;
            if (jsonData != null)
            {
                texturePath = jsonData.GetStringValue(jsonPath + "/Texture Path");
            }

            Texture2D tex = GetTextureFrom(texturePath, sourceName, suffix, out string name);
            Texture2D bakedTex = null;

            if (tex)
            {
                switch (shaderRef)
                {
                    case "_ThicknessMap":
                        // make sure to set the correct import settings for 
                        // these textures before using them for baking...
                        SetTextureImport(tex, name, FLAG_FOR_BAKE);
                        bakedTex = baker.BakeDefaultSkinThicknessMap(tex, name);
                        bakedThicknessMaps.Add(sharedMat, bakedTex);
                        break;

                    case "_DetailMap":
                        SetTextureImport(tex, name, FLAG_FOR_BAKE);
                        bakedTex = baker.BakeDefaultDetailMap(tex, name);
                        bakedDetailMaps.Add(sharedMat, bakedTex);
                        break;
                }
            }
        }

        private Texture2D GetTextureFrom(string texturePath, string materialName, string suffix, out string name)
        {
            Texture2D tex = null;
            name = "";

            // try to find the texture from the supplied texture path (usually from the json data).
            if (!string.IsNullOrEmpty(texturePath))
            {
                name = Path.GetFileNameWithoutExtension(texturePath);
                tex = Util.FindTexture(textureFolders.ToArray(), name);
            }

            // then try to find the texture from the material name and suffix.
            if (!tex)
            {
                name = materialName + "_" + suffix;
                tex = Util.FindTexture(textureFolders.ToArray(), name);
            }

            return tex;
        }

        private void SetTextureImport(Texture2D tex, string name, int flags = 0)
        {
            // now fix the import settings for the texture.
            string path = AssetDatabase.GetAssetPath(tex);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = 4096;

            // apply the sRGB and alpha settings for re-import.
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            if ((flags & FLAG_SRGB) > 0)
            {
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;                
                importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
                importer.mipMapBias = Importer.MIPMAP_BIAS;
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

            // add the texure path to the re-import paths.
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
                if (!importAssets.Contains(path)) importAssets.Add(path);
        }        

        private bool ConnectTextureTo(string materialName, Material mat, string shaderRef, string suffix, QuickJSON jsonData, string jsonPath, int flags = 0)
        {
            Texture2D tex = null;

            if (mat.HasProperty(shaderRef))
            {
                Vector2 offset = Vector2.zero;
                Vector2 tiling = Vector2.one;
                string texturePath = null;

                if (jsonData != null)
                {                    
                    if (jsonData.PathExists(jsonPath + "/Texture Path"))
                        texturePath = jsonData.GetStringValue(jsonPath + "/Texture Path");
                    if (jsonData.PathExists(jsonPath + "/Offset"))
                        offset = jsonData.GetVector2Value(jsonPath + "/Offset");
                    if (jsonData.PathExists(jsonPath + "/Tiling"))
                        tiling = jsonData.GetVector2Value(jsonPath + "/Tiling");
                }

                tex = GetTextureFrom(texturePath, materialName, suffix, out string name);

                if (tex)
                {
                    // set the texture ref in the material.
                    mat.SetTexture(shaderRef, tex);
                    mat.SetTextureOffset(shaderRef, offset);
                    mat.SetTextureScale(shaderRef, tiling);

                    Util.LogInfo("        Connected texture: " + tex.name);

                    SetTextureImport(tex, name, flags);
                }
            }

            return tex != null;
        }
    }
}
