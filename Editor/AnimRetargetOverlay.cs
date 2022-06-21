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
            isVisible = true;
            Undock();
            Undock();
            collapsed = false;            
            floatingPosition = new Vector2(
                1f,
                this.containerWindow.position.height - height - 3f
                );
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
            //if (!AnimPlayerGUI.useLightIcons) AnimPlayerGUI.useLightIcons = true;
            AnimRetargetGUI.DrawRetargeter();            

            if (Event.current.type == EventType.Repaint)
            {
                Rect last = GUILayoutUtility.GetLastRect();
                width = last.x + last.width;
                height = last.y + last.height;
            }
        }
    }
}
#endif
