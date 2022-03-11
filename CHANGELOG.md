Changelog
=========

### v 1.2.0
- Blender Tools to Unity pipeline implemented.
    - Using compute shaders for baking Unity packed textures from base Blender textures.
    - Eye Occlusion and Tearline shaders updated for Blender model space.
- Diffuse Color modifier correctly converted from linear to sRGB color space.

### v 1.1.1
- Amplify Shaders added.
- Amplify Baked material shaders added.
- Inclusion of MSAA coverage alpha blending hair shaders. (Amplify - URP/3D only)
- Bake Flow-To-Normal Converter for hair materials.
- Basic/Default material pass.
- Eye subsurface mask bake fix.
- HDRP thickness bake fix.
- Cornea detail mask bake fix.
- Reworked "AO Occlude All" in non-amplify hair shaders.
- Template Clean up.

### v 1.0.0
- Animation & Expression preview system.
- Eye Brightness, Saturation, Occlusion sharpness changes.
- Eye Occlusion colour changed to lit multiply blend. (Should no longer glow in the dark.)
- Parallax eyes shaders updated.
- Bake shaders updated.

### v 0.3.0
- Parallax eye shader + baking shaders.
- Fix to open/close mouth and eyes for gamebase and LOD characters.

### v 0.2.2
- Skin subsurface scattering fix for Unity 2021.1

### v 0.2.1
- Fix to 2-Pass mesh extraction crashing when no vertex colors were present.
- Fix to Bump maps being incorrectly detected during material and baking setups.
- Added normal strength calculations to built-in hair shaders.

### v 0.2.0
- Fix to eye occlusion compute bake shader.
- Adusted eye shader corner shadow gradient.
- Teeth, Tongue & Eye micro normals maps correctly set to normal maps.
- Added two pass hair material system, whereby all hair submeshes are extracted into single meshes and multiple materials can by applied to each hair mesh.
  Thus creating a two (or more) pass rendering system for hair that is fully compliant with SRP batching.
- **2021.2**
    - 2021.2 uses HDRP pipeline 12.0.0, which has some incompatibilities with previous versions:
    - Fix to correctly add diffusion profiles in HDRP 12.0.0.
    - Updates to hair shaders to correctly apply depth pre-pass and post-pass.
- **Built-in Pipeline**
    - Added emission to the Built-in high quality and baked shaders.
    - Removed post processing settings from the preview scene, as they caused errors in later versions.

### v 0.1.9
- **URP & Built-in Pipelines**
    - Added shaders and materials to use the build-in and Universal render pipeline.
- **Code base**
    - Bake shaders and materials updated to support Standard shader and URP/Lit shader.
    - Fixes to material setup texture discovery.

### v 0.1.8b
- Fix to baking functions with missing textures.

### v 0.1.8
- After applying materials or baking materials in the tool window, the generated prefab is selected in the project window.
- Characters in the preview window are replaced with the generated Prefab after material setup.
- Fixed baking LOD Group character materials.
- Baking LOD Group characters now also creates LOD Group prefab.

### v 0.1.7
- Reduced memory use for asset searches and character discovery.
- Prevented Import Tool window from holding on to character object references and hogging all the memory.
- Right Click menu "Import Character" now opens Import Tool window *only* for that character.
- Added refresh button on Import Tool window to rebuild the character list for when characters are added or removed.

### v 0.1.6
- Added custom diffusion profiles.

### v 0.1.5
- Reworked and cleaned up Teeth and Tongue shaders & bake shaders.
- Fix to editor code .asmdef preventing build for non editor platforms.

### v 0.1.4
- Reworked eye occlusion calculations.

### v 0.1.3
- Normal strength settings for all relevant custom shaders.
- All normal map, micro normal map and blend normal map textures default to Bump mode when empty.
- Emission texture and Emissive color properties added to all custom shaders.
- Fix localization issue with number conversion reading from Json data.
- Logs errors when Json data fails to read.
- Improved alpha channel manipulation in Hair shader.

### v 0.1.1
- Bake button will not activate until the character has been imported and set-up with High-Quality materials.
- Animation button will not activate until the character has been imported and set-up with either Default or High-Quality materials.

### v 0.1.0
- Initial release.