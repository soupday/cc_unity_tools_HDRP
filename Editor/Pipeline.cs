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
using UnityEditor;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Rendering;
#if HDRP_10_5_0_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition;
#endif

namespace Reallusion.Import
{
    public enum RenderPipeline { Unknown, HDRP, URP, Builtin }

    public enum MaterialType
    {
        None, Skin, Head, Eye, Cornea, EyeOcclusion, Tearline, Hair, Scalp,
        Eyelash, Teeth, Tongue, DefaultOpaque, DefaultAlpha
    }

    public enum MaterialQuality { Default, High, Baked }

    public static class Pipeline
    {
#if HDRP_10_5_0_OR_NEWER
        // Shaders
        //
        public const string SHADER_DEFAULT = "HDRP/Lit";
        public const string SHADER_DEFAULT_HAIR = "HDRP/Hair";
        //
        public const string SHADER_HQ_SKIN = "RL_SkinShaderVariants_HDRP";
        public const string SHADER_HQ_HEAD = "RL_SkinShaderVariants_HDRP";
        public const string SHADER_HQ_CORNEA = "RL_EyeShaderVariants_HDRP";
        public const string SHADER_HQ_EYE = "RL_EyeShaderVariants_HDRP";
        public const string SHADER_HQ_EYE_OCCLUSION = "RL_EyeOcclusionShader_HDRP";
        public const string SHADER_HQ_TEARLINE = "RL_TearlineShader_HDRP";
        public const string SHADER_HQ_HAIR = "RL_HairShaderVariants_HDRP";
        public const string SHADER_HQ_SCALPBASE = "HDRP/Lit";
        public const string SHADER_HQ_EYELASH = "HDRP/Lit";
        public const string SHADER_HQ_TEETH = "RL_TeethShader_HDRP";
        public const string SHADER_HQ_TONGUE = "RL_TongueShader_HDRP";
        public const string SHADER_HQ_ALPHACLIP = "HDRP/Lit";
        public const string SHADER_HQ_OPAQUE = "HDRP/Lit";

        // HQ Materials
        //
        public const string MATERIAL_HQ_SKIN = "RL_Template_HQ_Skin_HDRP";
        public const string MATERIAL_HQ_HEAD = "RL_Template_HQ_Head_HDRP";        
        public const string MATERIAL_HQ_CORNEA = "RL_Template_HQ_Cornea_HDRP";
        public const string MATERIAL_HQ_EYE = "RL_Template_HQ_Eye_HDRP";
        public const string MATERIAL_HQ_EYE_OCCLUSION = "RL_Template_HQ_EyeOcclusion_HDRP";
        public const string MATERIAL_HQ_TEARLINE = "RL_Template_HQ_Tearline_HDRP";
        public const string MATERIAL_HQ_HAIR = "RL_Template_HQ_Hair_HDRP";
        public const string MATERIAL_HQ_SCALPBASE = "RL_Template_Default_ScalpBase_HDRP";
        public const string MATERIAL_HQ_EYELASH = "RL_Template_Default_Eyelash_HDRP";
        public const string MATERIAL_HQ_TEETH = "RL_Template_HQ_Teeth_HDRP";
        public const string MATERIAL_HQ_TONGUE = "RL_Template_HQ_Tongue_HDRP";
        public const string MATERIAL_HQ_ALPHACLIP = "RL_Template_Default_AlphaClip_HDRP";
        public const string MATERIAL_HQ_OPAQUE = "RL_Template_Default_Opaque_HDRP";
        // variants
        public const string MATERIAL_HQ_CORNEA_REFRACTIVE = "RL_Template_HQ_CorneaRef_HDRP";

