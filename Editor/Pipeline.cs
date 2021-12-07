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
        public const string SHADER_HQ_CORNEA_PARALLAX = "RL_CorneaShaderParallax_HDRP";
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
        // 2 pass
        public const string SHADER_HQ_HAIR_1ST_PASS = "RL_HairShaderVariantsMulti_HDRP";
        public const string SHADER_HQ_HAIR_2ND_PASS = "RL_HairShaderVariantsMulti_HDRP";

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
        // 2 pass
        public const string MATERIAL_HQ_HAIR_1ST_PASS = "RL_Template_HQ_Hair_1st_Pass_HDRP";
        public const string MATERIAL_HQ_HAIR_2ND_PASS = "RL_Template_HQ_Hair_2nd_Pass_HDRP";

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
        public const string MATERIAL_BAKED_HEAD = "RL_Template_Baked_Skin_HDRP";              
        public const string MATERIAL_BAKED_CORNEA = "RL_Template_Baked_Cornea_HDRP";                
        public const string MATERIAL_BAKED_EYE = "RL_Template_Baked_Eye_HDRP";       
        public const string MATERIAL_BAKED_EYE_OCCLUSION = "RL_Template_Baked_EyeOcclusion_HDRP";
        public const string MATERIAL_BAKED_TEARLINE = "";
        public const string MATERIAL_BAKED_HAIR = "RL_Template_Baked_Hair_HDRP";        
        public const string MATERIAL_BAKED_SCALPBASE = "";
        public const string MATERIAL_BAKED_EYELASH = "";
        public const string MATERIAL_BAKED_TEETH = "RL_Template_Baked_Skin_HDRP";
        public const string MATERIAL_BAKED_TONGUE = "RL_Template_Baked_Skin_HDRP";
        public const string MATERIAL_BAKED_ALPHACLIP = "";
        public const string MATERIAL_BAKED_OPAQUE = "";
        // variants
        public const string MATERIAL_BAKED_CORNEA_CUSTOM = "RL_Template_Baked_CorneaCustom_HDRP";
        public const string MATERIAL_BAKED_CORNEA_REFRACTIVE = "RL_Template_Baked_CorneaRef_HDRP";        
        public const string MATERIAL_BAKED_EYE_CUSTOM = "RL_Template_Baked_EyeCustom_HDRP";
        public const string MATERIAL_BAKED_EYE_OCCLUSION_CUSTOM = "RL_Template_Baked_EyeOcclusionCustom_HDRP";
        public const string MATERIAL_BAKED_HAIR_CUSTOM = "RL_Template_Baked_HairCustom_HDRP";
        // 2 pass        
        public const string MATERIAL_BAKED_HAIR_1ST_PASS = "RL_Template_Baked_Hair_1st_Pass_HDRP";
        public const string MATERIAL_BAKED_HAIR_2ND_PASS = "RL_Template_Baked_Hair_2nd_Pass_HDRP";
        public const string MATERIAL_BAKED_HAIR_CUSTOM_1ST_PASS = "RL_Template_Baked_HairCustom_1st_Pass_HDRP";
        public const string MATERIAL_BAKED_HAIR_CUSTOM_2ND_PASS = "RL_Template_Baked_HairCustom_2nd_Pass_HDRP";
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
        // 2 pass
        public const string SHADER_HQ_HAIR_1ST_PASS = "RL_HairShaderVariants_1st_Pass_URP";
        public const string SHADER_HQ_HAIR_2ND_PASS = "RL_HairShaderVariants_2nd_Pass_URP";

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
        // 2 pass
        public const string MATERIAL_HQ_HAIR_1ST_PASS = "RL_Template_HQ_Hair_1st_Pass_URP";
        public const string MATERIAL_HQ_HAIR_2ND_PASS = "RL_Template_HQ_Hair_2nd_Pass_URP";

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
        public const string MATERIAL_BAKED_EYE = "RL_Template_Default_Opaque_URP";
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
        public const string MATERIAL_BAKED_CORNEA_CUSTOM = "RL_Template_Baked_CorneaCustom_URP";
        public const string MATERIAL_BAKED_CORNEA_REFRACTIVE = "RL_Template_Default_Opaque_URP";
        public const string MATERIAL_BAKED_EYE_CUSTOM = "RL_Template_Default_Opaque_URP";
        public const string MATERIAL_BAKED_EYE_OCCLUSION_CUSTOM = "RL_Template_Baked_EyeOcclusionCustom_URP";
        public const string MATERIAL_BAKED_HAIR_CUSTOM = "RL_Template_Baked_HairCustom_URP";
        // 2 pass
        public const string MATERIAL_BAKED_HAIR_1ST_PASS = "RL_Template_Baked_Hair_1st_Pass_URP";
        public const string MATERIAL_BAKED_HAIR_2ND_PASS = "RL_Template_Baked_Hair_2nd_Pass_URP";
        // for gamebase single material or actor core...
        public const string MATERIAL_DEFAULT_SINGLE_MATERIAL = "RL_Template_Default_SingleMaterial_URP";
