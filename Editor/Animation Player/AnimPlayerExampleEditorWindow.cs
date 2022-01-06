using UnityEngine;
using UnityEditor;


namespace Reallusion.Import
{
    public class AnimPlayerExampleEditorWindow : EditorWindow
    {
        Animator anim;

        [MenuItem("CC3/Animation Player", priority = 2)]
        public static void InitTool()
        {
            AnimPlayerExampleEditorWindow window = GetWindow<AnimPlayerExampleEditorWindow>("Animation Player ++ ");
            window.minSize = new Vector2(200f, 200f);
        }
         
        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            anim = (Animator)EditorGUILayout.ObjectField(new GUIContent("Model", "Animated model in scene"), anim, typeof(Animator), true);
            if (EditorGUI.EndChangeCheck())
            {
                //set the initially active model here               
                AnimPlayerIMGUI.animator = anim;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_ol_plus").image, "Add"), EditorStyles.toolbarButton))
            {
                //use this to create player
                AnimPlayerIMGUI.CreatePlayer();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_ol_minus").image, "Remove"), EditorStyles.toolbarButton))
            {
                //use this to destroy player
                AnimPlayerIMGUI.DestroyPlayer();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        //include this message in the tool
        void OnDestroy()
        {
            AnimPlayerIMGUI.DestroyPlayer();
        }
    }
}