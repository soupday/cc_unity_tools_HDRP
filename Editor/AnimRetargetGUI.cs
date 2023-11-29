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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.IO;

namespace Reallusion.Import
{
    public static class AnimRetargetGUI
    {
        // GUI variables
        private static Texture2D handImage;
        private static Texture2D closedMouthImage;
        private static Texture2D openMouthImage;
        private static Texture2D blendshapeImage;
        private static Texture2D saveImage;
        private static Texture2D resetImage;
        private static Texture2D unlockedImage;
        private static Texture2D lockedImage;

        private static float baseControlWidth = 173f;
        private static float sliderWidth = 303f;
        private static float textWidth = 66f;
        private static float textHeight = 18f;
        private static float largeIconDim = 60f;
        private static float smallIconDim = 30f;

        private static float shRange = 30f; // Angular Ranges in degrees
        private static float aRange = 30f;
        private static float lRange = 30f;
        private static float hRange = 30f;

        private static float yRange = 0.2f; //Raw y input range

        // GUI Control variables (Reset to this state)

        private static bool holdValues = false;

        private static int handPose = 0;
        private static bool closeMouth = false;
        private static float shoulderOffset = 0f;
        private static float armOffset = 0f;
        private static float armFBOffset = 0f;
        private static float backgroundArmOffset = 0f;
        private static float legOffset = 0f;
        private static float heelOffset = 0f;
        private static float heightOffset = 0f;

        private static AnimationClip OriginalClip => AnimPlayerGUI.OriginalClip;
        private static AnimationClip WorkingClip => AnimPlayerGUI.WorkingClip;
        private static Animator CharacterAnimator => AnimPlayerGUI.CharacterAnimator;

        private static Vector3 animatorPosition;
        private static Quaternion animatorRotation;

        // Function variables        
        public const string ANIM_FOLDER_NAME = "Animations";
        public const string RETARGET_FOLDER_NAME = "Retargeted";
        public const string RETARGET_SOURCE_PREFIX = "Imported";

        private static Dictionary<string, EditorCurveBinding> shoulderBindings;
        private static Dictionary<string, EditorCurveBinding> armBindings;
        private static Dictionary<string, EditorCurveBinding> armFBBindings;
        private static Dictionary<string, EditorCurveBinding> legBindings;
        private static Dictionary<string, EditorCurveBinding> heelBindings;
        private static Dictionary<string, EditorCurveBinding> heightBindings;

        public static void OpenRetargeter()//(PreviewScene ps, GameObject fbx)
        {
            if (!IsPlayerShown())
            {
#if SCENEVIEW_OVERLAY_COMPATIBLE
                //2021.2.0a17+  When GUI.Window is called from a static SceneView delegate, it is broken in 2021.2.0f1 - 2021.2.1f1
                //so we switch to overlays starting from an earlier version
                AnimRetargetOverlay.ShowAll();
#else
                //2020 LTS            
                AnimRetargetWindow.ShowPlayer();
#endif

                //Common
                Init();

                SceneView.RepaintAll();
            }
        }

        public static void CloseRetargeter()
        {
            if (IsPlayerShown())
            {
                //EditorApplication.update -= UpdateDelegate;

#if SCENEVIEW_OVERLAY_COMPATIBLE
                //2021.2.0a17+          
                AnimRetargetOverlay.HideAll();
#else
                //2020 LTS            
                AnimRetargetWindow.HidePlayer();
#endif

                //Common
                CleanUp();

                SceneView.RepaintAll();
            }
        }

        public static bool IsPlayerShown()
        {
#if SCENEVIEW_OVERLAY_COMPATIBLE
            //2021.2.0a17+
            return AnimRetargetOverlay.Visibility;
#else
            //2020 LTS            
            return AnimRetargetWindow.isShown;
#endif
        }

        static void Init()
        {
            string[] folders = new string[] { "Assets", "Packages" };
            closedMouthImage = Reallusion.Import.Util.FindTexture(folders, "RLIcon_Mask_Closed");
            openMouthImage = Reallusion.Import.Util.FindTexture(folders, "RLIcon_Mask_Open");
            handImage = Reallusion.Import.Util.FindTexture(folders, "RLIcon_Hand");
            blendshapeImage = Reallusion.Import.Util.FindTexture(folders, "RLIcon_Masks");
            saveImage = Reallusion.Import.Util.FindTexture(folders, "RLIcon_Save");
            resetImage = Reallusion.Import.Util.FindTexture(folders, "RLIcon_ActionReset");
            lockedImage = Reallusion.Import.Util.FindTexture(folders, "RLIcon_Locked");
            unlockedImage = Reallusion.Import.Util.FindTexture(folders, "RLIcon_Unlocked");

            RebuildClip();

            // reset all the clip flags to their default vaules            
            // set the animation player's Foot IK to off
            AnimPlayerGUI.ForceSettingsReset();
            AnimPlayerGUI.UpdateAnimator();
        }        

        static void CleanUp()
        {            
            // reset the player fully with the currently selected clip
            AnimPlayerGUI.SetupCharacterAndAnimation();
        }

        public static void ResetClip()
        {
            AnimPlayerGUI.ReCloneClip();
            holdValues = false;
            RebuildClip();
        }

        // Return all values to start - rebuild all bindings dicts
        public static void RebuildClip()
        {
            if (WorkingClip && CanClipLoop(WorkingClip))
            {
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(WorkingClip);
                if (!clipSettings.loopTime)
                {
                    clipSettings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(WorkingClip, clipSettings);
                }
            }

            if (OriginalClip)
            {
                EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(OriginalClip);

                shoulderBindings = new Dictionary<string, EditorCurveBinding>();

                for (int i = 0; i < curveBindings.Length; i++)
                {
                    if (shoulderCurveNames.Contains(curveBindings[i].propertyName))
                    {
                        shoulderBindings.Add(curveBindings[i].propertyName, curveBindings[i]);
                    }
                }

                armBindings = new Dictionary<string, EditorCurveBinding>();

                for (int i = 0; i < curveBindings.Length; i++)
                {
                    if (armCurveNames.Contains(curveBindings[i].propertyName))
                    {
                        armBindings.Add(curveBindings[i].propertyName, curveBindings[i]);
                    }
                }

                armFBBindings = new Dictionary<string, EditorCurveBinding>();

                for (int i = 0; i < curveBindings.Length; i++)
                {
                    if (armFBCurveNames.Contains(curveBindings[i].propertyName))
                    {
                        armFBBindings.Add(curveBindings[i].propertyName, curveBindings[i]);
                    }
                }

                legBindings = new Dictionary<string, EditorCurveBinding>();

                for (int i = 0; i < curveBindings.Length; i++)
                {
                    if (legCurveNames.Contains(curveBindings[i].propertyName))
                    {
                        legBindings.Add(curveBindings[i].propertyName, curveBindings[i]);
                    }
                }

                heelBindings = new Dictionary<string, EditorCurveBinding>();

                for (int i = 0; i < curveBindings.Length; i++)
                {
                    if (heelCurveNames.Contains(curveBindings[i].propertyName))
                    {
                        heelBindings.Add(curveBindings[i].propertyName, curveBindings[i]);
                    }
                }

                heightBindings = new Dictionary<string, EditorCurveBinding>();

                for (int i = 0; i < curveBindings.Length; i++)
                {
                    if (heightCurveNames.Contains(curveBindings[i].propertyName))
                    {
                        heightBindings.Add(curveBindings[i].propertyName, curveBindings[i]);
                    }
                }
            }

            if (!holdValues)
            {
                handPose = 0;
                closeMouth = false;
                shoulderOffset = 0f;
                armOffset = 0f;
                armFBOffset = 0f;
                backgroundArmOffset = 0f;
                legOffset = 0f;
                heelOffset = 0f;
                heightOffset = 0f;
            }
                      
            OffsetALL();
        }

