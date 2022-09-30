
**HDRP12 Shader Versions**:
	(Depth Prepass/Postpass Settings do not convert correctly from HDRP10)
	RL_HairShader_Baked_HDRP12
	RL_HairShader_Variants_HDRP12

**HDRP12 Material Versions**:
	RL_Template_Baked_HairCustom_HDRP12
	RL_Template_HQ_Hair_HDRP12

**Update HDRP12 Shaders**
	run Update HDRP12 Shaders.bat
		edit HDRP12 hair shaders: tick prepass and postpass options, untick "preserve specular lighting"



**HDRP12 Tesssellation Shaders**:
	(Only HDRP12+ supports shader graph tessellation)
	RL_CorneaShaderParallax_Baked_HDRP12_Tessellation
	RL_CorneaShaderParallax_HDRP12_Tessellation
	RL_HairShader_Baked_HDRP12_Tessellation
	RL_HairShader_Variants_HDRP12_Tessellation
	RL_HairShader_MultiPass_Baked_HDRP12_Tessellation
	RL_HairShader_MultiPass_Variants_HDRP12_Tessellation
	RL_SkinShader_Variants_HDRP12_Tessellation
	RL_TeethShader_HDRP12_Tessellation
	RL_TongueShader_HDRP12_Tessellation

**Custom materials with Tessellation enabled**:
	RL_Template_Baked_CorneaParallaxCustom_HDRP12_T
	RL_Template_Baked_HairCustom_1st_Pass_HDRP12_T
	RL_Template_Baked_HairCustom_2nd_Pass_HDRP12_T
	RL_Template_Baked_HairCustom_HDRP12_T
	RL_Template_HQ_CorneaParallax_HDRP12_T
	RL_Template_HQ_Hair_1st_Pass_HDRP12_T
	RL_Template_HQ_Hair_2nd_Pass_HDRP12_T
	RL_Template_HQ_Hair_HDRP12_T
	RL_Template_HQ_Head_HDRP12_T
	RL_Template_HQ_Skin_HDRP12_T
	RL_Template_HQ_Teeth_HDRP12_T
	RL_Template_HQ_Tongue_HDRP12_T

**Default shader (HDRP/Lit) materials with Tessellation enabled**:
	RL_Template_Baked_CorneaRefractiveCustom_HDRP_T
	RL_Template_Baked_EyeOcclusion_HDRP_T
	RL_Template_Baked_Skin_HDRP_T
	RL_Template_Default_AlphaClip_HDRP_T
	RL_Template_Default_Eyelash_HDRP_T
	RL_Template_Default_Opaque_HDRP_T
	RL_Template_Default_ScalpBase_HDRP_T
	RL_Template_Default_SingleMaterial_HDRP_T

**Update HDRP12 Tessellation Shaders**
	run Update Tessellation Shaders.bat:
		for each tessellation shader:
			enable tessellation
			change to Phong
			add float parameter: Tessellation Factor/_TessellationFactor/default 0/0 to 32/Slider
				connect to vertex shader input