        // Default Materials
        //
        public const string MATERIAL_DEFAULT_SKIN = "RL_Template_Default_Skin_HDRP";
        public const string MATERIAL_DEFAULT_HEAD = "RL_Template_Default_Skin_HDRP";
        public const string MATERIAL_DEFAULT_CORNEA = "RL_Template_Default_Opaque_HDRP";
        public const string MATERIAL_DEFAULT_EYE = "RL_Template_Default_Opaque_HDRP";
        public const string MATERIAL_DEFAULT_EYE_OCCLUSION = "RL_Template_Default_EyeOcclusion_HDRP";
        public const string MATERIAL_DEFAULT_TEARLINE = "RL_Template_Default_Tearline_HDRP";
        public const string MATERIAL_DEFAULT_HAIR = "RL_Template_Default_Hair_HDRP";
        public const string MATERIAL_DEFAULT_SCALPBASE = "RL_Template_Default_ScalpBase_HDRP";
        public const string MATERIAL_DEFAULT_EYELASH = "RL_Template_Default_Eyelash_HDRP";        
        public const string MATERIAL_DEFAULT_TEETH = "RL_Template_Default_Opaque_HDRP";
        public const string MATERIAL_DEFAULT_TONGUE = "RL_Template_Default_Opaque_HDRP";
        public const string MATERIAL_DEFAULT_ALPHACLIP = "RL_Template_Default_AlphaClip_HDRP";
        public const string MATERIAL_DEFAULT_OPAQUE = "RL_Template_Default_Opaque_HDRP";
        // variants
        public const string MATERIAL_DEFAULT_CORNEA_REFRACTIVE = "RL_Template_Default_Opaque_HDRP";

        // Baked Materials
        //
        public const string MATERIAL_BAKED_SKIN = "RL_Template_Baked_Skin_HDRP";
        public const string MATERIAL_BAKED_HEAD = "RL_Temaplte_Baked_Skin_HDRP";              
        public const string MATERIAL_BAKED_CORNEA = "RL_Template_Baked_Cornea_HDRP";                
        public const string MATERIAL_BAKED_EYE = "RL_Template_Baked_Eye_HDRP";       
        public const string MATERIAL_BAKED_EYE_OCCLUSION = "RL_Template_Baked_EyeOcclusion_HDRP";
        public const string MATERIAL_BAKED_TEARLINE = "RL_Template_Tearline_HDRP";
        public const string MATERIAL_BAKED_HAIR = "RL_Template_Baked_Hair_HDRP";        
        public const string MATERIAL_BAKED_SCALPBASE = "RL_Template_Default_ScalpBase_HDRP";
        public const string MATERIAL_BAKED_EYELASH = "RL_Template_Default_Eyelash_HDRP";
        public const string MATERIAL_BAKED_TEETH = "RL_Template_Baked_Skin_HDRP";
        public const string MATERIAL_BAKED_TONGUE = "RL_Template_Baked_Skin_HDRP";
        public const string MATERIAL_BAKED_ALPHACLIP = "RL_Template_Default_AlphaClip_HDRP";
        public const string MATERIAL_BAKED_OPAQUE = "RL_Template_Default_Opaque_HDRP";
        // variants
        public const string MATERIAL_BAKED_CORNEA_REFRACTIVE = "RL_Template_Baked_CorneaRef_HDRP";        
        public const string MATERIAL_BAKED_EYE_CUSTOM = "RL_Template_Baked_EyeCustom_HDRP";
        public const string MATERIAL_BAKED_EYE_OCCLUSION_CUSTOM = "RL_Template_Baked_EyeOcclusionCustom_HDRP";
        public const string MATERIAL_BAKED_HAIR_CUSTOM = "RL_Template_Baked_HairCustom_HDRP";
        // for gamebase single material or actor core...
        public const string MATERIAL_DEFAULT_SINGLE_MATERIAL = "RL_Template_Default_SingleMaterial_HDRP";
#elif URP_10_5_0_OR_NEWER
        // Shaders
        //
        public const string SHADER_DEFAULT = "Universal Render Pipeline/Lit";
        public const string SHADER_DEFAULT_HAIR = "Universal Render Pipeline/Hair"; // n/a
        //
        public const string SHADER_HQ_SKIN = "RL_SkinShaderVariants_URP";
        public const string SHADER_HQ_HEAD = "RL_SkinShaderVariants_URP";
        public const string SHADER_HQ_CORNEA = "RL_EyeShaderVariants_URP";
        public const string SHADER_HQ_EYE = "Universal Render Pipeline/Lit";
        public const string SHADER_HQ_EYE_OCCLUSION = "RL_EyeOcclusionShader_URP";
        public const string SHADER_HQ_TEARLINE = "RL_TearlineShader_URP";
        public const string SHADER_HQ_HAIR = "RL_HairShaderVariants_URP";
        public const string SHADER_HQ_SCALPBASE = "Universal Render Pipeline/Lit";
        public const string SHADER_HQ_EYELASH = "Universal Render Pipeline/Lit";
        public const string SHADER_HQ_TEETH = "RL_TeethShader_URP";
        public const string SHADER_HQ_TONGUE = "RL_TongueShader_URP";
        public const string SHADER_HQ_ALPHACLIP = "Universal Render Pipeline/Lit";
        public const string SHADER_HQ_OPAQUE = "Universal Render Pipeline/Lit";

