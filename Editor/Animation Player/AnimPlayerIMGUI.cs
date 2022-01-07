using UnityEngine;
using UnityEditor;

namespace Reallusion.Import
{
    public class AnimPlayerIMGUI
    {
        public static AnimationClip animationClip;
        public static Animator animator;
        public static bool play;
        public static float time, prev, current = 0f;
        //public static AnimationClip animationClip;
        public static bool foldOut = true;
        public static bool obeyFoldout = true;
        //public static bool visible = false;

        public static void SetCharacter(GameObject characterPrefab)
        {
            Animator anim = characterPrefab.GetComponent<Animator>();
            AnimationClip firstClip = Util.GetFirstAnimationClipFromCharacter(characterPrefab);
            SetPlayerTargets(anim, firstClip);
        }

        public static void SetPlayerTargets(Animator setAnimator, AnimationClip setClip)
        {
            if (setAnimator)
            {
                animator = setAnimator;

                if (setClip)
                {
                    if (AnimationMode.InAnimationMode())
                        AnimationMode.StopAnimationMode();

                    animationClip = setClip;
                    time = 0f;
                    play = false;

                    if (!AnimationMode.InAnimationMode())
                        AnimationMode.StartAnimationMode();

                    SampleOnce();
                }
            }
        }

        public static void DrawPlayer()
        {
            if (obeyFoldout)
            {
                foldOut = EditorGUI.Foldout(new Rect(4f, 0f, 316f, 26f), foldOut, "Animation Playback", EditorStyles.foldout);
                if (!foldOut) return;
            }

            GUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();
            animator = (Animator)EditorGUILayout.ObjectField(new GUIContent("Scene Model", "Animated model in scene"), animator, typeof(Animator), true);
            animationClip = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Animation", "Animation to play and manipulate"), animationClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (animationClip && animator)
                {
                    time = 0f;
                    play = false;
                    if (!AnimationMode.InAnimationMode())
                        AnimationMode.StartAnimationMode();

                    SampleOnce();
                }
                else
                {
                    time = 0f;
                    play = false;
                    if (AnimationMode.InAnimationMode())
                        AnimationMode.StopAnimationMode();
                }
            }

            EditorGUI.BeginDisabledGroup(!AnimationMode.InAnimationMode());

            if (animationClip != null)
            {
                float startTime = 0.0f;
                float stopTime = animationClip.length;
                time = EditorGUILayout.Slider(time, startTime, stopTime);
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
            }
            // "Animation.PrevKey"
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.PrevKey").image, "Previous Frame"), EditorStyles.toolbarButton))
            {
                play = false;
                time -= 0.0166f;
            }
            // "Animation.Play"
            //EditorGUI.BeginChangeCheck();
            play = GUILayout.Toggle(play, new GUIContent(EditorGUIUtility.IconContent("Animation.Play").image, "Play (Toggle)"), EditorStyles.toolbarButton);
            //if (EditorGUI.EndChangeCheck())
            //{
            //    prev = Time.realtimeSinceStartup;
            //}
            // "PauseButton"
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("PauseButton").image, "Pause"), EditorStyles.toolbarButton))
            {
                play = false;
            }
            // "Animation.NextKey"
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.NextKey").image, "Next Frame"), EditorStyles.toolbarButton))
            {
                play = false;
                time += 0.0166f;
            }
            // "Animation.LastKey"
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.LastKey").image, "Last Frame"), EditorStyles.toolbarButton))
            {
                play = false;
                time = animationClip.length;
            }

            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
            GUILayout.EndVertical();
            /*
            //Code for update message block
            if (!EditorApplication.isPlaying && AnimationMode.InAnimationMode())
            {
                if (animationClip && animator)
                {
                    if (play)
                    {
                        current = Time.realtimeSinceStartup;
                        time += (current - prev);
                        if (time >= animationClip.length)
                            time = 0f;
                        prev = current;
                    }

                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(animator.gameObject, animationClip, time);
                    AnimationMode.EndSampling();

                    SceneView.RepaintAll();
                }
            }
            */
        }

        static void SampleOnce()
        {
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(animator.gameObject, animationClip, time);
            AnimationMode.EndSampling();
        }

        public static void CreatePlayer(GameObject characterPrefab = null)
        {
            if (characterPrefab)
            {
                SetCharacter(characterPrefab);
            }

#if SCENEVIEW_OVERLAY_COMPATIBLE
            //2021.2.0a17+  When GUI.Window is called from a static SceneView delegate, it is broken in 2021.2.0f1 - 2021.2.1f1
            //so we switch to overlays starting from an earlier version
            if (AnimPlayerOverlay.exists)
                AnimPlayerOverlay.createdOverlay.Show();

            AnimPlayerIMGUI.obeyFoldout = false;
#else 
            //2020 LTS            
            AnimPlayerWindow.ShowPlayer();

            AnimPlayerIMGUI.obeyFoldout = true;
#endif
            //Common
            //visible = true;
            SceneView.RepaintAll();
        }

        public static void DestroyPlayer()
        {
#if SCENEVIEW_OVERLAY_COMPATIBLE
            //2021.2.0a17+          
            if (AnimPlayerOverlay.exists)
                AnimPlayerOverlay.createdOverlay.Hide();
#else
            //2020 LTS            
            AnimPlayerWindow.HidePlayer();
#endif
            //Common
            AnimPlayerIMGUI.play = false;
            AnimPlayerIMGUI.time = 0f;
            AnimPlayerIMGUI.animator = null;

            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();

            //visible = false;
            SceneView.RepaintAll();
        }

        public static bool IsPlayerShown()
        {
#if SCENEVIEW_OVERLAY_COMPATIBLE
            //2021.2.0a17+
            return AnimPlayerOverlay.createdOverlay.visible;
#else
            //2020 LTS            
            return AnimPlayerWindow.isShown;
#endif
        }
    }
}
