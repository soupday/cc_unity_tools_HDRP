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
        [MenuItem("CC3/Import Characters", priority = 1)]
        public static void InitCC3ImportGUI()
        {
            ImporterWindow.Init(ImporterWindow.Mode.multi, Selection.activeObject);
        }

        [MenuItem("CC3/Animation Player", priority = 2)]
        public static void ShowAnimationPlayer()
        {
            if (AnimPlayerGUI.IsPlayerShown())
            {
                AnimPlayerGUI.DestroyPlayer();
                WindowManager.showTools = false;
            }
            else
            {
                PreviewScene ps = PreviewScene.GetPreviewScene();
                if (ps.IsValid)
                {
                    AnimPlayerGUI.CreatePlayer(ps, ImporterWindow.Current?.Character?.Fbx);
                    WindowManager.showTools = true;
                }
            }
        }

        [MenuItem("Assets/CC3/Import Character", priority = 2000)]
        public static void InitAssetCC3ImportGUI()
        {
            ImporterWindow.Init(ImporterWindow.Mode.single, Selection.activeObject);
        }
         
        [MenuItem("Assets/CC3/Import Character", true)]
        public static bool ValidateInitAssetCC3ImportGUI()
        {
            if (Util.IsCC3Character(Selection.activeObject)) return true;
            return false;
        }

        [MenuItem("CC3/Preview Scene Tools/Orbit Scene View (Toggle)", priority = 210)]
        public static void DoOrbitSceneView()
        {
            WindowManager.DoSceneViewOrbit();
        }

        /*
        [MenuItem("CC3/Scene Tools/Orbit Scene View (Tracking)", priority = 211)]
        public static void DoOrbitSceneViewTracking()
        {
            WindowManager.DoSceneViewOrbitTracking();
        }*/

        [MenuItem("CC3/Preview Scene Tools/Match Scene Camera (Toggle)", priority = 212)]
        public static void DoMatchSceneCamera()
        {
            WindowManager.DoMatchSceneCamera();
        }

        [MenuItem("CC3/Preview Scene Tools/Screenshot", priority = 213)]
        public static void DoScreenShot()
        {
            WindowManager.TakeScreenShot();
        }

        [MenuItem("CC3/Test/Bake Gradient", priority = 220)]
        public static void DoTest()
        {
            CharacterInfo ci = ImporterWindow.Current.Character;
            ComputeBake baker = new ComputeBake(ci.Fbx, ci);
            Texture2D gradient = baker.BakeGradientMap("Assets\\Test", "Gradient");
        }
    }
}