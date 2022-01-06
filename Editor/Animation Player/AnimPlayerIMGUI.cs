using UnityEngine;
using UnityEditor;

namespace Reallusion.Import
{
    public class AnimPlayerIMGUI
    {
        public static AnimationClip originalClip;
        public static Animator animator;
        public static bool play;
        public static float time, prev, current = 0f;
        public static AnimationClip workingClip;
        public static bool foldOut = true;
        public static bool obeyFoldout = true;
        public static bool visible = false;

        public static void SetCharacter(GameObject scenePrefab)
        {
            Animator a = scenePrefab.GetComponent<Animator>();
            if (a)
            {
                animator = a;
                AnimationClip firstClip = Util.GetFirstAnimationClipFromCharacter(scenePrefab);
                originalClip = firstClip;
                workingClip = firstClip;
                time = 0f;
                play = false;

                if (a && firstClip)
                {
                    if (!AnimationMode.InAnimationMode())
                        AnimationMode.StartAnimationMode();
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
            originalClip = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Animation", "Animation to play and manipulate"), originalClip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (originalClip && animator)
                {
                    workingClip = originalClip;
                    time = 0f;
                    play = false;
                    if (!AnimationMode.InAnimationMode())
                        AnimationMode.StartAnimationMode();
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

            if (workingClip != null)
            {
                float startTime = 0.0f;
                float stopTime = workingClip.length;
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
                time -= Time.fixedDeltaTime;
            }
            // "Animation.Play"
            EditorGUI.BeginChangeCheck();
            play = GUILayout.Toggle(play, new GUIContent(EditorGUIUtility.IconContent("Animation.Play").image, "Play (Toggle)"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                prev = Time.realtimeSinceStartup;
            }
            // "PauseButton"
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("PauseButton").image, "Pause"), EditorStyles.toolbarButton))
            {
                play = false;
            }
            // "Animation.NextKey"
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.NextKey").image, "Next Frame"), EditorStyles.toolbarButton))
            {
                play = false;
                time += Time.fixedDeltaTime;
            }
            // "Animation.LastKey"
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Animation.LastKey").image, "Last Frame"), EditorStyles.toolbarButton))
            {
                play = false;
                time = workingClip.length;
            }

            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
            GUILayout.EndVertical();

            if (!EditorApplication.isPlaying && AnimationMode.InAnimationMode())
            {
                if (workingClip && animator)
                {
                    if (play)
                    {
                        current = Time.realtimeSinceStartup;
                        time += (current - prev);
                        if (time >= workingClip.length)
                            time = 0f;
                        prev = current;
                    }

                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(animator.gameObject, workingClip, time);
                    AnimationMode.EndSampling();

                    SceneView.RepaintAll();
                }
            }
        }
        public static void CreatePlayer()
        {
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
            visible = true;
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

            visible = false;
            SceneView.RepaintAll();
        }
    }
}
