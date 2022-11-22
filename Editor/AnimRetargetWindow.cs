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
    public class AnimRetargetWindow
    {
        public static bool isShown = false;
        
        public static void OnSceneGUI(SceneView sceneView)
        {
            float ypadding = 1f;
            float width = 320f;
            float height = 269f;

            float x = 3f;
            float y = sceneView.position.height - height - ypadding;

            sceneView.autoRepaintOnSceneChange = true;

            var windowOverlayRect = new Rect(x, y, width, height);
            GUIStyle window = new GUIStyle("window");
            
            GUILayout.Window("Animation Retarget".GetHashCode(), windowOverlayRect, DoWindow, "Animation Retarget Tools", window);
        }

        public static void ShowPlayer()
        {
            if (!isShown)
            {                
                SceneView.duringSceneGui += AnimRetargetWindow.OnSceneGUI;
                isShown = true;
            }
        }

        public static void HidePlayer()
        {
            if (isShown)
            {
                SceneView.duringSceneGui -= AnimRetargetWindow.OnSceneGUI;                
                isShown = false;
            }
        }

        public static void DoWindow(int id)
        {            
            AnimRetargetGUI.DrawRetargeter();
        }
    }
}
