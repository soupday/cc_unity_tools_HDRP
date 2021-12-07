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

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

namespace Reallusion.Import
{
    public static class Util
    {
        public static int LOG_LEVEL = 0;

        public static bool ImportCharacter(CharacterInfo info, MaterialQuality quality)
        {
            Importer importCharacter = new Importer(info);
            importCharacter.SetQuality(quality);

            return importCharacter.Import();
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

            return "Unknown";
        }

        public static List<string> GetValidCharacterGUIDS()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new string[] { "Assets" });
            List<string> results = new List<string>();

            foreach (string g in guids)
            {
                if (IsCC3Character(g))
                {
                    results.Add(g);
                }
            }

            return results;
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

            string[] fullSplit = fullPath.Split('\\');
            string[] baseSplit = basePath.Split('\\');

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
                relativePath += fullSplit[i] + "\\";
            }
            relativePath += fullSplit[fullSplit.Length - 1];

            return relativePath;
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
            // remove any ./ prefix from the json path
            if (jsonTexPath.iStartsWith("./"))
                jsonTexPath = jsonTexPath.Substring(2);
            // convert slashes to backslashes
            jsonTexPath = jsonTexPath.Replace("/", "\\");
            return Path.Combine(fbxPath, jsonTexPath);            
        }

        public static GameObject FindPreviewScenePrefab()
        {
            string[] texGuids;

            texGuids = AssetDatabase.FindAssets("t:prefab PreviewScenePrefab");

            foreach (string guid in texGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(assetPath);
                if (name.Equals("PreviewScenePrefab", System.StringComparison.InvariantCultureIgnoreCase))
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

            return sharedMaterial.name;
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
            // this power function must range from f(0) = 0 to f(1) = 1 and achieve 0.88 maximum smoothness at 0.5 specular
            // (0.5 specular being the default specular value for base max smoothness, visually detected as ~0.88 smoothness)
            // specular values from 0.5 to 1.0 will generate a max smoothness of 0.88 to 1.0.
            // Thus: P = ln(0.88) / ln(0.5) = 0.184424571f
            // This should approximate the specular mask for specular values > 0.2
            const float smoothnessStdMax = 0.88f;
            const float specularMid = 0.5f;
            float P = Mathf.Log(smoothnessStdMax) / Mathf.Log(specularMid);
            return smoothness * Mathf.Clamp01(Mathf.Pow(specular, P));
        }

        public static void DoTest(Object asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);

            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (Object o in objects)
            {
                Debug.Log(o);
            }
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
                !sourceName.iContains("Std_Nails")) return CharacterTreeView.LINKED_INDEX_SKIN;
            if (Pipeline.GetRenderPipeline() == RenderPipeline.HDRP)
            {
                if (shaderName.iContains(Pipeline.SHADER_HQ_EYE) ||
                    shaderName.iContains(Pipeline.SHADER_HQ_CORNEA)) return CharacterTreeView.LINKED_INDEX_CORNEA;
                if (shaderName.iContains(Pipeline.SHADER_HQ_CORNEA_PARALLAX)) return CharacterTreeView.LINKED_INDEX_CORNEA_PARALLAX;
            }
            else
            {
                // Eye is PBR in URP and Built-in                
                if (shaderName.iContains(Pipeline.SHADER_HQ_CORNEA)) return CharacterTreeView.LINKED_INDEX_CORNEA;
            }
            if (shaderName.iContains(Pipeline.SHADER_HQ_EYE_OCCLUSION)) return CharacterTreeView.LINKED_INDEX_EYE_OCCLUSION;
            if (shaderName.iContains(Pipeline.SHADER_HQ_TEARLINE)) return CharacterTreeView.LINKED_INDEX_TEARLINE;
            if (shaderName.iContains(Pipeline.SHADER_HQ_TEETH)) return CharacterTreeView.LINKED_INDEX_TEETH;
            if (shaderName.iContains(Pipeline.SHADER_HQ_HAIR)) return CharacterTreeView.LINKED_INDEX_HAIR;

            // then try by source material name:
            if (sourceName.iContains("Std_Skin_Head") || sourceName.iContains("Std_Skin_Body") ||
                sourceName.iContains("Std_Skin_Arm") || sourceName.iContains("Std_Skin_Leg"))
                return CharacterTreeView.LINKED_INDEX_SKIN;            
            if (sourceName.iContains("Std_Eye_Occlusion_")) return CharacterTreeView.LINKED_INDEX_EYE_OCCLUSION;
            if (sourceName.iContains("Std_Tearline_")) return CharacterTreeView.LINKED_INDEX_TEARLINE;
            if (Pipeline.GetRenderPipeline() == RenderPipeline.HDRP)
            {
                if (sourceName.iContains("Std_Eye_") || sourceName.iContains("Std_Cornea_")) return CharacterTreeView.LINKED_INDEX_CORNEA;
            }
            else
            {                
                if (sourceName.iContains("Std_Cornea_")) return CharacterTreeView.LINKED_INDEX_CORNEA;
            }
            if (sourceName.iContains("Std_Upper_Teeth") || sourceName.iContains("Std_Lower_Teeth")) return CharacterTreeView.LINKED_INDEX_TEETH;

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
                        //Debug.Log(searchName);
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

        public static void PreviewCharacter(GameObject character)
        {
            if (!character) return;

            GameObject prefab = GetCharacterPrefab(character);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;            
                        
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            GameObject.Instantiate(Util.FindPreviewScenePrefab(), Vector3.zero, Quaternion.identity);
            GameObject container = GameObject.Find("Character Container");
            if (container)
            {
                DestroyEditorChildObjects(container);

                GameObject clone = PrefabUtility.InstantiatePrefab(prefab ? prefab : character, container.transform) as GameObject;
                if (clone)
                {
                    Selection.activeGameObject = clone;
                    SceneView.FrameLastActiveSceneView();
                }
            }
        }        

        public static void AddPreviewCharacter(GameObject fbx, GameObject prefab, Vector3 offset, bool replace)
        {
            GameObject container = GameObject.Find("Character Container");
            if (container)
            {
                // don't replace an existing copy of the prefab...
                for (int i = 0; i < container.transform.childCount; i++)
                {
                    GameObject child = container.transform.GetChild(i).gameObject;
                    GameObject source = GetSourcePrefabFromObject(child);
                    if (source == prefab)
                    {
                        Debug.Log("Keeping existing generated prefab...");
                        child.transform.position = offset;
                        return;
                    }
                }

                for (int i = 0; i < container.transform.childCount; i++)
                {
                    GameObject child = container.transform.GetChild(i).gameObject;

                    GameObject source;
                    if (child.name.iContains("_lod") && child.transform.childCount == 1)
                        source = GetRootPrefabFromObject(child.transform.GetChild(0).gameObject);
                    else
                        source = GetRootPrefabFromObject(child);

                    if (source == fbx)
                    {
                        if (replace) GameObject.DestroyImmediate(child);
                        if (replace)
                            Debug.Log("Replacing preview character with generated Prefab.");
                        else
                            Debug.Log("Adding generated Prefab.");
                        GameObject clone = PrefabUtility.InstantiatePrefab(prefab, container.transform) as GameObject;
                        if (clone)
                        {
                            clone.transform.position += offset;
                            Selection.activeGameObject = clone;
                        }

                        return;
                    }
                }                
            }
        }      
        
        public static GameObject GetCharacterPrefab(GameObject fbx)
        { 
            string path = AssetDatabase.GetAssetPath(fbx);
            if (path.iEndsWith(".prefab")) return fbx;
            string folder = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string prefabPath = Path.Combine(folder, Importer.PREFABS_FOLDER, name + ".prefab");
            if (File.Exists(prefabPath))
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return null;
        }        

        public static GameObject GetSourcePrefabFromObject(Object obj)
        {            
            Object source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (source)
            {                
                Object parent = PrefabUtility.GetPrefabInstanceHandle(source);
                if (parent)
                {
                    if (parent.GetType() == typeof(GameObject))
                    {
                        return (GameObject)parent;
                    }
                }

                if (source.GetType() == typeof(GameObject))
                {
                    return (GameObject)source;
                }
            }

            return null;
        }

        public static GameObject GetRootPrefabFromObject(GameObject obj)
        {            
            GameObject prefab = GetSourcePrefabFromObject(obj);

            if (prefab)
            {
                GameObject parent = GetRootPrefabFromObject(prefab);

                if (parent)
                    return parent;
                else
                    return prefab;
            }

            return obj;
        }

        public static void LogInfo(string message)
        {
            if (LOG_LEVEL >= 2)
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
    }
}