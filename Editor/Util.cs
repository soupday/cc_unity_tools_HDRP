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

using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Linq;
using System.Data.Sql;

namespace Reallusion.Import
{
    public static class Util
    {
        public static int log_level = -1;
        public static int LOG_LEVEL
        {
            get
            {
                if (log_level == -1)
                {
                    if (EditorPrefs.HasKey("RL_Log_Level"))
                    {
                        log_level = EditorPrefs.GetInt("RL_Log_Level");
                    }
                    else
                    {
                        log_level = 0;
                        EditorPrefs.SetInt("RL_Log_Level", log_level);
                    }
                }
                return log_level;
            }

            set
            {
                log_level = value;
                EditorPrefs.SetInt("RL_Log_Level", value);
            }
        }

        public static bool IsCC3Character(Object obj)
        {
            if (!obj) return false;
            string assetPath = AssetDatabase.GetAssetPath(obj).ToLower();
            if (string.IsNullOrEmpty(assetPath)) return false;
            return IsCC3Character(AssetDatabase.AssetPathToGUID(assetPath));
        }

        public static bool IsCC3Character(string guid)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            return IsCC3CharacterAtPath(assetPath);
        }

        public static bool IsCC3CharacterAtPath(string assetPath)
        {            
            if (assetPath.iEndsWith(".fbx"))
            {
                string assetFolder = Path.GetDirectoryName(assetPath);
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                if (HasJSONAsset(assetFolder, assetName))
                {
                    if (AssetDatabase.IsValidFolder(Path.Combine(assetFolder, "textures", assetName)))
                    {
                        return true;
                    }
                }
            }
            else if (assetPath.iEndsWith(".blend"))
            {
                string assetFolder = Path.GetDirectoryName(assetPath);
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                if (HasJSONAsset(assetFolder, assetName))
                {
                    return true;
                }
            }

            return false;
        }        

        public static bool IsSavedPrefabInSelection()
        {
            if (Selection.gameObjects.Length > 1)
            {
                foreach (GameObject sel in Selection.gameObjects)
                {
                    GameObject instanceRoot = GetScenePrefabInstanceRoot(sel);
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
                    if (prefabSource)
                    {
                        if (AssetDatabase.GetAssetPath(prefabSource).iEndsWith(".prefab")) return true;
                    }
                }
            }
            else if (Selection.gameObjects.Length == 1)
            {
                GameObject instanceRoot = GetScenePrefabInstanceRoot(Selection.gameObjects[0]);
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);                
                if (prefabSource)
                {
                    if (AssetDatabase.GetAssetPath(prefabSource).iEndsWith(".prefab")) return true;
                }                
            }

