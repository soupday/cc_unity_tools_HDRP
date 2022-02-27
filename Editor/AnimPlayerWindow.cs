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
            if (AnimPlayerGUI.animFoldOut) height += 84f;
            if (AnimPlayerGUI.faceFoldOut) height += 90f;
            
            float x = sceneView.position.width - width - xpadding;
            float y = sceneView.position.height - height - ypadding;

            sceneView.autoRepaintOnSceneChange = true;            

            var windowOverlayRect = new Rect(x, y, width, height);
            GUILayout.Window("Animation Playback".GetHashCode(), windowOverlayRect, DoWindow, "Character Preview Tools");
        }

        public static void ShowPlayer() 
        {
            if (!isShown)
            {
                SceneView.duringSceneGui += AnimPlayerWindow.OnSceneGUI;
                isShown = true;
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
    }
}
