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
    public class AnimPlayerWindow
    {
        public static bool isShown = false;
        private static float xpadding = 6f;
#if SCENEVIEW_OVERLAY_COMPATIBLE
        private static float ypadding = 32f;  //delta of 26 pixels in case this window is used instead of an overlay 
#else
        private static float ypadding = 6f;
#endif
        private static float width = 320f;
        private static float height = 26f;
        
        public static void OnSceneGUI(SceneView sceneView)
        {
            height = 72f;
            if (AnimPlayerGUI.AnimFoldOut) height += 140f;
            if (AnimPlayerGUI.FaceFoldOut) height += 90f;
            
            float x = sceneView.position.width - width - xpadding;
            float y = sceneView.position.height - height - ypadding;

            sceneView.autoRepaintOnSceneChange = true;            

            var windowOverlayRect = new Rect(x, y, width, height);


            //GUILayout.Window("Animation Playback".GetHashCode(), windowOverlayRect, DoWindow, "Character Preview Tools");


            // to counter: Resolve of invalid GC handle. The handle is from a previous domain. The resolve operation is skipped.

            string name = EditorApplication.isPlaying ? "Character Preview Tools (Runtime)" : "Character Preview Tools";
            int id = EditorApplication.isPlaying ? "Animation Playback".GetHashCode() : "Animation Playback (Runtime)".GetHashCode();
            if (EditorApplication.isPlaying)
            {
                if (!doneOnce)
                {
                    GUILayout.Window(id, new Rect(), Empty, name);
                    doneOnce = true;
                }
            }
            GUILayout.Window(id, windowOverlayRect, DoWindow, name);
        }
        private static bool doneOnce = false;
        public static void ShowPlayer() 
        {
            if (!isShown)
            {                
                SceneView.duringSceneGui -= AnimPlayerWindow.OnSceneGUI;
                SceneView.duringSceneGui += AnimPlayerWindow.OnSceneGUI;
                isShown = true;
                doneOnce = false;
            }
        }
        
        public static void HidePlayer()
        {
            if (isShown)
            {                
                SceneView.duringSceneGui -= AnimPlayerWindow.OnSceneGUI;
                AnimPlayerGUI.CleanUp();
                
                isShown = false;
            }            
        }
        
        public static void DoWindow(int id)
        {            
            AnimPlayerGUI.DrawPlayer();
            AnimPlayerGUI.DrawFacialMorph();
        }
        
        public static void Empty(int id)
        {
            Util.LogDetail("Showing " + id);
        }
    }
}