        // HQ Materials
        //
        public const string MATERIAL_HQ_SKIN = "RL_Template_HQ_Skin_URP";
        public const string MATERIAL_HQ_HEAD = "RL_Template_HQ_Head_URP";
        public const string MATERIAL_HQ_CORNEA = "RL_Template_HQ_Cornea_URP";
        public const string MATERIAL_HQ_EYE = "RL_Template_HQ_Eye_URP";
        public const string MATERIAL_HQ_EYE_OCCLUSION = "RL_Template_HQ_EyeOcclusion_URP";
        public const string MATERIAL_HQ_TEARLINE = "RL_Template_HQ_Tearline_URP";
        public const string MATERIAL_HQ_HAIR = "RL_Template_HQ_Hair_URP";
        public const string MATERIAL_HQ_SCALPBASE = "RL_Template_Default_ScalpBase_URP";
        public const string MATERIAL_HQ_EYELASH = "RL_Template_Default_Eyelash_URP";
        public const string MATERIAL_HQ_TEETH = "RL_Template_HQ_Teeth_URP";
        public const string MATERIAL_HQ_TONGUE = "RL_Template_HQ_Tongue_URP";
        public const string MATERIAL_HQ_ALPHACLIP = "RL_Template_Default_AlphaClip_URP";
        public const string MATERIAL_HQ_OPAQUE = "RL_Template_Default_Opaque_URP";
        // variants
        public const string MATERIAL_HQ_CORNEA_REFRACTIVE = "RL_Template_HQ_CorneaRef_URP";

        // Default Materials
        //
        public const string MATERIAL_DEFAULT_SKIN = "RL_Template_Default_Skin_URP";
        public const string MATERIAL_DEFAULT_HEAD = "RL_Template_Default_Skin_URP";
        public const string MATERIAL_DEFAULT_CORNEA = "RL_Template_Default_Opaque_URP";
        public const string MATERIAL_DEFAULT_EYE = "RL_Template_Default_Opaque_URP";
        public const string MATERIAL_DEFAULT_EYE_OCCLUSION = "RL_Template_Default_EyeOcclusion_URP";
        public const string MATERIAL_DEFAULT_TEARLINE = "RL_Template_Default_Tearline_URP";
        public const string MATERIAL_DEFAULT_HAIR = "RL_Template_Default_Hair_URP";
        public const string MATERIAL_DEFAULT_SCALPBASE = "RL_Template_Default_ScalpBase_URP";
        public const string MATERIAL_DEFAULT_EYELASH = "RL_Template_Default_Eyelash_URP";
        public const string MATERIAL_DEFAULT_TEETH = "RL_Template_Default_Opaque_URP";
        public const string MATERIAL_DEFAULT_TONGUE = "RL_Template_Default_Opaque_URP";
        public const string MATERIAL_DEFAULT_ALPHACLIP = "RL_Template_Default_AlphaClip_URP";
        public const string MATERIAL_DEFAULT_OPAQUE = "RL_Template_Default_Opaque_URP";
        // variants
        public const string MATERIAL_DEFAULT_CORNEA_REFRACTIVE = "RL_Template_Default_Opaque_URP";

