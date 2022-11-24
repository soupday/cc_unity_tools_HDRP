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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using System;

namespace Reallusion.Import
{
    [InitializeOnLoad]
    public static class WindowManager
    {
        public static Scene currentScene;
        public static Scene previewSceneHandle;
        public static PreviewScene previewScene;
        public static bool openedInPreviewScene;
        public static bool showPlayer = true;
        public static bool showRetarget = false;
        private static bool eventsAdded = false;
        private static bool showPlayerAfterPlayMode = false;
        private static bool showRetargetAfterPlayMode = false;

        public delegate void OnTimer();
        public static OnTimer onTimer;
        private static float timer = 0f;

        static WindowManager()
        {
            EditorApplication.playModeStateChanged += WindowManager.OnPlayModeStateChanged;
            EditorApplication.update += WindowManager.MonitorScene;

            // Animation mode is a bit unpredictable, so leave it off by default for now
            showPlayer = false; // Importer.ANIMPLAYER_ON_BY_DEFAULT;
            currentScene = EditorSceneManager.GetActiveScene();

            previewScene = PreviewScene.FetchPreviewScene(currentScene);
            if (previewScene.IsValidPreviewScene)
            {
                openedInPreviewScene = true;
                previewSceneHandle = currentScene;
            }
            else
            {
                previewScene = default;
                previewSceneHandle = default;
            }

            if (!eventsAdded)
            {
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            }
        }

        public static void OnPlayModeStateChanged(PlayModeStateChange state)
        {            
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Debug.Log(state);
                showPlayerAfterPlayMode = showPlayer;
                showRetargetAfterPlayMode = showRetarget;
                showPlayer = false;
                showRetarget = false;
                AnimPlayerGUI.ClosePlayer();
                AnimRetargetGUI.CloseRetargeter();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log(state);
                showPlayer = showPlayerAfterPlayMode;
                showRetarget = showRetargetAfterPlayMode;
            }
        }

        public static void OnBeforeAssemblyReload()
        {
            if (AnimationMode.InAnimationMode())  
            { 
                Util.LogInfo("Disabling Animation Mode on editor assembly reload.");
                AnimationMode.StopAnimationMode();
            }

            if (LodSelectionWindow.Current)
            {
                Util.LogInfo("Closing Lod Selection Window on editor assembly reload.");
                LodSelectionWindow.Current.Close();
            }
        }

        public static PreviewScene OpenPreviewScene(GameObject prefab)
        {
            if (!prefab) return default;
            if (!IsPreviewScene && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return default;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            GameObject.Instantiate(Util.FindPreviewScenePrefab(), Vector3.zero, Quaternion.identity);

            previewSceneHandle = scene;
            previewScene = PreviewScene.FetchPreviewScene(scene);

            previewScene.PostProcessingAndLighting();
            previewScene.ShowPreviewCharacter(prefab);
            
            return previewScene;
        }        

        public static bool IsPreviewScene
        {
            get { return (EditorSceneManager.GetActiveScene() == previewSceneHandle && previewScene.IsValidPreviewScene); }
        }

        public static PreviewScene GetPreviewScene() 
        {
            if (IsPreviewScene) 
            {
                return previewScene;
            }            

            return default;
        }        
                
        private static void MonitorScene()  
        {                        
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = 0f;
                    onTimer();
                }
            }

            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (currentScene != activeScene)
            {
                currentScene = activeScene;                
                previewScene = GetPreviewScene();
            }

            bool isPlayerShown = AnimPlayerGUI.IsPlayerShown();            

