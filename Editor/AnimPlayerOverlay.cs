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
            Debug.Log("AnimPlayerOverlay::ShowAll()");
            foreach (AnimPlayerOverlay apo in createdOverlays)
            {
                apo.Show();
            }            
        }

        public static void HideAll()
        {
            visibility = false;
            Debug.Log("AnimPlayerOverlay::HideAll()");
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
            Debug.Log("AnimPlayerOverlay::Show()");
            isVisible = true;
            if (isInToolbar) Undock();
            collapsed = false;            
            floatingPosition = new Vector2(
                containerWindow.position.width - width - 3f,
                containerWindow.position.height - height - 3f
                );            
        }

        public void Hide()
        {
            Debug.Log("AnimPlayerOverlay::Hide()");
            isVisible = false;
        }

        public override void OnCreated()
        {
            Debug.Log("AnimPlayerOverlay::OnCreated()");
            createdOverlays.Add(this);            
        }

        public override void OnWillBeDestroyed()
        {
            Debug.Log("AnimPlayerOverlay::OnWillBeDestroyed()");
            if (createdOverlays.Contains(this))
            {
                Hide();
                createdOverlays.Remove(this);
            }

            base.OnWillBeDestroyed();
        }

        public override void OnGUI()
        {
            if (!AnimPlayerGUI.useLightIcons) AnimPlayerGUI.useLightIcons = true;
            AnimPlayerGUI.DrawPlayer();
            AnimPlayerGUI.DrawFacialMorph();

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
