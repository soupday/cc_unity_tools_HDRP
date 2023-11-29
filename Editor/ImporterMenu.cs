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

//#define SOUPDEV

using UnityEngine;
using UnityEditor;
using System.IO;

namespace Reallusion.Import
{
    public class ImporterMenu : Editor
    {        
        [MenuItem("Reallusion/Import Characters", priority = 1)]
        public static void InitCC3ImportGUI()
        {
            ImporterWindow.Init(ImporterWindow.Mode.multi, Selection.activeObject);
        }

        [MenuItem("Reallusion/Animation Player", priority = 2)]
        public static void ShowAnimationPlayer()
        {
            if (AnimPlayerGUI.IsPlayerShown())
            {
                WindowManager.HideAnimationPlayer(true);
            }
            else
            {
                WindowManager.ShowAnimationPlayer();
            }
        }

        [MenuItem("Reallusion/Animation Player", true)]
        public static bool ValidateShowAnimationPlayer()
        {
            //return PreviewScene.GetPreviewScene().IsValid && AnimPlayerGUI.IsPlayerShown();
            return true;
        }

        [MenuItem("Reallusion/Animation Retargeter", priority = 3)]
        public static void ShowAnimationRetargeter()
        {
            if (AnimRetargetGUI.IsPlayerShown())
            {
                WindowManager.HideAnimationRetargeter(true);
            }
            else
            {
                if (AnimPlayerGUI.IsPlayerShown())
                    WindowManager.ShowAnimationRetargeter();
            }
        }

#if HDRP_10_5_0_OR_NEWER
        [MenuItem("Reallusion/Misc Tools/Add HDRP Diffusion Profiles", priority = 180)]
        private static void DoAddDiffusionProfiles()
        {
            Pipeline.AddDiffusionProfilesHDRP();
        }
#endif

        [MenuItem("Reallusion/Animation Retargeter", true)]
        public static bool ValidateShowAnimationRetargeter()
        {
            return WindowManager.IsPreviewScene && AnimPlayerGUI.IsPlayerShown();
        }

        /*
        [MenuItem("Assets/Reallusion/Import Character (Single Character Mode)", priority = 2000)]
        public static void InitAssetCC3ImportGUI()
        {
            ImporterWindow.Init(ImporterWindow.Mode.single, Selection.activeObject);
        }
         
        [MenuItem("Assets/Reallusion/Import Character (Single Character Mode)", true)]
        public static bool ValidateInitAssetCC3ImportGUI()
        {
            if (Util.IsCC3Character(Selection.activeObject)) return true;
            return false;
        }*/
        
        // Scene Tools
        //

        [MenuItem("Reallusion/Preview Scene Tools/Match Scene Camera", priority = 210)]
        public static void DoMatchSceneCameraOnce()
        {
            WindowManager.DoMatchSceneCameraOnce();
        }

        [MenuItem("Reallusion/Preview Scene Tools/Match Scene Camera (Toggle)", priority = 211)]
        public static void DoMatchSceneCamera()
        {
            WindowManager.DoMatchSceneCamera();
        }

        [MenuItem("Reallusion/Preview Scene Tools/Match Scene Camera (Toggle)", true)]
        private static bool ValidateDoMatchSceneCamera()
        {            
            return WindowManager.IsPreviewScene;
        }

        [MenuItem("Reallusion/Preview Scene Tools/Orbit Scene View (Toggle)", priority = 212)]
        public static void DoOrbitSceneView()
        {
            WindowManager.DoSceneViewOrbit();
        }

        [MenuItem("Reallusion/Preview Scene Tools/Orbit Scene View (Toggle)", true)]
        private static bool ValidateDoOrbitSceneView()
        {
            return WindowManager.IsPreviewScene;
        }

        [MenuItem("Reallusion/Preview Scene Tools/Toggle All Scene Effects Off", priority = 230)]
        public static void DoToggleOff()
        {
            WindowManager.DoSceneToggleOffAll();
        }

        [MenuItem("Reallusion/Preview Scene Tools/Screenshot", priority = 250)]
        public static void DoScreenShot()
        {
            WindowManager.TakeScreenShot();
        }  

#if SOUPDEV        