        // Baked Materials
        //
        public const string MATERIAL_BAKED_SKIN = "RL_Template_Baked_Skin_URP";
        public const string MATERIAL_BAKED_HEAD = "RL_Temaplte_Baked_Skin_URP";
        public const string MATERIAL_BAKED_CORNEA = "RL_Template_Baked_Cornea_URP";
        public const string MATERIAL_BAKED_EYE = "RL_Template_Baked_Eye_URP";
        public const string MATERIAL_BAKED_EYE_OCCLUSION = "RL_Template_Baked_EyeOcclusion_URP";
        public const string MATERIAL_BAKED_TEARLINE = "RL_Template_Tearline_URP";
        public const string MATERIAL_BAKED_HAIR = "RL_Template_Baked_Hair_URP";
        public const string MATERIAL_BAKED_SCALPBASE = "RL_Template_Default_ScalpBase_URP";
        public const string MATERIAL_BAKED_EYELASH = "RL_Template_Default_Eyelash_URP";
        public const string MATERIAL_BAKED_TEETH = "RL_Template_Baked_Skin_URP";
        public const string MATERIAL_BAKED_TONGUE = "RL_Template_Baked_Skin_URP";
        public const string MATERIAL_BAKED_ALPHACLIP = "RL_Template_Default_AlphaClip_URP";
        public const string MATERIAL_BAKED_OPAQUE = "RL_Template_Default_Opaque_URP";
        // variants
        public const string MATERIAL_BAKED_CORNEA_REFRACTIVE = "RL_Template_Baked_CorneaRef_URP";
        public const string MATERIAL_BAKED_EYE_CUSTOM = "RL_Template_Baked_EyeCustom_URP";
        public const string MATERIAL_BAKED_EYE_OCCLUSION_CUSTOM = "RL_Template_Baked_EyeOcclusionCustom_URP";
        public const string MATERIAL_BAKED_HAIR_CUSTOM = "RL_Template_Baked_HairCustom_URP";
        // for gamebase single material or actor core...
        public const string MATERIAL_DEFAULT_SINGLE_MATERIAL = "RL_Template_Default_SingleMaterial_URP";
#else
        // Shaders
        //
        public const string SHADER_DEFAULT = "3D/Lit";
        public const string SHADER_DEFAULT_HAIR = "3D/Hair";
        //
        public const string SHADER_HQ_SKIN = "RL_SkinShaderVariants_3D";
        public const string SHADER_HQ_HEAD = "RL_SkinShaderVariants_3D";
        public const string SHADER_HQ_CORNEA = "RL_EyeShaderVariants_3D";
        public const string SHADER_HQ_EYE = "RL_EyeShaderVariants_3D";
        public const string SHADER_HQ_EYE_OCCLUSION = "RL_EyeOcclusionShader_3D";
        public const string SHADER_HQ_TEARLINE = "RL_TearlineShader_3D";
        public const string SHADER_HQ_HAIR = "RL_HairShaderVariants_3D";
        public const string SHADER_HQ_SCALPBASE = "3D/Lit";
        public const string SHADER_HQ_EYELASH = "3D/Lit";
        public const string SHADER_HQ_TEETH = "RL_TeethShader_3D";
        public const string SHADER_HQ_TONGUE = "RL_TongueShader_3D";
        public const string SHADER_HQ_ALPHACLIP = "3D/Lit";
        public const string SHADER_HQ_OPAQUE = "3D/Lit";

        // HQ Materials
        //
        public const string MATERIAL_HQ_SKIN = "RL_Template_Skin_3D";
        public const string MATERIAL_HQ_HEAD = "RL_Template_Head_3D";
        public const string MATERIAL_HQ_CORNEA = "RL_Template_Cornea_3D";
        public const string MATERIAL_HQ_EYE = "RL_Template_Eye_3D";
        public const string MATERIAL_HQ_EYE_OCCLUSION = "RL_Template_EyeOcclusion_3D";
        public const string MATERIAL_HQ_TEARLINE = "RL_Template_Tearline_3D";
        public const string MATERIAL_HQ_HAIR = "RL_Template_Hair_3D";
        public const string MATERIAL_HQ_SCALPBASE = "RL_Template_Default_ScalpBase_3D";
        public const string MATERIAL_HQ_EYELASH = "RL_Template_Default_Eyelash_3D";
        public const string MATERIAL_HQ_TEETH = "RL_Template_Teeth_3D";
        public const string MATERIAL_HQ_TONGUE = "RL_Template_Tongue_3D";
        public const string MATERIAL_HQ_ALPHACLIP = "RL_Template_Default_AlphaClip_3D";
        public const string MATERIAL_HQ_OPAQUE = "RL_Template_Default_Opaque_3D";
        // variants
        public const string MATERIAL_HQ_CORNEA_REFRACTIVE = "RL_Template_CorneaRef_3D";

