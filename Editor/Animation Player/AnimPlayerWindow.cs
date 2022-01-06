using UnityEngine;
using UnityEditor;

namespace Reallusion.Import
{
    public class AnimPlayerWindow : EditorWindow
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
            if (AnimPlayerIMGUI.foldOut)
            {
                height = 110f;
            }
            else
            {
                height = 26f;
            }
            float x = sceneView.position.width - width - xpadding;
            float y = sceneView.position.height - height - ypadding;

            var windowOverlayRect = new Rect(x, y, width, height);
            //shenanigans to make the time slider draggable (only in GUILayout.Window seems broken in GUI.Window)
            //and when the controls arent drawn when folded in GUILayout.Window doesnt respect MinWidth - so use fixed GUI.Window
            if (AnimPlayerIMGUI.foldOut)
                GUILayout.Window("Animation Playback".GetHashCode(), windowOverlayRect, DoWindow, "", GUILayout.MinWidth(width));
            else
                GUI.Window("Animation Playback".GetHashCode(), windowOverlayRect, DoWindow, "");
        }

        public static void ShowPlayer()
        {
            if (!isShown)
            {
                SceneView.duringSceneGui += AnimPlayerWindow.OnSceneGUI;
                isShown = true;
            }
            else            
                Debug.Log("AnimPlayerWindow already open - no need for new delegate");
        }

        public static void HidePlayer()
        {
            if (isShown)
            {
                SceneView.duringSceneGui -= AnimPlayerWindow.OnSceneGUI;
                isShown = false;
            }
            else
                Debug.Log("AnimPlayerWindow not open - no need to remove delegate");
        }

        public static void DoWindow(int id)
        {
            AnimPlayerIMGUI.DrawPlayer();
        }
    }
}
