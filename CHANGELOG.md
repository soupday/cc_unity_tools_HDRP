Changelog
=========

### v 1.4.3
- Tries to untangle instalod duplication suffixes on accessories.
    - inc. Script error fix that was blocking build materials when instalod meshes were not found in Json data.
- Cast shadows removed from Tearline, Eye occlusion and scalp meshes.
- Stand alone shader packages for the various pipelines included in /Packages/ folder.

### v 1.4.2
- OSX and Linux file path support.
- Tweaked character model importer settings for better normal import and blend shape normal generation. Should reduce mesh smoothing issues.
- Disabled Menu Mesh Tools when not working on a generated prefab. (The tools cannot operate directly on an FBX)
- Fixed Bake not replacing materials with baked versions.
- Eye Occlusion shaders factor in Occlusion Color alpha value.

### v 1.4.1
- Traditional material glossiness fix.
- High poly (sub-division) hair mesh extraction fix.
- SSS Diffusion profile added.
- Remembers last used lighting preset on character preview change.
- Character icon list side bar can be dragged into a more compact list view.
    - Character list sorted alphabetically.
- When HDRP Ray tracing is enabled:
    - The material build function will turn off ray tracing on the Scalp (when separeated), Eye Occlusion and Tearline meshes as it causes darkening artifacts on the underlying skin and eye surfaces.
    - (Typically the scalp is only separated from the hair materials when two-pass hair is enabled)
- Bake Hair function now only bakes the result of the 'Enable Color' properties into the diffuse maps of the hair materials. Press again to revert to original diffuse maps.
- Improved ActorCore and ActorBuild single material detection.

### v 1.4.0
- Import & Setup
    - ActorBuild detection separated from ActorCore (ActorBuild can have more advanced materials)
    - Fix to Instalod merged material characters detection.
    - Better detection of non-standard characters.
        - Rig animation type override (Humanoid / Generic) for non-standard characters.
    - Bone LOD characters reduced skeleton avatar generation fixed.
    - Animation player chooses from available animations when none found in character.
    - Blender to Unity imports fix for bounding box root bone issue.
    - Character Tree view shows all child renderer meshes regardless of depth.
- Materials & Shaders
    - Lit SSS shader added for URP and 3D pipelines.
    - SSS material support added.
    - Traditional material support added.
    - Shader properties arrangement pass.
    - Material setup fix for non character objects (e.g. props from iClone)
    - Additional missing texture checks added to character bake function.
    - Amplify shaders recompiled with ASE 1.9.1.2.
    - Limbus dark ring corrected, iris should look brighter, clearer and less fuzzy at the edges.
- Other
    - LOD combining function added.
        - Characters and LOD variants must be in same folder.
    - New icons added.
    - To maintain consistency across Unity versions, the HDRP package will be split into 2 versions:
        - HDRP10 for the High Definition Render Pipeline version 10 and 11: Unity 2020.3 upto 2021.1
        - HDRP12 for the High Definition Render Pipeline version 12 and 13: Unity 2021.2 upwards.

### v 1.3.9
- Due to shader incompatibilities between URP10 and URP12, the URP package must be split into 2 versions:
    - URP10 for the Univeral Render Pipeline version 10 and 11: Unity 2020.3 upto 2021.1
    - URP12 for the Univeral Render Pipeline version 12 and 13: Unity 2021.2 upwards.

### v 1.3.8
- Added tessellation shaders and templates to 3D and URP pipelines.
- Upgraded to Amplify 1.9 shaders in 3D and URP pipelines.
- Fixed some issues with hair shaders in 3D and URP.
- Updated Hair shaders and eye shaders.
- Added Hair Bake preview button.
- Updated preview scene lighting presets, should all look roughly the same lighting across all three pipelines.

### v 1.3.7
- Backported function to mid release 2020.3 and 2021.1 SaveAssetIfDirty removed.
- GUIDFromAssetPath Unity 2019 fix for built-in pipeline.

