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

namespace Reallusion.Import
{
    public static class AnimPlayerGUI
    {
        #region AnimPlayer  

        private static bool play = false;        
        private static float time, prev, current = 0f;
        public static bool AnimFoldOut { get; private set; } = true;
        public static FacialProfile MeshFacialProfile { get; private set; }
        public static FacialProfile ClipFacialProfile { get; private set; }        
        public static AnimationClip OriginalClip { get; set; }        
        public static AnimationClip WorkingClip { get ; set; }
        public static Animator CharacterAnimator { get; set; }

        private static double updateTime = 0f;
        private static double deltaTime = 0f;
        private static double frameTime = 1f;
        private static bool forceUpdate = false;
        private static FacialProfile defaultProfile = new FacialProfile(ExpressionProfile.ExPlus, VisemeProfile.PairsCC3);

        public static void OpenPlayer(GameObject scenePrefab)
        {
            if (scenePrefab)
            {
                scenePrefab = Util.TryResetScenePrefab(scenePrefab);
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

                EditorApplication.update += UpdateDelegate;
            }
        }

        public static void ClosePlayer()  
        {
            if (IsPlayerShown())
            {
                EditorApplication.update -= UpdateDelegate;

                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();

                if (CharacterAnimator)       
                {
                    GameObject scenePrefab = Util.GetScenePrefabInstanceRoot(CharacterAnimator.gameObject);
                    Util.TryResetScenePrefab(scenePrefab);
                }

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
            if (!scenePrefab && WindowManager.IsPreviewScene)
                scenePrefab = WindowManager.GetPreviewScene().GetPreviewCharacter();

            if (scenePrefab)  
            {                                
                Animator animator = scenePrefab.GetComponent<Animator>();
                if (!animator) animator = scenePrefab.GetComponentInChildren<Animator>();
                GameObject sceneFbx = Util.FindRootPrefabAssetFromSceneObject(scenePrefab);
                AnimationClip clip = Util.GetFirstAnimationClipFromCharacter(sceneFbx);
                if (sceneFbx && clip)
                    clip = AnimRetargetGUI.TryGetRetargetedAnimationClip(sceneFbx, clip);
                UpdateAnimatorClip(animator, clip);
            }         
        }

        static public void UpdateAnimatorClip(Animator animator, AnimationClip clip)
        {
            if (doneInitFace) ResetFace(true, true);

            // stop animation mode
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();

            if (!animator || CharacterAnimator != animator) doneInitFace = false;

            CharacterAnimator = animator;
            OriginalClip = clip;
            WorkingClip = CloneClip(OriginalClip);

            AnimRetargetGUI.RebuildClip();

            MeshFacialProfile = FacialProfileMapper.GetMeshFacialProfile(animator ? animator.gameObject : null);
            ClipFacialProfile = FacialProfileMapper.GetAnimationClipFacialProfile(clip);
            
            time = 0f;
            play = false;            

            // intitialise the face refs if needed
            if (!doneInitFace) InitFace();

            // finally, apply the face
            //ApplyFace();

            if (WorkingClip && CharacterAnimator)
            {
                // also restarts animation mode
                SampleOnce();
            }                        
        }

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

        public static void DrawPlayer()
        {            
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

                EditorGUI.BeginDisabledGroup(!AnimationMode.InAnimationMode());

                if (WorkingClip != null)
                {
                    float startTime = 0.0f;
                    float stopTime = WorkingClip.length;
                    EditorGUI.BeginChangeCheck();
                    time = EditorGUILayout.Slider(time, startTime, stopTime);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ResetFace();
                    }
                }
                else
                {
                    float value = 0f;
                    value = EditorGUILayout.Slider(value, 0f, 1f); //disabled dummy entry
                }

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                // "Animation.FirstKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.FirstKey").image, "First Frame"), EditorStyles.toolbarButton))
                {
                    play = false;
                    time = 0f;
                    ResetFace();
                }
                // "Animation.PrevKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.PrevKey").image, "Previous Frame"), EditorStyles.toolbarButton))
                {
                    play = false;
                    time -= 0.0166f;
                    ResetFace();
                }
                // "Animation.Play"
                EditorGUI.BeginChangeCheck();
                play = GUILayout.Toggle(play, new GUIContent(EditorGUIUtility.IconContent("Animation.Play").image, "Play (Toggle)"), EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {                    
                    ResetFace();
                }
                // "PauseButton"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("PauseButton").image, "Pause"), EditorStyles.toolbarButton))
                {
                    play = false;
                    ResetFace();
                }
                // "Animation.NextKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.NextKey").image, "Next Frame"), EditorStyles.toolbarButton))
                {
                    play = false;
                    time += 0.0166f;
                    ResetFace();
                }
                // "Animation.LastKey"
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.LastKey").image, "Last Frame"), EditorStyles.toolbarButton))
                {
                    play = false;
                    time = WorkingClip.length;
                    ResetFace();
                }

                if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) play = false;                

                GUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();

                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }

        public static void SampleOnce()
        {
            if (CharacterAnimator && WorkingClip)
            {
                if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(CharacterAnimator.gameObject, WorkingClip, time);
                AnimationMode.EndSampling();
            }
        }
        
        private static void UpdateDelegate()
        {
            if (updateTime == 0f) updateTime = EditorApplication.timeSinceStartup;
            deltaTime = EditorApplication.timeSinceStartup - updateTime;
            updateTime = EditorApplication.timeSinceStartup;

            AdjustEyes();

            if (!EditorApplication.isPlaying && AnimationMode.InAnimationMode())
            {
                if (WorkingClip && CharacterAnimator)
                {
                    if (play)
                    {
                        double frameDuration = 1.0f / WorkingClip.frameRate;

                        time += (float)deltaTime;
                        frameTime += deltaTime;
                        if (time >= WorkingClip.length)
                            time = 0f;

                        if (frameTime < frameDuration) return;
                        frameTime = 0f;
                    }
                    else
                        frameTime = 1f;

                    if (current != time || forceUpdate)
                    {
                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(CharacterAnimator.gameObject, WorkingClip, time);
                        AnimationMode.EndSampling();
                        SceneView.RepaintAll();
                        
                        AnimPlayerGUI.current = time;
                        forceUpdate = false;
                    }
                }
            }
        }

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