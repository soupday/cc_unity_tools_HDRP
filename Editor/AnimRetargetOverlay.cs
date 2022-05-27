#if SCENEVIEW_OVERLAY_COMPATIBLE
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Reallusion.Import
{
    [Overlay(typeof(SceneView), "Animation Retarget Tools", "Animation Retarget Tools")]
    public class AnimRetargetOverlay : IMGUIOverlay, ITransientOverlay
    {
        public static AnimRetargetOverlay createdOverlay { get; private set; }
        public static bool exists { get { return createdOverlay != null; } }
        bool isVisible;
        public bool visible { get { return isVisible; } }

        public static float width;
        public static float height;
        AnimRetargetOverlay()
        {
            isVisible = false;
        }

        public void Show()
        {
            isVisible = true;

            createdOverlay.Undock();
            createdOverlay.Undock();

            //if (createdOverlay.isInToolbar)
            //    createdOverlay.Undock();

            createdOverlay.collapsed = false;            
            createdOverlay.floatingPosition = new Vector2(
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
            if (createdOverlay == null)
                createdOverlay = this;
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
