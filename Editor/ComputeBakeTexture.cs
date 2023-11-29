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
using UnityEditor;
using UnityEngine;

namespace Reallusion.Import
{
    public class ComputeBakeTexture
    {
        private readonly RenderTexture renderTexture;
        private readonly Texture2D saveTexture;
        public readonly int width;
        public readonly int height;        
        public readonly string folderPath;
        public readonly string textureName;
        public readonly int flags;

        private const string COMPUTE_SHADER_RESULT = "Result";

        public bool IsRGB { get { return (flags & Importer.FLAG_SRGB) > 0; } }
        public bool IsNormal { get { return (flags & Importer.FLAG_NORMAL) > 0; } }
        public bool IsAlphaClip { get { return (flags & Importer.FLAG_ALPHA_CLIP) > 0; } }
        public bool IsHair { get { return (flags & Importer.FLAG_HAIR) > 0; } }
        public bool IsAlphaData { get { return (flags & Importer.FLAG_ALPHA_DATA) > 0; } }

        public ComputeBakeTexture(Vector2Int size, string folder, string name, int flags = 0)
        {
            width = size.x;
            height = size.y;
            this.flags = flags;
            // RenderTextureReadWrite.sRGB/Linear is ignored:
            //     on windows platforms it is always linear
            //     on MacOS platforms it is always gamma
            renderTexture = new RenderTexture(width, height, 0,
                RenderTextureFormat.ARGB32,
                IsRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            saveTexture = new Texture2D(width, height, TextureFormat.ARGB32, true, !IsRGB);
            folderPath = folder;
            textureName = name;
        }

        private string WritePNG()
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), folderPath, textureName + ".png");

            Util.EnsureAssetsFolderExists(folderPath);

            byte[] pngArray = saveTexture.EncodeToPNG();

            File.WriteAllBytes(filePath, pngArray);

            string assetPath = Util.GetRelativePath(filePath);
            if (File.Exists(filePath)) return assetPath;
            else return "";
        }

        private void ApplyImportSettings(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                AssetDatabase.Refresh();

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
                if (importer)
                {                    
                    importer.textureType = IsNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    importer.sRGBTexture = IsRGB;
                    importer.alphaIsTransparency = IsRGB && !IsAlphaData;
                    importer.maxTextureSize = 4096;
                    importer.mipmapEnabled = true;
                    importer.mipMapBias = Importer.MIPMAP_BIAS;
                    if (IsHair)
                    {                        
                        importer.mipMapsPreserveCoverage = true;
                        importer.alphaTestReferenceValue = Importer.MIPMAP_ALPHA_CLIP_HAIR_BAKED;
                    }         
                    else if (IsAlphaClip)
                    {                        
                        importer.mipMapsPreserveCoverage = true;
                        importer.alphaTestReferenceValue = 0.5f;
                    }
                    else
                    {
                        importer.mipMapsPreserveCoverage = false;
                    }
                    importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
                    if ((flags & Importer.FLAG_READ_WRITE) > 0)
                    {
                        importer.isReadable = true;
                    }
                    if ((flags & Importer.FLAG_UNCOMPRESSED) > 0)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.compressionQuality = 0;
                    }
                    importer.SaveAndReimport();
                    //AssetDatabase.WriteImportSettingsIfDirty(filePath);
                }
            }
        }

        public void Create(ComputeShader shader, int kernel)
        {
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            shader.SetTexture(kernel, COMPUTE_SHADER_RESULT, renderTexture);
        }

        public Texture2D SaveAndReimport()
        {
            // copy the GPU render texture to a real texture2D
            RenderTexture old = RenderTexture.active;
            RenderTexture.active = renderTexture;
            saveTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            saveTexture.Apply();
            RenderTexture.active = old;

            string filePath = WritePNG();
            ApplyImportSettings(filePath);

            Texture2D assetTex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            return assetTex;
        }
    }
}
