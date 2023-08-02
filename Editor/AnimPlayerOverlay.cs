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
    [Overlay(typeof(SceneView), "Reallusion Import Tools", "Character Preview Tools")]
    public class AnimPlayerOverlay : IMGUIOverlay, ITransientOverlay
    {
        //public static AnimPlayerOverlay createdOverlay { get; private set; }
        public static List<AnimPlayerOverlay> createdOverlays = new List<AnimPlayerOverlay>();
        public static bool exists { get { return createdOverlays.Count > 0; } }
        private bool isVisible;
        public bool visible { get { return isVisible; } }
        private static bool visibility = false;
        public static bool Visibility { get { return visibility; } }
        public static float width;
        public static float height;
        public static float containerHeight;
        public static float containerWidth;
        public static bool setInitialPosition = false;

        public static bool AnyVisible()
        {
            foreach (AnimPlayerOverlay apo in createdOverlays)
            {
                if (apo.isVisible) return true;
            }
            return false;
        }

        public static void ShowAll()
        {
            visibility = true;
            foreach (AnimPlayerOverlay apo in createdOverlays)
            {
                apo.Show();
            }
        }

        public static void HideAll()
        {
            visibility = false;
            foreach (AnimPlayerOverlay apo in createdOverlays)
            {
                apo.Hide();
            }
        }

        AnimPlayerOverlay()
        {
            isVisible = visibility;
        }

        public void Show()
        {
            if (isInToolbar) Undock();
            collapsed = false;
            setInitialPosition = true;
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
            if (!AnimPlayerGUI.UseLightIcons) AnimPlayerGUI.UseLightIcons = true;
            AnimPlayerGUI.DrawPlayer();
            AnimPlayerGUI.DrawFacialMorph();

            if (setInitialPosition)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    Rect last = GUILayoutUtility.GetLastRect();
                    width = last.x + last.width;
                    height = last.y + last.height;
                    containerHeight = this.containerWindow.position.height;
                    containerWidth = this.containerWindow.position.width;
                    floatingPosition = new Vector2(containerWidth - width - 14f, containerHeight - height - 23f);
                    setInitialPosition = false;
                }
            }
        }
    }
}
#endif