        [MenuItem("Reallusion/Dev/Bake Gradient", priority = 220)]
        public static void DoTest()
        {
            CharacterInfo ci = ImporterWindow.Current.Character;
            ComputeBake baker = new ComputeBake(ci.Fbx, ci);
            Texture2D gradient = baker.BakeGradientMap("Assets" + Path.DirectorySeparatorChar + "Test", "Gradient");
        }

        [MenuItem("Reallusion/Dev/Color Convert", priority = 220)]
        public static void DoCC()
        {
            // Aqua
            Color c = new Color(34f / 255f, 47f / 255f, 46f / 255f);
            // Authority
            //Color c = new Color(10f / 255f, 21f / 255f, 29f / 255f);
            
            Debug.Log(Util.LinearTosRGB(c));
        }

        [MenuItem("Reallusion/Dev/Channel Pack 1A", priority = 220)]
        public static void DoSet1A()
        {            
            CharacterInfo ci = ImporterWindow.Current.Character;
            ComputeBake baker = new ComputeBake(ci.Fbx, ci);
            string[] folders = new string[] { "Assets", "Packages" };

            // Wrinkle Mask Set 1A:
            //
            // R: head_wm1_normal_head_wm1_blink_L / R              head_wm1_msk_01: R + G
            // G: head_wm1_normal_head_wm1_browRaiseInner_L / R     head_wm1_msk_01: B + A
            // B: head_wm1_normal_head_wm1_purse_DL / R             head_wm1_msk_03: G + B
            // A: head_wm1_normal_head_wm1_purse_UL / R             head_wm1_msk_03: A + head_wm1_msk_04:R

            Texture2D redChannelL = Util.FindTexture(folders, "head_wm1_msk_01");
            Texture2D redChannelR = Util.FindTexture(folders, "head_wm1_msk_01");
            Vector4 redMaskL = new Vector4(1, 0, 0, 0);
            Vector4 redMaskR = new Vector4(0, 1, 0, 0);

            Texture2D greenChannelL = Util.FindTexture(folders, "head_wm1_msk_01");
            Texture2D greenChannelR = Util.FindTexture(folders, "head_wm1_msk_01");
            Vector4 greenMaskL = new Vector4(0, 0, 1, 0);
            Vector4 greenMaskR = new Vector4(0, 0, 0, 1);

            Texture2D blueChannelL = Util.FindTexture(folders, "head_wm1_msk_03");
            Texture2D blueChannelR = Util.FindTexture(folders, "head_wm1_msk_03");
            Vector4 blueMaskL = new Vector4(0, 1, 0, 0);
            Vector4 blueMaskR = new Vector4(0, 0, 1, 0);

            Texture2D alphaChannelL = Util.FindTexture(folders, "head_wm1_msk_03");
            Texture2D alphaChannelR = Util.FindTexture(folders, "head_wm1_msk_04");
            Vector4 alphaMaskL = new Vector4(0, 0, 0, 1);
            Vector4 alphaMaskR = new Vector4(1, 0, 0, 0);

            Texture2D pack = baker.BakeChannelPackSymmetryLinear("Assets" + Path.DirectorySeparatorChar + "Test",
                redChannelL, greenChannelL, blueChannelL, alphaChannelL,
                redChannelR, greenChannelR, blueChannelR, alphaChannelR,
                redMaskL, greenMaskL, blueMaskL, alphaMaskL,
                redMaskR, greenMaskR, blueMaskR, alphaMaskR, 
                512, "RL_WrinkleMask_Set1A");
        }

