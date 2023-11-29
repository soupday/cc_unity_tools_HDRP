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
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEditor.Animations;
using System;

namespace Reallusion.Import
{
    public static class AnimPlayerGUI
    {
        #region AnimPlayer  

        //private static bool play = false;        
        //private static float time, prev, current = 0f;
        public static bool AnimFoldOut { get; private set; } = true;
        public static FacialProfile MeshFacialProfile { get; private set; }
        public static FacialProfile ClipFacialProfile { get; private set; }        
        public static AnimationClip OriginalClip { get; set; }        
        public static AnimationClip WorkingClip { get ; set; }
        public static Animator CharacterAnimator { get; set; }

        //private static double updateTime = 0f;
        //private static double deltaTime = 0f;
        //private static double frameTime = 1f;
        
        private static bool forceUpdate = false;
        private static FacialProfile defaultProfile = new FacialProfile(ExpressionProfile.ExPlus, VisemeProfile.PairsCC3);

        public static void OpenPlayer(GameObject scenePrefab)
        {
            if (scenePrefab)
            {
                //scenePrefab = Util.TryResetScenePrefab(scenePrefab);                
                SetCharacter(scenePrefab);
            }

            if (!IsPlayerShown())
            {
#if SCENEVIEW_OVERLAY_COMPATIBLE
                //2021.2.0a17+  When GUI.Window is called from a static SceneView delegate, it is broken in 2021.2.0f1 - 2021.2.1f1
                //so we switch to overlays starting from an earlier version
                AnimPlayerOverlay.ShowAll();
#else
                //2020 LTS            
                AnimPlayerWindow.ShowPlayer();
#endif

                //Common            
                SceneView.RepaintAll();

                EditorApplication.update -= UpdateCallback;
                EditorApplication.update += UpdateCallback;
                EditorApplication.playModeStateChanged -= PlayStateChangeCallback;
                EditorApplication.playModeStateChanged += PlayStateChangeCallback;
            }
        }

        public static void ClosePlayer()  
        {
            if (IsPlayerShown())
            {
                //clean up controller here
                ResetToBaseAnimatorController();

                EditorApplication.update -= UpdateCallback;
                EditorApplication.playModeStateChanged -= PlayStateChangeCallback;

                //if (CharacterAnimator)       
                ///{
                    //GameObject scenePrefab = Util.GetScenePrefabInstanceRoot(CharacterAnimator.gameObject);
                    //Util.TryResetScenePrefab(scenePrefab);
                //}

#if SCENEVIEW_OVERLAY_COMPATIBLE
                //2021.2.0a17+          
                AnimPlayerOverlay.HideAll();
#else
                //2020 LTS            
                AnimPlayerWindow.HidePlayer();
#endif
                //Common
                play = false;
                time = 0f;
                CharacterAnimator = null;
                WorkingClip = null;

                SceneView.RepaintAll();
            }
        }

        public static bool IsPlayerShown()
        {
#if SCENEVIEW_OVERLAY_COMPATIBLE
            //2021.2.0a17+
            return AnimPlayerOverlay.Visibility;
#else
            //2020 LTS            
            return AnimPlayerWindow.isShown;
#endif
        }