        // Default Materials
        //
        public const string MATERIAL_DEFAULT_SKIN = "RL_Template_Default_Skin_3D";
        public const string MATERIAL_DEFAULT_HEAD = "RL_Template_Default_Skin_3D";
        public const string MATERIAL_DEFAULT_CORNEA = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_DEFAULT_EYE = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_DEFAULT_EYE_OCCLUSION = "RL_Template_EyeOcclusion_3D";
        public const string MATERIAL_DEFAULT_TEARLINE = "RL_Template_Tearline_3D";
        public const string MATERIAL_DEFAULT_HAIR = "RL_Template_Default_Hair_3D";
        public const string MATERIAL_DEFAULT_SCALPBASE = "RL_Template_Default_ScalpBase_3D";
        public const string MATERIAL_DEFAULT_EYELASH = "RL_Template_Default_Eyelash_3D";
        public const string MATERIAL_DEFAULT_TEETH = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_DEFAULT_TONGUE = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_DEFAULT_ALPHACLIP = "RL_Template_Default_AlphaClip_3D";
        public const string MATERIAL_DEFAULT_OPAQUE = "RL_Template_Default_Opaque_3D";
        // variants
        public const string MATERIAL_DEFAULT_CORNEA_REFRACTIVE = "RL_Template_Default_Opaque_3D";

        // Baked Materials
        //
        public const string MATERIAL_BAKED_SKIN = "RL_Template_Baked_Skin_3D";
        public const string MATERIAL_BAKED_HEAD = "RL_Temaplte_Baked_Skin_3D";
        public const string MATERIAL_BAKED_CORNEA = "RL_Template_Baked_Cornea_3D";
        public const string MATERIAL_BAKED_EYE = "RL_Template_Baked_Eye_3D";
        public const string MATERIAL_BAKED_EYE_OCCLUSION = "RL_Template_EyeOcclusion_3D";
        public const string MATERIAL_BAKED_TEARLINE = "RL_Template_Tearline_3D";
        public const string MATERIAL_BAKED_HAIR = "RL_Template_Baked_Hair_3D";
        public const string MATERIAL_BAKED_SCALPBASE = "RL_Template_Default_ScalpBase_3D";
        public const string MATERIAL_BAKED_EYELASH = "RL_Template_Default_Eyelash_3D";
        public const string MATERIAL_BAKED_TEETH = "RL_Template_Baked_Skin_3D";
        public const string MATERIAL_BAKED_TONGUE = "RL_Template_Baked_Skin_3D";
        public const string MATERIAL_BAKED_ALPHACLIP = "RL_Template_Default_AlphaClip_3D";
        public const string MATERIAL_BAKED_OPAQUE = "RL_Template_Default_Opaque_3D";
        // variants
        public const string MATERIAL_BAKED_CORNEA_REFRACTIVE = "RL_Template_Baked_CorneaRef_3D";
        public const string MATERIAL_BAKED_EYE_CUSTOM = "RL_Template_Baked_EyeCustom_3D";
        public const string MATERIAL_BAKED_HAIR_CUSTOM = "RL_Template_Baked_HairCustom_3D";
#endif

        private static Dictionary<MaterialType, string> DICT_SHADERS = new Dictionary<MaterialType, string>
        {
            { MaterialType.Skin, SHADER_HQ_SKIN },
            { MaterialType.Head, SHADER_HQ_HEAD },
            { MaterialType.Cornea, SHADER_HQ_CORNEA },
            { MaterialType.Eye, SHADER_HQ_EYE },
            { MaterialType.EyeOcclusion, SHADER_HQ_EYE_OCCLUSION },
            { MaterialType.Tearline, SHADER_HQ_TEARLINE },
            { MaterialType.Hair, SHADER_HQ_HAIR },
            { MaterialType.Scalp, SHADER_HQ_SCALPBASE },
            { MaterialType.Eyelash, SHADER_HQ_EYELASH },
            { MaterialType.Teeth, SHADER_HQ_TEETH },
            { MaterialType.Tongue, SHADER_HQ_TONGUE },
            { MaterialType.DefaultAlpha, SHADER_HQ_ALPHACLIP },
            { MaterialType.DefaultOpaque, SHADER_HQ_OPAQUE },
        };