            if (IsPreviewScene) 
            {                
                if (showPlayer && !isPlayerShown)
                {
                    ShowAnimationPlayer();                    
                }
                else if (!showPlayer && isPlayerShown)
                {
                    HideAnimationPlayer(false);
                }
            }
            else if (openedInPreviewScene)
            {
                if (isPlayerShown)
                {
                    HideAnimationPlayer(false);                    
                }
            }
        }

        public static void DoSceneViewOrbit()
        {
            if (!isSceneViewOrbit)
            {
                trackTarget = null;
                EditorApplication.update += SceneViewOrbitUpdate;
                isSceneViewOrbit = true;
            }
            else
            {
                trackTarget = null;
                EditorApplication.update -= SceneViewOrbitUpdate;
                isSceneViewOrbit = false;
            }
        }

        public static void StopSceneViewOrbit()
        {
            if (isSceneViewOrbit)
            {
                EditorApplication.update -= SceneViewOrbitUpdate;
                trackTarget = null;
                isSceneViewOrbit = true;
            }
        }

        public static void DoSceneViewOrbitTracking()
        {            
            if (!isSceneViewOrbit)
            {
                trackTarget = Selection.activeTransform;
                if (trackTarget)
                {
                    pivotDisplacement = SceneView.lastActiveSceneView.pivot - trackTarget.position;
                    EditorApplication.update += SceneViewOrbitUpdate;
                    isSceneViewOrbit = true;
                }
            }
            else
            {
                EditorApplication.update -= SceneViewOrbitUpdate;
                isSceneViewOrbit = false;
            }

        }

        private static bool isSceneViewOrbit;
        private static Vector3 pivotDisplacement;
        private static Transform trackTarget;        

        static void SceneViewOrbitUpdate()
        {
            if (isSceneViewOrbit)
            {
                SceneView scene = SceneView.lastActiveSceneView;
                Vector3 pivot = scene.pivot;
                Vector3 pos = scene.camera.transform.position;
                Vector3 boom = pos - pivot;
                if (trackTarget)
                {
                    pivot = trackTarget.position + pivotDisplacement;
                }
                float fov = scene.cameraSettings.fieldOfView;
                float dist = scene.cameraDistance;                
                float size = Mathf.Sin(Mathf.Deg2Rad * fov / 2f) * dist;

                boom = Quaternion.AngleAxis(0.1f, Vector3.up) * boom;
                scene.LookAtDirect(pivot, Quaternion.LookRotation(-boom, Vector3.up), size);
                SceneView.RepaintAll();
            }
        }

        public static void DoMatchSceneCameraOnce()
        {
            if (isMatchSceneViewCamera) StopMatchSceneCamera();

            isMatchSceneViewCamera = true;
            MatchSceneCameraUpdate();
            isMatchSceneViewCamera = false;
        }

        public static void DoMatchSceneCamera()
        {
            if (!isMatchSceneViewCamera)
            {
                EditorApplication.update += MatchSceneCameraUpdate;
                isMatchSceneViewCamera = true;
            }
            else
            {
                EditorApplication.update -= MatchSceneCameraUpdate;
                isMatchSceneViewCamera = false;
            }
        }

        public static void DoSceneToggleOffAll()
        {
            if (isMatchSceneViewCamera)
            {
                EditorApplication.update -= MatchSceneCameraUpdate;
                isMatchSceneViewCamera = false;
            }

            if (isSceneViewOrbit)
            {                
                EditorApplication.update -= SceneViewOrbitUpdate;
                isSceneViewOrbit = false;
                trackTarget = null;
            }
        }

        public static void StopMatchSceneCamera()
        {
            if (isSceneViewOrbit)
            {
                EditorApplication.update -= MatchSceneCameraUpdate;
                isMatchSceneViewCamera = true;
            }
        }        

        private static bool isMatchSceneViewCamera;
        
        static void MatchSceneCameraUpdate()
        {
            if (isMatchSceneViewCamera)
            {
                Transform cam = previewScene.GetCamera();
                SceneView scene = SceneView.lastActiveSceneView;                
                Vector3 pos = scene.camera.transform.position;
                Quaternion rot = scene.camera.transform.rotation;
                float fov = scene.camera.fieldOfView;                
                cam.position = pos;
                cam.rotation = rot;
                Camera camera = cam.GetComponent<Camera>();
                if (camera)
                {
                    camera.fieldOfView = fov;
                }
            }
        }

        public static void TakeScreenShot()
        {
            string dateStamp = DateTime.Now.ToString("yyMMdd-hhmmss");
            string fileName = "Screenshot-" + dateStamp + ".png";
            Util.LogAlways("Saving screenshot to: " + fileName);
            ScreenCapture.CaptureScreenshot(fileName);
        }

        public static void ShowAnimationPlayer()
        {
            GameObject scenePrefab = null;

            if (IsPreviewScene) scenePrefab = GetPreviewScene().GetPreviewCharacter();            
            if (!scenePrefab) scenePrefab = Selection.activeGameObject;

            AnimPlayerGUI.OpenPlayer(scenePrefab);
            openedInPreviewScene = IsPreviewScene;

            if (showRetarget) ShowAnimationRetargeter();

            showPlayer = true;
        }

        public static void HideAnimationPlayer(bool updateShowPlayer)
        {
            if (AnimPlayerGUI.IsPlayerShown()) AnimPlayerGUI.ResetFace();

            AnimPlayerGUI.ClosePlayer();

            HideAnimationRetargeter(false);

            if (updateShowPlayer)
                showPlayer = false;
        }

        public static void ShowAnimationRetargeter()
        {
            if (AnimPlayerGUI.IsPlayerShown())
            {
                AnimRetargetGUI.OpenRetargeter();
                showRetarget = true;
            }
        }

        public static void HideAnimationRetargeter(bool updateShowRetarget)
        {
            AnimRetargetGUI.CloseRetargeter();
            
            if (updateShowRetarget)
                showRetarget = false;
        }      
        
        public static bool StopAnimationMode(UnityEngine.Object obj = null)
        {
            bool inAnimationMode = false;
            if (AnimationMode.InAnimationMode())
            {
                inAnimationMode = true;
                AnimationMode.StopAnimationMode();
                if (obj)
                {
                    GameObject scenePrefab = Util.GetScenePrefabInstanceRoot(obj);
                    Util.TryResetScenePrefab(scenePrefab);
                }
            }

            return inAnimationMode;
        }

        public static void RestartAnimationMode(bool inAnimationMode)
        {
            if (inAnimationMode)
            {
                if (!AnimationMode.InAnimationMode())
                    AnimationMode.StartAnimationMode();
            }
        }        

        public static void StartTimer(float delay)
        {
            timer = delay;
        }
    }
}
