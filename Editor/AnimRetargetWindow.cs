using UnityEngine;
using UnityEditor;

namespace Reallusion.Import
{
    public class AnimRetargetWindow
    {
        public static bool isShown = false;
        
        public static void OnSceneGUI(SceneView sceneView)
        {
            float ypadding = 1f;
            float width = 320f;
            float height = 269f;

            float x = 3f;
            float y = sceneView.position.height - height - ypadding;

            sceneView.autoRepaintOnSceneChange = true;

            var windowOverlayRect = new Rect(x, y, width, height);
            GUIStyle window = new GUIStyle("window");
            
            GUILayout.Window("Animation Retarget".GetHashCode(), windowOverlayRect, DoWindow, "Animation Retarget Tools", window);
        }

        public static void ShowPlayer()
        {
            if (!isShown)
            {                
                SceneView.duringSceneGui += AnimRetargetWindow.OnSceneGUI;
                isShown = true;
            }
        }

        public static void HidePlayer()
        {
            if (isShown)
            {
                SceneView.duringSceneGui -= AnimRetargetWindow.OnSceneGUI;                
                isShown = false;
            }
        }

        public static void DoWindow(int id)
        {            
            AnimRetargetGUI.DrawRetargeter();
        }
    }
}