### v 1.3.6
- Runtime components Build fix.

### v 1.3.5
- Fixes
    - Collider manager now remembers the currently selected collider.
    - Collider manager editor now has rotation adjustment sliders.
    - Animation Player is closed on entering Play mode.
    - Save and Recall physics settings now works correctly in Editor mode and remembers settings per character.

### v 1.3.4
- Cloth physics added
    - Colliders generated from character JSON data.
    - Weightmaps used to generate cloth coefficients.
    - Runtime helper scripts to adjust colliders, weight maps and cloth settings in play mode for real time feedback.
    - Save and recall feature, so play mode changes can be retained (currently only one character's settings can be saved)
    - Optimized collider detection.
    - Collider margin adjust and weightmap based detection threshold settings.

### v 1.3.3
- Updates / Additions:
    - Tessellation option added to build settings. (Currently HDRP12 Only)
    - Lighting presets added to preview scene, and lighting cycle button added to importer window. (All pipelines)
    - Lock retarget values toggle added. (To no longer reset on changing animation)
    - Animation Processor (Importer Window) now also extracts fully facial expression retargeted animations from FBX.
    - Animation Player and Retargeter can now work in any Editor scene. (Note: the character being animated will show at the origin of it's current parent)
    - Match camera to scene view button added to importer window.
- Shader / Material adjustments:
    - HDRP12 Tessellation shaders added.
    - HDRP hair 2-pass shadow clip fixes.
    - HDRP hair no longer uses flipped back lighting by default. (Less shadow artifacts and rim lighting works much better without it)
    - Hair shader smoothness and specular tweaks. (Should look more consistent between the 3 pipelines)
    - Adjustments to Eye and Eye occlusion colors and saturation to more accurately match the coloring in CC3/4.
    - Facial hair naterials are now generated with thinner opacity and less smoothness and specularity. (Should no longer look too thick and too shiny)
    - Compute bake shaders updated with shader changes.
- Fixes to:
    - Facial expression retargeter:
        - Now correctly maps based on the source facial profile as well as the target profile.
        - Includes all tongue blendshapes.
        - Correctly remaps compatible viseme profiles. (8+7 split visemes cannot be retargeted to the 15 direct visemes).
        - Button now color coded to the estimated quality of the retarget:
            - Green - should retarget completely.
            - Yellow - some BlendShapes will not restarget.
            - Red - most BlendShapes will not retarget.
        - A report of the retarget, listing how the BlendShapes are remapped and which could not be retarget is logged to the console.
    - Animation player and retargetter UI and internal logic, should be less glitchy.
    - No longer reprocesses the same textures when used by different materials during material build.
    - Character prefab no longer gets stuck in current Animation Player pose after recompiling editor assembly.
    - Numerous fixes to internal workings and code refactors.

### v 1.3.2
- HDRP12 (Unity 2021.2+) Single pass hair shader fixes.
- HDRP Skin SSS transmission thickness min-max remap parameters.

### v 1.3.1
- Arm Flexion correction added.

### v 1.3.0
- Animation correction and facial expression retargeting system added.
    - Correction sliders for shoulders, arms, legs, heels and height.
    - Fix open mouth in 3rd party animations for target CC3/4 character.
    - Retarget facial expression animation tracks to target CC3/4 character.
- Alembic material setup feature added. (Unity Alembic package must be installed)

### v 1.2.3
- Skin diffuse Color modifier color-space correction.
- Hair diffuse Color modifier added.
- ActorBuild and ActorScan characters from CC4 correctly detected and set to Humanoid Rig.

### v 1.2.2
- Skin Diffuse Color modifier added.
- Built-in pipeline version reduced minimum version to 2019.4.11f1.

### v 1.2.1
- Fix to Subsurface modifiers.
- Iris Color and Iris Cloudy Color added to eye parameters.
- Moved menu name from **CC3** to **Reallusion**.

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