        private static Dictionary<MaterialType, string> DICT_MATERIALS_DEFAULT = new Dictionary<MaterialType, string>
        {
            { MaterialType.Skin, MATERIAL_DEFAULT_SKIN },
            { MaterialType.Head, MATERIAL_DEFAULT_HEAD },
            { MaterialType.Cornea, MATERIAL_DEFAULT_CORNEA },
            { MaterialType.Eye, MATERIAL_DEFAULT_EYE },
            { MaterialType.EyeOcclusion, MATERIAL_DEFAULT_EYE_OCCLUSION },
            { MaterialType.Tearline, MATERIAL_DEFAULT_TEARLINE },
            { MaterialType.Hair, MATERIAL_DEFAULT_HAIR },
            { MaterialType.Scalp, MATERIAL_DEFAULT_SCALPBASE },
            { MaterialType.Eyelash, MATERIAL_DEFAULT_EYELASH },
            { MaterialType.Teeth, MATERIAL_DEFAULT_TEETH },
            { MaterialType.Tongue, MATERIAL_DEFAULT_TONGUE },
            { MaterialType.DefaultAlpha, MATERIAL_DEFAULT_ALPHACLIP },
            { MaterialType.DefaultOpaque, MATERIAL_DEFAULT_OPAQUE },
        };

        private static Dictionary<MaterialType, string> DICT_MATERIALS_HQ = new Dictionary<MaterialType, string>
        {
            { MaterialType.Skin, MATERIAL_HQ_SKIN },
            { MaterialType.Head, MATERIAL_HQ_HEAD },
            { MaterialType.Cornea, MATERIAL_HQ_CORNEA },
            { MaterialType.Eye, MATERIAL_HQ_EYE },
            { MaterialType.EyeOcclusion, MATERIAL_HQ_EYE_OCCLUSION },
            { MaterialType.Tearline, MATERIAL_HQ_TEARLINE },
            { MaterialType.Hair, MATERIAL_HQ_HAIR },
            { MaterialType.Scalp, MATERIAL_HQ_SCALPBASE },
            { MaterialType.Eyelash, MATERIAL_HQ_EYELASH },
            { MaterialType.Teeth, MATERIAL_HQ_TEETH },
            { MaterialType.Tongue, MATERIAL_HQ_TONGUE },
            { MaterialType.DefaultAlpha, MATERIAL_HQ_ALPHACLIP },
            { MaterialType.DefaultOpaque, MATERIAL_HQ_OPAQUE },
        };

        private static Dictionary<MaterialType, string> DICT_MATERIALS_BAKED = new Dictionary<MaterialType, string>
        {
            { MaterialType.Skin, MATERIAL_BAKED_SKIN },
            { MaterialType.Head, MATERIAL_BAKED_HEAD },
            { MaterialType.Cornea, MATERIAL_BAKED_CORNEA },
            { MaterialType.Eye, MATERIAL_BAKED_EYE },
            { MaterialType.EyeOcclusion, MATERIAL_BAKED_EYE_OCCLUSION },
            { MaterialType.Tearline, MATERIAL_BAKED_TEARLINE },
            { MaterialType.Hair, MATERIAL_BAKED_HAIR },
            { MaterialType.Scalp, MATERIAL_BAKED_SCALPBASE },
            { MaterialType.Eyelash, MATERIAL_BAKED_EYELASH },
            { MaterialType.Teeth, MATERIAL_BAKED_TEETH },
            { MaterialType.Tongue, MATERIAL_BAKED_TONGUE },
            { MaterialType.DefaultAlpha, MATERIAL_BAKED_ALPHACLIP },
            { MaterialType.DefaultOpaque, MATERIAL_BAKED_OPAQUE },
        };


        public static RenderPipeline GetRenderPipeline()
        {
#if HDRP_10_5_0_OR_NEWER
            return RenderPipeline.HDRP;
#elif URP_10_5_0_OR_NEWER
            return RenderPipeline.URP;
#else
            return RenderPipeline.Builtin;
#endif
        }

        public static void ResetMaterial(Material mat)
        {
#if HDRP_10_5_0_OR_NEWER
            HDShaderUtils.ResetMaterialKeywords(mat);
#endif
        }

        public static void AddDiffusionProfilesHDRP()
        {
#if HDRP_10_5_0_OR_NEWER
            HDRenderPipelineAsset pipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            string assetPath = AssetDatabase.GetAssetPath(pipelineAsset);            

            SerializedObject hdrp = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(assetPath)[0]);
            SerializedProperty list = hdrp.FindProperty("diffusionProfileSettingsList");
            
            SerializedProperty item;
            int index;

            Object skinProfileAsset = Util.FindAsset("RL_Skin_Profile");
            Object teethProfileAsset = Util.FindAsset("RL_Teeth_Profile");