        public static void DrawRetargeter()
        {
            if (!(OriginalClip && WorkingClip)) GUI.enabled = false;
            else if (!AnimPlayerGUI.CharacterAnimator) GUI.enabled = false;
            else GUI.enabled = true;

            // All retarget controls
            GUILayout.BeginVertical();
            // Horizontal Group of 3 controls `Hand` `Jaw` and `Blendshapes`
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box", GUILayout.Width(baseControlWidth));  // Hand control box - Width used to impose layout footprint for overlay
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(handImage, "Switch between hand modes - Original animation info - Static open hand pose - Static closed hand pose. (This only affects pose of the fingers)."), GUILayout.Width(largeIconDim), GUILayout.Height(largeIconDim)))
            {
                handPose++;
                if (handPose > 2) handPose = 0;
                ApplyPose(handPose);                
            }
            GUILayout.BeginVertical();

            GUIStyle radioSelectionStyle = new GUIStyle(EditorStyles.radioButton);
            radioSelectionStyle.padding = new RectOffset(24, 0, 0, 0);
            GUIContent[] contents = new GUIContent[]
            {
                new GUIContent("Original", "Use the hand pose/animation from the original animation clip."),
                new GUIContent("Open", "Use a static neutral open hand pose for the full animation clip."),
                new GUIContent("Closed", "Use a static neutral closed hand pose for the full animation clip.")
            };
            EditorGUI.BeginChangeCheck();
            handPose = GUILayout.SelectionGrid(handPose, contents, 1, radioSelectionStyle);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyPose(handPose);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical(); // End of Hand control


            GUILayout.BeginVertical("box"); // Jaw control box       
            if (GUILayout.Button(new GUIContent(closeMouth ? closedMouthImage : openMouthImage, string.Format("STATUS: " + (closeMouth ? "ON" : "OFF") + ":  Toggle to CLOSE THE JAW of any animation imported without proper jaw information.  Toggling this ON will overwrite any jaw animation.  Toggling OFF will use the jaw animation from the selected animation clip.")), GUILayout.Width(largeIconDim), GUILayout.Height(largeIconDim)))
            {
                closeMouth = !closeMouth;
                CloseMouthToggle(closeMouth);
            }
            GUILayout.EndVertical(); // End of Jaw control
            
            GUILayout.BeginVertical("box"); // Blendshapes control box
            Color backgroundColor = GUI.backgroundColor;
            Color tint = Color.green;
            FacialProfile mfp = AnimPlayerGUI.MeshFacialProfile;
            FacialProfile cfp = AnimPlayerGUI.ClipFacialProfile;
            if (!mfp.HasFacialShapes || !cfp.HasFacialShapes)
            {
                GUI.enabled = false;
                tint = backgroundColor;
            }
            if (!mfp.IsSameProfileFrom(cfp))
            {
                if (mfp.expressionProfile != ExpressionProfile.None && 
                    cfp.expressionProfile != ExpressionProfile.None)
                {
                    // ExpPlus or Extended to Standard will not retarget well, show a red warning color
                    if (mfp.expressionProfile == ExpressionProfile.Std)
                        tint = Color.red;
                    // retargeting from CC3 standard should work with everything
                    else if (cfp.expressionProfile == ExpressionProfile.Std)
                        tint = Color.green;
                    // otherwise show a yellow warning color
                    else
                        tint = Color.yellow;
                }

                if (mfp.visemeProfile != cfp.visemeProfile)
                {
                    if (mfp.visemeProfile == VisemeProfile.Direct || cfp.visemeProfile == VisemeProfile.Direct)
                    {
                        // Direct to Paired visemes won't work.
                        tint = Color.red;
                    }
                }
            }
            
            GUI.backgroundColor = Color.Lerp(backgroundColor, tint, 0.25f);
            if (GUILayout.Button(new GUIContent(blendshapeImage, "Copy all BlendShape animations from the selected animation clip to all of the relevant objects (e.g. facial hair) in the selected Scene Model."), GUILayout.Width(largeIconDim), GUILayout.Height(largeIconDim)))
            {
                RetargetBlendShapes(OriginalClip, WorkingClip, CharacterAnimator.gameObject);
                AnimPlayerGUI.UpdateAnimator();
            }
            GUI.backgroundColor = backgroundColor;
            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal(); // End of Blendshapes control