        [MenuItem("Reallusion/Dev/Channel Pack 1B", priority = 220)]
        public static void DoSet1B()
        {
            CharacterInfo ci = ImporterWindow.Current.Character;
            ComputeBake baker = new ComputeBake(ci.Fbx, ci);
            string[] folders = new string[] { "Assets", "Packages" };

            // Wrinkle Mask Set 1B:
            // 
            // R: head_wm1_normal_head_wm1_browRaiseOuter_L / R     head_wm1_msk_02: R + G
            // G: head_wm1_normal_head_wm1_chinRaise_L / R          head_wm1_msk_02: B + A
            // B: head_wm1_normal_head_wm1_jawOpen                  head_wm1_msk_03:R
            // A: head_wm1_normal_head_wm1_squintInner_L / R        head_wm1_msk_04: G + B

            Texture2D redChannelL = Util.FindTexture(folders, "head_wm1_msk_02");
            Texture2D redChannelR = Util.FindTexture(folders, "head_wm1_msk_02");
            Vector4 redMaskL = new Vector4(1, 0, 0, 0);
            Vector4 redMaskR = new Vector4(0, 1, 0, 0);

            Texture2D greenChannelL = Util.FindTexture(folders, "head_wm1_msk_02");
            Texture2D greenChannelR = Util.FindTexture(folders, "head_wm1_msk_02");
            Vector4 greenMaskL = new Vector4(0, 0, 1, 0);
            Vector4 greenMaskR = new Vector4(0, 0, 0, 1);

            Texture2D blueChannelL = Util.FindTexture(folders, "head_wm1_msk_03");
            Texture2D blueChannelR = Util.FindTexture(folders, "head_wm1_msk_03");
            Vector4 blueMaskL = new Vector4(1, 0, 0, 0);
            Vector4 blueMaskR = new Vector4(1, 0, 0, 0);

            Texture2D alphaChannelL = Util.FindTexture(folders, "head_wm1_msk_04");
            Texture2D alphaChannelR = Util.FindTexture(folders, "head_wm1_msk_04");
            Vector4 alphaMaskL = new Vector4(0, 1, 0, 0);
            Vector4 alphaMaskR = new Vector4(0, 0, 1, 0);

            Texture2D pack = baker.BakeChannelPackSymmetryLinear("Assets" + Path.DirectorySeparatorChar + "Test",
                redChannelL, greenChannelL, blueChannelL, alphaChannelL,
                redChannelR, greenChannelR, blueChannelR, alphaChannelR,
                redMaskL, greenMaskL, blueMaskL, alphaMaskL,
                redMaskR, greenMaskR, blueMaskR, alphaMaskR,
                512, "RL_WrinkleMask_Set1B");
        }

        [MenuItem("Reallusion/Dev/Channel Pack 2", priority = 220)]
        public static void DoSet2()
        {
            CharacterInfo ci = ImporterWindow.Current.Character;
            ComputeBake baker = new ComputeBake(ci.Fbx, ci);
            string[] folders = new string[] { "Assets", "Packages" };

            // Wrinkle Mask Set 2:

            // R: head_wm2_normal_head_wm2_browsDown_L / R           head_wm2_msk_01: R + G
            // G: head_wm2_normal_head_wm2_browsLateral_L / R        head_wm2_msk_01: B + A
            // B: head_wm2_normal_head_wm2_mouthStretch_L / R        head_wm2_msk_02: R + G
            // A: head_wm2_normal_head_wm2_neckStretch_L / R         head_wm2_msk_02: B + A

            Texture2D redChannelL = Util.FindTexture(folders, "head_wm2_msk_01");
            Texture2D redChannelR = Util.FindTexture(folders, "head_wm2_msk_01");
            Vector4 redMaskL = new Vector4(1, 0, 0, 0);
            Vector4 redMaskR = new Vector4(0, 1, 0, 0);

            Texture2D greenChannelL = Util.FindTexture(folders, "head_wm2_msk_01");
            Texture2D greenChannelR = Util.FindTexture(folders, "head_wm2_msk_01");
            Vector4 greenMaskL = new Vector4(0, 0, 1, 0);
            Vector4 greenMaskR = new Vector4(0, 0, 0, 1);

            Texture2D blueChannelL = Util.FindTexture(folders, "head_wm2_msk_02");
            Texture2D blueChannelR = Util.FindTexture(folders, "head_wm2_msk_02");
            Vector4 blueMaskL = new Vector4(1, 0, 0, 0);
            Vector4 blueMaskR = new Vector4(0, 1, 0, 0);

            Texture2D alphaChannelL = Util.FindTexture(folders, "head_wm2_msk_02");
            Texture2D alphaChannelR = Util.FindTexture(folders, "head_wm2_msk_02");
            Vector4 alphaMaskL = new Vector4(0, 0, 1, 0);
            Vector4 alphaMaskR = new Vector4(0, 0, 0, 1);

            Texture2D pack = baker.BakeChannelPackSymmetryLinear("Assets" + Path.DirectorySeparatorChar + "Test",
                redChannelL, greenChannelL, blueChannelL, alphaChannelL,
                redChannelR, greenChannelR, blueChannelR, alphaChannelR,
                redMaskL, greenMaskL, blueMaskL, alphaMaskL,
                redMaskR, greenMaskR, blueMaskR, alphaMaskR,
                512, "RL_WrinkleMask_Set2");
        }