        public static void SetCharacter(GameObject scenePrefab)
        {
            if (scenePrefab)
                Util.LogDetail("scenePrefab.name: " + scenePrefab.name + " " + PrefabUtility.IsPartOfPrefabInstance(scenePrefab));            

            if (!scenePrefab && WindowManager.IsPreviewScene)
                scenePrefab = WindowManager.GetPreviewScene().GetPreviewCharacter();

            if (scenePrefab)  
            {                                
                Animator animator = scenePrefab.GetComponent<Animator>();
                if (!animator) animator = scenePrefab.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(scenePrefab))
                    {
                        GameObject sceneFbx = Util.FindRootPrefabAssetFromSceneObject(scenePrefab);
                        // in edit mode - find the first animation clip
                        AnimationClip clip = Util.GetFirstAnimationClipFromCharacter(sceneFbx);
                        if (sceneFbx && clip)
                            clip = AnimRetargetGUI.TryGetRetargetedAnimationClip(sceneFbx, clip);
                        UpdateAnimatorClip(animator, clip);
                    }
                    else
                    {
                        if (EditorApplication.isPlaying)
                        {
                            // in play mode - try to recover the stored last played animation
                            if (Util.TryDeSerializeAssetFromEditorPrefs<AnimationClip>(out Object obj, WindowManager.clipKey))
                            {
                                UpdateAnimatorClip(animator, obj as AnimationClip);
                            }
                        }
                    }
                }
            }         
        }

        static public void UpdateAnimatorClip(Animator animator, AnimationClip clip)
        {
            if (doneInitFace) ResetFace(true, true);

            if (!animator || CharacterAnimator != animator) doneInitFace = false;

            CharacterAnimator = animator;            
            OriginalClip = clip;
            
            SetupCharacterAndAnimation();

            AnimRetargetGUI.RebuildClip();

            MeshFacialProfile = FacialProfileMapper.GetMeshFacialProfile(animator ? animator.gameObject : null);
            ClipFacialProfile = FacialProfileMapper.GetAnimationClipFacialProfile(clip);
            
            time = 0f;
            play = false;            

            // intitialise the face refs if needed
            if (!doneInitFace) InitFace();

            // finally, apply the face
            ApplyFace();
        }

        #region Animator Setup
        // ----------------------------------------------------------------------------
        public static bool showMessages = false;
        public static bool sceneFocus = false;

        // initial selection
        [SerializeField]
        private static AnimatorController originalAnimatorController;

        // Animaton Controller
        [SerializeField]
        private static AnimatorController playbackAnimatorController;
        private static string controllerName = "--Temp-CCiC-Animator-Controller";
        private static string overrideName = "--Temp-CCiC-Override-Controller";
        private static string dirString = "Assets/";
        private static string controllerPath;
        private static string defaultState = "default_state";
        [SerializeField]
        private static int defaultStateNameHash;
        private static string paramDirection = "param_direction";
        [SerializeField]
        private static AnimatorState playingState;
        [SerializeField]
        public static int controlStateHash { get; set; }

        // animator/animation settings
        public static bool FootIK = true;
        [Flags]
        enum AnimatorFlags
        {
            None = 0,
            AnimateOnTheSpot = 1,
            ShowMirrorImage = 2,
            AutoLoopPlayback = 4,
            Everything = ~0
        }

        static AnimatorFlags flagSettings;

        // Animaton Override Controller
        [SerializeField]
        public static AnimatorOverrideController animatorOverrideController;
        

        // Playback
        private static bool play = false;
        private static float playbackSpeed = 1f;

        [SerializeField]
        public static float time = 0f;
        private static string realTime = "";

        // Update
        private static double updateTime = 0f;
        private static double deltaTime = 0f;
        private static double frameTime = 1f;
        private static double current = 0f;

        [SerializeField]
        private static bool wasPlaying;

        // GUIStyles
        private static Styles guiStyles;

        [SerializeField] private static List<BoneItem> boneItemList;
        [SerializeField] public static bool isTracking = false;
        [SerializeField] public static GameObject lastTracked;
        [SerializeField] public static bool trackingPermitted = true;
        private static string boneNotFound = "not found";

        // ----------------------------------------------------------------------------

        public static void SetupCharacterAndAnimation()
        {
            if (CharacterAnimator == null) return; // don't try and set up an override controller if the char has no animator

            // retain the original AnimatorController from the scene model
            //originalAnimatorController = GetControllerFromAnimator(CharacterAnimator);

            // construct and use a new controller with specific parameters
            playbackAnimatorController = CreateAnimatiorController();
            CharacterAnimator.runtimeAnimatorController = playbackAnimatorController;

            // create an animation override controller from the new controller
            animatorOverrideController = CreateAnimatorOverrideController();
            CharacterAnimator.runtimeAnimatorController = animatorOverrideController;

            // defaults contain some flags that must be set in the selected animation
            ApplyDefaultSettings();

            // select the original clip using the override controller
            // this method is normally used to switch animations during play mode            
            SelectOverrideAnimation(OriginalClip, animatorOverrideController);

            // reset the animation player
            ResetAnimationPlayer();
        }
        
        private static AnimatorController CreateAnimatiorController()
        {
            controllerPath = dirString + controllerName + ".controller";

            Util.LogDetail("Creating Temporary file " + controllerPath);
            AnimatorController a = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            a.name = controllerName;
            // play mode parameters
            a.AddParameter(paramDirection, AnimatorControllerParameterType.Float);
            AnimatorStateMachine rootStateMachine = a.layers[0].stateMachine;
            AnimatorState baseState = rootStateMachine.AddState(defaultState);            
            baseState.iKOnFeet = FootIK;
            // play mode parameters
            baseState.speedParameter = paramDirection;
            baseState.speedParameterActive = true;
            baseState.motion = OriginalClip;
            playingState = baseState;
            controlStateHash = baseState.nameHash;
            return a;
        }

        private static AnimatorOverrideController CreateAnimatorOverrideController()
        {
            var aoc = new AnimatorOverrideController(CharacterAnimator.runtimeAnimatorController);
            aoc.name = overrideName;
            return aoc;
        }

        private static void ApplyDefaultSettings()
        {
            flagSettings = AnimatorFlags.AutoLoopPlayback;
            SetFootIK(true);
            CharacterAnimator.SetFloat(paramDirection, 0f);
            if (CharacterAnimator != null)
            {
                CharacterAnimator.applyRootMotion = true;
            }
        }

        // Important IK enable/disable function used by the retargeter
        // (or you can't see the effects of changing the heel curves)
        public static void SetFootIK(bool enable)
        {
            FootIK = enable;

            if (playbackAnimatorController)
            {
                AnimatorControllerLayer[] allLayer = playbackAnimatorController.layers;
                for (int i = 0; i < allLayer.Length; i++)
                {
                    ChildAnimatorState[] states = allLayer[i].stateMachine.states;
                    for (int j = 0; j < states.Length; j++)
                    {
                        if (states[j].state.nameHash == controlStateHash)
                        {
                            states[j].state.iKOnFeet = FootIK;
                            allLayer[i].iKPass = FootIK;
                        }
                    }
                }

                if (EditorApplication.isPlaying) CharacterAnimator.gameObject.SetActive(false);
                playbackAnimatorController.layers = allLayer;
                if (EditorApplication.isPlaying) CharacterAnimator.gameObject.SetActive(true);                
            }
        }

        // called by the retargetter to revert all settings to default
        public static void ForceSettingsReset()
        {
            ApplyDefaultSettings();
            SetFootIK(false);
            SetClipSettings(WorkingClip);
            FirstFrameButton();
        }

        private static void SelectOverrideAnimation(AnimationClip clip, AnimatorOverrideController aoc)
        {
            ResetAnimationPlayer();            
            var clone = GameObject.Instantiate(clip);
            clone.name = clip.name;
            SetClipSettings(clone);  // update the bake flags in AnimationClipSettings
                                     // origingal clipsettings are untouched and should be copied
                                     // directly into any saved animations from the retargeter
            WorkingClip = clone;

            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(aoc.overridesCount);
            aoc.GetOverrides(overrides);

            foreach (var v in overrides)
            {
                Util.LogDetail("Overrides: " + " Key: " + v.Key + " Value: " + v.Value);
            }

            overrides[0] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[0].Key, WorkingClip);
            aoc.ApplyOverrides(overrides);
            FirstFrameButton();
        }

        public static void SelectOverrideAnimationWithoutReset(AnimationClip clip, AnimatorOverrideController aoc)
        {
            WorkingClip = clip;

            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(aoc.overridesCount);
            aoc.GetOverrides(overrides);

            foreach (var v in overrides)
            {
                Util.LogDetail("Overrides: " + " Key: " + v.Key + " Value: " + v.Value);
            }

            overrides[0] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[0].Key, WorkingClip);
            aoc.ApplyOverrides(overrides);
        }

        private static void ResetAnimationPlayer()
        {
            play = false;
            time = 0f;
            playbackSpeed = 1f;

            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.SetFloat(paramDirection, 0f);
                CharacterAnimator.Play(controlStateHash, 0, time);
            }
            else
            {
                CharacterAnimator.Update(time);
            }
            CharacterAnimator.gameObject.transform.localPosition = Vector3.zero;
            CharacterAnimator.gameObject.transform.rotation = Quaternion.identity;
        }

        private static void SetClipSettings(AnimationClip clip)
        {
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.mirror = flagSettings.HasFlag(AnimatorFlags.ShowMirrorImage);
            clipSettings.loopBlendPositionXZ = !flagSettings.HasFlag(AnimatorFlags.AnimateOnTheSpot);
            clipSettings.loopBlendPositionY = !flagSettings.HasFlag(AnimatorFlags.AnimateOnTheSpot);
            clipSettings.loopBlendOrientation = !flagSettings.HasFlag(AnimatorFlags.AnimateOnTheSpot);
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            CharacterAnimator.applyRootMotion = !flagSettings.HasFlag(AnimatorFlags.AnimateOnTheSpot);
        }

        public static void ResetToBaseAnimatorController()
        {
            // look up the original prefab corresponding to the model in the preview scene
            // extract the path reference to the animator controller being used by the original prefab
            // check the scene model is using the override controller created above
            // replace the runtime animator controller of the scene model with the animatorcontroller asset at path
            // destroy the disk asset temp override controller (that was created above)

            GameObject characterPrefab = Util.GetScenePrefabInstanceRoot(CharacterAnimator);

            if (!characterPrefab) return;

            Util.LogDetail(("Attempting to reset: " + characterPrefab.name));            

            GameObject basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(characterPrefab);

            if (basePrefab != null)
            {
                if (true) //(PrefabUtility.IsAnyPrefabInstanceRoot(basePrefab))
                {
                    string prefabPath = AssetDatabase.GetAssetPath(basePrefab);
                    Util.LogDetail((basePrefab.name + "Prefab instance root found: " + prefabPath));

                    Util.LogDetail("Loaded Prefab: " + basePrefab.name);
                    Animator baseAnimator = basePrefab.GetComponent<Animator>();
                    if (!baseAnimator) baseAnimator = basePrefab.GetComponentInChildren<Animator>();
                    if (baseAnimator != null)
                    {
                        Util.LogDetail("Prefab Animator: " + baseAnimator.name);
                        if (baseAnimator.runtimeAnimatorController)
                        {
                            Util.LogDetail("Prefab Animator Controller: " + baseAnimator.runtimeAnimatorController.name);
                            string controllerpath = AssetDatabase.GetAssetPath(baseAnimator.runtimeAnimatorController);
                            Util.LogDetail("Prefab Animator Controller Path: " + controllerpath);
                            AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerpath);

                            if (CharacterAnimator.runtimeAnimatorController != null)
                            {
                                // ensure the created override controller is the one on the animator
                                // to avoid wiping user generated controller (it will have to be a disk asset - but nevertheless)
                                Util.LogDetail("Current controller on character: " + CharacterAnimator.runtimeAnimatorController.name);
                                if (CharacterAnimator.runtimeAnimatorController.GetType() == typeof(AnimatorOverrideController) && CharacterAnimator.runtimeAnimatorController.name == overrideName)
                                {
                                    Util.LogDetail("Created override controller found: can reset");
                                    CharacterAnimator.runtimeAnimatorController = baseController;
                                }
                            }
                        }
                        else
                        {
                            Util.LogDetail("NO Prefab Animator Controller");
                            CharacterAnimator.runtimeAnimatorController = null;
                        }
                    }
                }
            }
            DestroyAnimationController();
        }

        private static void DestroyAnimationController()
        {
            Object tempControllerAsset = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (tempControllerAsset != null)
            {
                if (tempControllerAsset.GetType() == typeof(AnimatorController))
                {
                    //if (showMessages) 

                    Util.LogDetail("Override controller: " + controllerPath + " exists -- removing");
                    AssetDatabase.DeleteAsset(controllerPath);
                }
            }
        }

        #endregion Animator Setup

        public static void ReCloneClip()
        {
            WorkingClip = CloneClip(OriginalClip);

            time = 0f;
            play = false;            
        }

        public static AnimationClip CloneClip(AnimationClip clip)
        {
            if (clip)
            {
                var clone = Object.Instantiate(clip);
                clone.name = clip.name;
                AnimationClip clonedClip = clone as AnimationClip;                

                return clonedClip;
            }
            
            return null;            
        }

        #region IMGUI
        public class Styles
        {
            public GUIStyle settingsButton;
            public GUIStyle playbackLabelStyle;
            public GUIStyle playIconStyle;
            public GUIStyle trackIconStyle;

            public Styles()
            {
                settingsButton = new GUIStyle("toolbarbutton");
                playbackLabelStyle = new GUIStyle("label");
                playbackLabelStyle.alignment = TextAnchor.MiddleRight;

                playIconStyle = new GUIStyle("label");
                playIconStyle.contentOffset = new Vector2(5f, -4f);

                trackIconStyle = new GUIStyle("label");
                trackIconStyle.contentOffset = new Vector2(-6f, -4f);

            }
        }

        public static void DrawPlayer()
        {
            if (guiStyles == null)
                guiStyles = new Styles();

            GUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            AnimFoldOut = EditorGUILayout.Foldout(AnimFoldOut, "Animation Playback", EditorStyles.foldout);
            if (EditorGUI.EndChangeCheck())
            {
                //if (foldOut && FacialMorphIMGUI.FoldOut)
                //    FacialMorphIMGUI.FoldOut = false;
                doOnceCatchMouse = true;
            }
            if (AnimFoldOut)
            {
                EditorGUI.BeginChangeCheck();
                Animator selectedAnimator = (Animator)EditorGUILayout.ObjectField(new GUIContent("Scene Model", "Animated model in scene"), CharacterAnimator, typeof(Animator), true);
                AnimationClip selectedClip = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Animation", "Animation to play and manipulate"), OriginalClip, typeof(AnimationClip), false);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateAnimatorClip(selectedAnimator, selectedClip);
                }

                GUI.enabled = WorkingClip && CharacterAnimator;




                // BEGIN PREFS AREA

                EditorGUILayout.Space();
                var rect = EditorGUILayout.BeginHorizontal();
                Handles.color = Color.gray * 0.2f;
                Handles.DrawLine(new Vector2(rect.x + 15, rect.y), new Vector2(rect.width - 15, rect.y));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();

                GUILayout.Space(4f);
                EditorGUI.BeginDisabledGroup(AnimRetargetGUI.IsPlayerShown());
                EditorGUI.BeginChangeCheck();
                FootIK = GUILayout.Toggle(FootIK, new GUIContent("IK", FootIK ? "Toggle feet IK - Currently: ON" : "Toggle feet IK - Currently: OFF"), guiStyles.settingsButton, GUILayout.Width(24f), GUILayout.Height(24f));
                if (EditorGUI.EndChangeCheck())
                {
                    ToggleIKButton();
                }
                EditorGUI.EndDisabledGroup();
                // Camera Bone Tracker

                if (!CheckTackingStatus())
                    CancelBoneTracking(false); //object focus lost - arrange ui to reflect that, but dont fight with the scene camera

                EditorGUI.BeginDisabledGroup(!trackingPermitted);
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Camera Gizmo").image, "Select individual bone to track with the scene camera."), guiStyles.settingsButton, GUILayout.Width(24f), GUILayout.Height(24f)))
                {
                    GenerateBoneMenu();
                }
                Texture cancelTrack = isTracking ? EditorGUIUtility.IconContent("toolbarsearchCancelButtonActive").image : EditorGUIUtility.IconContent("toolbarsearchCancelButtonOff").image;
                string cancelTrackTxt = isTracking ? "Cancel camera tracking" : "Camera tracking controls.";

                EditorGUI.BeginDisabledGroup(!isTracking);
                if (GUILayout.Button(new GUIContent(cancelTrack, cancelTrackTxt), guiStyles.trackIconStyle, GUILayout.Width(24f), GUILayout.Height(24f)))
                {
                    CancelBoneTracking(true); //tracking deliberately cancelled - leave scene camera in last position with last tracked object still selected
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                playbackSpeed = GUILayout.HorizontalSlider(playbackSpeed, -1f, 2f, GUILayout.Width(130f));
                if (Math.Abs(1 - playbackSpeed) < 0.1f)
                    playbackSpeed = 1f;
                if (EditorGUI.EndChangeCheck())
                {
                    PlaybackSpeedSlider();
                }
                GUILayout.Label(new GUIContent(PlaybackText(), "Playback Speed"), guiStyles.playbackLabelStyle, GUILayout.MaxWidth(43f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Refresh").image, "Reset model to T-Pose and player to defaults"), guiStyles.settingsButton, GUILayout.Width(24f), GUILayout.Height(24f)))
                {
                    ResetCharacterAndPlayer();
                }

                EditorGUI.BeginDisabledGroup(AnimRetargetGUI.IsPlayerShown());
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d__Menu").image, "Animation Clip Preferences"), guiStyles.settingsButton, GUILayout.Width(20f), GUILayout.Height(20f)))
                {
                    ShowPrefsGenericMenu();
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.Space(4f);

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.EndHorizontal();

                EditorGUILayout.Space();
                var rect2 = EditorGUILayout.BeginHorizontal();
                Handles.color = Color.gray * 0.2f; //new Color(0.1372f, 0.1372f, 0.1372f, 1.0f);
                Handles.DrawLine(new Vector2(rect2.x + 15f, rect2.y), new Vector2(rect2.width - 15f, rect2.y));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                // END PREFS AREA

                GUILayout.BeginHorizontal();
                if (CharacterAnimator && WorkingClip)
                {
                    EditorGUI.BeginChangeCheck();
                    time = GUILayout.HorizontalSlider(time, 0f, 1f, GUILayout.Height(24f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ScrubTimeline();
                    }

                    realTime = TimeText();
                    EditorGUI.BeginChangeCheck();
                    realTime = EditorGUILayout.DelayedTextField(new GUIContent("", "Time index. Accepts numerical input."), realTime, GUILayout.MaxWidth(42f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ParseTimeInput();
                    }
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.HorizontalSlider(0f, 0f, 1f, GUILayout.Height(24f));
                    EditorGUILayout.DelayedTextField(new GUIContent("", "Time index. Accepts numerical input."), "", GUILayout.MaxWidth(42f));
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginDisabledGroup(!(CharacterAnimator && WorkingClip));
                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                // "Animation.FirstKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.FirstKey").image, "First Frame"), EditorStyles.toolbarButton))
                {
                    FirstFrameButton();
                }

                // "Animation.PrevKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.PrevKey").image, "Previous Frame"), EditorStyles.toolbarButton))
                {
                    PrevFrameButton();
                }

                // "Animation.Play"
                Texture playButton = playbackSpeed > 0 ? EditorGUIUtility.IconContent("d_forward@2x").image : EditorGUIUtility.IconContent("d_back@2x").image;
                Texture pauseButton = EditorGUIUtility.IconContent("PauseButton").image;
                
                // play/pause: "Animation.Play" / "PauseButton"

                if (GUILayout.Button(new GUIContent(play ? pauseButton : playButton, play ? "Pause" : "Play"), EditorStyles.toolbarButton, GUILayout.Height(30f)))
                {
                    PlayPauseButton();
                }

                // "Animation.NextKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.NextKey").image, "Next Frame"), EditorStyles.toolbarButton))
                {
                    NextFrameButton();
                }

                // "Animation.LastKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.LastKey").image, "Last Frame"), EditorStyles.toolbarButton))
                {
                    LastFrameButton();
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.Space(10f);
                //GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.GameView").image, "Controls for 'Play Mode'"), guiStyles.playIconStyle, GUILayout.Width(24f), GUILayout.Height(24f));
                                
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_ViewToolOrbit On").image, "Select the character root."), EditorStyles.toolbarButton))
                {
                    if (ColliderManagerEditor.EditMode)
                    {
                        Selection.activeObject = null;
                    }
                    else
                    {
                        Selection.activeObject = selectedAnimator.gameObject;
                    }
                        
                }

                Texture bigPlayButton = EditorApplication.isPlaying ? EditorGUIUtility.IconContent("preAudioPlayOn").image : EditorGUIUtility.IconContent("preAudioPlayOff").image;
                string playToggleTxt = EditorApplication.isPlaying ? "Exit 'Play Mode'." : "Enter 'Play Mode' and focus on the scene view window. This is to be used to evaluate play mode physics whilst allowing visualization of objects such as colliders.";
                if (GUILayout.Button(new GUIContent(bigPlayButton, playToggleTxt), EditorStyles.toolbarButton, GUILayout.Height(24f), GUILayout.Width(60f)))
                {
                    ApplicationPlayToggle();
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        #endregion IMGUI
        #region Button Events/Functions
        // Button functions

        // "Toggle feet IK"
        private static void ToggleIKButton()
        {
            // Alternative method - retrieve a copy of the layers - modify then reapply
            // find the controlstate by nameHash
            SetFootIK(FootIK);

            // originally using the cached default state directly ...
            // both methods encounter errors when changing foot ik during runtime
            // unless the gameObject is disabled/re-enabled
            /*
            if (EditorApplication.isPlaying) sceneAnimator.gameObject.SetActive(false);
            playingState.iKOnFeet = FootIK;
            if (EditorApplication.isPlaying) sceneAnimator.gameObject.SetActive(true);
            */
            if (EditorApplication.isPlaying)
            {
                //ResetAnimationPlayer();
                CharacterAnimator.Play(controlStateHash, 0, time);
                CharacterAnimator.SetFloat(paramDirection, play ? playbackSpeed : 0f);
            }
            else
            {
                CharacterAnimator.Update(time);
            }
        }
        
        // playback speed slider sets speed multiplier directly in edit mode but requires an update in play mode
        private static void PlaybackSpeedSlider()
        {
            if (EditorApplication.isPlaying)
            {
                if (play)
                    CharacterAnimator.SetFloat(paramDirection, playbackSpeed);
                else
                    CharacterAnimator.SetFloat(paramDirection, 0f);

                CharacterAnimator.Play(controlStateHash, 0, time);
            }
        }

        // "Reset Model to T-Pose and player to defaults"
        public static void ResetCharacterAndPlayer()
        {
            // re-apply all defaults
            ApplyDefaultSettings();

            // reset the player
            ResetAnimationPlayer();

            // clear all the animation data
            WorkingClip = null;
            OriginalClip = null;
            
            // clear the animation controller + override controller
            // remove the on-disk temporary controller
            ResetToBaseAnimatorController();
            
            //DestroyAnimationController();
            // revert character pose to original T-Pose
            ResetCharacterPose();
            // user can now select a new animation for playing
        }

        // "Animation Clip Preferences"
        private static void ShowPrefsGenericMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Animate On The Spot"), flagSettings.HasFlag(AnimatorFlags.AnimateOnTheSpot), OnPrefSelected, AnimatorFlags.AnimateOnTheSpot);
            menu.AddItem(new GUIContent("Show Mirror Image"), flagSettings.HasFlag(AnimatorFlags.ShowMirrorImage), OnPrefSelected, AnimatorFlags.ShowMirrorImage);
            menu.AddItem(new GUIContent("Auto Loop Playback"), flagSettings.HasFlag(AnimatorFlags.AutoLoopPlayback), OnPrefSelected, AnimatorFlags.AutoLoopPlayback);
            menu.ShowAsContext();
        }

        private static void OnPrefSelected(object obj)
        {
            AnimatorFlags f = (AnimatorFlags)obj;
            if (flagSettings.HasFlag(f))
                flagSettings ^= f;
            else
                flagSettings |= f;

            if (WorkingClip)
            {
                if (f == AnimatorFlags.AnimateOnTheSpot || f == AnimatorFlags.ShowMirrorImage)
                {
                    SetClipSettings(WorkingClip);
                    ResetAnimationPlayer();
                }
            }
        }

        // Time Slider
        public static void ScrubTimeline()
        {
            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.Play(controlStateHash, 0, time);
            }
            else
            {
                UpdateAnimator();
            }
        }

        // "Time index. Accepts numerical input."
        private static void ParseTimeInput()
        {
            float parsedTime;
            if (float.TryParse(realTime, out parsedTime))
            {
                if (parsedTime > WorkingClip.length)
                    parsedTime = WorkingClip.length;
                if (parsedTime < 0f)
                    parsedTime = 0f;

                time = parsedTime / WorkingClip.length;
            }
            else
            {
                realTime = TimeText();
            }
            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.Play(controlStateHash, 0, time);
            }
        }

        // "Animation.FirstKey"
        private static void FirstFrameButton()
        {
            play = false;
            time = 0f;

            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.Play(controlStateHash, 0, time);
                CharacterAnimator.SetFloat(paramDirection, 0f);
            }
            else
            {
                UpdateAnimator();
            }
        }

        // "Animation.PrevKey"
        private static void PrevFrameButton()
        {
            play = false;
            time -= 0.0166f / WorkingClip.length;

            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.Play(controlStateHash, 0, time);
                CharacterAnimator.SetFloat(paramDirection, 0f);
            }
            else
            {
                UpdateAnimator();
            }
        }

        // "Animation.Play"
        private static void PlayButton()
        {
            play = true;
            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.Play(controlStateHash, 0, time);
                CharacterAnimator.SetFloat(paramDirection, playbackSpeed);
            }
            else
            {
                CharacterAnimator.Update(time);
                CharacterAnimator.Play(controlStateHash, 0, time);
            }
        }

        // "PauseButton"
        private static void PauseButton()
        {
            play = false;
            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.SetFloat(paramDirection, 0f);
            }
        }

        // "Animation.Play" / "PauseButton"
        private static void PlayPauseButton()
        {
            if (play)
                PauseButton();
            else
                PlayButton();
        }

        // "Animation.NextKey"
        private static void NextFrameButton()
        {
            play = false;
            time += 0.0166f / WorkingClip.length;

            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.Play(controlStateHash, 0, time);
                CharacterAnimator.SetFloat(paramDirection, 0f);
            }
            else
            {
                UpdateAnimator();
            }
        }

        // "Animation.LastKey"
        private static void LastFrameButton()
        {
            play = false;
            time = 1f;

            if (EditorApplication.isPlaying)
            {
                CharacterAnimator.Play(controlStateHash, 0, time);
                CharacterAnimator.SetFloat(paramDirection, 0f);
            }
            else
            {
                UpdateAnimator();
            }
        }

        private static void ApplicationPlayToggle()
        {
            // button to enter play mode and retain scene view
            //
            // if the application is not playing and will enter play mode:
            //                              set the flag to true 
            //                              callback will focus the view back to the scene window
            // if the application is playing:
            //                              set the flag to false
            //                              entering play mode maually wont cause callback to refocus on the scene
            if (!EditorApplication.isPlaying)
                Util.SerializeBoolToEditorPrefs(true, WindowManager.sceneFocus);
            
            EditorApplication.isPlaying = !EditorApplication.isPlaying;
        }

        public static void UpdateAnimator()
        {
            if (EditorApplication.isPlaying || CharacterAnimator == null) return;
            if (CharacterAnimator.runtimeAnimatorController)  // delayed call to grab scene focus may cause null reference error with absent controller
            {
                if (CharacterAnimator.runtimeAnimatorController.name == overrideName)
                {
                    CharacterAnimator.Update(0f);
                    CharacterAnimator.Play(controlStateHash, 0, time);
                }
            }
        }

        private static string TimeText()
        {
            return string.Format("{0}s", (time * WorkingClip.length).ToString("0.00"));
        }

        private static string PlaybackText()
        {
            return string.Format("{0}x", playbackSpeed.ToString("0.00"));
        }

        #endregion Button Events/Functions

        #region Pose reset and bone tracking
        public static void ResetCharacterPose()
        {
            bool canFindAvatar = false;
            if (CharacterAnimator != null)
            {
                if (CharacterAnimator.avatar != null)
                {
                    canFindAvatar = true;
                }
            }
            if (!canFindAvatar)
            {
                Util.LogWarn("No Avatar found to reset pose to.");
                return;
            }

            Avatar characterAvatar = CharacterAnimator.avatar;
            SkeletonBone[] characterBones = characterAvatar.humanDescription.skeleton; // array of all imported objects now in the prefab (has CC names) in T-pose
            Transform[] prefabObjects = CharacterAnimator.gameObject.GetComponentsInChildren<Transform>();

            int boneIndex;

            foreach (string humanBoneName in HumanTrait.BoneName)
            {
                // find the characaterBones array indices corresponding to the mechanim bones (listed in HumanTrait.BoneName    
                boneIndex = FindSkeletonBoneIndex(FindSkeletonBoneName(humanBoneName, characterAvatar), characterBones);

                // iterate through all the transforms in the prefab and when a mechanim bone is matched - set it's transform to that in the correspoding skeletonbone struct (obtained from the avatar)
                if (boneIndex != -1)
                {
                    foreach (Transform t in prefabObjects)
                    {
                        if (t.name == characterBones[boneIndex].name)
                        {
                            t.localPosition = characterBones[boneIndex].position;
                            t.localRotation = characterBones[boneIndex].rotation;
                            t.localScale = characterBones[boneIndex].scale;
                        }
                    }
                }
            }
        }

        private static int FindSkeletonBoneIndex(string skeletonBoneName, SkeletonBone[] bones)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].name.Equals(skeletonBoneName, System.StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }
            return -1;
        }

        public static string FindSkeletonBoneName(string humanBoneName, Avatar avatar)
        {            
            for (int i = 0; i < avatar.humanDescription.human.Length; i++)
            {
                if (avatar.humanDescription.human[i].humanName.Equals(humanBoneName, System.StringComparison.InvariantCultureIgnoreCase))
                    return avatar.humanDescription.human[i].boneName;
            }
            return boneNotFound;
        }

        public class BoneItem
        {
            public string humanBoneName;
            public string skeletonBoneName;
            public bool selected;

            public BoneItem(string humanBoneName, string skeletonBoneName)
            {
                this.humanBoneName = humanBoneName;
                this.skeletonBoneName = skeletonBoneName;
                this.selected = false;
            }
        }

        private static void MakeBoneMenuList()
        {
            boneItemList = new List<BoneItem>();

            foreach (string boneName in orderedHumanBones)
            {
                string skeletonBoneName = FindSkeletonBoneName(boneName, CharacterAnimator.avatar);
                if (skeletonBoneName != boneNotFound)
                    boneItemList.Add(new BoneItem(boneName, skeletonBoneName));
                //boneItemList.Add(new BoneItem(boneName, FindSkeletonBoneName(boneName, CharacterAnimator.avatar)));
            }
        }

        private static void GenerateBoneMenu()
        {
            if (boneItemList == null)
                MakeBoneMenuList();

            GenericMenu menu = new GenericMenu();
            foreach (BoneItem boneItem in boneItemList)
            {
                menu.AddItem(new GUIContent(boneItem.humanBoneName), boneItem.selected, BoneMenuCallback, boneItem);
            }
            menu.ShowAsContext();
        }

        private static void BoneMenuCallback(object obj)
        {
            DeselectAllBones();

            BoneItem item = obj as BoneItem;
            if (TrySelectBone(item))
            {
                isTracking = true;
                TrackBone(item);
            }
        }

        private static void TrackBone(BoneItem boneItem)
        {
            SceneView scene = SceneView.lastActiveSceneView;
            GameObject g = GameObject.Find(boneItem.skeletonBoneName);
            Selection.activeGameObject = g;
            lastTracked = g;
            scene.FrameSelected(true, true);
            scene.FrameSelected(true, true);
            scene.Repaint();
        }

        public static void ForbidTracking() 
        {
            // this is called by the collider manager editor script to use its own tracking while editing colliders
            // this avoids having an objected selected since its control handles will be visible and cause problems
            
            if (isTracking)
            {
                isTracking = false;
                Selection.activeObject = null;
            }

            trackingPermitted = false;

            if (boneItemList != null)
            {
                foreach (BoneItem boneItem in boneItemList)
                {
                    if (boneItem.selected)
                        boneItem.selected = false;
                }
            }
        }

        public static void AllowTracking()
        {
            trackingPermitted = true;
        }

        public static void ReEstablishTracking(string humanBoneName)
        {
            //if (boneItemList == null)
            MakeBoneMenuList();
            foreach (BoneItem boneItem in boneItemList)
            {
                if (boneItem.humanBoneName == humanBoneName)
                {
                    boneItem.selected = true;
                    isTracking = true;
                    TrackBone(boneItem);
                    return;
                }
            }
        }

        private static bool TrySelectBone(BoneItem boneSelection)
        {
            int idx = boneItemList.FindIndex(x => x.humanBoneName == boneSelection.humanBoneName);
            bool select = (idx != -1);
            if (select)
                boneItemList[idx].selected = true;

            return select;
        }

        private static bool CheckTackingStatus()
        {
            return Selection.activeGameObject == lastTracked;
        }

        private static void CancelBoneTracking(bool refocusScene)
        {
            if (refocusScene)
                StopTracking();

            DeselectAllBones();
            isTracking = false;
        }

        private static void StopTracking()
        {
            if (isTracking)
            {
                SceneView scene = SceneView.lastActiveSceneView;
                scene.FrameSelected(false, false);
                scene.FrameSelected(false, false);
            }
        }

        private static void DeselectAllBones()
        {
            if (boneItemList == null) return;
            foreach (BoneItem boneItem in boneItemList)
            {
                boneItem.selected = false;
            }
        }

        private static string[] orderedHumanBones =
        {
            "LeftEye",
            "RightEye",
            "Jaw",
            "Head",
            "Neck",
            "LeftShoulder",
            "RightShoulder",
            "LeftUpperArm",
            "RightUpperArm",
            "LeftLowerArm",
            "RightLowerArm",
            "LeftHand",
            "RightHand",
            "UpperChest",
            "Chest",
            "Spine",
            "Hips",
            "LeftUpperLeg",
            "RightUpperLeg",
            "LeftLowerLeg",
            "RightLowerLeg",
            "LeftFoot",
            "RightFoot",
            "LeftToes",
            "RightToes"
            /*
            "Left Thumb Proximal",
            "Left Thumb Intermediate",
            "Left Thumb Distal",
            "Left Index Proximal",
            "Left Index Intermediate",
            "Left Index Distal",
            "Left Middle Proximal",
            "Left Middle Intermediate",
            "Left Middle Distal",
            "Left Ring Proximal",
            "Left Ring Intermediate",
            "Left Ring Distal",
            "Left Little Proximal",
            "Left Little Intermediate",
            "Left Little Distal",
            "Right Thumb Proximal",
            "Right Thumb Intermediate",
            "Right Thumb Distal",
            "Right Index Proximal",
            "Right Index Intermediate",
            "Right Index Distal",
            "Right Middle Proximal",
            "Right Middle Intermediate",
            "Right Middle Distal",
            "Right Ring Proximal",
            "Right Ring Intermediate",
            "Right Ring Distal",
            "Right Little Proximal",
            "Right Little Intermediate",
            "Right Little Distal"
            */
        };

        #endregion Pose reset and bone tracking
        

        #region Update
        private static void PlayStateChangeCallback(PlayModeStateChange state)
        {
            wasPlaying = play;
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    {
                        play = false;
                        Util.TrySerializeAssetToEditorPrefs(OriginalClip, WindowManager.clipKey);
                        Util.SerializeIntToEditorPrefs(controlStateHash, WindowManager.controlStateHashKey);
                        Util.SerializeFloatToEditorPrefs(time, WindowManager.timeKey);
                        Util.SerializeBoolToEditorPrefs(isTracking, WindowManager.trackingStatusKey);
                        if (isTracking)
                        {                            
                            foreach (BoneItem boneItem in boneItemList)
                            {
                                if (boneItem.selected)
                                {
                                    Util.SerializeStringToEditorPrefs(boneItem.humanBoneName, WindowManager.lastTrackedBoneKey);
                                }

                            }
                        }

                        //replace original animator controller
                        ResetToBaseAnimatorController();

                        break;
                    }
                case PlayModeStateChange.EnteredPlayMode:
                    {                        
                        break;
                    }
                case PlayModeStateChange.ExitingPlayMode:
                    {
                        break;
                    }
                case PlayModeStateChange.EnteredEditMode:
                    {
                        break;
                    }
            }
        }
        public static int delayFrames = 0;
        private static void UpdateCallback()
        {
            if (delayFrames > 0)
            {
                delayFrames--;
                if (delayFrames == 0)
                    AnimPlayerGUI.ScrubTimeline();

                return;
            }

            if (updateTime == 0f) updateTime = EditorApplication.timeSinceStartup;
            deltaTime = EditorApplication.timeSinceStartup - updateTime;
            updateTime = EditorApplication.timeSinceStartup;

            AdjustEyes();

            if (WorkingClip && CharacterAnimator)
            {
                if (play)
                {
                    double frameDuration = 1.0f / WorkingClip.frameRate;

                    time += ((float)deltaTime / WorkingClip.length) * playbackSpeed;
                    frameTime += deltaTime;
                    if (time >= 1)
                    {
                        time = flagSettings.HasFlag(AnimatorFlags.AutoLoopPlayback) ? 0f : 1f;
                        if (EditorApplication.isPlaying)
                            CharacterAnimator.Play(controlStateHash, 0, time);
                    }

                    if (time < 0)
                    {
                        time = flagSettings.HasFlag(AnimatorFlags.AutoLoopPlayback) ? 1f : 0f;
                        if (EditorApplication.isPlaying)
                            CharacterAnimator.Play(controlStateHash, 0, time);
                    }

                    if (frameTime < frameDuration)
                        return;

                    frameTime = 0f;
                }
                else
                {
                    frameTime = 1f;
                }

                if (current != time)
                {
                    UpdateAnimator();
                    //Repaint();  // repaint the gui to smooth the timer + slider display
                    current = time;
                }
            }
        }


        #endregion Update

        #endregion AnimPlayer

        #region FaceMorph

        public static bool FaceFoldOut { get; private set; } = true;
        public static bool UseLightIcons { get; set; } = false;
        private static bool doOnce = true;
        private static bool doOnceCatchMouse = true;
        private static bool eyeChanged = false;
        private static bool doneInitFace = false;


        private static float EXPRESSIVENESS = 0.25f;
        private static Dictionary<string, float> EXPRESSION;

        private static float Xpos = 0f;
        private static float RestXpos = 0f;
        private static float Ypos = 0f;
        private static float RestYpos = 0f;

        private static Texture2D eyeControlImage;
        private static Texture2D jawIconImage;
        private static Texture2D blinkIconImage;
        private static Texture2D faceDefault;
        private static Texture2D faceAngryImage;
        private static Texture2D faceDisgust;
        private static Texture2D faceFear;
        private static Texture2D faceHappy;
        private static Texture2D faceSad;
        private static Texture2D faceSurprise;
        private static GUIStyle transparentBoxStyle;
        private static Color outlineColor = Color.gray;
        private static Color selectedColor = Color.gray;
        private static Color mouseOverColor = Color.gray;
        private static Rect last;
        private static Vector2 eyeVal;
        private static Vector2 eyeRef;
        private static float jawVal;
        private static float jawRef;
        private static float blinkVal;
        private static float blinkRef;
        private static double resetClickTimer;

        private static Quaternion camDir;

        const float ICON_FACE_SIZE = 48f;

        public static void StartUp()
        {
            CleanUp();

            selectedColor = new Color(0.19f, 0.58f, 0.75f, 0.5f);
            mouseOverColor = new Color(0.45f, 0.45f, 0.45f, 0.5f);
            outlineColor = mouseOverColor;

            string[] folders = new string[] { "Assets", "Packages" };
            eyeControlImage = Util.FindTexture(folders, UseLightIcons ? "RLIcon_Eye_Gry" : "RLIcon_Eye_Blk");
            jawIconImage = Util.FindTexture(folders, UseLightIcons ? "RLIcon_Mouth_Gry" : "RLIcon_Mouth_Blk");
            blinkIconImage = Util.FindTexture(folders, UseLightIcons ? "RLIcon_Blink_Gry" : "RLIcon_Blink_Blk");
            faceDefault = Util.FindTexture(folders, "RLIcon_FaceDefault");
            faceAngryImage = Util.FindTexture(folders, "RLIcon_FaceAngry");
            faceDisgust = Util.FindTexture(folders, "RLIcon_FaceDisgust");
            faceFear = Util.FindTexture(folders, "RLIcon_FaceFear");
            faceHappy = Util.FindTexture(folders, "RLIcon_FaceHappy");
            faceSad = Util.FindTexture(folders, "RLIcon_FaceSad");
            faceSurprise = Util.FindTexture(folders, "RLIcon_FaceSurprise");

            transparentBoxStyle = new GUIStyle(GUI.skin.box);
            Texture2D transparent = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            transparent.SetPixel(0, 0, new Color(1f, 1f, 1f, 0f));
            transparent.Apply();
            transparentBoxStyle.normal.background = transparent;

            InitFace();
        }

        public static void InitFace()
        {
            if (CharacterAnimator == null) return;

            EXPRESSIVENESS = 0f;
            EXPRESSION = null;            

            Object obj = CharacterAnimator.gameObject;
            GameObject root = Util.GetScenePrefabInstanceRoot(obj);
            
            if (root)
            {
                GameObject leftEye = MeshUtil.FindCharacterBone(root, "CC_Base_L_Eye", "L_Eye");
                GameObject rightEye = MeshUtil.FindCharacterBone(root, "CC_Base_R_Eye", "R_Eye");
                GameObject jawBone = MeshUtil.FindCharacterBone(root, "CC_Base_JawRoot", "JawRoot");

                if (leftEye && rightEye)
                {
                    Vector3 euler = leftEye.transform.localRotation.eulerAngles;
                    eyeRef = new Vector2(euler.z, euler.x);
                    eyeVal = eyeRef;
                }

                doOnceCatchMouse = true;

                if (jawBone)
                {
                    Transform jaw = jawBone.transform;
                    Quaternion rotation = jaw.localRotation;
                    Vector3 euler = rotation.eulerAngles;
                    jawRef = euler.z;
                    jawVal = jawRef;
                }

                if (!FacialProfileMapper.GetCharacterBlendShapeWeight(root, "Eye_Blink", 
                    new FacialProfile(ExpressionProfile.Std, VisemeProfile.None), 
                    MeshFacialProfile, out blinkRef))
                {
                    FacialProfileMapper.GetCharacterBlendShapeWeight(root, "Eye_Blink_L",
                        new FacialProfile(ExpressionProfile.Std, VisemeProfile.None),
                        MeshFacialProfile, out blinkRef);
                }
                blinkVal = blinkRef;                            
            }

            doneInitFace = true;
        }

        public static void ResetFace(bool update = true, bool full = false)
        {
            SetNeutralExpression();
            if (full)
            {
                EXPRESSIVENESS = 0f;
                EXPRESSION = null;
            }
            Xpos = RestXpos;
            Ypos = RestYpos;
            eyeVal = eyeRef;
            eyeChanged = true;
            jawVal = jawRef;
            AdjustMouth(jawVal);
            blinkVal = blinkRef;
            AdjustBlink(blinkVal);
            forceUpdate = update;
        }

        public static void ApplyFace()
        {
            eyeChanged = true;
            if (EXPRESSION != null)
                SetFacialExpression(EXPRESSION, true);
            AdjustMouth(jawVal);
            AdjustBlink(blinkVal);
        }

        public static void ResetFaceViewCamera(Object obj = null)
        {
            if (obj == null && CharacterAnimator) obj = CharacterAnimator.gameObject;
            if (obj == null) return;
            GameObject root = Util.GetScenePrefabInstanceRoot(obj);

            if (root)
            {
                Vector3 lookAt = Vector3.zero;

                GameObject head = MeshUtil.FindCharacterBone(root, "CC_Base_Head", "Head");
                GameObject leftEye = MeshUtil.FindCharacterBone(root, "CC_Base_L_Eye", "L_Eye");
                GameObject rightEye = MeshUtil.FindCharacterBone(root, "CC_Base_R_Eye", "R_Eye");

                if (head && leftEye && rightEye)
                    lookAt = (head.transform.position + leftEye.transform.position + rightEye.transform.position) / 3f;
                else if (head)
                    lookAt = head.transform.position;

                if (head)
                {                    
                    //foreach (SceneView sv in SceneView.sceneViews)
                    //{
                        SceneView.lastActiveSceneView.LookAt(lookAt, GetLookBackDir(), 0.25f);
                    //}
                }
            }
        }

        public static void CleanUp()
        {
            doOnce = true;
            doOnceCatchMouse = true;
            doneInitFace = false;            
            EXPRESSION = null;
            EXPRESSIVENESS = 0f;
        }

        public static void DrawFacialMorph()
        {
            if (doOnce) 
            {
                StartUp();
                doOnce = !doOnce;
            }

            EditorGUI.BeginDisabledGroup(play);
            EditorGUI.BeginChangeCheck();
            FaceFoldOut = EditorGUILayout.Foldout(FaceFoldOut, "Facial Expression", EditorStyles.foldout);
            if (EditorGUI.EndChangeCheck())
            {
                //if (foldOut && AnimPlayerIMGUI.FoldOut)
                //    AnimPlayerIMGUI.FoldOut = false;
            }
            if (FaceFoldOut && Event.current.type == EventType.Repaint)
            {
                last = GUILayoutUtility.GetLastRect();
            }

            if (FaceFoldOut)
            {
                GUI.enabled = WorkingClip && CharacterAnimator;

                //Directly positioned controls
                float xPadding = 6f;
                float yPadding = 3f;
                float yFoldoutOffset = 18f;
                float eyeControlWidth = 100f;
                float eyeControlHeight = 50f;

                Rect eyeControlRect = new Rect(last.x + xPadding, last.y + yFoldoutOffset + yPadding, eyeControlWidth, eyeControlHeight);

                Rect rightTopRowIcon = new Rect(eyeControlRect.x + eyeControlRect.width + xPadding * 2,
                                                eyeControlRect.y, 24f, 24f);

                Rect rightTopRowSlider = new Rect(rightTopRowIcon.x + rightTopRowIcon.width + +xPadding * 2,
                                                eyeControlRect.y, 100f, 24f);

                Rect rightSecRowIcon = new Rect(eyeControlRect.x + eyeControlRect.width + xPadding * 2,
                                                rightTopRowIcon.y + rightTopRowIcon.height + yPadding, 24f, 24f);

                Rect rightSecRowSlider = new Rect(rightTopRowIcon.x + rightTopRowIcon.width + +xPadding * 2,
                                                rightTopRowSlider.y + rightTopRowIcon.height + yPadding, 100f, 24f);

                Rect rightRefreshButton = new Rect(rightTopRowSlider.x + rightTopRowSlider.width + xPadding * 3,
                                                rightTopRowSlider.y + rightTopRowSlider.height - yPadding * 6, 32f, 32f);

                eyeVal = CatchMouse(eyeControlRect, eyeRef, invertX: true, invertY: true);

                GUI.DrawTexture(rightTopRowIcon, jawIconImage);
                EditorGUI.BeginChangeCheck();
                jawVal = GUI.HorizontalSlider(rightTopRowSlider, jawVal, jawRef - 25f, jawRef + 0f);
                if (EditorGUI.EndChangeCheck())
                {
                    SetIndividualBlendShape("A25_Jaw_Open", Mathf.InverseLerp(jawRef + 0f, jawRef - 25f, jawVal) * 70f);
                    AdjustMouth(jawVal);
                }

                GUI.DrawTexture(rightSecRowIcon, blinkIconImage);
                EditorGUI.BeginChangeCheck();
                blinkVal = GUI.HorizontalSlider(rightSecRowSlider, blinkVal, -30f, 100f);
                if (EditorGUI.EndChangeCheck())
                {
                    AdjustBlink(blinkVal);
                }

                if (GUI.Button(rightRefreshButton, new GUIContent(EditorGUIUtility.IconContent("Refresh").image, "Reset Face and View")))
                {
                    ResetFace(true, true);

                    // double click test
                    if (resetClickTimer > 0f && (EditorApplication.timeSinceStartup - resetClickTimer < 0.5f))
                    {
                        ResetFaceViewCamera();
                        WindowManager.StopSceneViewOrbit();
                        WindowManager.StopMatchSceneCamera();
                    }

                    resetClickTimer = EditorApplication.timeSinceStartup;                    

                    SceneView.RepaintAll();
                }

                GUILayout.BeginVertical();
                //Shenanigans with 2021 overlays
                //Invisible GUILayout boxes to determine the boundaries of the panel in overlay mode
                //the height is determined by the vertical box + the horizontal GULayout group below it
                GUILayout.Box("", transparentBoxStyle, GUILayout.Width(1f), GUILayout.Height(45f));  //total height = this + button strip
                GUILayout.Box("", transparentBoxStyle, GUILayout.Width(308f), GUILayout.Height(1f)); //total width

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(faceAngryImage, "Angry Face"), GUILayout.Height(ICON_FACE_SIZE), GUILayout.Width(ICON_FACE_SIZE)))
                {
                    ResetFace(false);
                    SetFacialExpression(MeshFacialProfile.expressionProfile == ExpressionProfile.Std ? FACE_ANGRY : FACE_ANGRY_EXT);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(faceDisgust, "Disgusted Face"), GUILayout.Height(ICON_FACE_SIZE), GUILayout.Width(ICON_FACE_SIZE)))
                {
                    ResetFace(false);
                    SetFacialExpression(MeshFacialProfile.expressionProfile == ExpressionProfile.Std ? FACE_DISGUST : FACE_DISGUST_EXT);

                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(faceFear, "Fearful Face"), GUILayout.Height(ICON_FACE_SIZE), GUILayout.Width(ICON_FACE_SIZE)))
                {
                    ResetFace(false);
                    SetFacialExpression(MeshFacialProfile.expressionProfile == ExpressionProfile.Std ? FACE_FEAR : FACE_FEAR_EXT);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(faceHappy, "Happy Face"), GUILayout.Height(ICON_FACE_SIZE), GUILayout.Width(ICON_FACE_SIZE)))
                {
                    ResetFace(false);
                    SetFacialExpression(MeshFacialProfile.expressionProfile == ExpressionProfile.Std ? FACE_HAPPY : FACE_HAPPY_EXT);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(faceSad, "Sad Face"), GUILayout.Height(ICON_FACE_SIZE), GUILayout.Width(ICON_FACE_SIZE)))
                {
                    ResetFace(false);
                    SetFacialExpression(MeshFacialProfile.expressionProfile == ExpressionProfile.Std ? FACE_SAD : FACE_SAD_EXT);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(faceSurprise, "Surprised Face"), GUILayout.Height(ICON_FACE_SIZE), GUILayout.Width(ICON_FACE_SIZE)))
                {
                    ResetFace(false);
                    SetFacialExpression(MeshFacialProfile.expressionProfile == ExpressionProfile.Std ? FACE_SURPRISE : FACE_SURPRISE_EXT);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();
        }

        static Vector2 CatchMouse(Rect controlAreaRect, Vector2 referenceVector2, bool invertX, bool invertY)
        {
            if (doOnceCatchMouse && Event.current.type == EventType.Repaint)
            {
                Xpos = controlAreaRect.x + controlAreaRect.width / 2;
                RestXpos = Xpos;
                Ypos = controlAreaRect.y + controlAreaRect.height / 2;
                RestYpos = Ypos;
                doOnceCatchMouse = !doOnceCatchMouse;
            }

            Rect buttonRect = new Rect(Xpos - 8f, Ypos - 8f, 16f, 16f);
            GUI.DrawTexture(controlAreaRect, eyeControlImage);

            GUI.DrawTexture(buttonRect, EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo").image);
            int buttonId = GUIUtility.GetControlID(FocusType.Passive, buttonRect);
            int controlAreaId = GUIUtility.GetControlID(FocusType.Passive, controlAreaRect);

            Event mouseEvent = Event.current;

            if (controlAreaRect.Contains(mouseEvent.mousePosition))
            {
                if (mouseEvent.type == EventType.MouseDown && mouseEvent.clickCount == 2)
                {
                    // Double click event
                    SnapViewToHead();
                }
            }

            if (controlAreaRect.Contains(mouseEvent.mousePosition))
            {
                switch (mouseEvent.GetTypeForControl(controlAreaId))
                {
                    case EventType.MouseDown:
                        {
                            if (controlAreaRect.Contains(mouseEvent.mousePosition))
                            {
                                outlineColor = selectedColor;
                                GUIUtility.hotControl = controlAreaId;
                                Event.current.Use();

                                /*
                                Xpos = Mathf.Clamp(mouseEvent.mousePosition.x,
                                                   controlAreaRect.x,
                                                   controlAreaRect.x + controlAreaRect.width);

                                Ypos = Mathf.Clamp(mouseEvent.mousePosition.y,
                                                   controlAreaRect.y,
                                                   controlAreaRect.y + controlAreaRect.height);

                                eyeChanged = true;
                                */
                            }
                        }
                        break;

                    case EventType.MouseDrag:
                        {
                            if (GUIUtility.hotControl == controlAreaId)
                            {
                                Xpos = Mathf.Lerp(Xpos, Mathf.Clamp(mouseEvent.mousePosition.x,
                                                        controlAreaRect.x,
                                                        controlAreaRect.x + controlAreaRect.width), 0.25f);

                                Ypos = Mathf.Lerp(Ypos, Mathf.Clamp(mouseEvent.mousePosition.y,
                                                        controlAreaRect.y,
                                                        controlAreaRect.y + controlAreaRect.height), 0.25f);

                                eyeChanged = true;
                            }
                        }
                        break;

                    case EventType.MouseUp:
                        {
                            outlineColor = mouseOverColor;                            
                            GUIUtility.hotControl = 0;
                        }
                        break;

                    case EventType.Repaint:
                        {
                            GUI.DrawTexture(position: controlAreaRect, image: Texture2D.whiteTexture, scaleMode: ScaleMode.StretchToFill,
                                    alphaBlend: false, imageAspect: 1f, color: outlineColor, borderWidth: 1, borderRadius: 1);
                            SceneView.RepaintAll();
                        }
                        break;
                }
            }
            float relX = (Xpos - controlAreaRect.width / 2 - controlAreaRect.x) * (invertX ? -1 : 1);
            float relY = (Ypos - controlAreaRect.height / 2 - controlAreaRect.y) * (invertY ? -1 : 1);

            Vector2 output = new Vector2(referenceVector2.x + relX, referenceVector2.y + relY);
            return output;
        }        
        
        public static void AdjustEyes()
        {
            if (!eyeChanged) return;

            Vector2 input = eyeVal;

            //wrap around values
            if (input.x > 360f) input.x -= 360f;
            if (input.x < -360f) input.x += 360f;
            if (input.y > 360f) input.y -= 360f;
            if (input.y < -360f) input.y += 360f;

            if (AnimPlayerGUI.CharacterAnimator == null) return;
            Object obj = AnimPlayerGUI.CharacterAnimator.gameObject;

            GameObject root = Util.GetScenePrefabInstanceRoot(obj);

            if (root)
            {
                GameObject leftEye = MeshUtil.FindCharacterBone(root, "CC_Base_L_Eye", "L_Eye");
                GameObject rightEye = MeshUtil.FindCharacterBone(root, "CC_Base_R_Eye", "R_Eye");

                if (leftEye && rightEye)
                {
                    Vector3 euler = leftEye.transform.localRotation.eulerAngles;
                    float leftRight = Mathf.DeltaAngle(eyeVal.x, eyeRef.x) / 45f;
                    float upDown = Mathf.DeltaAngle(eyeVal.y, eyeRef.y) / 24f;
                    float lookUpValue = upDown < 0f ? -upDown * 100f: 0;
                    float lookDownValue = upDown >= 0f ? upDown * 100f: 0;
                    float lookLeftValue = leftRight >= 0f ? leftRight * 100f : 0;
                    float lookRightValue = leftRight < 0f ? -leftRight * 100f : 0;
                    SetIndividualBlendShape("A06_Eye_Look_Up_Left", lookUpValue);
                    SetIndividualBlendShape("A07_Eye_Look_Up_Right", lookUpValue);
                    SetIndividualBlendShape("A08_Eye_Look_Down_Left", lookDownValue);
                    SetIndividualBlendShape("A09_Eye_Look_Down_Right", lookDownValue);
                    SetIndividualBlendShape("A10_Eye_Look_Out_Left", lookLeftValue);
                    SetIndividualBlendShape("A11_Eye_Look_In_Left", lookRightValue);
                    SetIndividualBlendShape("A12_Eye_Look_In_Right", lookLeftValue);
                    SetIndividualBlendShape("A13_Eye_Look_Out_Right", lookRightValue);
                    euler.z = input.x;
                    euler.x = input.y;

                    Quaternion rotation = Quaternion.identity;
                    rotation.eulerAngles = euler;
                    leftEye.transform.localRotation = rotation;
                    rightEye.transform.localRotation = rotation;
                }
            }

            eyeChanged = false;
        }

        static void AdjustMouth(float input)
        {
            if (AnimPlayerGUI.CharacterAnimator == null) return;
            Object obj = AnimPlayerGUI.CharacterAnimator.gameObject;

            GameObject root = Util.GetScenePrefabInstanceRoot(obj);

            if (root)
            {
                GameObject jawBone = MeshUtil.FindCharacterBone(root, "CC_Base_JawRoot", "JawRoot");
                if (jawBone)
                {
                    Transform jaw = jawBone.transform;
                    Quaternion rotation = jaw.localRotation;
                    Vector3 euler = rotation.eulerAngles;
                    euler.z = input;
                    jaw.localEulerAngles = euler;
                }
            }
        }

        static void AdjustBlink(float input)
        {
            if (AnimPlayerGUI.CharacterAnimator == null) return;
            Object obj = AnimPlayerGUI.CharacterAnimator.gameObject;

            GameObject root = Util.GetScenePrefabInstanceRoot(obj);            

            SetCharacterBlendShape(root, "A14_Eye_Blink_Left", input);
            SetCharacterBlendShape(root, "A15_Eye_Blink_Right", input);            
        }

        private static bool SetCharacterBlendShape(GameObject characterRoot, string blendShapeName, float weight)
        {
            return FacialProfileMapper.SetCharacterBlendShape(characterRoot, blendShapeName, 
                defaultProfile, MeshFacialProfile, weight);
        }        

        static void SetFacialExpression(Dictionary<string, float> dict, bool restore = false)
        {
            if (CharacterAnimator == null) return;
            if (dict == null) return;

            Object obj = CharacterAnimator.gameObject;

            if (!restore)
            {
                if (dict != FACE_NEUTRAL)
                {
                    if (EXPRESSION != dict)
                    {
                        EXPRESSIVENESS = 0.25f;
                        EXPRESSION = dict;
                    }
                    else
                    {
                        EXPRESSIVENESS += 0.25f;
                        if (EXPRESSIVENESS > 1.0f) EXPRESSIVENESS = 0f;
                    }
                }
            }

            GameObject root = Util.GetScenePrefabInstanceRoot(obj);

            if (root)
            {
                foreach (KeyValuePair<string, float> entry in dict)
                {
                    string shapeName = entry.Key;                    

                    if (shapeName.iEquals("Turn_Jaw") || shapeName.iEquals("A25_Jaw_Open"))
                    {
                        if (!restore)
                        {
                            float mod = 1f;
                            if (shapeName.iEquals("A25_Jaw_Open")) mod = 0.25f;
                            jawVal = jawRef - (entry.Value * mod * EXPRESSIVENESS);
                            AdjustMouth(jawVal);
                        }
                    }

                    SetCharacterBlendShape(root, shapeName, entry.Value * EXPRESSIVENESS);                    
                }
            }
        }

        static void SetNeutralExpression()
        {
            SetFacialExpression(FACE_NEUTRAL, true);
        }

        static void SetIndividualBlendShape(string individualShapeName, float value)
        {
            if (CharacterAnimator == null) return;
            Object obj = CharacterAnimator.gameObject;
            GameObject root = Util.GetScenePrefabInstanceRoot(obj);
            SetCharacterBlendShape(root, individualShapeName, value);
        }        

        static void SnapViewToHead()
        {
            if (AnimPlayerGUI.CharacterAnimator == null) return;

            Object obj = AnimPlayerGUI.CharacterAnimator.gameObject;
            GameObject root = Util.GetScenePrefabInstanceRoot(obj);

            if (root)
            {
                Vector3 snapLookAt = Vector3.zero;

                GameObject head = MeshUtil.FindCharacterBone(root, "CC_Base_Head", "Head");
                GameObject leftEye = MeshUtil.FindCharacterBone(root, "CC_Base_L_Eye", "L_Eye");
                GameObject rightEye = MeshUtil.FindCharacterBone(root, "CC_Base_R_Eye", "R_Eye");

                if (head && leftEye && rightEye)
                    snapLookAt = (head.transform.position + leftEye.transform.position + rightEye.transform.position) / 3f;
                else if (head)
                    snapLookAt = head.transform.position;

                if (head)
                {
                    camDir = Quaternion.AngleAxis(180f, head.transform.up) * head.transform.rotation;

                    //foreach (SceneView sv in SceneView.sceneViews)
                    //{
                        SceneView.lastActiveSceneView.LookAt(snapLookAt, camDir, 0.15f);
                    //}

                    SceneView.RepaintAll();
                }
            }
        }

        static Quaternion GetLookBackDir()
        {
            Quaternion rot = new Quaternion();
            Vector3 euler = rot.eulerAngles;
            euler.y = -180f;
            rot.eulerAngles = euler;
            return rot;
        }

        // Facial Expressions
        public static Dictionary<string, float> FACE_HAPPY = new Dictionary<string, float>
        {
            {"Brow_Raise_Inner_L", 0f },
            {"Brow_Raise_Inner_R", 0f },
            {"Brow_Raise_Outer_L", 0f },
            {"Brow_Raise_Outer_R", 0f },
            {"Brow_Drop_L", 0f },
            {"Brow_Drop_R", 0f },
            {"Brow_Raise_L", 70f },
            {"Brow_Raise_R", 70f },

            {"Eye_Wide_L", 40f },
            {"Eye_Wide_R", 40f },
            {"Eye_Squint_L", 30f },
            {"Eye_Squint_R", 30f },

            {"Nose_Scrunch", 0f },
            {"Nose_Nostrils_Flare", 40f },
            {"Cheek_Raise_L", 30f },
            {"Cheek_Raise_R", 30f },

            {"Mouth_Frown", 0f },
            {"Mouth_Blow", 0f },
            {"Mouth_Pucker", 0f },
            {"Mouth_Widen", 0f },
            {"Mouth_Widen_Sides", 0f },
            {"Mouth_Smile", 70f },
            {"Mouth_Smile_L", 40f },
            {"Mouth_Smile_R", 40f },
            {"Mouth_Dimple_L", 0f },
            {"Mouth_Dimple_R", 0f },
            {"Mouth_Plosive", 0f },
            {"Mouth_Lips_Open", 10f },
            {"Mouth_Lips_Part", 0f },
            {"Mouth_Bottom_Lip_Down", 70f },
            {"Mouth_Top_Lip_Up", 20f },
            {"Mouth_Bottom_Lip_Under", 30f },
            {"Mouth_Snarl_Upper_L", -20f },
            {"Mouth_Snarl_Upper_R", -20f },
            {"Mouth_Snarl_Lower_L", 0f },
            {"Mouth_Snarl_Lower_R", 0f },
            {"Mouth_Up", 30f },
            {"Mouth_Down", 0f },
            {"Mouth_Open", 0f },

            {"Turn_Jaw", 9f },
        };

        public static Dictionary<string, float> FACE_SAD = new Dictionary<string, float>
        {
            {"Brow_Raise_Inner_L", 100f },
            {"Brow_Raise_Inner_R", 100f },
            {"Brow_Raise_Outer_L", 0f },
            {"Brow_Raise_Outer_R", 0f },
            {"Brow_Drop_L", 40f },
            {"Brow_Drop_R", 40f },
            {"Brow_Raise_L", 0f },
            {"Brow_Raise_R", 0f },

            {"Eye_Wide_L", 40f },
            {"Eye_Wide_R", 40f },
            {"Eye_Squint_L", 20f },
            {"Eye_Squint_R", 20f },

            {"Nose_Scrunch", 0f },
            {"Nose_Nostrils_Flare", 0f },
            {"Cheek_Raise_L", 60f },
            {"Cheek_Raise_R", 60f },

            {"Mouth_Frown", 30f },
            {"Mouth_Blow", 20f },
            {"Mouth_Pucker", 0f },
            {"Mouth_Widen", 30f },
            {"Mouth_Widen_Sides", 0f },
            {"Mouth_Smile", 0f },
            {"Mouth_Smile_L", 0f },
            {"Mouth_Smile_R", 0f },
            {"Mouth_Dimple_L", 0f },
            {"Mouth_Dimple_R", 0f },
            {"Mouth_Plosive", 0f },
            {"Mouth_Lips_Open", 0f },
            {"Mouth_Lips_Part", 30f },
            {"Mouth_Bottom_Lip_Down", 0f },
            {"Mouth_Top_Lip_Up", 30f },
            {"Mouth_Bottom_Lip_Under", 0f },
            {"Mouth_Snarl_Upper_L", 0f },
            {"Mouth_Snarl_Upper_R", 0f },
            {"Mouth_Snarl_Lower_L", 0f },
            {"Mouth_Snarl_Lower_R", 0f },
            {"Mouth_Up", 0f },
            {"Mouth_Down", 60f },
            {"Mouth_Open", 0f },

            {"Turn_Jaw", 9f },
        };

        public static Dictionary<string, float> FACE_ANGRY = new Dictionary<string, float>
        {
            {"Brow_Raise_Inner_L", 0f },
            {"Brow_Raise_Inner_R", 0f },
            {"Brow_Raise_Outer_L", 50f },
            {"Brow_Raise_Outer_R", 50f },
            {"Brow_Drop_L", 0f },
            {"Brow_Drop_R", 0f },
            {"Brow_Raise_L", 0f },
            {"Brow_Raise_R", 0f },

            {"Eye_Wide_L", 100f },
            {"Eye_Wide_R", 100f },
            {"Eye_Squint_L", 60f },
            {"Eye_Squint_R", 60f },

            {"Nose_Scrunch", 80f },
            {"Nose_Nostrils_Flare", 0f },
            {"Cheek_Raise_L", 100f },
            {"Cheek_Raise_R", 100f },

            {"Mouth_Frown", 80f },
            {"Mouth_Blow", 0f },
            {"Mouth_Pucker", 30f },
            {"Mouth_Widen", 0f },
            {"Mouth_Widen_Sides", 0f },
            {"Mouth_Smile", 0f },
            {"Mouth_Smile_L", 0f },
            {"Mouth_Smile_R", 0f },
            {"Mouth_Dimple_L", 0f },
            {"Mouth_Dimple_R", 0f },
            {"Mouth_Plosive", 50f },
            {"Mouth_Lips_Open", 0f },
            {"Mouth_Lips_Part", 0f },
            {"Mouth_Bottom_Lip_Down", 60f },
            {"Mouth_Top_Lip_Up", 100f },
            {"Mouth_Bottom_Lip_Under", 0f },
            {"Mouth_Snarl_Upper_L", 0f },
            {"Mouth_Snarl_Upper_R", 0f },
            {"Mouth_Snarl_Lower_L", 0f },
            {"Mouth_Snarl_Lower_R", 0f },
            {"Mouth_Up", 50f },
            {"Mouth_Down", 0f },
            {"Mouth_Open", 0f },

            {"Turn_Jaw", 20f },
        };

        public static Dictionary<string, float> FACE_DISGUST = new Dictionary<string, float>
        {
            {"Brow_Raise_Inner_L", 0f },
            {"Brow_Raise_Inner_R", 0f },
            {"Brow_Raise_Outer_L", 60f },
            {"Brow_Raise_Outer_R", 60f },
            {"Brow_Drop_L", 70f },
            {"Brow_Drop_R", 70f },
            {"Brow_Raise_L", 0f },
            {"Brow_Raise_R", 0f },

            {"Eye_Wide_L", 0f },
            {"Eye_Wide_R", 0f },
            {"Eye_Squint_L", 20f },
            {"Eye_Squint_R", 20f },

            {"Nose_Scrunch", 100f },
            {"Nose_Nostrils_Flare", 0f },
            {"Cheek_Raise_L", 60f },
            {"Cheek_Raise_R", 60f },

            {"Mouth_Frown", 30f },
            {"Mouth_Blow", 0f },
            {"Mouth_Pucker", 0f },
            {"Mouth_Widen", 0f },
            {"Mouth_Widen_Sides", 0f },
            {"Mouth_Smile", 0f },
            {"Mouth_Smile_L", 0f },
            {"Mouth_Smile_R", 0f },
            {"Mouth_Dimple_L", 30f },
            {"Mouth_Dimple_R", 30f },
            {"Mouth_Plosive", 0f },
            {"Mouth_Lips_Open", 0f },
            {"Mouth_Lips_Part", 0f },
            {"Mouth_Bottom_Lip_Down", 0f },
            {"Mouth_Top_Lip_Up", 100f },
            {"Mouth_Bottom_Lip_Under", 0f },
            {"Mouth_Snarl_Upper_L", 0f },
            {"Mouth_Snarl_Upper_R", 0f },
            {"Mouth_Snarl_Lower_L", 20f },
            {"Mouth_Snarl_Lower_R", 20f },
            {"Mouth_Up", 0f },
            {"Mouth_Down", 40f },
            {"Mouth_Open", 0f },

            {"Turn_Jaw", 9f },
        };

        public static Dictionary<string, float> FACE_FEAR = new Dictionary<string, float>
        {
            {"Brow_Raise_Inner_L", 80f },
            {"Brow_Raise_Inner_R", 80f },
            {"Brow_Raise_Outer_L", 0f },
            {"Brow_Raise_Outer_R", 0f },
            {"Brow_Drop_L", 0f },
            {"Brow_Drop_R", 0f },
            {"Brow_Raise_L", 0f },
            {"Brow_Raise_R", 0f },

            {"Eye_Wide_L", 100f },
            {"Eye_Wide_R", 100f },
            {"Eye_Squint_L", 100f },
            {"Eye_Squint_R", 100f },

            {"Nose_Scrunch", 60f },
            {"Nose_Nostrils_Flare", 0f },
            {"Cheek_Raise_L", 100f },
            {"Cheek_Raise_R", 100f },

            {"Mouth_Frown", 70f },
            {"Mouth_Blow", 0f },
            {"Mouth_Pucker", 30f },
            {"Mouth_Widen", 40f },
            {"Mouth_Widen_Sides", 20f },
            {"Mouth_Smile", 0f },
            {"Mouth_Smile_L", 0f },
            {"Mouth_Smile_R", 0f },
            {"Mouth_Dimple_L", 0f },
            {"Mouth_Dimple_R", 0f },
            {"Mouth_Plosive", 0f },
            {"Mouth_Lips_Open", 0f },
            {"Mouth_Lips_Part", 0f },
            {"Mouth_Bottom_Lip_Down", 30f },
            {"Mouth_Top_Lip_Up", 100f },
            {"Mouth_Bottom_Lip_Under", 0f },
            {"Mouth_Snarl_Upper_L", 0f },
            {"Mouth_Snarl_Upper_R", 0f },
            {"Mouth_Snarl_Lower_L", 30f },
            {"Mouth_Snarl_Lower_R", 30f },
            {"Mouth_Up", 0f },
            {"Mouth_Down", 0f },
            {"Mouth_Open", 0f },

            {"Turn_Jaw", 20f },
        };

        public static Dictionary<string, float> FACE_SURPRISE = new Dictionary<string, float>
        {
            {"Brow_Raise_Inner_L", 70f },
            {"Brow_Raise_Inner_R", 70f },
            {"Brow_Raise_Outer_L", 0f },
            {"Brow_Raise_Outer_R", 0f },
            {"Brow_Drop_L", 0f },
            {"Brow_Drop_R", 0f },
            {"Brow_Raise_L", 100f },
            {"Brow_Raise_R", 100f },

            {"Eye_Wide_L", 100f },
            {"Eye_Wide_R", 100f },
            {"Eye_Squint_L", 0f },
            {"Eye_Squint_R", 0f },

            {"Nose_Scrunch", 0f },
            {"Nose_Nostrils_Flare", 30f },
            {"Cheek_Raise_L", 70f },
            {"Cheek_Raise_R", 70f },

            {"Mouth_Frown", 0f },
            {"Mouth_Blow", 0f },
            {"Mouth_Pucker", 0f },
            {"Mouth_Widen", 0f },
            {"Mouth_Widen_Sides", 0f },
            {"Mouth_Smile", 60f },
            {"Mouth_Smile_L", 0f },
            {"Mouth_Smile_R", 0f },
            {"Mouth_Dimple_L", 0f },
            {"Mouth_Dimple_R", 0f },
            {"Mouth_Plosive", 0f },
            {"Mouth_Lips_Open", 0f },
            {"Mouth_Lips_Part", 0f },
            {"Mouth_Bottom_Lip_Down", 0f },
            {"Mouth_Top_Lip_Up", 0f },
            {"Mouth_Bottom_Lip_Under", 0f },
            {"Mouth_Snarl_Upper_L", 0f },
            {"Mouth_Snarl_Upper_R", 0f },
            {"Mouth_Snarl_Lower_L", 0f },
            {"Mouth_Snarl_Lower_R", 0f },
            {"Mouth_Up", 90f },
            {"Mouth_Down", 0f },
            {"Mouth_Open", 100f },

            {"Turn_Jaw", 20f },
        };

        public static Dictionary<string, float> FACE_NEUTRAL = new Dictionary<string, float>
        {
            {"Brow_Raise_Inner_L", 0f },
            {"Brow_Raise_Inner_R", 0f },
            {"Brow_Raise_Outer_L", 0f },
            {"Brow_Raise_Outer_R", 0f },
            {"Brow_Drop_L", 0f },
            {"Brow_Drop_R", 0f },
            {"Brow_Raise_L", 0f },
            {"Brow_Raise_R", 0f },

            {"Eye_Wide_L", 0f },
            {"Eye_Wide_R", 0f },
            {"Eye_Squint_L", 0f },
            {"Eye_Squint_R", 0f },

            {"Nose_Scrunch", 0f },
            {"Nose_Nostrils_Flare", 0f },
            {"Cheek_Raise_L", 0f },
            {"Cheek_Raise_R", 0f },

            {"Mouth_Frown", 0f },
            {"Mouth_Blow", 0f },
            {"Mouth_Pucker", 0f },
            {"Mouth_Widen", 0f },
            {"Mouth_Widen_Sides", 0f },
            {"Mouth_Smile", 0f },
            {"Mouth_Smile_L", 0f },
            {"Mouth_Smile_R", 0f },
            {"Mouth_Dimple_L", 0f },
            {"Mouth_Dimple_R", 0f },
            {"Mouth_Plosive", 0f },
            {"Mouth_Lips_Open", 0f },
            {"Mouth_Lips_Part", 0f },
            {"Mouth_Bottom_Lip_Down", 0f },
            {"Mouth_Top_Lip_Up", 0f },
            {"Mouth_Bottom_Lip_Under", 0f },
            {"Mouth_Snarl_Upper_L", 0f },
            {"Mouth_Snarl_Upper_R", 0f },
            {"Mouth_Snarl_Lower_L", 0f },
            {"Mouth_Snarl_Lower_R", 0f },
            {"Mouth_Up", 0f },
            {"Mouth_Down", 0f },
            {"Mouth_Open", 0f },

            {"A01_Brow_Inner_Up", 0f },
            {"A02_Brow_Down_Left", 0f },
            {"A03_Brow_Down_Right", 0f },
            {"A04_Brow_Outer_Up_Left", 0f },
            {"A05_Brow_Outer_Up_Right", 0f },
            {"A06_Eye_Look_Up_Left", 0f },
            {"A07_Eye_Look_Up_Right", 0f },
            {"A08_Eye_Look_Down_Left", 0f },
            {"A09_Eye_Look_Down_Right", 0f },
            {"A10_Eye_Look_Out_Left", 0f },
            {"A11_Eye_Look_In_Left", 0f },
            {"A12_Eye_Look_In_Right", 0f },
            {"A13_Eye_Look_Out_Right", 0f },
            {"A14_Eye_Blink_Left", 0f },
            {"A15_Eye_Blink_Right", 0f },
            {"A16_Eye_Squint_Left", 0f },
            {"A17_Eye_Squint_Right", 0f },
            {"A18_Eye_Wide_Left", 0f },
            {"A19_Eye_Wide_Right", 0f },
            {"A20_Cheek_Puff", 0f },
            {"A21_Cheek_Squint_Left", 0f },
            {"A22_Cheek_Squint_Right", 0f },
            {"A23_Nose_Sneer_Left", 0f },
            {"A24_Nose_Sneer_Right", 0f },
            {"A25_Jaw_Open", 0f },
            {"A26_Jaw_Forward", 0f },
            {"A27_Jaw_Left", 0f },
            {"A28_Jaw_Right", 0f },
            {"A29_Mouth_Funnel", 0f },
            {"A30_Mouth_Pucker", 0f },
            {"A31_Mouth_Left", 0f },
            {"A32_Mouth_Right", 0f },
            {"A33_Mouth_Roll_Upper", 0f },
            {"A34_Mouth_Roll_Lower", 0f },
            {"A35_Mouth_Shrug_Upper", 0f },
            {"A36_Mouth_Shrug_Lower", 0f },
            {"A37_Mouth_Close", 0f },
            {"A38_Mouth_Smile_Left", 0f },
            {"A39_Mouth_Smile_Right", 0f },
            {"A40_Mouth_Frown_Left", 0f },
            {"A41_Mouth_Frown_Right", 0f },
            {"A42_Mouth_Dimple_Left", 0f },
            {"A43_Mouth_Dimple_Right", 0f },
            {"A44_Mouth_Upper_Up_Left", 0f },
            {"A45_Mouth_Upper_Up_Right", 0f },
            {"A46_Mouth_Lower_Down_Left", 0f },
            {"A47_Mouth_Lower_Down_Right", 0f },
            {"A48_Mouth_Press_Left", 0f },
            {"A49_Mouth_Press_Right", 0f },
            {"A50_Mouth_Stretch_Left", 0f },
            {"A51_Mouth_Stretch_Right", 0f },

            {"Turn_Jaw", 0f },
        };





        public static Dictionary<string, float> FACE_HAPPY_EXT = new Dictionary<string, float>
        {
            {"A01_Brow_Inner_Up", 100f },
            {"A02_Brow_Down_Left", -30f },
            {"A03_Brow_Down_Right", -30f },
            {"A04_Brow_Outer_Up_Left", 100f },
            {"A05_Brow_Outer_Up_Right", 100f },
            {"A06_Eye_Look_Up_Left", 0f },
            {"A07_Eye_Look_Up_Right", 0f },
            {"A08_Eye_Look_Down_Left", 0f },
            {"A09_Eye_Look_Down_Right", 0f },
            {"A10_Eye_Look_Out_Left", 0f },
            {"A11_Eye_Look_In_Left", 0f },
            {"A12_Eye_Look_In_Right", 0f },
            {"A13_Eye_Look_Out_Right", 0f },
            {"A14_Eye_Blink_Left", 0f },
            {"A15_Eye_Blink_Right", 0f },
            {"A16_Eye_Squint_Left", 0f },
            {"A17_Eye_Squint_Right", 0f },
            {"A18_Eye_Wide_Left", 100f },
            {"A19_Eye_Wide_Right", 100f },
            {"A20_Cheek_Puff", 0f },
            {"A21_Cheek_Squint_Left", 0f },
            {"A22_Cheek_Squint_Right", 0f },
            {"A23_Nose_Sneer_Left", 0f },
            {"A24_Nose_Sneer_Right", 0f },
            {"A25_Jaw_Open", 40f },
            {"A26_Jaw_Forward", 0f },
            {"A27_Jaw_Left", 0f },
            {"A28_Jaw_Right", 0f },
            {"A29_Mouth_Funnel", 0f },
            {"A30_Mouth_Pucker", 0f },
            {"A31_Mouth_Left", 0f },
            {"A32_Mouth_Right", 0f },
            {"A33_Mouth_Roll_Upper", 0f },
            {"A34_Mouth_Roll_Lower", 0f },
            {"A35_Mouth_Shrug_Upper", 70f },
            {"A36_Mouth_Shrug_Lower", -70f },
            {"A37_Mouth_Close", 0f },
            {"A38_Mouth_Smile_Left", 100f },
            {"A39_Mouth_Smile_Right", 100f },
            {"A40_Mouth_Frown_Left", 0f },
            {"A41_Mouth_Frown_Right", 0f },
            {"A42_Mouth_Dimple_Left", 0f },
            {"A43_Mouth_Dimple_Right", 0f },
            {"A44_Mouth_Upper_Up_Left", 0f },
            {"A45_Mouth_Upper_Up_Right", 0f },
            {"A46_Mouth_Lower_Down_Left", 90f },
            {"A47_Mouth_Lower_Down_Right", 90f },
            {"A48_Mouth_Press_Left", 0f },
            {"A49_Mouth_Press_Right", 0f },
            {"A50_Mouth_Stretch_Left", 0f },
            {"A51_Mouth_Stretch_Right", 0f },
        };

        public static Dictionary<string, float> FACE_SAD_EXT = new Dictionary<string, float>
        {
            {"A01_Brow_Inner_Up", 100f },
            {"A02_Brow_Down_Left", -50f },
            {"A03_Brow_Down_Right", -50f },
            {"A04_Brow_Outer_Up_Left", 0f },
            {"A05_Brow_Outer_Up_Right", 0f },
            {"A06_Eye_Look_Up_Left", 0f },
            {"A07_Eye_Look_Up_Right", 0f },
            {"A08_Eye_Look_Down_Left", 0f },
            {"A09_Eye_Look_Down_Right", 0f },
            {"A10_Eye_Look_Out_Left", 0f },
            {"A11_Eye_Look_In_Left", 0f },
            {"A12_Eye_Look_In_Right", 0f },
            {"A13_Eye_Look_Out_Right", 0f },
            {"A14_Eye_Blink_Left", 0f },
            {"A15_Eye_Blink_Right", 0f },
            {"A16_Eye_Squint_Left", 100f },
            {"A17_Eye_Squint_Right", 100f },
            {"A18_Eye_Wide_Left", 0f },
            {"A19_Eye_Wide_Right", 0f },
            {"A20_Cheek_Puff", 0f },
            {"A21_Cheek_Squint_Left", 100f },
            {"A22_Cheek_Squint_Right", 100f },
            {"A23_Nose_Sneer_Left", 0f },
            {"A24_Nose_Sneer_Right", 0f },
            {"A25_Jaw_Open", 30f },
            {"A26_Jaw_Forward", 0f },
            {"A27_Jaw_Left", 0f },
            {"A28_Jaw_Right", 0f },
            {"A29_Mouth_Funnel", 0f },
            {"A30_Mouth_Pucker", 0f },
            {"A31_Mouth_Left", 0f },
            {"A32_Mouth_Right", 0f },
            {"A33_Mouth_Roll_Upper", 0f },
            {"A34_Mouth_Roll_Lower", 60f },
            {"A35_Mouth_Shrug_Upper", 20f },
            {"A36_Mouth_Shrug_Lower", 40f },
            {"A37_Mouth_Close", 0f },
            {"A38_Mouth_Smile_Left", 0f },
            {"A39_Mouth_Smile_Right", 0f },
            {"A40_Mouth_Frown_Left", 40f },
            {"A41_Mouth_Frown_Right", 40f },
            {"A42_Mouth_Dimple_Left", 0f },
            {"A43_Mouth_Dimple_Right", 0f },
            {"A44_Mouth_Upper_Up_Left", 0f },
            {"A45_Mouth_Upper_Up_Right", 0f },
            {"A46_Mouth_Lower_Down_Left", 0f },
            {"A47_Mouth_Lower_Down_Right", 0f },
            {"A48_Mouth_Press_Left", 0f },
            {"A49_Mouth_Press_Right", 0f },
            {"A50_Mouth_Stretch_Left", 0f },
            {"A51_Mouth_Stretch_Right", 0f },
        };

        public static Dictionary<string, float> FACE_ANGRY_EXT = new Dictionary<string, float>
        {
            {"A01_Brow_Inner_Up", 0f },
            {"A02_Brow_Down_Left", 100f },
            {"A03_Brow_Down_Right", 100f },
            {"A04_Brow_Outer_Up_Left", 40f },
            {"A05_Brow_Outer_Up_Right", 40f },
            {"A06_Eye_Look_Up_Left", 0f },
            {"A07_Eye_Look_Up_Right", 0f },
            {"A08_Eye_Look_Down_Left", 0f },
            {"A09_Eye_Look_Down_Right", 0f },
            {"A10_Eye_Look_Out_Left", 0f },
            {"A11_Eye_Look_In_Left", 0f },
            {"A12_Eye_Look_In_Right", 0f },
            {"A13_Eye_Look_Out_Right", 0f },
            {"A14_Eye_Blink_Left", 0f },
            {"A15_Eye_Blink_Right", 0f },
            {"A16_Eye_Squint_Left", 0f },
            {"A17_Eye_Squint_Right", 0f },
            {"A18_Eye_Wide_Left", 100f },
            {"A19_Eye_Wide_Right", 100f },
            {"A20_Cheek_Puff", 0f },
            {"A21_Cheek_Squint_Left", 100f },
            {"A22_Cheek_Squint_Right", 100f },
            {"A23_Nose_Sneer_Left", 40f },
            {"A24_Nose_Sneer_Right", 40f },
            {"A25_Jaw_Open", 70f },
            {"A26_Jaw_Forward", 0f },
            {"A27_Jaw_Left", 0f },
            {"A28_Jaw_Right", 0f },
            {"A29_Mouth_Funnel", 0f },
            {"A30_Mouth_Pucker", 0f },
            {"A31_Mouth_Left", 0f },
            {"A32_Mouth_Right", 0f },
            {"A33_Mouth_Roll_Upper", 0f },
            {"A34_Mouth_Roll_Lower", 0f },
            {"A35_Mouth_Shrug_Upper", -10f },
            {"A36_Mouth_Shrug_Lower", -30f },
            {"A37_Mouth_Close", 0f },
            {"A38_Mouth_Smile_Left", 0f },
            {"A39_Mouth_Smile_Right", 0f },
            {"A40_Mouth_Frown_Left", 0f },
            {"A41_Mouth_Frown_Right", 0f },
            {"A42_Mouth_Dimple_Left", 0f },
            {"A43_Mouth_Dimple_Right", 0f },
            {"A44_Mouth_Upper_Up_Left", 100f },
            {"A45_Mouth_Upper_Up_Right", 100f },
            {"A46_Mouth_Lower_Down_Left", 100f },
            {"A47_Mouth_Lower_Down_Right", 100f },
            {"A48_Mouth_Press_Left", 0f },
            {"A49_Mouth_Press_Right", 0f },
            {"A50_Mouth_Stretch_Left", 0f },
            {"A51_Mouth_Stretch_Right", 0f },
        };

        public static Dictionary<string, float> FACE_DISGUST_EXT = new Dictionary<string, float>
        {
            {"A01_Brow_Inner_Up", 0f },
            {"A02_Brow_Down_Left", 60f },
            {"A03_Brow_Down_Right", 60f },
            {"A04_Brow_Outer_Up_Left", 70f },
            {"A05_Brow_Outer_Up_Right", 70f },
            {"A06_Eye_Look_Up_Left", 0f },
            {"A07_Eye_Look_Up_Right", 0f },
            {"A08_Eye_Look_Down_Left", 0f },
            {"A09_Eye_Look_Down_Right", 0f },
            {"A10_Eye_Look_Out_Left", 0f },
            {"A11_Eye_Look_In_Left", 0f },
            {"A12_Eye_Look_In_Right", 0f },
            {"A13_Eye_Look_Out_Right", 0f },
            {"A14_Eye_Blink_Left", 0f },
            {"A15_Eye_Blink_Right", 0f },
            {"A16_Eye_Squint_Left", 0f },
            {"A17_Eye_Squint_Right", 0f },
            {"A18_Eye_Wide_Left", 0f },
            {"A19_Eye_Wide_Right", 0f },
            {"A20_Cheek_Puff", 0f },
            {"A21_Cheek_Squint_Left", 100f },
            {"A22_Cheek_Squint_Right", 100f },
            {"A23_Nose_Sneer_Left", 100f },
            {"A24_Nose_Sneer_Right", 100f },
            {"A25_Jaw_Open", 20f },
            {"A26_Jaw_Forward", 0f },
            {"A27_Jaw_Left", 0f },
            {"A28_Jaw_Right", 0f },
            {"A29_Mouth_Funnel", 0f },
            {"A30_Mouth_Pucker", 0f },
            {"A31_Mouth_Left", 0f },
            {"A32_Mouth_Right", 0f },
            {"A33_Mouth_Roll_Upper", 0f },
            {"A34_Mouth_Roll_Lower", 0f },
            {"A35_Mouth_Shrug_Upper", 20f },
            {"A36_Mouth_Shrug_Lower", 30f },
            {"A37_Mouth_Close", 0f },
            {"A38_Mouth_Smile_Left", 0f },
            {"A39_Mouth_Smile_Right", 0f },
            {"A40_Mouth_Frown_Left", 30f },
            {"A41_Mouth_Frown_Right", 30f },
            {"A42_Mouth_Dimple_Left", 0f },
            {"A43_Mouth_Dimple_Right", 0f },
            {"A44_Mouth_Upper_Up_Left", 70f },
            {"A45_Mouth_Upper_Up_Right", 70f },
            {"A46_Mouth_Lower_Down_Left", 50f },
            {"A47_Mouth_Lower_Down_Right", 50f },
            {"A48_Mouth_Press_Left", 0f },
            {"A49_Mouth_Press_Right", 0f },
            {"A50_Mouth_Stretch_Left", 40f },
            {"A51_Mouth_Stretch_Right", 40f },
        };

        public static Dictionary<string, float> FACE_FEAR_EXT = new Dictionary<string, float>
        {
            {"A01_Brow_Inner_Up", 100f },
            {"A02_Brow_Down_Left", -30f },
            {"A03_Brow_Down_Right", -30f },
            {"A04_Brow_Outer_Up_Left", 100f },
            {"A05_Brow_Outer_Up_Right", 100f },
            {"A06_Eye_Look_Up_Left", 0f },
            {"A07_Eye_Look_Up_Right", 0f },
            {"A08_Eye_Look_Down_Left", 0f },
            {"A09_Eye_Look_Down_Right", 0f },
            {"A10_Eye_Look_Out_Left", 0f },
            {"A11_Eye_Look_In_Left", 0f },
            {"A12_Eye_Look_In_Right", 0f },
            {"A13_Eye_Look_Out_Right", 0f },
            {"A14_Eye_Blink_Left", 0f },
            {"A15_Eye_Blink_Right", 0f },
            {"A16_Eye_Squint_Left", 100f },
            {"A17_Eye_Squint_Right", 100f },
            {"A18_Eye_Wide_Left", 100f },
            {"A19_Eye_Wide_Right", 100f },
            {"A20_Cheek_Puff", 0f },
            {"A21_Cheek_Squint_Left", 100f },
            {"A22_Cheek_Squint_Right", 100f },
            {"A23_Nose_Sneer_Left", 30f },
            {"A24_Nose_Sneer_Right", 30f },
            {"A25_Jaw_Open", 60f },
            {"A26_Jaw_Forward", 0f },
            {"A27_Jaw_Left", 0f },
            {"A28_Jaw_Right", 0f },
            {"A29_Mouth_Funnel", 0f },
            {"A30_Mouth_Pucker", 0f },
            {"A31_Mouth_Left", 0f },
            {"A32_Mouth_Right", 0f },
            {"A33_Mouth_Roll_Upper", 0f },
            {"A34_Mouth_Roll_Lower", 0f },
            {"A35_Mouth_Shrug_Upper", 0f },
            {"A36_Mouth_Shrug_Lower", 0f },
            {"A37_Mouth_Close", 0f },
            {"A38_Mouth_Smile_Left", 0f },
            {"A39_Mouth_Smile_Right", 0f },
            {"A40_Mouth_Frown_Left", 40f },
            {"A41_Mouth_Frown_Right", 40f },
            {"A42_Mouth_Dimple_Left", 0f },
            {"A43_Mouth_Dimple_Right", 0f },
            {"A44_Mouth_Upper_Up_Left", 40f },
            {"A45_Mouth_Upper_Up_Right", 40f },
            {"A46_Mouth_Lower_Down_Left", 70f },
            {"A47_Mouth_Lower_Down_Right", 70f },
            {"A48_Mouth_Press_Left", 0f },
            {"A49_Mouth_Press_Right", 0f },
            {"A50_Mouth_Stretch_Left", 0f },
            {"A51_Mouth_Stretch_Right", 0f },
        };

        public static Dictionary<string, float> FACE_SURPRISE_EXT = new Dictionary<string, float>
        {
            {"A01_Brow_Inner_Up", 100f },
            {"A02_Brow_Down_Left", -50f },
            {"A03_Brow_Down_Right", -50f },
            {"A04_Brow_Outer_Up_Left", 100f },
            {"A05_Brow_Outer_Up_Right", 100f },
            {"A06_Eye_Look_Up_Left", 0f },
            {"A07_Eye_Look_Up_Right", 0f },
            {"A08_Eye_Look_Down_Left", 0f },
            {"A09_Eye_Look_Down_Right", 0f },
            {"A10_Eye_Look_Out_Left", 0f },
            {"A11_Eye_Look_In_Left", 0f },
            {"A12_Eye_Look_In_Right", 0f },
            {"A13_Eye_Look_Out_Right", 0f },
            {"A14_Eye_Blink_Left", 0f },
            {"A15_Eye_Blink_Right", 0f },
            {"A16_Eye_Squint_Left", 0f },
            {"A17_Eye_Squint_Right", 0f },
            {"A18_Eye_Wide_Left", 100f },
            {"A19_Eye_Wide_Right", 100f },
            {"A20_Cheek_Puff", 0f },
            {"A21_Cheek_Squint_Left", 0f },
            {"A22_Cheek_Squint_Right", 0f },
            {"A23_Nose_Sneer_Left", 0f },
            {"A24_Nose_Sneer_Right", 0f },
            {"A25_Jaw_Open", 50f },
            {"A26_Jaw_Forward", 0f },
            {"A27_Jaw_Left", 0f },
            {"A28_Jaw_Right", 0f },
            {"A29_Mouth_Funnel", 30f },
            {"A30_Mouth_Pucker", 0f },
            {"A31_Mouth_Left", 0f },
            {"A32_Mouth_Right", 0f },
            {"A33_Mouth_Roll_Upper", 0f },
            {"A34_Mouth_Roll_Lower", 0f },
            {"A35_Mouth_Shrug_Upper", 30f },
            {"A36_Mouth_Shrug_Lower", 0f },
            {"A37_Mouth_Close", 0f },
            {"A38_Mouth_Smile_Left", 0f },
            {"A39_Mouth_Smile_Right", 0f },
            {"A40_Mouth_Frown_Left", 0f },
            {"A41_Mouth_Frown_Right", 0f },
            {"A42_Mouth_Dimple_Left", 30f },
            {"A43_Mouth_Dimple_Right", 30f },
            {"A44_Mouth_Upper_Up_Left", 30f },
            {"A45_Mouth_Upper_Up_Right", 30f },
            {"A46_Mouth_Lower_Down_Left", 60f },
            {"A47_Mouth_Lower_Down_Right", 60f },
            {"A48_Mouth_Press_Left", 0f },
            {"A49_Mouth_Press_Right", 0f },
            {"A50_Mouth_Stretch_Left", 0f },
            {"A51_Mouth_Stretch_Right", 0f },
        };

        #endregion FaceMorph        
    }
}