            // Control box for animation curve adjustment sliders
            GUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(GUILayout.Width(sliderWidth));
            GUILayout.Label(new GUIContent("Shoulder", "Adjust the Up-Down displacement of the Shoulders across the whole animation."), GUILayout.Width(textWidth), GUILayout.Height(textHeight));
            shoulderOffset = EditorGUILayout.Slider(shoulderOffset, -shRange, shRange);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                OffsetShoulders();
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(GUILayout.Width(sliderWidth));
            GUILayout.Label(new GUIContent("Arm", "Adjust the Upper Arm Up-Down rotation. Controls the 'lift' of the arms."), GUILayout.Width(textWidth), GUILayout.Height(textHeight));
            armOffset = EditorGUILayout.Slider(armOffset, -aRange, aRange);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                OffsetArms();
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(GUILayout.Width(sliderWidth));
            GUILayout.Label(new GUIContent("(Flexion)", "Adjust the Upper Arm Front-Back rotation. Controls the 'Flexion' or 'Extension' of the arms."), GUILayout.Width(textWidth), GUILayout.Height(textHeight));
            armFBOffset = EditorGUILayout.Slider(armFBOffset, -aRange, aRange);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                OffsetArmsFB();                
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(GUILayout.Width(sliderWidth));
            GUILayout.Label(new GUIContent("Leg", "Adjust the Upper Leg In-Out rotation. Controls the width of the character's stance."), GUILayout.Width(textWidth), GUILayout.Height(textHeight));
            legOffset = EditorGUILayout.Slider(legOffset, -lRange, lRange);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                OffsetLegs();                
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(GUILayout.Width(sliderWidth));
            GUILayout.Label(new GUIContent("Heel", "Ajdust the angle of the Foot Up-Down rotation. Controls the angle of the heel."), GUILayout.Width(textWidth), GUILayout.Height(textHeight));
            heelOffset = EditorGUILayout.Slider(heelOffset, -hRange, hRange);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                OffsetHeel();                
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(GUILayout.Width(sliderWidth));
            GUILayout.Label(new GUIContent("Height", "Adjust the vertical 'y' displacement of the character."), GUILayout.Width(textWidth), GUILayout.Height(textHeight));
            heightOffset = EditorGUILayout.Slider(heightOffset, -yRange, yRange);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                OffsetHeight();
            }
            GUILayout.EndVertical(); // End of animation curve adjustment sliders

            // Lower close, reset and save controls
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box");  // close button
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_clear").image, "Close this window."), GUILayout.Width(smallIconDim), GUILayout.Height(smallIconDim)))
            {
                CloseRetargeter();
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical("box");  // hold button
            if (GUILayout.Button(new GUIContent(holdValues ? lockedImage : unlockedImage, string.Format("STATUS: " + (holdValues ? "LOCKED VALUES : slider settings are retained when animation is changed." : "UNLOCKED VALUES : slider settings are reset when animation is changed."))), GUILayout.Width(smallIconDim), GUILayout.Height(smallIconDim)))
            {
                holdValues = !holdValues;
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical("box");  // reset button
            if (GUILayout.Button(new GUIContent(resetImage, "Reset all slider settings and applied modifications."), GUILayout.Width(smallIconDim), GUILayout.Height(smallIconDim)))
            {                
                ResetClip();
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical("box"); // save button
            if (GUILayout.Button(new GUIContent(saveImage, "Save the modified animation to the 'Project Assets'.  This will create a new animation in the 'Home Directory' of the selected model named <Model Name>_<Animation Name>.anim"), GUILayout.Width(smallIconDim), GUILayout.Height(smallIconDim)))
            {
                GameObject scenePrefab = AnimPlayerGUI.CharacterAnimator.gameObject;
                GameObject fbxAsset = Util.FindRootPrefabAssetFromSceneObject(scenePrefab);
                if (fbxAsset)
                {
                    string characterFbxPath = AssetDatabase.GetAssetPath(fbxAsset);
                    string assetPath = GenerateClipAssetPath(OriginalClip, characterFbxPath);
                    WriteAnimationToAssetDatabase(WorkingClip, assetPath, true);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal(); // End of reset and save controls

            GUILayout.EndVertical();
            // End of retarget controls
        }

        public static bool CanClipLoop(AnimationClip clip)
        {
            bool canLoop = true;
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
            foreach (EditorCurveBinding binding in curveBindings)
            {
                Keyframe[] testKeys = AnimationUtility.GetEditorCurve(clip, binding).keys;
                if (Math.Round(testKeys[0].value, 2) != Math.Round(testKeys[testKeys.Length - 1].value, 2))
                {
                    canLoop = false;
                }
            }
            return canLoop;
        }

        static void CloseMouthToggle(bool close)
        {
            if (!(OriginalClip && WorkingClip)) return;

            bool found = false;
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(OriginalClip);
            EditorCurveBinding targetBinding = new EditorCurveBinding();
            AnimationCurve jawCurve = new AnimationCurve();
            Keyframe[] jawKeys;

            foreach (EditorCurveBinding binding in curveBindings)
            {
                if (binding.propertyName.Equals(jawClose))
                {
                    targetBinding = binding;
                    found = true;
                }
            }

            if (found)
            {
                jawCurve = AnimationUtility.GetEditorCurve(OriginalClip, targetBinding);
            }
            else
            {
                targetBinding = new EditorCurveBinding() { propertyName = jawClose, type = typeof(Animator) };
                jawKeys = new Keyframe[] {
                    new Keyframe( 0f, 0f ),
                    new Keyframe( OriginalClip.length, 0f )
                };
                jawCurve.keys = jawKeys;
            }

            if (close)
            {
                jawKeys = jawCurve.keys;
                for (int i = 0; i < jawKeys.Length; i++)
                {
                    jawKeys[i].value = 1;
                }
                jawCurve.keys = jawKeys;
            }
            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);
            AnimationUtility.SetEditorCurve(swapClip, targetBinding, jawCurve);
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);
            AnimPlayerGUI.UpdateAnimator();
        }

        static void ApplyPose(int mode)
        {
            if (!(OriginalClip && WorkingClip)) return;

            switch (mode)
            {
                case 0:
                    {
                        ResetPose();
                        break;
                    }
                case 1:
                    {
                        SetPose(openHandPose);
                        break;
                    }
                case 2:
                    {
                        SetPose(closedHandPose);
                        break;
                    }
            }
        }

        static void SetPose(Dictionary<string, float> pose)
        {
            if (!(OriginalClip && WorkingClip)) return;

            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(OriginalClip);
            foreach (EditorCurveBinding binding in curveBindings)
            {
                foreach (KeyValuePair<string, float> p in pose)
                {
                    if (binding.propertyName.Equals(p.Key))
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(OriginalClip, binding);
                        Keyframe[] keys = curve.keys;
                        for (int i = 0; i < keys.Length; i++)
                        {
                            keys[i].value = p.Value;
                        }
                        curve.keys = keys;
                        AnimationUtility.SetEditorCurve(swapClip, binding, curve);
                    }
                }
            }
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);
            AnimPlayerGUI.UpdateAnimator();
        }

        static void ResetPose()
        {
            if (!(OriginalClip && WorkingClip)) return;

            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(OriginalClip);
            foreach (EditorCurveBinding binding in curveBindings)
            {
                if (handCurves.Contains(binding.propertyName))
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(OriginalClip, binding);
                    AnimationUtility.SetEditorCurve(swapClip, binding, curve);
                }
            }
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);
            AnimPlayerGUI.UpdateAnimator();
        }

        static void OffsetALL()
        {
            OffsetShoulders();
            OffsetArms();
            OffsetArmsFB();
            OffsetLegs();
            OffsetHeel();
            OffsetHeight();
            CloseMouthToggle(closeMouth);
            ApplyPose(handPose);            
        }

        static void SetEditorCurves(AnimationClip clip, List<EditorCurveBinding> bindings, List<AnimationCurve> curves)
        {
#if UNITY_2020_3_OR_NEWER
            AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), curves.ToArray());
#else
            int numClips = bindings.Count;
            for (int i = 0; i < numClips; i++)
            {
                AnimationUtility.SetEditorCurve(clip, bindings[i], curves[i]);
            }