        [MenuItem("Reallusion/Dev/Channel Pack 3", priority = 220)]
        public static void DoSet3()
        {
            CharacterInfo ci = ImporterWindow.Current.Character;
            ComputeBake baker = new ComputeBake(ci.Fbx, ci);
            string[] folders = new string[] { "Assets", "Packages" };

            // Wrinkle Mask Set 3:

            // R: head_wm3_normal_head_wm3_cheekRaiseInner_L / R    head_wm3_msk_01: R + G
            // G: head_wm3_normal_head_wm3_cheekRaiseOuter_L / R    head_wm3_msk_01: B + A
            // B: head_wm3_normal_head_wm3_cheekRaiseUpper_L / R    head_wm3_msk_02: R + G
            // A: head_wm3_normal_head_wm3_smile_L / R              head_wm3_msk_02: B + A

            Texture2D redChannelL = Util.FindTexture(folders, "head_wm3_msk_01");
            Texture2D redChannelR = Util.FindTexture(folders, "head_wm3_msk_01");
            Vector4 redMaskL = new Vector4(1, 0, 0, 0);
            Vector4 redMaskR = new Vector4(0, 1, 0, 0);

            Texture2D greenChannelL = Util.FindTexture(folders, "head_wm3_msk_01");
            Texture2D greenChannelR = Util.FindTexture(folders, "head_wm3_msk_01");
            Vector4 greenMaskL = new Vector4(0, 0, 1, 0);
            Vector4 greenMaskR = new Vector4(0, 0, 0, 1);

            Texture2D blueChannelL = Util.FindTexture(folders, "head_wm3_msk_02");
            Texture2D blueChannelR = Util.FindTexture(folders, "head_wm3_msk_02");
            Vector4 blueMaskL = new Vector4(1, 0, 0, 0);
            Vector4 blueMaskR = new Vector4(0, 1, 0, 0);

            Texture2D alphaChannelL = Util.FindTexture(folders, "head_wm3_msk_02");
            Texture2D alphaChannelR = Util.FindTexture(folders, "head_wm3_msk_02");
            Vector4 alphaMaskL = new Vector4(0, 0, 1, 0);
            Vector4 alphaMaskR = new Vector4(0, 0, 0, 1);

            Texture2D pack = baker.BakeChannelPackSymmetryLinear("Assets" + Path.DirectorySeparatorChar + "Test",
                redChannelL, greenChannelL, blueChannelL, alphaChannelL,
                redChannelR, greenChannelR, blueChannelR, alphaChannelR,
                redMaskL, greenMaskL, blueMaskL, alphaMaskL,
                redMaskR, greenMaskR, blueMaskR, alphaMaskR,
                512, "RL_WrinkleMask_Set3");
        }

