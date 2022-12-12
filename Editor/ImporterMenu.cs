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

using UnityEngine;
using UnityEditor;

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

        /*
        [MenuItem("Reallusion/Test/Bake Gradient", priority = 220)]
        public static void DoTest()
        {
            CharacterInfo ci = ImporterWindow.Current.Character;
            ComputeBake baker = new ComputeBake(ci.Fbx, ci);
            Texture2D gradient = baker.BakeGradientMap("Assets" + Path.DirectorySeparatorChar + "Test", "Gradient");
        }
        */
    }
}