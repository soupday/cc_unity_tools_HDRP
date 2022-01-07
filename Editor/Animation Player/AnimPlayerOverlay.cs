#if SCENEVIEW_OVERLAY_COMPATIBLE
using UnityEditor;
using UnityEditor.Overlays;

namespace Reallusion.Import
{
    [Overlay(typeof(SceneView), "Reallusion Import Preview Animaton Player", "Animation Playback")]
    public class AnimPlayerOverlay : IMGUIOverlay, ITransientOverlay
    {
        public static AnimPlayerOverlay createdOverlay { get; private set; }
        public static bool exists { get { return createdOverlay != null; } }
        
        //const string id = "Preview animaton player";
        bool isVisible;

        public bool visible { get { return isVisible; } }
        
        AnimPlayerOverlay()
        {
            isVisible = false;
        }

        public void Show()
        {
            isVisible = true;
        }

        public void Hide()
        {
            isVisible = false;
        }

        public override void OnCreated()
        {            
            createdOverlay = this;
        }

        public override void OnGUI()
        {
            AnimPlayerIMGUI.DrawPlayer();
        }
    }
}
#endif