        [MenuItem("Reallusion/Dev/Channel Pack 12", priority = 220)]
        public static void DoSet12()
        {
            CharacterInfo ci = ImporterWindow.Current.Character;
            ComputeBake baker = new ComputeBake(ci.Fbx, ci);
            string[] folders = new string[] { "Assets", "Packages" };

            // Wrinkle Mask Set 12:

            // R: head_wm3_normal_head_wm13_lips_DL / R          head_wm13_msk_01: R + G
            // G: head_wm3_normal_head_wm13_lips_UL / R          head_wm13_msk_01: B + A
            // B: head_wm2_normal_head_wm2_noseWrinkler_L / R    head_wm2_msk_03: R + G
            // A: head_wm2_normal_head_wm2_noseCrease_L / R      head_wm2_msk_03: B + A

            Texture2D redChannelL = Util.FindTexture(folders, "head_wm13_msk_01");
            Texture2D redChannelR = Util.FindTexture(folders, "head_wm13_msk_01");
            Vector4 redMaskL = new Vector4(1, 0, 0, 0);
            Vector4 redMaskR = new Vector4(0, 1, 0, 0);

            Texture2D greenChannelL = Util.FindTexture(folders, "head_wm13_msk_01");
            Texture2D greenChannelR = Util.FindTexture(folders, "head_wm13_msk_01");
            Vector4 greenMaskL = new Vector4(0, 0, 1, 0);
            Vector4 greenMaskR = new Vector4(0, 0, 0, 1);

            Texture2D blueChannelL = Util.FindTexture(folders, "head_wm2_msk_03");
            Texture2D blueChannelR = Util.FindTexture(folders, "head_wm2_msk_03");
            Vector4 blueMaskL = new Vector4(1, 0, 0, 0);
            Vector4 blueMaskR = new Vector4(0, 1, 0, 0);

            Texture2D alphaChannelL = Util.FindTexture(folders, "head_wm2_msk_03");
            Texture2D alphaChannelR = Util.FindTexture(folders, "head_wm2_msk_03");
            Vector4 alphaMaskL = new Vector4(0, 0, 1, 0);
            Vector4 alphaMaskR = new Vector4(0, 0, 0, 1);

            Texture2D pack = baker.BakeChannelPackSymmetryLinear("Assets" + Path.DirectorySeparatorChar + "Test",
                redChannelL, greenChannelL, blueChannelL, alphaChannelL,
                redChannelR, greenChannelR, blueChannelR, alphaChannelR,
                redMaskL, greenMaskL, blueMaskL, alphaMaskL,
                redMaskR, greenMaskR, blueMaskR, alphaMaskR,
                512, "RL_WrinkleMask_Set12");
        }



        [MenuItem("Reallusion/Dev/Proc Bump", priority = 220)]
        public static void DoProcBump()
        {
            int width = 2048;
            int height = 2048;

            Texture2D bumpMap = new Texture2D(width, height, TextureFormat.RGBA32, true);

            Color[] pixels = bumpMap.GetPixels();
            float[] values = new float[pixels.Length]; 

            float[] rx = new float[100];
            float[] ry = new float[100];
            float[] rf = new float[100];

            float scale = 1f;

            for (int i = 0; i < 100; i++)
            {
                rx[i] = Random.Range(0f, width);
                ry[i] = Random.Range(0f, height);
                rf[i] = Random.Range(0f, 1f);
            }

            float maxValue = 0f;
            float minValue = 0f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float map = 0.0f;
                    for (int i = 0; i < 100; i++)
                    {
                        float xrx = x - rx[i];
                        float yry = y - ry[i];
                        map += Mathf.Sin((Mathf.Sqrt(xrx * xrx + yry * yry) / (2.08f + 5.0f * rf[i])) / scale);
                    }
                    float rgb = map/100f;
                    values[y * width + x] = rgb;
                    maxValue = Mathf.Max(maxValue, rgb);
                    minValue = Mathf.Min(minValue, rgb);
                }
            }            

            for (int i = 0; i < pixels.Length; i++)
            {
                float rgb = Mathf.InverseLerp(minValue, maxValue, values[i]);
                pixels[i] = new Color(rgb, rgb, rgb, 1.0f);
            }            

            bumpMap.SetPixels(pixels);

            WritePNG("Assets/Temp", bumpMap, "ProcBump");            
        }

        private static string WritePNG(string folderPath, Texture2D saveTexture, string textureName)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), folderPath, textureName + ".png");

            Util.EnsureAssetsFolderExists(folderPath);

            byte[] pngArray = saveTexture.EncodeToPNG();

            File.WriteAllBytes(filePath, pngArray);

            string assetPath = Util.GetRelativePath(filePath);
            if (File.Exists(filePath)) return assetPath;
            else return "";
        }


        [MenuItem("Reallusion/Dev/Magica Weight Map", priority = 220)]
        public static void DoMagicaWeightMap()
        {
            CharacterInfo currentCharacter = ImporterWindow.Current.Character;

            string[] folders = new string[] { "Assets", "Packages" };            
            Texture2D physXWeightMap = Util.FindTexture(folders, "physXWeightMapTest");

            string folder = ComputeBake.BakeTexturesFolder(currentCharacter.path);
            string name = "magicaWeightMapTest";
            float threshold = 1f / 255f;
            Vector2Int size = new Vector2Int(64, 64);
            // should create the texture in: <current character folder>/Baked/<character name>/Textures

            Texture2D magicaWeightMap = ComputeBake.BakeMagicaWeightMap(physXWeightMap, threshold, size, folder, name);
        }

#endif
    }
}