#endif
        }

        static void OffsetShoulders()
        {
            if (!(OriginalClip && WorkingClip)) return;
                        
            List<EditorCurveBinding> applicableBindings = new List<EditorCurveBinding>();
            List<AnimationCurve> applicableCurves = new List<AnimationCurve>();

            foreach (KeyValuePair<string, EditorCurveBinding> bind in shoulderBindings)
            {
                float scale = 0f;
                bool eval = false;
                bool subtract = true;
                bool update = false;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(OriginalClip, bind.Value);
                Keyframe[] keys = curve.keys;

                switch (bind.Key)
                {
                    case lShoulder:
                        {
                            scale = srScale;
                            eval = true;
                            subtract = true;
                        }
                        break;
                    case rShoulder:
                        {
                            scale = srScale;
                            eval = true;
                            subtract = true;
                        }
                        break;
                    case lArm:
                    case lArmFB:
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                            update = true;
                        }
                        break;
                    case rArm:
                    case rArmFB:
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                            update = true;
                        }
                        break;
                    case lArmTwist:
                        {
                            scale = atScale;
                            eval = false;
                            subtract = true;
                        }
                        break;
                    case rArmTwist:
                        {
                            scale = atScale;
                            eval = false;
                            subtract = true;
                        }
                        break;
                }

                float diff = shoulderOffset * scale;
                if (update)
                {
                    backgroundArmOffset = diff / arScale;
                    diff = (backgroundArmOffset + armOffset) * scale;
                }

                for (int a = 0; a < keys.Length; a++)
                {
                    keys[a].value = eval ? EvaluateValue(keys[a].value, subtract ? -diff : diff) : keys[a].value + (subtract ? -diff : diff);
                }
                curve.keys = keys;
                for (int b = 0; b < keys.Length; b++)
                {
                    curve.SmoothTangents(b, 0.0f);
                }
                applicableBindings.Add(bind.Value);
                applicableCurves.Add(curve);
            }            
            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);
            SetEditorCurves(swapClip, applicableBindings, applicableCurves);
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);            
            AnimPlayerGUI.UpdateAnimator();
        }

        static void OffsetArms()
        {
            if (!(OriginalClip && WorkingClip)) return;

            List<EditorCurveBinding> applicableBindings = new List<EditorCurveBinding>();
            List<AnimationCurve> applicableCurves = new List<AnimationCurve>();

            foreach (KeyValuePair<string, EditorCurveBinding> bind in armBindings)
            {
                float scale = 0f;
                bool eval = false;
                bool subtract = true;
                bool includeBackgroundVal = false;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(OriginalClip, bind.Value);
                Keyframe[] keys = curve.keys;

                switch (bind.Key)
                {
                    case lArm:                    
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                            includeBackgroundVal = true;
                        }
                        break;
                    case rArm:                    
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                            includeBackgroundVal = true;
                        }
                        break;
                    case lArmTwist:
                        {
                            scale = atScale;
                            eval = false;
                            subtract = true;
                        }
                        break;
                    case rArmTwist:
                        {
                            scale = atScale;
                            eval = false;
                            subtract = true;
                        }
                        break;
                }

                float diff = armOffset * scale;
                if (includeBackgroundVal)
                {
                    diff = (backgroundArmOffset + armOffset) * scale;
                }

                for (int a = 0; a < keys.Length; a++)
                {

                    keys[a].value = eval ? EvaluateValue(keys[a].value, subtract ? -diff : diff) : keys[a].value + (subtract ? -diff : diff);
                }
                curve.keys = keys;
                for (int b = 0; b < keys.Length; b++)
                {
                    curve.SmoothTangents(b, 0.0f);
                }
                applicableBindings.Add(bind.Value);
                applicableCurves.Add(curve);
            }
            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);
            SetEditorCurves(swapClip, applicableBindings, applicableCurves);
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);
            AnimPlayerGUI.UpdateAnimator();
        }

        static void OffsetArmsFB()
        {
            if (!(OriginalClip && WorkingClip)) return;

            List<EditorCurveBinding> applicableBindings = new List<EditorCurveBinding>();
            List<AnimationCurve> applicableCurves = new List<AnimationCurve>();

            foreach (KeyValuePair<string, EditorCurveBinding> bind in armFBBindings)
            {
                float scale = 0f;
                bool eval = false;
                bool subtract = true;
                bool includeBackgroundVal = false;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(OriginalClip, bind.Value);
                Keyframe[] keys = curve.keys;

                switch (bind.Key)
                {                    
                    case lArmFB:
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                            includeBackgroundVal = false;
                        }
                        break;
                    case rArmFB:
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                            includeBackgroundVal = false;
                        }
                        break;                    
                }

                float diff = armFBOffset * scale;
                if (includeBackgroundVal)
                {
                    diff = (backgroundArmOffset + armFBOffset) * scale;
                }

                for (int a = 0; a < keys.Length; a++)
                {

                    keys[a].value = eval ? EvaluateValue(keys[a].value, subtract ? -diff : diff) : keys[a].value + (subtract ? -diff : diff);
                }
                curve.keys = keys;
                for (int b = 0; b < keys.Length; b++)
                {
                    curve.SmoothTangents(b, 0.0f);
                }
                applicableBindings.Add(bind.Value);
                applicableCurves.Add(curve);
            }
            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);
            SetEditorCurves(swapClip, applicableBindings, applicableCurves);
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);
            AnimPlayerGUI.UpdateAnimator();
        }

        static void OffsetLegs()
        {
            if (!(OriginalClip && WorkingClip)) return;

            List<EditorCurveBinding> applicableBindings = new List<EditorCurveBinding>();
            List<AnimationCurve> applicableCurves = new List<AnimationCurve>();

            foreach (KeyValuePair<string, EditorCurveBinding> bind in legBindings)
            {
                float scale = 0f;
                bool eval = false;
                bool subtract = true;

                AnimationCurve curve = AnimationUtility.GetEditorCurve(OriginalClip, bind.Value);
                Keyframe[] keys = curve.keys;

                switch (bind.Key)
                {
                    case lLeg:
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                        }
                        break;
                    case rLeg:
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                        }
                        break;
                    case lFootTwist:
                        {
                            scale = ftScale;
                            eval = false;
                            subtract = true;
                        }
                        break;
                    case rFootTwist:
                        {
                            scale = ftScale;
                            eval = false;
                            subtract = true;
                        }
                        break;
                }

                float diff = legOffset * scale;

                for (int a = 0; a < keys.Length; a++)
                {

                    keys[a].value = eval ? EvaluateValue(keys[a].value, subtract ? -diff : diff) : keys[a].value + (subtract ? -diff : diff);
                }
                curve.keys = keys;
                for (int b = 0; b < keys.Length; b++)
                {
                    curve.SmoothTangents(b, 0.0f);
                }
                applicableBindings.Add(bind.Value);
                applicableCurves.Add(curve);
            }
            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);
            SetEditorCurves(swapClip, applicableBindings, applicableCurves);
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);
            AnimPlayerGUI.UpdateAnimator();
        }

        static void OffsetHeel()
        {
            if (!(OriginalClip && WorkingClip)) return;

            List<EditorCurveBinding> applicableBindings = new List<EditorCurveBinding>();
            List<AnimationCurve> applicableCurves = new List<AnimationCurve>();

            foreach (KeyValuePair<string, EditorCurveBinding> bind in heelBindings)
            {
                float scale = 0f;
                bool eval = false;
                bool subtract = true;

                AnimationCurve curve = AnimationUtility.GetEditorCurve(OriginalClip, bind.Value);
                Keyframe[] keys = curve.keys;

                switch (bind.Key)
                {
                    case lFoot:
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                        }
                        break;
                    case rFoot:
                        {
                            scale = arScale;
                            eval = true;
                            subtract = false;
                        }
                        break;
                    case lToes:
                        {
                            scale = trScale;
                            eval = false;
                            subtract = true;
                        }
                        break;
                    case rToes:
                        {
                            scale = trScale;
                            eval = false;
                            subtract = true;
                        }
                        break;
                }

                float diff = heelOffset * scale;

                for (int a = 0; a < keys.Length; a++)
                {

                    keys[a].value = eval ? EvaluateValue(keys[a].value, subtract ? -diff : diff) : keys[a].value + (subtract ? -diff : diff);
                }
                curve.keys = keys;
                for (int b = 0; b < keys.Length; b++)
                {
                    curve.SmoothTangents(b, 0.0f);
                }
                applicableBindings.Add(bind.Value);
                applicableCurves.Add(curve);
            }
            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);
            SetEditorCurves(swapClip, applicableBindings, applicableCurves);
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);
            AnimPlayerGUI.UpdateAnimator();
        }

        static void OffsetHeight()
        {
            if (!(OriginalClip && WorkingClip)) return;

            List<EditorCurveBinding> applicableBindings = new List<EditorCurveBinding>();
            List<AnimationCurve> applicableCurves = new List<AnimationCurve>();

            foreach (KeyValuePair<string, EditorCurveBinding> bind in heightBindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(OriginalClip, bind.Value);
                Keyframe[] keys = curve.keys;

                float diff = heightOffset;

                for (int a = 0; a < keys.Length; a++)
                {
                    keys[a].value = keys[a].value + diff;
                }
                curve.keys = keys;
                for (int b = 0; b < keys.Length; b++)
                {
                    curve.SmoothTangents(b, 0.0f);
                }
                applicableBindings.Add(bind.Value);
                applicableCurves.Add(curve);
            }
            AnimationClip swapClip = AnimPlayerGUI.CloneClip(WorkingClip);
            SetEditorCurves(swapClip, applicableBindings, applicableCurves);
            AnimPlayerGUI.SelectOverrideAnimationWithoutReset(swapClip, AnimPlayerGUI.animatorOverrideController);
            AnimPlayerGUI.UpdateAnimator();
        }

        static float EvaluateValue(float currentKeyValue, float deltaValue)
        {
            //if currently above zero   
            if (currentKeyValue >= 0f)
            {
                //if it ends up below zero then the negative contribution must be x2
                if ((currentKeyValue + deltaValue) < 0f)
                {
                    return (currentKeyValue + deltaValue) * 2f;
                }
                else
                //if it ends up above zero then return sum
                {
                    return currentKeyValue + deltaValue;
                }
            }

            //if currently bleow zero
            if (currentKeyValue < 0f)
            {
                //if both are negative then double the contribution from delta and return
                if (deltaValue < 0f)
                {
                    return currentKeyValue + deltaValue * 2f;
                }
                else
                {
                    //if delta is positive then we have to consider where it will end up with a below zero contribution * 2
                    if ((currentKeyValue + deltaValue * 2f) < 0f)
                    {
                        //where the value simply ends up still negative then we can return that
                        return currentKeyValue + deltaValue * 2f;
                    }
                    else
                    {
                        //where the value ends up positive we must return half the positive value
                        return (currentKeyValue + deltaValue * 2f) / 2f;
                    }
                }
            }
            return 3f;  // go wrong spectacularly
        }

        static float logtime = 0f;

        public static void CopyCurve(AnimationClip originalClip, AnimationClip workingClip, string goName, 
                                     string targetPropertyName, EditorCurveBinding sourceCurveBinding)
        {
            float time = Time.realtimeSinceStartup;

            EditorCurveBinding workingBinding = new EditorCurveBinding()
            {
                path = goName,
                type = typeof(SkinnedMeshRenderer),
                propertyName = targetPropertyName
            };

            if (AnimationUtility.GetEditorCurve(workingClip, workingBinding) == null || 
                targetPropertyName != sourceCurveBinding.propertyName)
            {
                AnimationCurve workingCurve = AnimationUtility.GetEditorCurve(originalClip, sourceCurveBinding);
                AnimationUtility.SetEditorCurve(workingClip, workingBinding, workingCurve);
            }

            logtime += Time.realtimeSinceStartup - time;
        }

        public static bool CurveHasData(EditorCurveBinding binding, AnimationClip clip)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

            if (curve != null)
            {
                if (curve.length > 2) return true;
                for (int i = 0; i < curve.length; i++)
                {
                    if (Mathf.Abs(curve.keys[i].value) > 0.001f) return true;
                }
            }

            return false;
        }

        static void RetargetBlendShapes(AnimationClip originalClip, AnimationClip workingClip, 
            GameObject targetCharacterModel, bool log = true)
        {
            if (!(originalClip && workingClip)) return;

            const string blendShapePrefix = "blendShape."; 
            
            Transform[] targetAssetData = targetCharacterModel.GetComponentsInChildren<Transform>();
            FacialProfile meshProfile = FacialProfileMapper.GetMeshFacialProfile(targetCharacterModel);
            if (!meshProfile.HasFacialShapes)
            {
                if (log) Util.LogWarn("Character has no facial blend shapes!");
                return;
            }
            FacialProfile animProfile = FacialProfileMapper.GetAnimationClipFacialProfile(workingClip);
            if (!animProfile.HasFacialShapes)
            {
                if (log) Util.LogWarn("Animation has no facial blend shapes!");
                return;
            }

            if (log)
            {
                if (!meshProfile.IsSameProfileFrom(animProfile))
                {
                    Util.LogWarn("Retargeting to Facial Profile: " + meshProfile + ", From: " + animProfile + "\n" +
                                     "Warning: Character mesh facial profile does not match the animation facial profile.\n" +
                                     "Facial expression retargeting may not have the expected or desired results.\n");
                }
                else
                {
                    Util.LogAlways("Retargeting to Facial Profile: " + meshProfile + ", From: " + animProfile + "\n");
                }
            }

            EditorCurveBinding[] sourceCurveBindings = AnimationUtility.GetCurveBindings(workingClip);

            // Find all of the blendshape relevant binding paths that are not needed in the target animation        
            List<string> uniqueSourcePaths = new List<string>();
            foreach (EditorCurveBinding binding in sourceCurveBindings)
            {
                if (binding.propertyName.StartsWith(blendShapePrefix))
                {
                    if (!uniqueSourcePaths.Contains(binding.path))
                        uniqueSourcePaths.Add(binding.path);
                }
            }

            List<string> validTargetPaths = new List<string>();
            foreach (Transform t in targetAssetData)
            {
                GameObject go = t.gameObject;
                if (go.GetComponent<SkinnedMeshRenderer>())
                {
                    if (go.GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount > 0)
                    {
                        validTargetPaths.Add(go.name);
                    }
                }
            }

            List<string> pathsToPurge = new List<string>();
            foreach (string path in uniqueSourcePaths)
            {
                if (!validTargetPaths.Contains(path))
                {
                    pathsToPurge.Add(path);
                }
            }

            logtime = 0f;
            string report = "";

            // build a cache of the blend shape names and their curve bindings:
            Dictionary<string, EditorCurveBinding> cache = new Dictionary<string, EditorCurveBinding>();            
            for (int i = 0; i < sourceCurveBindings.Length; i++)
            {
                if (CurveHasData(sourceCurveBindings[i], workingClip) && 
                    sourceCurveBindings[i].propertyName.StartsWith(blendShapePrefix))
                {
                    string blendShapeName = sourceCurveBindings[i].propertyName.Substring(blendShapePrefix.Length);
                    string profileBlendShapeName = meshProfile.GetMappingFrom(blendShapeName, animProfile);                    
                    if (!string.IsNullOrEmpty(profileBlendShapeName))
                    {
                        List<string> multiProfileName = FacialProfileMapper.GetMultiShapeNames(profileBlendShapeName);
                        if (multiProfileName.Count == 1)
                        {
                            if (!cache.ContainsKey(profileBlendShapeName))
                            {
                                cache.Add(profileBlendShapeName, sourceCurveBindings[i]);
                                report += "Mapping: " + profileBlendShapeName + " from " + blendShapeName + "\n";
                            }
                        }
                        else
                        {
                            foreach (string multiShapeName in multiProfileName)
                            {
                                if (!cache.ContainsKey(multiShapeName))
                                {
                                    cache.Add(multiShapeName, sourceCurveBindings[i]);
                                    report += "Mapping (multi): " + multiShapeName + " from " + blendShapeName + "\n";
                                }
                            }
                        }
                    }
                }
            }

            List<string> mappedBlendShapes = new List<string>();

            // apply the curves to the target animation
            foreach (Transform t in targetAssetData)
            {
                GameObject go = t.gameObject;
                SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                if (smr && smr.sharedMesh && smr.sharedMesh.blendShapeCount > 0)
                {
                    for (int j = 0; j < smr.sharedMesh.blendShapeCount; j++)
                    {
                        string blendShapeName = smr.sharedMesh.GetBlendShapeName(j);
                        string targetPropertyName = blendShapePrefix + blendShapeName;                        

                        if (cache.TryGetValue(blendShapeName, out EditorCurveBinding sourceCurveBinding))
                        {                            
                            CopyCurve(originalClip, workingClip, go.name, targetPropertyName, sourceCurveBinding);

                            if (!mappedBlendShapes.Contains(blendShapeName))
                                mappedBlendShapes.Add(blendShapeName);
                        }
                        else
                        {
                            //report += "Could not map blendshape: " + blendShapeName + " in object: " + go.name + "\n";
                        }
                    }
                }
            }

            report += "\n";
            int curvesFailedToMap = 0;
            foreach (string shape in cache.Keys)
            {                
                if (!mappedBlendShapes.Contains(shape))
                {
                    curvesFailedToMap++;
                    report += "Could not find BlendShape: " + shape + " in target character.\n";
                }                
            }

            string reportHeader = "Blendshape Mapping report:\n";
            if (curvesFailedToMap == 0) reportHeader += "All " + cache.Count + " BlendShape curves retargeted!\n\n";
            else reportHeader += curvesFailedToMap + " out of " + cache.Count + " BlendShape curves could not be retargeted!\n\n";

            if (log) Util.LogAlways(reportHeader + report);

            bool PURGE = true; 
            // Purge all curves from the animation that dont have a valid path in the target object                    
            if (PURGE)
            {
                EditorCurveBinding[] targetCurveBindings = AnimationUtility.GetCurveBindings(workingClip);
                for (int k = 0; k < targetCurveBindings.Length; k++)
                {
                    if (pathsToPurge.Contains(targetCurveBindings[k].path))
                    {
                        AnimationUtility.SetEditorCurve(workingClip, targetCurveBindings[k], null);
                    }
                    else
                    {
                        // purge all extra blend shape animations
                        if (targetCurveBindings[k].propertyName.StartsWith(blendShapePrefix))
                        {
                            string blendShapeName = targetCurveBindings[k].propertyName.Substring(blendShapePrefix.Length);
                            if (!cache.ContainsKey(blendShapeName))
                            {
                                AnimationUtility.SetEditorCurve(workingClip, targetCurveBindings[k], null);
                            }
                        }
                    }
                }                
            }
        }

        static string GenerateClipAssetPath(AnimationClip originalClip, string characterFbxPath, string prefix = "", bool overwrite = false)
        {
            if (!originalClip || string.IsNullOrEmpty(characterFbxPath)) return null;

            string characterName = Path.GetFileNameWithoutExtension(characterFbxPath);
            string fbxFolder = Path.GetDirectoryName(characterFbxPath);
            string animFolder = Path.Combine(fbxFolder, ANIM_FOLDER_NAME, characterName);
            Util.EnsureAssetsFolderExists(animFolder);
            string clipName = originalClip.name;
            if (clipName.iStartsWith(characterName + "_"))
                clipName = clipName.Remove(0, characterName.Length + 1);

            if (string.IsNullOrEmpty(prefix))
            {
                string clipPath = AssetDatabase.GetAssetPath(originalClip);
                string clipFile = Path.GetFileNameWithoutExtension(clipPath);
                if (!clipPath.iEndsWith(".anim")) prefix = clipFile;
            }

            string animName = NameAnimation(characterName, clipName, prefix);
            string assetPath = Path.Combine(animFolder, animName + ".anim");

            if (!overwrite)
            {
                if (!Util.AssetPathIsEmpty(assetPath))
                {
                    for (int i = 0; i < 999; i++)
                    {
                        string extension = string.Format("{0:000}", i);
                        assetPath = Path.Combine(animFolder, animName + "_" + extension + ".anim");
                        if (Util.AssetPathIsEmpty(assetPath)) break;
                    }
                }
            }

            return assetPath;
        }

        static AnimationClip WriteAnimationToAssetDatabase(AnimationClip workingClip, string assetPath, bool originalSettings = false)
        {            
            if (string.IsNullOrEmpty(assetPath)) return null;

            Util.LogDetail("Writing Asset: " + assetPath);

            var output = Object.Instantiate(workingClip);  // clone so that workingClip isn't locked to an on-disk asset
            AnimationClip outputClip = output as AnimationClip;

            if (originalSettings)
            {
                // **Addition** for the edit mode animator player: the clip settings of the working clip
                // may contain user set flags that are for evaluation purposes only (e.g. loopBlendPositionXZ)
                // the original clip's settings should be copied to the output clip and the loop flag set as
                // per the user preference to auto loop the animation.

                // record the user preferred loop status 
                AnimationClipSettings outputClipSettings = AnimationUtility.GetAnimationClipSettings(outputClip);
                bool isLooping = outputClipSettings.loopTime;

                // obtain the original settings
                AnimationClipSettings originalClipSettings = AnimationUtility.GetAnimationClipSettings(OriginalClip);

                // re-impose the loop status            
                originalClipSettings.loopTime = isLooping;

                //update the output clip with the looping modified original settings
                AnimationUtility.SetAnimationClipSettings(outputClip, outputClipSettings);

                // the correct settings can now be written to disk - but the in memory copy used by the
                // player/re-tartgeter will be untouched so end users dont see a behaviour change after saving

                // **End of addition**
            }            
            
            AnimationClip asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (asset == null)
            {
                // New
                Util.LogDetail("Writing New Asset: " + assetPath);
                AssetDatabase.CreateAsset(outputClip, assetPath);
            }
            else
            {
                Util.LogDetail("Updating Existing Asset: " + assetPath);
                outputClip.name = asset.name;
                EditorUtility.CopySerialized(outputClip, asset);
                AssetDatabase.SaveAssets();
            }

            asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            Selection.objects = new Object[] { asset };
            return asset;
        }

        static string NameAnimation(string characterName, string clipName, string prefix)
        {
            string animName;
            if (string.IsNullOrEmpty(prefix))
                animName = characterName + "_" + clipName;
            else
                animName = characterName + "_" + prefix + "_" + clipName;
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(invalid)));
            return r.Replace(animName, "_");
        }

        // Curve Master Data

        // Jaw curve
        const string jawClose = "Jaw Close";

        // Shoulder, Six curves to consider
        const string lShoulder = "Left Shoulder Down-Up";
        const string lArm = "Left Arm Down-Up";
        const string lArmFB = "Left Arm Front-Back";
        const string lArmTwist = "Left Arm Twist In-Out";

        const string rShoulder = "Right Shoulder Down-Up";
        const string rArm = "Right Arm Down-Up";
        const string rArmFB = "Right Arm Front-Back";
        const string rArmTwist = "Right Arm Twist In-Out";

        // Arm, Four Curves to consider
        // lArm lArmTwist rArm rArmTwist

        // Leg, Four Curves to consider
        const string lLeg = "Left Upper Leg In-Out";
        const string lFootTwist = "Left Foot Twist In-Out";

        const string rLeg = "Right Upper Leg In-Out";
        const string rFootTwist = "Right Foot Twist In-Out";

        // Heel, Four Curves to consider
        const string lFoot = "Left Foot Up-Down";
        const string lToes = "Left Toes Up-Down";

        const string rFoot = "Right Foot Up-Down";
        const string rToes = "Right Toes Up-Down";

        // Height, One Curve to consider
        const string yRoot = "RootT.y";

        static string[] shoulderCurveNames = new string[]
                {
                    lShoulder,
                    lArm,
                    lArmTwist,
                    rShoulder,
                    rArm,
                    rArmTwist
                };

        static string[] armCurveNames = new string[]
                {
                    lArm,
                    lArmTwist,
                    rArm,
                    rArmTwist
                };

        static string[] armFBCurveNames = new string[]
                {
                    lArmFB,                    
                    rArmFB,                    
                };

        static string[] legCurveNames = new string[]
                {
                    lLeg,
                    lFootTwist,
                    rLeg,
                    rFootTwist
                };

        static string[] heelCurveNames = new string[]
                {
                    lFoot,
                    lToes,
                    rFoot,
                    rToes
                };

        static string[] heightCurveNames = new string[]
                {
                    yRoot
                };

        //Translation ratios to convert angles to Mechanim values
        const float srScale = 12f / 360f; // Shoulder Rotation scale
        const float arScale = 3.6f / 360f; // Arm Rotation scale
        const float atScale = 1f / 360f; // Arm Twist scale
        const float ftScale = 8f / 360f; // Foot Twist scale
        const float trScale = 4f / 360f; // Toe rotation scale

        // Pose Master Data
        private static void ExtractPose()
        {
            string dictName = "openHandPose";
            string filename = "pose";
            string extension = ".cs";

            string searchString = "hand.";
            float timeStamp = 0.1f;

            EditorCurveBinding[] sourceCurveBindings = AnimationUtility.GetCurveBindings(WorkingClip);

            string pathString = "Dictionary<string, float> " + dictName + " = new Dictionary<string, float>()\r";
            pathString += "{\r";
            foreach (EditorCurveBinding binding in sourceCurveBindings)
            {
                if (binding.propertyName.ToLower().Contains(searchString))
                {
                    pathString += "\t{ \"" + binding.propertyName + "\", ";
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(WorkingClip, binding);
                    float value = curve.Evaluate(timeStamp);
                    pathString += value + "f },\r";
                }
            }
            pathString += "};";
            string path = "Assets/" + filename + extension;
            System.IO.File.WriteAllText(path, pathString);
        }

        public static void GenerateCharacterTargetedAnimations(string motionAssetPath, 
            GameObject targetCharacterModel, bool replaceIfExists)
        {
            AnimationClip[] clips = Util.GetAllAnimationClipsFromCharacter(motionAssetPath);            

            if (!targetCharacterModel) targetCharacterModel = Util.FindCharacterPrefabAsset(motionAssetPath);
            if (!targetCharacterModel) return;

            string firstPath = null;

            if (clips.Length > 0)
            {
                int index = 0;
                foreach (AnimationClip clip in clips)
                {
                    string assetPath = GenerateClipAssetPath(clip, motionAssetPath, RETARGET_SOURCE_PREFIX, true);
                    if (string.IsNullOrEmpty(firstPath)) firstPath = assetPath;
                    if (File.Exists(assetPath) && !replaceIfExists) continue;
                    AnimationClip workingClip = AnimPlayerGUI.CloneClip(clip);
                    RetargetBlendShapes(clip, workingClip, targetCharacterModel, false);
                    AnimationClip asset = WriteAnimationToAssetDatabase(workingClip, assetPath, false);
                    index++;
                }

                if (!string.IsNullOrEmpty(firstPath))
                    AnimPlayerGUI.UpdateAnimatorClip(CharacterAnimator, 
                                                     AssetDatabase.LoadAssetAtPath<AnimationClip>(firstPath));
            }
        }

        /// <summary>
        /// Tries to get the retargeted version of the animation clip from the given source animation clip, 
        /// usually from the original character fbx.
        /// </summary>
        public static AnimationClip TryGetRetargetedAnimationClip(GameObject fbxAsset, AnimationClip clip)
        {
            try
            {
                if (clip)
                {
                    string fbxPath = AssetDatabase.GetAssetPath(fbxAsset);
                    string characterName = Path.GetFileNameWithoutExtension(fbxPath);
                    string fbxFolder = Path.GetDirectoryName(fbxPath);
                    string animFolder = Path.Combine(fbxFolder, ANIM_FOLDER_NAME, characterName);

                    string animName = NameAnimation(characterName, clip.name, RETARGET_SOURCE_PREFIX);
                    string assetPath = Path.Combine(animFolder, animName + ".anim");
                    AnimationClip retargetedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                    if (retargetedClip) return retargetedClip;
                }
                return clip;
            }
            catch (Exception)
            {
                return clip;
            }
        }


        static Dictionary<string, float> openHandPose = new Dictionary<string, float>()
        {
            { "LeftHand.Thumb.1 Stretched", -1.141453f },
            { "LeftHand.Thumb.Spread", -0.4620222f },
            { "LeftHand.Thumb.2 Stretched", 0.5442108f },
            { "LeftHand.Thumb.3 Stretched", 0.4577243f },
            { "LeftHand.Index.1 Stretched", 0.3184956f },
            { "LeftHand.Index.Spread", -0.4479268f },
            { "LeftHand.Index.2 Stretched", 0.2451891f },
            { "LeftHand.Index.3 Stretched", 0.6176971f },
            { "LeftHand.Middle.1 Stretched", 0.09830929f },
            { "LeftHand.Middle.Spread", -0.5679846f },
            { "LeftHand.Middle.2 Stretched", 0.3699116f },
            { "LeftHand.Middle.3 Stretched", 0.3705207f },
            { "LeftHand.Ring.1 Stretched", 0.09632754f },
            { "LeftHand.Ring.Spread", -0.5876712f },
            { "LeftHand.Ring.2 Stretched", 0.1289254f },
            { "LeftHand.Ring.3 Stretched", 0.3732445f },
            { "LeftHand.Little.1 Stretched", 0.09448492f },
            { "LeftHand.Little.Spread", -0.4517526f },
            { "LeftHand.Little.2 Stretched", -0.003889897f },
            { "LeftHand.Little.3 Stretched", -0.04161567f },
            { "RightHand.Thumb.1 Stretched", -1.135697f },
            { "RightHand.Thumb.Spread", -0.4576517f },
            { "RightHand.Thumb.2 Stretched", 0.5427816f },
            { "RightHand.Thumb.3 Stretched", 0.4549177f },
            { "RightHand.Index.1 Stretched", 0.3184868f },
            { "RightHand.Index.Spread", -0.4478924f },
            { "RightHand.Index.2 Stretched", 0.2451727f },
            { "RightHand.Index.3 Stretched", 0.617752f },
            { "RightHand.Middle.1 Stretched", 0.09830251f },
            { "RightHand.Middle.Spread", -0.5680417f },
            { "RightHand.Middle.2 Stretched", 0.3699542f },
            { "RightHand.Middle.3 Stretched", 0.3705046f },
            { "RightHand.Ring.1 Stretched", 0.09632745f },
            { "RightHand.Ring.Spread", -0.5876312f },
            { "RightHand.Ring.2 Stretched", 0.1288746f },
            { "RightHand.Ring.3 Stretched", 0.3732805f },
            { "RightHand.Little.1 Stretched", 0.09454078f },
            { "RightHand.Little.Spread", -0.4516154f },
            { "RightHand.Little.2 Stretched", -0.04165318f },
            { "RightHand.Little.3 Stretched", -0.04163568f },
        };

        static Dictionary<string, float> closedHandPose = new Dictionary<string, float>()
        {
            { "LeftHand.Thumb.1 Stretched", -1.141455f },
            { "LeftHand.Thumb.Spread", -0.4620211f },
            { "LeftHand.Thumb.2 Stretched", 0.3974656f },
            { "LeftHand.Thumb.3 Stretched", -0.0122656f },
            { "LeftHand.Index.1 Stretched", -0.4441552f },
            { "LeftHand.Index.Spread", -0.3593751f },
            { "LeftHand.Index.2 Stretched", -0.8875571f },
            { "LeftHand.Index.3 Stretched", -0.3460926f },
            { "LeftHand.Middle.1 Stretched", -0.5940282f },
            { "LeftHand.Middle.Spread", -0.4824f },
            { "LeftHand.Middle.2 Stretched", -0.7796204f },
            { "LeftHand.Middle.3 Stretched", -0.3495999f },
            { "LeftHand.Ring.1 Stretched", -0.5579048f },
            { "LeftHand.Ring.Spread", -1.060186f },
            { "LeftHand.Ring.2 Stretched", -1.001659f },
            { "LeftHand.Ring.3 Stretched", -0.1538185f },
            { "LeftHand.Little.1 Stretched", -0.5157003f },
            { "LeftHand.Little.Spread", -0.5512691f },
            { "LeftHand.Little.2 Stretched", -0.6109533f },
            { "LeftHand.Little.3 Stretched", -0.4368959f },
            { "RightHand.Thumb.1 Stretched", -1.141842f },
            { "RightHand.Thumb.Spread", -0.4619166f },
            { "RightHand.Thumb.2 Stretched", 0.3966853f },
            { "RightHand.Thumb.3 Stretched", -0.01453214f },
            { "RightHand.Index.1 Stretched", -0.4441575f },
            { "RightHand.Index.Spread", -0.3588968f },
            { "RightHand.Index.2 Stretched", -0.887614f },
            { "RightHand.Index.3 Stretched", -0.3457543f },
            { "RightHand.Middle.1 Stretched", -0.5940221f },
            { "RightHand.Middle.Spread", -0.4824342f },
            { "RightHand.Middle.2 Stretched", -0.7796109f },
            { "RightHand.Middle.3 Stretched", -0.3495855f },
            { "RightHand.Ring.1 Stretched", -0.557913f },
            { "RightHand.Ring.Spread", -1.060112f },
            { "RightHand.Ring.2 Stretched", -1.001655f },
            { "RightHand.Ring.3 Stretched", -0.1538157f },
            { "RightHand.Little.1 Stretched", -0.5156479f },
            { "RightHand.Little.Spread", -0.5513764f },
            { "RightHand.Little.2 Stretched", -0.64873f },
            { "RightHand.Little.3 Stretched", -0.4367864f },
        };

        static string[] handCurves = new string[]
        {
            "LeftHand.Thumb.1 Stretched",
            "LeftHand.Thumb.Spread",
            "LeftHand.Thumb.2 Stretched",
            "LeftHand.Thumb.3 Stretched",
            "LeftHand.Index.1 Stretched",
            "LeftHand.Index.Spread",
            "LeftHand.Index.2 Stretched",
            "LeftHand.Index.3 Stretched",
            "LeftHand.Middle.1 Stretched",
            "LeftHand.Middle.Spread",
            "LeftHand.Middle.2 Stretched",
            "LeftHand.Middle.3 Stretched",
            "LeftHand.Ring.1 Stretched",
            "LeftHand.Ring.Spread",
            "LeftHand.Ring.2 Stretched",
            "LeftHand.Ring.3 Stretched",
            "LeftHand.Little.1 Stretched",
            "LeftHand.Little.Spread",
            "LeftHand.Little.2 Stretched",
            "LeftHand.Little.3 Stretched",
            "RightHand.Thumb.1 Stretched",
            "RightHand.Thumb.Spread",
            "RightHand.Thumb.2 Stretched",
            "RightHand.Thumb.3 Stretched",
            "RightHand.Index.1 Stretched",
            "RightHand.Index.Spread",
            "RightHand.Index.2 Stretched",
            "RightHand.Index.3 Stretched",
            "RightHand.Middle.1 Stretched",
            "RightHand.Middle.Spread",
            "RightHand.Middle.2 Stretched",
            "RightHand.Middle.3 Stretched",
            "RightHand.Ring.1 Stretched",
            "RightHand.Ring.Spread",
            "RightHand.Ring.2 Stretched",
            "RightHand.Ring.3 Stretched",
            "RightHand.Little.1 Stretched",
            "RightHand.Little.Spread",
            "RightHand.Little.2 Stretched",
            "RightHand.Little.3 Stretched"
        };
    }
}