            bool addSkinProfile = true;
            bool addTeethProfile = true;
            foreach (SerializedProperty p in list)
            {
                if (p.objectReferenceValue == skinProfileAsset) addSkinProfile = false;
                if (p.objectReferenceValue == teethProfileAsset) addTeethProfile = false;
            }

            if (addSkinProfile)
            {
                index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                item = list.GetArrayElementAtIndex(index);
                item.objectReferenceValue = skinProfileAsset;
            }

            if (addTeethProfile)
            {
                index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                item = list.GetArrayElementAtIndex(index);
                item.objectReferenceValue = teethProfileAsset;
            }

            if (addSkinProfile || addTeethProfile)
                hdrp.ApplyModifiedProperties();
#endif
        }

        public static Shader GetDefaultShader()
        {
            return Shader.Find(SHADER_DEFAULT);
        }

        public static Dictionary<MaterialType, string> GetShaderDictionary()
        {            
            return DICT_SHADERS;            
        }

        public static Dictionary<MaterialType, string> GetMaterialDictionary(MaterialQuality quality)
        {
            if (quality == MaterialQuality.High) return DICT_MATERIALS_HQ;
            else if (quality == MaterialQuality.Baked) return DICT_MATERIALS_BAKED;
            return DICT_MATERIALS_DEFAULT;
        }

        public static Material GetQualityMaterial(MaterialType materialType, MaterialQuality quality, CharacterInfo info)
        {
            if (info.Generation == BaseGeneration.ActorCore) 
                return Util.FindMaterial(MATERIAL_DEFAULT_SINGLE_MATERIAL);

            // option based overrides
            if (materialType == MaterialType.Cornea && quality == MaterialQuality.High && info.qualRefractiveEyes)
                return Util.FindMaterial(MATERIAL_HQ_CORNEA_REFRACTIVE);

            if (materialType == MaterialType.Hair && quality == MaterialQuality.Baked && info.bakeCustomShaders)
                return Util.FindMaterial(MATERIAL_BAKED_HAIR_CUSTOM);

            if (materialType == MaterialType.Cornea && quality == MaterialQuality.Baked && info.qualRefractiveEyes)
                return Util.FindMaterial(MATERIAL_BAKED_CORNEA_REFRACTIVE);

            if (materialType == MaterialType.Eye && quality == MaterialQuality.Baked && info.bakeCustomShaders)
                return Util.FindMaterial(MATERIAL_BAKED_EYE_CUSTOM);

            if (materialType == MaterialType.EyeOcclusion && quality == MaterialQuality.Baked && info.bakeCustomShaders)
                return Util.FindMaterial(MATERIAL_BAKED_EYE_OCCLUSION_CUSTOM);            

            // fetch the material dictionary for this quality setting:
            Dictionary<MaterialType, string> materialDictionary = GetMaterialDictionary(quality);

            // return the material named in the dictionary...
            if (materialDictionary != null && materialDictionary.ContainsKey(materialType))
            {
                return Util.FindMaterial(materialDictionary[materialType]);
            }

            return GetDefaultMaterial(quality);
        }

        public static Material GetDefaultMaterial(MaterialQuality quality)
        {
            switch (quality)
            {
                case MaterialQuality.Baked: return Util.FindMaterial(MATERIAL_BAKED_OPAQUE);
                case MaterialQuality.High: return Util.FindMaterial(MATERIAL_HQ_OPAQUE);
                case MaterialQuality.Default: return Util.FindMaterial(MATERIAL_DEFAULT_OPAQUE);
            }

            return null;
        }

        public static Material GetTemplateMaterial(MaterialType materialType, MaterialQuality quality, CharacterInfo info)
        {
            Material template = GetQualityMaterial(materialType, quality, info);

            if (!template)
                template = GetDefaultMaterial(quality);

            if (!template)
                Debug.LogError("Unable to find Template Material for: " + materialType + "/" + quality);

            return template;
        }

        public static bool IsShaderFor(string shaderName, params MaterialType[] materialType)
        {
            Dictionary<MaterialType, string> shaderDictionary = GetShaderDictionary();

            foreach (MaterialType type in materialType)
                if (shaderName.iEndsWith(shaderDictionary[type]))
                    return true;            

            return false;
        }      
    }
}
