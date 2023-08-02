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

#if SCENEVIEW_OVERLAY_COMPATIBLE
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Reallusion.Import
{
    [Overlay(typeof(SceneView), "Animation Retarget Tools", "Animation Retarget Tools")]
    public class AnimRetargetOverlay : IMGUIOverlay, ITransientOverlay
    {
        public static List<AnimRetargetOverlay> createdOverlays = new List<AnimRetargetOverlay>();
        public static bool exists { get { return createdOverlays.Count > 0; } }
        private bool isVisible;
        public bool visible { get { return isVisible; } }
        private static bool visibility = false;
        public static bool Visibility { get { return visibility; } }
        public static float width;
        public static float height;
        public static float containerHeight;
        public static bool setInitialPosition = false;

        public static bool AnyVisible()
        {
            foreach (AnimRetargetOverlay aro in createdOverlays)
            {
                if (aro.isVisible) return true;
            }
            return false;
        }

        public static void ShowAll()
        {
            visibility = true;
            foreach (AnimRetargetOverlay aro in createdOverlays)
            {
                aro.Show();
            }
        }

        public static void HideAll()
        {
            visibility = false;
            foreach (AnimRetargetOverlay aro in createdOverlays)
            {
                aro.Hide();
            }
        }

        AnimRetargetOverlay()
        {
            isVisible = visibility;            
        }                

        public void Show()
        {
            setInitialPosition = true;            
            Undock();
            Undock();
            collapsed = false;
            floatingPosition = new Vector2(
                1f,
                this.containerWindow.position.height - height - 3f
                );
            isVisible = true;
        }

        public void Hide()
        {
            isVisible = false;
        }

        public override void OnCreated()
        {            
            createdOverlays.Add(this);
        }

        public override void OnWillBeDestroyed()
        {            
            if (createdOverlays.Contains(this))
            {
                Hide();
                createdOverlays.Remove(this);
            }

            base.OnWillBeDestroyed();
        }

        public override void OnGUI()
        {
            AnimRetargetGUI.DrawRetargeter();

            if (setInitialPosition)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    Rect last = GUILayoutUtility.GetLastRect();
                    width = last.x + last.width;
                    height = last.y + last.height;
                    containerHeight = this.containerWindow.position.height;
                    floatingPosition = new Vector2(1f, containerHeight - height - 23f);
                    setInitialPosition = false;
                }
            }
        }
    }
}
#endif