#else
        // Shaders
        //
        public const string SHADER_DEFAULT = "Standard";
        public const string SHADER_DEFAULT_HAIR = "Standard";
        //
        public const string SHADER_HQ_SKIN = "RL_SkinShaderVariants_3D";
        public const string SHADER_HQ_HEAD = "RL_SkinShaderVariants_3D";
        public const string SHADER_HQ_CORNEA = "RL_CorneaShader_3D";
        public const string SHADER_HQ_EYE = "Standard";
        public const string SHADER_HQ_EYE_OCCLUSION = "RL_EyeOcclusionShader_3D";
        public const string SHADER_HQ_TEARLINE = "RL_TearlineShader_3D";
        public const string SHADER_HQ_HAIR = "RL_HairShaderVariants_3D";        
        public const string SHADER_HQ_SCALPBASE = "Standard";
        public const string SHADER_HQ_EYELASH = "Standard";
        public const string SHADER_HQ_TEETH = "RL_TeethShader_3D";
        public const string SHADER_HQ_TONGUE = "RL_TongueShader_3D";
        public const string SHADER_HQ_ALPHACLIP = "Standard";
        public const string SHADER_HQ_OPAQUE = "Standard";
        // 2 pass
        public const string SHADER_HQ_HAIR_1ST_PASS = "RL_HairShaderVariants_1st_Pass_3D";
        public const string SHADER_HQ_HAIR_2ND_PASS = "RL_HairShaderVariants_2nd_Pass_3D";

        // HQ Materials
        //
        public const string MATERIAL_HQ_SKIN = "RL_Template_HQ_Skin_3D";
        public const string MATERIAL_HQ_HEAD = "RL_Template_HQ_Head_3D";
        public const string MATERIAL_HQ_CORNEA = "RL_Template_HQ_Cornea_3D";
        public const string MATERIAL_HQ_EYE = "RL_Template_HQ_Eye_3D";
        public const string MATERIAL_HQ_EYE_OCCLUSION = "RL_Template_HQ_EyeOcclusion_3D";
        public const string MATERIAL_HQ_TEARLINE = "RL_Template_HQ_Tearline_3D";
        public const string MATERIAL_HQ_HAIR = "RL_Template_HQ_Hair_3D";
        public const string MATERIAL_HQ_SCALPBASE = "RL_Template_Default_ScalpBase_3D";
        public const string MATERIAL_HQ_EYELASH = "RL_Template_Default_Eyelash_3D";
        public const string MATERIAL_HQ_TEETH = "RL_Template_HQ_Teeth_3D";
        public const string MATERIAL_HQ_TONGUE = "RL_Template_HQ_Tongue_3D";
        public const string MATERIAL_HQ_ALPHACLIP = "RL_Template_Default_AlphaClip_3D";
        public const string MATERIAL_HQ_OPAQUE = "RL_Template_Default_Opaque_3D";
        // variants
        public const string MATERIAL_HQ_CORNEA_REFRACTIVE = "RL_Template_HQ_CorneaRef_3D";
        // 2 pass
        public const string MATERIAL_HQ_HAIR_1ST_PASS = "RL_Template_HQ_Hair_1st_Pass_3D";
        public const string MATERIAL_HQ_HAIR_2ND_PASS = "RL_Template_HQ_Hair_2nd_Pass_3D";

        // Default Materials
        //
        public const string MATERIAL_DEFAULT_SKIN = "RL_Template_Default_Skin_3D";
        public const string MATERIAL_DEFAULT_HEAD = "RL_Template_Default_Skin_3D";
        public const string MATERIAL_DEFAULT_CORNEA = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_DEFAULT_EYE = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_DEFAULT_EYE_OCCLUSION = "RL_Template_Default_EyeOcclusion_3D";
        public const string MATERIAL_DEFAULT_TEARLINE = "RL_Template_Default_Tearline_3D";
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
        public const string MATERIAL_BAKED_EYE = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_BAKED_EYE_OCCLUSION = "RL_Template_Baked_EyeOcclusion_3D";
        public const string MATERIAL_BAKED_TEARLINE = "RL_Template_Tearline_3D";
        public const string MATERIAL_BAKED_HAIR = "RL_Template_Baked_Hair_3D";
        public const string MATERIAL_BAKED_SCALPBASE = "RL_Template_Default_ScalpBase_3D";
        public const string MATERIAL_BAKED_EYELASH = "RL_Template_Default_Eyelash_3D";
        public const string MATERIAL_BAKED_TEETH = "RL_Template_Baked_Skin_3D";
        public const string MATERIAL_BAKED_TONGUE = "RL_Template_Baked_Skin_3D";
        public const string MATERIAL_BAKED_ALPHACLIP = "RL_Template_Default_AlphaClip_3D";
        public const string MATERIAL_BAKED_OPAQUE = "RL_Template_Default_Opaque_3D";
        // variants
        public const string MATERIAL_BAKED_CORNEA_CUSTOM = "RL_Template_Baked_CorneaCustom_3D";
        public const string MATERIAL_BAKED_CORNEA_REFRACTIVE = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_BAKED_EYE_CUSTOM = "RL_Template_Default_Opaque_3D";
        public const string MATERIAL_BAKED_EYE_OCCLUSION_CUSTOM = "RL_Template_Baked_EyeOcclusionCustom_3D";
        public const string MATERIAL_BAKED_HAIR_CUSTOM = "RL_Template_Baked_HairCustom_3D";
        // 2 pass
        public const string MATERIAL_BAKED_HAIR_1ST_PASS = "RL_Template_Baked_Hair_1st_Pass_3D";
        public const string MATERIAL_BAKED_HAIR_2ND_PASS = "RL_Template_Baked_Hair_2nd_Pass_3D";
        // for gamebase single material or actor core...
        public const string MATERIAL_DEFAULT_SINGLE_MATERIAL = "RL_Template_Default_SingleMaterial_3D";
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

        public static RenderPipeline RP => GetRenderPipeline();
        public static bool is3D => RP == RenderPipeline.Builtin;
        public static bool isURP => RP == RenderPipeline.URP;
        public static bool isHDRP => RP == RenderPipeline.HDRP;


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
#if HDRP_12_0_0_OR_NEWER
            RenderPipelineGlobalSettings pipelineSettings = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
            if (!pipelineSettings) return;
            string assetPath = AssetDatabase.GetAssetPath(pipelineSettings);
