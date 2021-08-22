Changelog
=========

### v 0.1.7
- Reduced memory use for asset searches and character discovery.
- Cached key character lookup data, should improve importer window memory use and responsiveness.

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

### TODO
- Tiling and offset when baking textures?...
- Refresh button on tools window.
- Back button in the import window.