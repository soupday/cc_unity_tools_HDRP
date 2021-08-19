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

namespace Reallusion.Import
{
    public static class Util
    {        
        public static bool iEquals(this string a, string b)
        {
            return a.Equals(b, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool iContains(this string a, string b)
        {
            return a.ToLowerInvariant().Contains(b.ToLowerInvariant());
        }

        public static bool iStartsWith(this string a, string b)
        {
            return a.StartsWith(b, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool iEndsWith(this string a, string b)
        {
            return a.EndsWith(b, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static void SetRemapRange(this Material mat, string shaderRef, float from, float to)
        {
            Color range;
            range.r = from;
            range.g = to;
            range.b = 0f;
            range.a = 0f;
            mat.SetColor(shaderRef, range);
        }

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
            string assetFolder = Path.GetDirectoryName(assetPath);
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                if (assetPath.EndsWith(".fbx", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    string assetName = Path.GetFileNameWithoutExtension(assetPath);
                    string[] searchFolders = { assetFolder };

                    if (GetJSONAsset(assetName, searchFolders) != null)
                    {
                        if (AssetDatabase.IsValidFolder(Path.Combine(assetFolder, "textures", assetName)))
                        {
                            return true;
                        }
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
        
        public static TextAsset GetJSONAsset(string name, string[] folders)
        {
            string[] foundGUIDs = AssetDatabase.FindAssets(name, folders);
            foreach (string guid in foundGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith(".json", System.StringComparison.InvariantCultureIgnoreCase))
                    return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            }
            return null;
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
                    if (shader.name.EndsWith(name, System.StringComparison.InvariantCultureIgnoreCase))
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

            string[] guids = AssetDatabase.FindAssets("t:material", folders);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat)
                {
                    if (mat.name.EndsWith(name, System.StringComparison.InvariantCultureIgnoreCase))
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

            texGuids = AssetDatabase.FindAssets("t:texture2d", folders);

            foreach (string guid in texGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string texName = Path.GetFileNameWithoutExtension(assetPath);
                if (texName.EndsWith(search, System.StringComparison.CurrentCultureIgnoreCase))
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
                if (assetName.EndsWith(search, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                }
            }

            return null;
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
            if ((shaderName.iContains(Pipeline.SHADER_HQ_HEAD) || 
                 shaderName.iContains(Pipeline.SHADER_HQ_SKIN)) && 
                !sourceName.iContains("Std_Nails")) return 0;
            if (shaderName.iContains(Pipeline.SHADER_HQ_EYE) || 
                shaderName.iContains(Pipeline.SHADER_HQ_CORNEA)) return 1;
            if (shaderName.iContains(Pipeline.SHADER_HQ_EYE_OCCLUSION)) return 2;
            if (shaderName.iContains(Pipeline.SHADER_HQ_TEARLINE)) return 3;
            if (shaderName.iContains(Pipeline.SHADER_HQ_TEETH)) return 4;
            if (shaderName.iContains(Pipeline.SHADER_HQ_HAIR)) return 5;

            if (sourceName.iContains("Std_Skin_Head") || sourceName.iContains("Std_Skin_Body") ||
                sourceName.iContains("Std_Skin_Arm") || sourceName.iContains("Std_Skin_Leg"))
                return 0;            
            if (sourceName.iContains("Std_Eye_Occlusion_")) return 2;
            if (sourceName.iContains("Std_Tearline_")) return 3;
            if (sourceName.iContains("Std_Eye_") || sourceName.iContains("Std_Cornea_")) return 1;
            if (sourceName.iContains("Std_Upper_Teeth") || sourceName.iContains("Std_Lower_Teeth")) return 4;
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
                string[] searchGuids = AssetDatabase.FindAssets("t:material " + name, assetFolders);
                foreach (string guid in searchGuids)
                {
                    string searchPath = AssetDatabase.GUIDToAssetPath(guid);
                    string searchName = Path.GetFileNameWithoutExtension(searchPath).ToLowerInvariant();
                    if (searchName.Contains(name))
                    {
                        Debug.Log(searchName);
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

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            //old code block
            /*
            UnityEngine.SceneManagement.Scene previewScene = EditorSceneManager.OpenScene("Assets/CC3Import/Scenes/PreviewScene.unity");

            if (previewScene.IsValid())
            {
                if (previewScene.isLoaded)
                {
                    GameObject container = GameObject.Find("Character Container");
                    if (container)
                    {
                        DestroyEditorChildObjects(container);

                        GameObject clone = PrefabUtility.InstantiatePrefab(character, container.transform) as GameObject;
                        if (clone)
                        {
                            Selection.activeGameObject = clone;
                            SceneView.FrameLastActiveSceneView();
                        }
                    }
                }
            }
            */
            
            //new code block start
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            GameObject.Instantiate(Util.FindPreviewScenePrefab(), Vector3.zero, Quaternion.identity);
            GameObject container = GameObject.Find("Character Container");
            if (container)
            {
                DestroyEditorChildObjects(container);

                GameObject clone = PrefabUtility.InstantiatePrefab(character, container.transform) as GameObject;
                if (clone)
                {
                    Selection.activeGameObject = clone;
                    SceneView.FrameLastActiveSceneView();
                }
            }
            //new code block end
            
        }        

        public static GameObject GetPrefabFromObject(Object obj)
        {
            Object source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            Object parent = PrefabUtility.GetPrefabInstanceHandle(source);
            if (parent.GetType() == typeof(GameObject))
            {
                return (GameObject)parent;
            }

            return null;
        }
    }
}