#else
            HDRenderPipelineAsset pipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (!pipelineAsset) return;
            string assetPath = AssetDatabase.GetAssetPath(pipelineAsset);
#endif

            SerializedObject hdrp = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(assetPath)[0]);
            if (hdrp == null) return;
            SerializedProperty list = hdrp.FindProperty("diffusionProfileSettingsList");
            if (list == null) return;

            SerializedProperty item;
            int index;

            Object skinProfileAsset = Util.FindAsset("RL_Skin_Profile");
            Object teethProfileAsset = Util.FindAsset("RL_Teeth_Profile");
            Object eyeProfileAsset = Util.FindAsset("RL_Eye_Profile");

            if (!skinProfileAsset || !teethProfileAsset || !eyeProfileAsset) return;

            bool addSkinProfile = true;
            bool addTeethProfile = true;
            bool addEyeProfile = true;
            foreach (SerializedProperty p in list)
            {
                if (p.objectReferenceValue == skinProfileAsset) addSkinProfile = false;
                if (p.objectReferenceValue == teethProfileAsset) addTeethProfile = false;
                if (p.objectReferenceValue == eyeProfileAsset) addEyeProfile = false;
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

            if (addEyeProfile)
            {
                index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                item = list.GetArrayElementAtIndex(index);
                item.objectReferenceValue = eyeProfileAsset;
            }

            if (addSkinProfile || addTeethProfile || addEyeProfile)
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

            if (quality == MaterialQuality.High) // option overrides for high quality materials
            {
                if (info.qualRefractiveEyes)
                {
                    // HQ refractive eyes have transparent refractive cornea
                    if (materialType == MaterialType.Cornea)
                        return Util.FindMaterial(MATERIAL_HQ_CORNEA_REFRACTIVE);
                }
                else
                {
                    // HQ parallax eyes doesn't use Eye material (set to default PBR)
                    if (materialType == MaterialType.Eye)
                        return Util.FindMaterial(MATERIAL_DEFAULT_OPAQUE);                    
                }
            }
            else if (quality == MaterialQuality.Baked) // option overrides for baked materials
            {
                if (info.bakeCustomShaders)
                {
                    if (info.qualRefractiveEyes)
                    {
                        // custom baked refractive eyes need vertex displacement for iris depth
                        if (materialType == MaterialType.Eye)
                            return Util.FindMaterial(MATERIAL_BAKED_EYE_CUSTOM);
                    }
                    else
                    {
                        // custom baked parallax eyes use UV parallax effect in cornea
                        if (materialType == MaterialType.Cornea)
                            return Util.FindMaterial(MATERIAL_BAKED_CORNEA_CUSTOM);
                    }

                    // custom baked hair uses vertex color blending
                    if (materialType == MaterialType.Hair)
                        return Util.FindMaterial(MATERIAL_BAKED_HAIR_CUSTOM);
                                        
                    // custom baked eye occlusion for vertex displacement
                    if (materialType == MaterialType.EyeOcclusion)
                        return Util.FindMaterial(MATERIAL_BAKED_EYE_OCCLUSION_CUSTOM);
                }                

                if (info.qualRefractiveEyes)
                {
                    // custom or not, baked refractive cornea is the same (cornea transparency)
                    if (materialType == MaterialType.Cornea)
                        return Util.FindMaterial(MATERIAL_BAKED_CORNEA_REFRACTIVE);
                }
            }

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

        public static Material GetCustomTemplateMaterial(string templateName, MaterialQuality quality)
        {
            Material template = Util.FindMaterial(templateName);

            if (!template)
                template = GetDefaultMaterial(quality);
            
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
