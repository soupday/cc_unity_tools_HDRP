#if SCENEVIEW_OVERLAY_COMPATIBLE
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Reallusion.Import
{
    [Overlay(typeof(SceneView), "Reallusion Import Tools", "Character Preview Tools")]
    public class AnimPlayerOverlay : IMGUIOverlay, ITransientOverlay
    {
        public static AnimPlayerOverlay createdOverlay { get; private set; }
        public static bool exists { get { return createdOverlay != null; } }
        bool isVisible;
        public bool visible { get { return isVisible; } }

        public static float width;
        public static float height;
        AnimPlayerOverlay()
        {
            isVisible = false;
        }

        public void Show()
        {
            isVisible = true;
            if (createdOverlay.isInToolbar)
                createdOverlay.Undock();

            createdOverlay.collapsed = false;            
            createdOverlay.floatingPosition = new Vector2(
                                                        this.containerWindow.position.width - width - 3f,
                                                        this.containerWindow.position.height - height - 3f
                                                         );

            //SceneView scene = EditorWindow.GetWindow<SceneView>();
            //createdOverlay.floatingPosition = new Vector2(scene.position.width - width - 3f, scene.position.height - height - 3f);
            
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