            return false;
        }
        
        public static Color LinearTosRGBOld(Color c)
        {
            if (c.r < 0f) c.r = 0f;
            if (c.g < 0f) c.g = 0f;
            if (c.b < 0f) c.b = 0f;
            c.r = Mathf.Max(1.055f * Mathf.Pow(c.r, 0.416666667f) - 0.055f, 0f);
            c.g = Mathf.Max(1.055f * Mathf.Pow(c.g, 0.416666667f) - 0.055f, 0f);
            c.b = Mathf.Max(1.055f * Mathf.Pow(c.b, 0.416666667f) - 0.055f, 0f);
            return c;
        }

        public static float LinearTosRGB(float c)
        {
            float lo = c * 12.92f;
            float hi = (Mathf.Pow(Mathf.Max(Mathf.Abs(c), 1.192092896e-07f), (1.0f / 2.4f)) * 1.055f) - 0.055f;
            return c <= 0.0031308f ? lo : hi;
        }

        public static Color LinearTosRGB(Color c)
        {
            return new Color(LinearTosRGB(c.r), LinearTosRGB(c.g), LinearTosRGB(c.b), c.a);
        }

        public static Color sRGBToLinear(Color c)
        {            
            Vector3 linearRGBLo;
            linearRGBLo.x = c.r / 12.92f;
            linearRGBLo.y = c.g / 12.92f;
            linearRGBLo.z = c.b / 12.92f;

            Vector3 linearRGBHi;
            linearRGBHi.x = Mathf.Pow(Mathf.Max(Mathf.Abs((c.r + 0.055f) / 1.055f), 1.192092896e-07f), 2.4f);
            linearRGBHi.y = Mathf.Pow(Mathf.Max(Mathf.Abs((c.g + 0.055f) / 1.055f), 1.192092896e-07f), 2.4f);
            linearRGBHi.z = Mathf.Pow(Mathf.Max(Mathf.Abs((c.b + 0.055f) / 1.055f), 1.192092896e-07f), 2.4f);

            c.r = (c.r <= 0.04045f) ? linearRGBLo.x : linearRGBHi.x;
            c.g = (c.g <= 0.04045f) ? linearRGBLo.y : linearRGBHi.y;
            c.b = (c.b <= 0.04045f) ? linearRGBLo.z : linearRGBHi.z;

            return c;
        }

        public static Color ScaleRGB(this Color c, float scale)
        {
            float a = c.a;
            c *= scale;
            c.a = a;
            return c;
        }
        
        public static bool HasJSONAsset(string folder, string name)
        {
            string jsonPath = Path.Combine(folder, name + ".json");
            return File.Exists(jsonPath);
        }

        public static QuickJSON GetJsonData(string jsonPath)
        {            
            if (File.Exists(jsonPath))
            {
                TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
                QuickJSON jsonData = new QuickJSON(jsonAsset.text);
                return jsonData;
            }
            
            return null;
        }

        public static string GetJsonGenerationString(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                StreamReader jsonFile = File.OpenText(jsonPath);
                string line;
                int count = 0;
                while ((line = jsonFile.ReadLine()) != null)
                {
                    if (line.Contains("\"Generation\":"))
                    {
                        int colon = line.IndexOf(':');
                        if (colon >= 0)
                        {
                            int q1 = line.IndexOf('"', colon + 1);
                            int q2 = line.IndexOf('"', q1 + 1);
                            if (q1 >= 0 && q2 >= 0)
                            {
                                string generation = line.Substring(q1 + 1, q2 - q1 - 1);                                
                                return generation;
                            }
                        }
                        break;
                    }
                    if (count++ > 25) break;
                }
            }

            return "";
        }

        public struct CharacterSort
        {
            public string guid;
            public string name;
        }

        public static List<string> GetValidCharacterGUIDS()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new string[] { "Assets" });            
            List<CharacterSort> results = new List<CharacterSort>();

            foreach (string g in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(g);
                if (IsCC3CharacterAtPath(assetPath))
                {
                    string name = Path.GetFileNameWithoutExtension(assetPath);
                    results.Add(new CharacterSort() { guid = g, name = name });
                }
            }
            
            List<string> sortedGuids = new List<string>(results.Count);
            foreach (CharacterSort cs in results.OrderBy(o => o.name)) sortedGuids.Add(cs.guid);

            return sortedGuids;
        }

        public static void ImportPaths(List<string> paths)
        {
            List<string> done = new List<string>();
            foreach (string path in paths)
            {
                if (!string.IsNullOrEmpty(path) && !done.Contains(path))
                {
                    AssetDatabase.WriteImportSettingsIfDirty(path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    done.Add(path);
                }
            }
        }

        public static string CreateFolder(string path, string name)
        {
            string folderPath = Path.Combine(path, name);

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return folderPath;
            }

            string guid = AssetDatabase.CreateFolder(path, name);
            return AssetDatabase.GUIDToAssetPath(guid);
        }

        public static bool EnsureAssetsFolderExists(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return true;
            if (AssetDatabase.IsValidFolder(folder)) return true;
            if (folder.Equals("Assets", System.StringComparison.InvariantCultureIgnoreCase)) return true;

            string parentFolder = Path.GetDirectoryName(folder);
            string folderName = Path.GetFileName(folder);

            if (EnsureAssetsFolderExists(parentFolder))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
                return true;
            }

            return false;
        }

        public static string GetRelativePath(string fullPath)
        {
            fullPath = Path.GetFullPath(fullPath);
            string basePath = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));

            string[] fullSplit;
            string[] baseSplit;

            // check OS and split the path accordingly
            // There is also Path.DirectorySeparatorChar
            // But this will remind me to treat the absolute path roots differently (If I ever need to)
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                fullSplit = fullPath.Split('\\');
                baseSplit = basePath.Split('\\');
            }
            else if (Application.platform == RuntimePlatform.OSXEditor || 
                     Application.platform == RuntimePlatform.LinuxEditor)
            {
                fullSplit = fullPath.Split('/');
                baseSplit = basePath.Split('/');
            }
            else
            {
                Debug.LogError("Unsupported Platform: " + Application.platform);
                return fullPath;
            }

            int sharedRootIndex = -1;

            for (int i = 0; i < baseSplit.Length; i++)
            {
                if (fullSplit[i] == baseSplit[i])
                    sharedRootIndex = i;
                else
                    break;
            }

            if (sharedRootIndex == -1) return fullPath;

            string relativePath = "";

            for (int i = sharedRootIndex + 1; i < fullSplit.Length - 1; i++)
            {
                relativePath += fullSplit[i] + Path.DirectorySeparatorChar;
            }
            relativePath += fullSplit[fullSplit.Length - 1];

            return relativePath;
        }

        public static string GetAssetFolder(params Object[] assets)
        {
            foreach (Object o in assets)            
            {
                if (o)
                {
                    string assetPath = AssetDatabase.GetAssetPath(o);
                    return Path.GetDirectoryName(assetPath);
                }
            }

            return null;
        }

        public static string GetCharacterFolder(string fbxPath, out string characterName)
        {            
            characterName = Path.GetFileNameWithoutExtension(fbxPath);
            return Path.GetDirectoryName(fbxPath);            
        }

        public static string GetCharacterFolder(GameObject prefabAsset, out string characterName)
        {
            if (!prefabAsset)
            {
                characterName = "";
                return "";
            }
            GameObject fbxAsset = FindRootPrefabAssetFromSceneObject(prefabAsset);            
            string fbxPath = AssetDatabase.GetAssetPath(fbxAsset);
            characterName = Path.GetFileNameWithoutExtension(fbxPath);
            return Path.GetDirectoryName(fbxPath);
        }

        public static ComputeShader FindComputeShader(string name)
        {
            string[] shaderGuids = AssetDatabase.FindAssets("t:computeshader");

            foreach (string guid in shaderGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ComputeShader shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
                if (shader != null)
                {
                    if (shader.name.iEquals(name))
                    {
                        return shader;
                    }
                }
            }

            return null;
        }

        public static Material FindMaterial(string name, string[] folders = null)
        {
            if (folders == null) folders = new string[] { "Assets", "Packages" };

            string[] guids = AssetDatabase.FindAssets(name + " t:material", folders);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat)
                {
                    if (mat.name.iEquals(name))
                    {
                        return mat;
                    }
                }
            }

            return null;
        }

        public static Material FindCustomMaterial(string name, bool useTessellation, string[] folders = null)
        {
            Material template = null;
            Material foundTemplate = null;
            bool foundHDRPorURP12 = false;

            if (Pipeline.isHDRP12 || Pipeline.isURP12)
            {
                string templateName = name + "12";
                foundTemplate = FindMaterial(templateName, folders);
                if (foundTemplate)
                {
                    name = templateName;
                    template = foundTemplate;
                    foundHDRPorURP12 = true;
                }
            }

            if (Importer.USE_AMPLIFY_SHADER)
            {
                // There are cases where there is an URP12_Amplify shader but no corresponding URP12 base shader
                if (Pipeline.isURP12 && !foundHDRPorURP12)
                {
                    string templateName = name + "12_Amplify";
                    foundTemplate = FindMaterial(templateName, folders);
                    if (foundTemplate)
                    {
                        name = templateName;
                        template = foundTemplate;
                        foundHDRPorURP12 = true;
                    }
                }

                if (!foundTemplate)
                {
                    string templateName = name + "_Amplify";
                    foundTemplate = FindMaterial(templateName, folders);
                    if (foundTemplate)
                    {
                        name = templateName;
                        template = foundTemplate;
                    }
                }
            }

            if (useTessellation)
            {
                foundTemplate = null;

                // There are cases where there is an HDRP12_T shader but no corresponding HDRP12 base shader
                if (Pipeline.isHDRP12 && !foundHDRPorURP12)
                {
                    string templateName = name + "12_T";
                    foundTemplate = FindMaterial(templateName, folders);
                    if (foundTemplate)
                    {
                        name = templateName;
                        template = foundTemplate;
                        foundHDRPorURP12 = true;
                    }
                }
                
                if (!foundTemplate)
                {
                    string templateName = name + "_T";
                    foundTemplate = FindMaterial(templateName, folders);
                    if (foundTemplate)
                    {
                        name = templateName;
                        template = foundTemplate;
                    }
                }
            }

            if (template) return template;

            return FindMaterial(name, folders);
        }

        public static Texture2D FindTexture(string[] folders, string search)
        {
            string[] texGuids;

            texGuids = AssetDatabase.FindAssets(search + " t:texture2d", folders);

            foreach (string guid in texGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string texName = Path.GetFileNameWithoutExtension(assetPath);
                if (texName.iEquals(search))
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }
            }

            return null;
        }

        public static AnimationClip FindAnimation(string[] folders, string search, bool exactMatch = true, bool matchStart = true)
        {
            string[] guids;

            guids = AssetDatabase.FindAssets(search + " t:animation", folders);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string animName = Path.GetFileNameWithoutExtension(assetPath);
                if (string.IsNullOrEmpty(search))
                {
                    return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                }
                else if (exactMatch)
                {
                    if (animName.iEquals(search)) return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                }
                else
                {
                    if (matchStart)
                    {
                        if (animName.iStartsWith(search)) return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                    }
                    else
                    {
                        if (animName.iContains(search)) return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                    }
                }
            }

            return null;
        }

        public static Object FindAsset(string search, string[] folders = null)
        {
            if (folders == null) folders = new string[] { "Assets", "Packages" };

            string[] guids = AssetDatabase.FindAssets(search, folders);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                if (assetName.iEquals(search))
                {
                    return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                }
            }

            return null;
        }

        public static string CombineJsonTexPath(string fbxPath, string jsonTexPath)
        {
            // remove any ./ .\ prefix from the start of the json texture path
            if (jsonTexPath.iStartsWith("./") || jsonTexPath.iStartsWith(".\\"))
                jsonTexPath = jsonTexPath.Substring(2);            

            // convert slashes/backslashes to OS dependant separator
            if (Path.DirectorySeparatorChar != '\\') jsonTexPath = jsonTexPath.Replace('\\', Path.DirectorySeparatorChar);
            if (Path.DirectorySeparatorChar != '/') jsonTexPath = jsonTexPath.Replace('/', Path.DirectorySeparatorChar);

            return Path.Combine(fbxPath, jsonTexPath);
        }

        public static GameObject FindPreviewScenePrefab()
        {
            string[] texGuids;

            texGuids = AssetDatabase.FindAssets("t:prefab RL_PreviewScenePrefab");

            foreach (string guid in texGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(assetPath);
                if (name.Equals("RL_PreviewScenePrefab", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                }
            }

            return null;
        }

        public static string GetSourceMaterialName(string fbxPath, Material sharedMaterial)
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> importerRemaps = importer.GetExternalObjectMap();

            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> pair in importerRemaps)
            {
                if (pair.Value == sharedMaterial) return pair.Key.name;
            }

            string sourceName = sharedMaterial.name;

            if (sourceName.iContains("_1st_Pass"))
                sourceName = sourceName.Substring(0, sourceName.IndexOf("_1st_Pass", System.StringComparison.InvariantCultureIgnoreCase));

            if (sourceName.iContains("_2nd_Pass"))
                sourceName = sourceName.Substring(0, sourceName.IndexOf("_2nd_Pass", System.StringComparison.InvariantCultureIgnoreCase));

            return sourceName;
        }

        // example functions
        // from unity docs
        public static void ExtractFromAsset(Object subAsset, string destinationPath)
        {
            string assetPath = AssetDatabase.GetAssetPath(subAsset);

            var clone = Object.Instantiate(subAsset);
            AssetDatabase.CreateAsset(clone, destinationPath);

            var assetImporter = AssetImporter.GetAtPath(assetPath);
            assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(subAsset), clone);

            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        public static float CombineSpecularToSmoothness(float specular, float smoothness)
        {
            // as there is no specular mask channel, I am simulating the specular mask by clamping the smoothness
            // between 0 and a root curve function of the specular value: i.e. specularSmoothness = smoothness * pow(specular, P)
            // this power function must range from f(0) = 0 to f(1) = 1 and achieve 0.897 maximum smoothness at 0.5 specular
            // (0.5 specular being the default specular value for base max smoothness, visually detected as ~0.88 smoothness)
            // specular values from 0.5 to 1.0 will generate a max smoothness of 0.897 to 1.0.
            // Thus: P = ln(0.897) / ln(0.5) = 0.184424571f
            // This should approximate the specular mask for specular values > 0.2
            const float smoothnessStdMax = Importer.MAX_SMOOTHNESS;
            const float specularMid = 0.5f;
            float P = Mathf.Log(smoothnessStdMax) / Mathf.Log(specularMid);
            return smoothness * Mathf.Clamp(Mathf.Pow(specular, P), 0f, smoothnessStdMax);
        }        

        public static string GetShaderName(Material mat)
        {
            if (mat && mat.shader)
            {
                string[] split = mat.shader.name.Split('/');
                return split[split.Length - 1];
            }

            return "None";
        }                

        public static string[] GetLinkedMaterialNames(string name)
        {
            name = name.ToLowerInvariant();

            if (name.Contains("std_eye_occlusion_")) return new[] { "std_eye_occlusion_l", "std_eye_occlusion_r" };
            if (name.Contains("std_skin_head") || name.Contains("std_skin_body") ||
                name.Contains("std_skin_arm") || name.Contains("std_skin_leg"))
                return new[] { "std_skin_head", "std_skin_body", "std_skin_arm", "std_skin_leg" };
            return null;
        }

        public static int GetLinkedMaterialIndex(string sourceName, string shaderName)
        {
            // identify linked materials by shader name:
            if ((shaderName.iContains(Pipeline.SHADER_HQ_HEAD) || 
                 shaderName.iContains(Pipeline.SHADER_HQ_SKIN)) && 
                !sourceName.iContains("_Nails")) return CharacterTreeView.LINKED_INDEX_SKIN;

            if (shaderName.iContains(Pipeline.SHADER_HQ_EYE) ||
                shaderName.iContains(Pipeline.SHADER_HQ_EYE_PARALLAX)) return CharacterTreeView.LINKED_INDEX_EYE;

            if (shaderName.iContains(Pipeline.SHADER_HQ_CORNEA) ||
                shaderName.iContains(Pipeline.SHADER_HQ_CORNEA_PARALLAX) ||
                shaderName.iContains(Pipeline.SHADER_HQ_CORNEA_REFRACTIVE) ||
                shaderName.iContains(Pipeline.SHADER_HQ_EYE_REFRACTIVE)) return CharacterTreeView.LINKED_INDEX_CORNEA;

            if (shaderName.iContains(Pipeline.SHADER_HQ_EYE_OCCLUSION)) return CharacterTreeView.LINKED_INDEX_EYE_OCCLUSION;
            if (shaderName.iContains(Pipeline.SHADER_HQ_TEARLINE)) return CharacterTreeView.LINKED_INDEX_TEARLINE;
            if (shaderName.iContains(Pipeline.SHADER_HQ_TEETH)) return CharacterTreeView.LINKED_INDEX_TEETH;
            if (shaderName.iContains(Pipeline.SHADER_HQ_HAIR) ||
                shaderName.iContains(Pipeline.SHADER_HQ_HAIR_COVERAGE)) return CharacterTreeView.LINKED_INDEX_HAIR;

            // then try by source material name:
            if (sourceName.iContains("_Skin_Head") || sourceName.iContains("_Skin_Body") ||
                sourceName.iContains("_Skin_Arm") || sourceName.iContains("_Skin_Leg"))
                return CharacterTreeView.LINKED_INDEX_SKIN;            
            if (sourceName.iContains("_Eye_Occlusion_")) return CharacterTreeView.LINKED_INDEX_EYE_OCCLUSION;
            if (sourceName.iContains("_Tearline_")) return CharacterTreeView.LINKED_INDEX_TEARLINE;
            if (sourceName.iContains("_Eye_") || sourceName.iContains("_Cornea_")) return CharacterTreeView.LINKED_INDEX_CORNEA;            
            if (sourceName.iContains("_Upper_Teeth") || sourceName.iContains("_Lower_Teeth")) return CharacterTreeView.LINKED_INDEX_TEETH;

            return -1;
        }

        public static Material[] GetLinkedMaterials(Material mat)
        {
            string[] names = GetLinkedMaterialNames(mat.name);
            string assetPath = AssetDatabase.GetAssetPath(mat);
            string[] assetFolders = new[] { Path.GetDirectoryName(assetPath) };
            List<Material> results = new List<Material>();

            foreach (string name in names)
            {
                string[] searchGuids = AssetDatabase.FindAssets(name + " t:material", assetFolders);
                foreach (string guid in searchGuids)
                {
                    string searchPath = AssetDatabase.GUIDToAssetPath(guid);
                    string searchName = Path.GetFileNameWithoutExtension(searchPath).ToLowerInvariant();
                    if (searchName.Contains(name))
                    {
                        results.Add(AssetDatabase.LoadAssetAtPath<Material>(searchPath));
                    }
                }
            }

            return results.ToArray();
        }

        public static void UpdateLinkedMaterial(Material mat, Material[] linkedMaterials, string shaderRef, float value, bool linked)
        {
            if (linked)
            {
                foreach (Material m in linkedMaterials)
                    if (m.HasProperty(shaderRef))
                        m.SetFloat(shaderRef, value);
            }
            else
                mat.SetFloat(shaderRef, value);
        }

        public static void UpdateLinkedMaterial(Material mat, Material[] linkedMaterials, string shaderRef, Color value, bool linked)
        {
            if (linked)
            {
                foreach (Material m in linkedMaterials)
                    if (m.HasProperty(shaderRef))
                        m.SetColor(shaderRef, value);
            }
            else
                mat.SetColor(shaderRef, value);
        }

        public static bool GetMultiPassMaterials(Material target, out Material firstPass, out Material secondPass)
        {
            firstPass = null;
            secondPass = null;

            string path = AssetDatabase.GetAssetPath(target);
            string folder = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string pass1Path = Path.Combine(folder, name + "_1st_Pass.mat");
            string pass2Path = Path.Combine(folder, name + "_2nd_Pass.mat");
            if (File.Exists(pass1Path)) firstPass = AssetDatabase.LoadAssetAtPath<Material>(pass1Path);
            if (File.Exists(pass2Path)) secondPass = AssetDatabase.LoadAssetAtPath<Material>(pass2Path);

            if (firstPass && secondPass) return true;
            return false;
        }

        public static void DestroyEditorChildObjects(GameObject obj)
        {
            GameObject[] children = new GameObject[obj.transform.childCount];

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                children[i] = obj.transform.GetChild(i).gameObject;
            }

            foreach (GameObject child in children)
            {
                GameObject.DestroyImmediate(child);
            }
        }        
        
        public static AnimationClip GetFirstAnimationClipFromCharacter(GameObject sourceFbx)
        {
            AnimationClip found = null;

            if (sourceFbx)
            {                
                Object[] data = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(sourceFbx));
                foreach (Object subObject in data)
                {
                    if (subObject.GetType().Equals(typeof(AnimationClip)))
                    {
                        found = (AnimationClip)subObject;

                        // try to return the first non T-Pose.
                        if (!found.name.iContains("T-Pose")) return found;
                    }
                }

                // if that didn't work, then the prefab is a non-variant derivative (probably LODGroup)                
                string name = sourceFbx.name;
                string path = AssetDatabase.GetAssetPath(sourceFbx);
                string prefabFolder = Path.GetDirectoryName(path);
                string prefabAnimationFolder = Path.Combine(prefabFolder, "Animations");
                string parentFolder = Path.GetDirectoryName(prefabFolder);
                string parentAnimationFolder = Path.Combine(parentFolder, "Animations");
                List<string> folders = new List<string>();
                if (AssetDatabase.IsValidFolder(parentAnimationFolder)) folders.Add(parentAnimationFolder);
                if (AssetDatabase.IsValidFolder(prefabAnimationFolder)) folders.Add(prefabAnimationFolder);
                if (AssetDatabase.IsValidFolder(parentFolder)) folders.Add(parentFolder);
                folders.Add(prefabFolder);
                string[] f = folders.ToArray();

                // first look for an animation that matches the prefab name
                found = FindAnimation(f, name, false, true);

                // then look for an animation that matches the base name of the character (before any _LodN)
                if (!found)
                {
                    int index = name.IndexOf("_Lod", System.StringComparison.InvariantCultureIgnoreCase);
                    if (index > 0)
                    {
                        name = name.Substring(0, index);
                        found = FindAnimation(f, name, false, true);
                    }
                }

                // then just try and find the first available animation
                if (!found)
                {
                    found = FindAnimation(f, "");
                }
            }

            return found;
        }

        public static AnimationClip[] GetAllAnimationClipsFromCharacter(string sourceFbxPath)
        {
            List<AnimationClip> clips = new List<AnimationClip>();

            if (!string.IsNullOrEmpty(sourceFbxPath))
            {
                Object[] data = AssetDatabase.LoadAllAssetRepresentationsAtPath(sourceFbxPath);
                foreach (Object subObject in data)
                {
                    if (subObject.GetType().Equals(typeof(AnimationClip)))
                    {
                        AnimationClip found = (AnimationClip)subObject;
                        if (found.name.iContains("T-Pose")) continue;
                        clips.Add(found);
                    }
                }
            }

            return clips.ToArray();
        }
        
        public static GameObject FindCharacterPrefabAsset(GameObject fbxAsset, bool baked = false)
        { 
            string path = AssetDatabase.GetAssetPath(fbxAsset);
            if (!string.IsNullOrEmpty(path))
            {
                if (path.iEndsWith(".prefab")) return fbxAsset;
                string folder = Path.GetDirectoryName(path);
                string name = Path.GetFileNameWithoutExtension(path);
                string searchName = name;
                if (baked) searchName = name + Importer.BAKE_SUFFIX;
                string prefabPath = Path.Combine(folder, Importer.PREFABS_FOLDER, searchName + ".prefab");
                if (File.Exists(prefabPath))
                    return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            return null;
        }

        public static GameObject FindCharacterPrefabAsset(string fbxPath, bool baked = false)
        {            
            if (!string.IsNullOrEmpty(fbxPath))
            {
                if (fbxPath.iEndsWith(".prefab")) 
                    return AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                string folder = Path.GetDirectoryName(fbxPath);
                string name = Path.GetFileNameWithoutExtension(fbxPath);
                string searchName = name;
                if (baked) searchName = name + Importer.BAKE_SUFFIX;
                string prefabPath = Path.Combine(folder, Importer.PREFABS_FOLDER, searchName + ".prefab");
                if (File.Exists(prefabPath))
                    return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            return null;
        }

        public static bool FindCharacterPrefabs(GameObject fbxAsset, out GameObject mainPrefab, out GameObject bakedPrefab)
        {
            string path = AssetDatabase.GetAssetPath(fbxAsset);
            string folder = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string prefabPath = Path.Combine(folder, Importer.PREFABS_FOLDER, name + ".prefab");
            string bakedPrefabPath = Path.Combine(folder, Importer.PREFABS_FOLDER, name + Importer.BAKE_SUFFIX + ".prefab");

            mainPrefab = null;
            bakedPrefab = null;

            if (File.Exists(prefabPath))
                mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            else
                mainPrefab = fbxAsset;

            if (File.Exists(bakedPrefabPath))
                bakedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bakedPrefabPath);

            return (mainPrefab || bakedPrefab);
        }

        public static GameObject GetScenePrefabInstanceRoot(Object sceneObject)
        {
            if (sceneObject)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(sceneObject))
                {
                    Object instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(sceneObject);
                    if (!instanceRoot) instanceRoot = sceneObject;

                    if (instanceRoot.GetType() == typeof(GameObject))
                        return (GameObject)instanceRoot;
                }
            }

            return null;
        }        

        public static GameObject FindRootPrefabAsset(GameObject prefabAsset)
        {
            if (prefabAsset)
            {
                Object source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabAsset);
                if (source)
                {
                    if (source.GetType() == typeof(GameObject))
                    {
                        return (GameObject)source;
                    }
                }
            }

            return null;
        }

        public static GameObject FindRootPrefabAssetFromSceneObject(Object sceneObject)
        {            
            GameObject instanceRoot = GetScenePrefabInstanceRoot(sceneObject);

            return FindRootPrefabAsset(instanceRoot);
        }        

        public static void ResetPrefabTransforms(GameObject prefabRoot, GameObject prefabObj = null)
        {
            if (!prefabObj && prefabRoot)
            {
                prefabObj = prefabRoot;
            }

            if (prefabObj)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabObj);

                // dont reset the root transform...
                if (source && source != prefabObj)
                {
                    if (prefabObj != prefabRoot)
                    {
                        bool resetPos = false;
                        bool resetRot = false;
                        bool resetSca = false;
                        if (prefabObj.transform.localPosition != source.transform.localPosition) resetPos = true;
                        if (prefabObj.transform.localRotation != source.transform.localRotation) resetRot = true;
                        if (prefabObj.transform.localScale != source.transform.localScale) resetSca = true;
                        if (resetPos) prefabObj.transform.localPosition = source.transform.localPosition;
                        if (resetRot) prefabObj.transform.localRotation = source.transform.localRotation;
                        if (resetSca) prefabObj.transform.localScale = source.transform.localScale;                        
                    }                    

                    for (int i = 0; i < prefabObj.transform.childCount; i++)
                    {
                        Transform child = prefabObj.transform.GetChild(i);
                        ResetPrefabTransforms(prefabRoot, child.gameObject);
                    }
                }
            }
        }

        public static GameObject TryResetScenePrefab(GameObject scenePrefab)  
        {
            if (PrefabNeedsReset(scenePrefab))
            {
                Util.LogInfo("Resetting Prefab");
                ResetPrefabTransforms(scenePrefab);
                /*
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(scenePrefab);
                Transform t = scenePrefab.transform;
                Transform parent = t.parent;
                Vector3 pos = t.position;
                Quaternion rot = t.rotation;
                Vector3 sca = t.localScale;
                GameObject.DestroyImmediate(scenePrefab);
                scenePrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource);
                scenePrefab.transform.parent = parent;
                scenePrefab.transform.position = pos;
                scenePrefab.transform.rotation = rot;
                scenePrefab.transform.localScale = sca;
                */
            }

            return scenePrefab;
        }

        public static bool PrefabNeedsReset(GameObject prefabObj)
        {            
            if (prefabObj)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabObj);
                if (source && source != prefabObj)
                {
                    bool resetPos = false;
                    bool resetRot = false;
                    bool resetSca = false;
                    if (prefabObj.transform.position != source.transform.position) resetPos = true;
                    if (prefabObj.transform.rotation != source.transform.rotation) resetRot = true;
                    if (prefabObj.transform.localScale != source.transform.localScale) resetSca = true;
                    if (resetPos || resetRot || resetSca)
                    {
                        return true;
                    }

                    for (int i = 0; i < prefabObj.transform.childCount; i++)
                    {
                        Transform child = prefabObj.transform.GetChild(i);
                        bool result = PrefabNeedsReset(child.gameObject);
                        if (result) return true;
                    }
                }
            }

            return false;
        }

        public static void FindSceneObjects(Transform root, string search, List<GameObject> found)
        {
            if (root.name.iStartsWith(search)) found.Add(root.gameObject);

            for (int i = 0; i < root.childCount; i++)
            {
                FindSceneObjects(root.GetChild(i), search, found);
            }
        }
        
        public static Transform FindChildRecursive(Transform root, string search)
        {
            if (root.name.iEquals(search)) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), search);
                if (found) return found;
            }

            return null;
        }     
        
        public static bool AssetPathExists(string assetPath)
        {
            return File.Exists(assetPath);
        }

        public static bool AssetPathIsEmpty(string assetPath)
        {
            const string emptyGuid = "00000000000000000000000000000000";
            string pathGUID = AssetDatabase.AssetPathToGUID(assetPath);

            return (pathGUID.Equals(emptyGuid) || string.IsNullOrEmpty(pathGUID));
        }

        public static bool HasMaterialKeywords(GameObject obj, params string[] keywords)
        {
            SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();

            if (smr)
            {
                foreach (Material mat in smr.sharedMaterials)
                {
                    if (mat && Util.NameContainsKeywords(mat.name, keywords)) return true;
                }
            }

            return false;
        }        

        public static bool NameContainsKeywords(string name, params string[] keyword)
        {
            foreach (string k in keyword)
            {
                int start = name.IndexOf(k, System.StringComparison.InvariantCultureIgnoreCase);
                int after = start + k.Length;

                if (start >= 0)
                {
                    // is keyword in name separated by underscores
                    if (name.iStartsWith(k + "_") ||
                        name.iEndsWith("_" + k) ||
                        name.iContains("_" + k + "_") ||
                        name.iEquals(k))
                        return true;

                    // match distinct keyword at start of name (any capitalization) or captitalized anywhere else
                    if (start == 0 || char.IsUpper(name[start]))
                    {
                        if (after >= name.Length || !char.IsLower(name[after]))
                            return true;
                    }
                }
            }
            return false;
        }






        public static GameObject EditPrefabContents(GameObject prefabAsset)
        {
            GameObject prefabRoot;
            string currentPrefabAssetPath = AssetDatabase.GetAssetPath(prefabAsset);
            prefabRoot = PrefabUtility.LoadPrefabContents(currentPrefabAssetPath);
            return prefabRoot;
        }

        public static void SaveAndUnloadPrefabContents(GameObject prefabAsset, GameObject prefabContents)
        {
            string currentPrefabAssetPath = AssetDatabase.GetAssetPath(prefabAsset);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, currentPrefabAssetPath, out bool success);
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }




        private static Editor MakeEditor(string guid)
        {
            Object o = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object));
            return Editor.CreateEditor(o);
        }

        const string prefsFailString = "xxxxxxxxxxxxxx";
        const char delimiterChar = ',';

        public static bool TrySerializeAssetToEditorPrefs(Object asset, string editorPrefsKey)
        {            
            int assetInstanceID = asset.GetInstanceID();
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(assetInstanceID, out string guid, out long localid))
            {
                string outString = assetInstanceID.ToString() + delimiterChar + guid.ToString() + delimiterChar + localid.ToString();
                LogDetail("Instance ID: " + assetInstanceID.ToString());
                LogDetail("GUID: " + guid.ToString());
                LogDetail("localID: " + localid.ToString());
                LogDetail("outString: " + outString);

                EditorPrefs.SetString(editorPrefsKey, outString);
                return true;
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(assetInstanceID);
                LogWarn("Cannot get GUID and ID for: " + asset.name + " at path: " + path);
                EditorPrefs.SetString(editorPrefsKey, prefsFailString);
                return false;
            }
        }

        public static bool TryDeSerializeAssetFromEditorPrefs<T>(out Object asset, string editorPrefsKey)
        {            
            bool storedAsset = false;
            string assetString = "";
            if (EditorPrefs.HasKey(editorPrefsKey))
            {
                assetString = EditorPrefs.GetString(editorPrefsKey);
                if (assetString == prefsFailString)
                {
                    LogInfo("Asset storage had failed - no asset to recover");
                }
                else
                {
                    storedAsset = true;
                }
            }
            else
            {
                LogWarn("No asset reference found");
            }

            if (storedAsset)
            {
                string[] split = assetString.Split(new char[] { delimiterChar });

                LogDetail("assetString: " + assetString);
                LogDetail("split count: " + split.Length);                

                if (split.Length == 3)
                {
                    int assetInstanceID = int.Parse(split[0]);
                    string guid = split[1];
                    long localid = long.Parse(split[2]);

                    LogDetail("Found Instance ID: " + assetInstanceID.ToString());
                    LogDetail("Found GUID: " + guid.ToString());
                    LogDetail("Found localID: " + localid.ToString());

                    Object[] potentials = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GUIDToAssetPath(guid));

                    LogDetail(potentials.Length + " Sub objects found for GUID: " + guid);
                    if (potentials.Length == 0)
                    {
                        Object potentialAsset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object));
                        if (potentialAsset != null)
                        {
                            LogDetail(potentialAsset.GetType().Name);
                            if (potentialAsset.GetType() == typeof(T))
                            {
                                LogDetail("Successfully found single asset: " + potentialAsset.GetType().Name + " Named: " + potentialAsset.name);
                                asset = potentialAsset;
                                return true;
                            }
                        }
                    }
                    else
                    {
                        foreach (Object potential in potentials)
                        {
                            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(potential.GetInstanceID(), out string tryGuid, out long tryLocalid))
                            {
                                if (guid == tryGuid && tryLocalid == localid)
                                {
                                    if (potential.GetType() == typeof(T))
                                    {
                                        LogDetail("Successfully found embedded asset: " + potential.GetType().Name + " Named: " + potential.name);
                                        asset = potential;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            asset = null;
            return false;
        }

        public static void SerializeBoolToEditorPrefs(bool value, string editorPrefsKey)
        {
            EditorPrefs.SetBool(editorPrefsKey, value);
        }

        public static bool TryDeSerializeBoolFromEditorPrefs(out bool value, string editorPrefsKey)
        {
            if (!EditorPrefs.HasKey(editorPrefsKey))
            {
                value = false;
                return false;
            }
            else
            {
                value = EditorPrefs.GetBool(editorPrefsKey);
                return true;
            }
        }

        public static void SerializeFloatToEditorPrefs(float value, string editorPrefsKey)
        {
            EditorPrefs.SetFloat(editorPrefsKey, value);
        }

        public static bool TryDeSerializeFloatFromEditorPrefs(out float value, string editorPrefsKey)
        {
            if (!EditorPrefs.HasKey(editorPrefsKey))
            {
                value = 0f;
                return false;
            }
            else
            {
                value = EditorPrefs.GetFloat(editorPrefsKey);
                return true;
            }
        }

        public static void SerializeStringToEditorPrefs(string value, string editorPrefsKey)
        {
            EditorPrefs.SetString(editorPrefsKey, value);
        }

        public static bool TryDeserializeStringFromEditorPrefs(out string value, string editorPrefsKey)
        {
            if (!EditorPrefs.HasKey(editorPrefsKey))
            {
                value = null;
                return false;
            }
            else
            {
                value = EditorPrefs.GetString(editorPrefsKey);
                return true;
            }
        }

        public static void SerializeIntToEditorPrefs(int value, string editorPrefsKey)
        {
            EditorPrefs.SetInt(editorPrefsKey, value);
        }

        public static bool TryDeserializeIntFromEditorPrefs(out int value, string editorPrefsKey)
        {
            if (!EditorPrefs.HasKey(editorPrefsKey))
            {
                value = 0;
                return false;
            }
            else
            {
                value = EditorPrefs.GetInt(editorPrefsKey);
                return true;
            }
        }

        public static void LogInfo(string message)
        {
            if (LOG_LEVEL >= 2)
            {
                Debug.Log(message);
            }
        }

        public static void LogDetail(string message)
        {
            if (LOG_LEVEL >= 3)
            {
                Debug.Log(message);
            }
        }

        public static void LogWarn(string message)
        {
            if (LOG_LEVEL >= 1)
            {
                Debug.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (LOG_LEVEL >= 0)
            {
                Debug.LogError(message);
            }
        }

        public static void LogAlways(string message)
        {
            Debug.Log(message);
        }


        public static void TransferSkinnedMeshes(GameObject fromPrefab, GameObject toPrefab)
        {
            GameObject fromInstanceRoot = GameObject.Instantiate(fromPrefab);
            GameObject toInstanceRoot = GameObject.Instantiate(toPrefab);
            Transform[] toTransforms = toInstanceRoot.GetComponentsInChildren<Transform>();
            SkinnedMeshRenderer[] renderers = fromInstanceRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer smr in renderers)
            {
                GameObject newMesh = GameObject.Instantiate(smr.gameObject);
                newMesh.transform.SetParent(toInstanceRoot.transform, true);
                SkinnedMeshRenderer newSMR = newMesh.GetComponent<SkinnedMeshRenderer>();
                for (int i = 0; i < newSMR.bones.Length; i++)
                {
                    string boneName = smr.bones[i].name;
                    Transform toBone = System.Array.Find(toTransforms, t => t.name.Equals(boneName));
                    if (toBone) newSMR.bones[i] = toBone;
                }                
            }
        }
    }    
}