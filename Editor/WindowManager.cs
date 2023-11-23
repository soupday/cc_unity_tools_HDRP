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

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using Scene = UnityEngine.SceneManagement.Scene;

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
        public static bool batchProcess = false;
        private static bool eventsAdded = false;
        private static bool showPlayerAfterPlayMode = false;
        private static bool showRetargetAfterPlayMode = false;

        public delegate void OnTimer();
        public static OnTimer onTimer;
        private static float timer = 0f;

        //unique editorprefs key names
        public const string sceneFocus = "RL_Scene_Focus_Key_0000";
        public const string clipKey = "RL_Animation_Asset_Key_0000";
        public const string animatorControllerKey = "RL_Character_Animator_Ctrl_Key_0000";
        public const string trackingStatusKey = "RL_Bone_Tracking_Key_0000";
        public const string lastTrackedBoneKey = "RL_Last_Tracked_Bone_Key_0000";
        public const string controlStateHashKey = "RL_Animator_Ctrl_Hash_Key_0000";
        public const string timeKey = "RL_Animation_Play_Position_Key_0000";


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
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    {
                        break;
                    }
                case PlayModeStateChange.EnteredPlayMode:
                    {
                        showPlayerAfterPlayMode = showPlayer;
                        showRetargetAfterPlayMode = showRetarget;
                        showPlayer = false;
                        showRetarget = false;
                        AnimPlayerGUI.ClosePlayer();
                        AnimRetargetGUI.CloseRetargeter();                        

                        if (Util.TryDeSerializeBoolFromEditorPrefs(out bool val, WindowManager.sceneFocus))
                        {
                            if (val)
                            {
                                //GrabLastSceneFocus();                                
                                Util.SerializeBoolToEditorPrefs(false, WindowManager.sceneFocus);
                                ShowAnimationPlayer();                                
                                if (Util.TryDeSerializeFloatFromEditorPrefs(out float timeCode, WindowManager.timeKey))
                                {
                                    //set the play position
                                    AnimPlayerGUI.time = timeCode;
                                    //slightly delay startup to allow the animator to initialize
                                    AnimPlayerGUI.delayFrames = 2;
                                }
                            }
                        }

                        if (Util.TryDeserializeIntFromEditorPrefs(out int hash, WindowManager.controlStateHashKey))
                        {
                            AnimPlayerGUI.controlStateHash = hash;
                        }

                        
                        if (Util.TryDeSerializeBoolFromEditorPrefs(out bool track, WindowManager.trackingStatusKey))
                        {
                            AnimPlayerGUI.isTracking = track;
                            if (track)
                            {
                                if (Util.TryDeserializeStringFromEditorPrefs(out string bone, WindowManager.lastTrackedBoneKey))
                                {
                                    AnimPlayerGUI.ReEstablishTracking(bone);
                                }
                            }
                            Util.SerializeBoolToEditorPrefs(false, WindowManager.trackingStatusKey);
                        }
                       
                        break;
                    }
                case PlayModeStateChange.ExitingPlayMode:
                    {

                        break;
                    }
                case PlayModeStateChange.EnteredEditMode:
                    {
                        showPlayer = showPlayerAfterPlayMode;
                        showRetarget = showRetargetAfterPlayMode;

                        break;
                    }
            }
        }

        public static void OnBeforeAssemblyReload()
        {
            if (AnimPlayerGUI.IsPlayerShown())
            {
                HideAnimationPlayer(true);
                HideAnimationRetargeter(true);
            }            
        }

        public static PreviewScene OpenPreviewScene(GameObject prefab)
        {
            if (!prefab) return default;
            if (!IsPreviewScene && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return default;

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
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

        public static GameObject GetSelectedOrPreviewCharacter()
        {
            GameObject characterPrefab = null;

            if (Selection.activeGameObject)
            {
                string s = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (string.IsNullOrEmpty(s))
                {
                    GameObject selectedPrefab = Util.GetScenePrefabInstanceRoot(Selection.activeGameObject);
                    if (selectedPrefab && selectedPrefab.GetComponent<Animator>())
                    {
                        characterPrefab = selectedPrefab;
                    }
                }
            }

            if (!characterPrefab && IsPreviewScene)
            {
                characterPrefab = GetPreviewScene().GetPreviewCharacter();
            }

            return characterPrefab;
        }

        public static void GrabLastSceneFocus()
        {
            EditorApplication.delayCall += DelayedGrabSceneFocus; // GC error caused when moving scene focus back during the same frame as opening the player window
        }

        private static void DelayedGrabSceneFocus()
        {
            SceneView.lastActiveSceneView.Focus();
        }


        public static void ShowAnimationPlayer()
        {
            GameObject scenePrefab = GetSelectedOrPreviewCharacter();

            if (scenePrefab)
            {
                AnimPlayerGUI.OpenPlayer(scenePrefab);
                openedInPreviewScene = IsPreviewScene;

                if (showRetarget) ShowAnimationRetargeter();

                showPlayer = true;
                if (EditorApplication.isPlaying)
                {
                    WindowManager.GrabLastSceneFocus();
                }
            }
            else
            {
                Util.LogWarn("No compatible animated character!");
            }
        }

        public static void HideAnimationPlayer(bool updateShowPlayer)
        {
            if (AnimPlayerGUI.IsPlayerShown())
            {
                AnimPlayerGUI.ResetFace();
                AnimPlayerGUI.ResetCharacterPose();
                AnimPlayerGUI.ClosePlayer();
            }
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
        
        public static void StartTimer(float delay)
        {
            timer = delay;
        }        
    }
}
