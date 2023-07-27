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

using UnityEditor;
using UnityEngine;
using System.IO;

namespace Reallusion.Import
{
    public class ShaderPackageManifest : ScriptableObject
    {
        public string packageVersion = "0.0.0";
        public string shaderPackageGuid = "0";

        public const string SHADER_PACKAGE_GUID_3D = "bb0033482f21aca428935ae427801441";
        public const string SHADER_PACKAGE_GUID_URP10 = "262039ac86a922246b86ddb4223895f0";
        public const string SHADER_PACKAGE_GUID_URP12 = "c071ccb632d0d9e42a46d20aa5f82ebd";
        public const string SHADER_PACKAGE_GUID_HDRP10 = "ac3c47e6d2a7bc840be841aa1783fad9";
        public const string SHADER_PACKAGE_GUID_HDRP12 = "7752b315a58f7ef4489dd777221f3432";
        public const string SHADER_PACKAGE_PATH_3D = "Assets\\CCiC Unity Tools 3D";
        public const string SHADER_PACKAGE_PATH_URP10 = "Assets\\CCiC Unity Tools URP10";
        public const string SHADER_PACKAGE_PATH_URP12 = "Assets\\CCiC Unity Tools URP12";
        public const string SHADER_PACKAGE_PATH_HDRP10 = "Assets\\CCiC Unity Tools HDRP10";
        public const string SHADER_PACKAGE_PATH_HDRP12 = "Assets\\CCiC Unity Tools HDRP12";
        public const string SHADER_PACKAGE_MANIFEST_GUID = "dcd86d18493245c478e0879a56d8e81f";

        /*
        [MenuItem("Reallusion/Misc Tools/Import Shader Package", priority = 200)]
        private static void DoImportShaderPackage()
        {
            ShaderPackageManifest.ImportShaderPackage();
        }
        */

        public static ShaderPackageManifest GetShaderManifest(string packageFolder)
        {
            ShaderPackageManifest manifest = null;
            string manifestPath = AssetDatabase.GUIDToAssetPath(SHADER_PACKAGE_MANIFEST_GUID);

            // try to load manifest from the expected GUID
            if (!string.IsNullOrEmpty(manifestPath))
            {
                manifest = AssetDatabase.LoadAssetAtPath<ShaderPackageManifest>(manifestPath);
            }

            if (manifest == null)
            {
                // try to load manifest from the expected shader package folder
                manifestPath = Path.Combine(packageFolder, "ShaderPackageManifest.asset");
                if (!string.IsNullOrEmpty(manifestPath))
                {
                    manifest = AssetDatabase.LoadAssetAtPath<ShaderPackageManifest>(manifestPath);
                }

                // otherwise create a new one.
                if (manifest == null)
                {
                    manifest = ScriptableObject.CreateInstance<ShaderPackageManifest>();
                    Util.EnsureAssetsFolderExists(packageFolder);
                    AssetDatabase.CreateAsset(manifest, manifestPath);
                }
            }

            return manifest;
        }

        public static void UpdateShaderManifest(ScriptableObject manifest)
        {
            EditorUtility.SetDirty(manifest);
#if UNITY_2021_2_OR_NEWER
			AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(manifest)));
#else
            AssetDatabase.SaveAssets();
#endif
        }

        public static bool ImportShaderPackage()
        {
#if HDRP_12_0_0_OR_NEWER
            string shaderPackageGuid = SHADER_PACKAGE_GUID_HDRP12;
            string shaderPackageFolder = SHADER_PACKAGE_PATH_HDRP12;
#elif HDRP_10_5_0_OR_NEWER
            string shaderPackageGuid = SHADER_PACKAGE_GUID_HDRP10;
            string shaderPackageFolder = SHADER_PACKAGE_PATH_HDRP10;
#elif URP_12_0_0_OR_NEWER
            string shaderPackageGuid = SHADER_PACKAGE_GUID_URP12;
            string shaderPackageFolder = SHADER_PACKAGE_PATH_URP12;
#elif URP_10_5_0_OR_NEWER
            string shaderPackageGuid = SHADER_PACKAGE_GUID_URP10;
            string shaderPackageFolder = SHADER_PACKAGE_PATH_URP10;
#else
            string shaderPackageGuid = SHADER_PACKAGE_GUID_3D;
            string shaderPackageFolder = SHADER_PACKAGE_PATH_3D;
#endif
            string shaderPackagePath = AssetDatabase.GUIDToAssetPath(shaderPackageGuid);

            // check shader package manifest exists and is current version
            bool installPackage = true;

            ShaderPackageManifest manifest = GetShaderManifest(shaderPackageFolder);

            // if no shader package manifest, install package
            if (manifest != null &&
                manifest.shaderPackageGuid == shaderPackageGuid &&
                manifest.packageVersion == Pipeline.FULL_VERSION)
            {
                Util.LogInfo("CC/iC Unity Tools Shader Package up to date.");
                installPackage = false;
            }

            if (installPackage)
            {
                Util.LogInfo("Installing CC/iC Unity Tools Shader Package: " + Pipeline.FULL_VERSION);
                AssetDatabase.ImportPackage(shaderPackagePath, false);
                manifest = GetShaderManifest(shaderPackageFolder);
                manifest.packageVersion = Pipeline.FULL_VERSION;
                manifest.shaderPackageGuid = shaderPackageGuid;
                UpdateShaderManifest(manifest);
            }

            return false;
        }
    }
}
