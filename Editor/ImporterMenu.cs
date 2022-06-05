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
            if (WindowManager.showPlayer)
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
            if (WindowManager.showRetarget)
            {
                WindowManager.HideAnimationRetargeter(true);
            }
            else
            {
                WindowManager.ShowAnimationRetargeter();
            }
        }

        [MenuItem("Reallusion/Animation Retargeter", true)]
        public static bool ValidateShowAnimationRetargeter()
        {
            return PreviewScene.GetPreviewScene().IsValid && AnimPlayerGUI.IsPlayerShown();
        }

        [MenuItem("Assets/Reallusion/Import Character", priority = 2000)]
        public static void InitAssetCC3ImportGUI()
        {
            ImporterWindow.Init(ImporterWindow.Mode.single, Selection.activeObject);
        }
         
        [MenuItem("Assets/Reallusion/Import Character", true)]
        public static bool ValidateInitAssetCC3ImportGUI()
        {
            if (Util.IsCC3Character(Selection.activeObject)) return true;
            return false;
        }

        [MenuItem("Reallusion/Preview Scene Tools/Orbit Scene View (Toggle)", priority = 210)]
        public static void DoOrbitSceneView()
        {
            WindowManager.DoSceneViewOrbit();
        }

        [MenuItem("Reallusion/Preview Scene Tools/Orbit Scene View (Toggle)", true)]
        private static bool ValudidateDoOrbitSceneView()
        {
            PreviewScene ps = PreviewScene.GetPreviewScene();
            return ps.IsValid;
        }

        /*
        [MenuItem("Reallusion/Scene Tools/Orbit Scene View (Tracking)", priority = 211)]
        public static void DoOrbitSceneViewTracking()
        {
            WindowManager.DoSceneViewOrbitTracking();
        }*/

        [MenuItem("Reallusion/Preview Scene Tools/Match Scene Camera", priority = 212)]
        public static void DoMatchSceneCameraOnce()
        {
            WindowManager.DoMatchSceneCameraOnce();
        }

        [MenuItem("Reallusion/Preview Scene Tools/Match Scene Camera (Toggle)", priority = 212)]
        public static void DoMatchSceneCamera()
        {
            WindowManager.DoMatchSceneCamera();
        }

        [MenuItem("Reallusion/Preview Scene Tools/Match Scene Camera (Toggle)", true)]
        private static bool ValudidateDoMatchSceneCamera()
        {
            PreviewScene ps = PreviewScene.GetPreviewScene();
            return ps.IsValid;
        }

        [MenuItem("Reallusion/Preview Scene Tools/Screenshot", priority = 213)]
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
            Texture2D gradient = baker.BakeGradientMap("Assets\\Test", "Gradient");
        }
        */
    }
}