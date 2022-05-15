/* 
 * Copyright (C) 2021 Victor Soupday
 * This file is part of CC3_Unity_Tools <https://github.com/soupday/cc3_unity_tools>
 * 
 * CC3_Unity_Tools is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * CC3_Unity_Tools is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with CC3_Unity_Tools.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Reallusion.Import
{
    public class ImporterWindow : EditorWindow
    {
        public enum Mode { none, single, multi }

        private static string windowTitle = "CC/iC Importer HDRP";
        private static CharacterInfo contextCharacter;
        private static List<CharacterInfo> validCharacters;
        private static string backScenePath;
        private static Mode mode;
        private static ImporterWindow currentWindow;        
        public static ImporterWindow Current { get { return currentWindow; } }        
        public CharacterInfo Character { get { return contextCharacter; } }        
                        
        private Vector2 iconScrollView;
        private bool previewCharacterAfterGUI;
        private bool refreshAfterGUI;
        private bool buildAfterGUI;
        private bool bakeAfterGUI;
        public enum ImporterWindowMode { Build, Bake, Settings }
        private ImporterWindowMode windowMode = ImporterWindowMode.Build;

        const float ICON_SIZE = 64f;
        const float WINDOW_MARGIN = 4f;
        const float TOP_PADDING = 16f;
        const float ACTION_BUTTON_SIZE = 40f;
        const float ACTION_BUTTON_SPACE = 4f;
        const float BUTTON_HEIGHT = 40f;
        const float INFO_HEIGHT = 80f;
        const float OPTION_HEIGHT = 170f;
        const float ACTION_HEIGHT = 76f;
        const float ICON_WIDTH = 100f;
        const float ACTION_WIDTH = ACTION_BUTTON_SIZE + 12f;
        const float TITLE_SPACE = 12f;
        const float ROW_SPACE = 4f;

        private static GUIStyle logStyle, mainStyle, buttonStyle, labelStyle, boldStyle, iconStyle;
        private static Texture2D iconUnprocessed;
        private static Texture2D iconBasic;
        private static Texture2D iconHQ;
        private static Texture2D iconBaked;
        private static Texture2D iconMixed;
        private static Texture2D iconActionBake;
        private static Texture2D iconActionPreview;
        private static Texture2D iconActionRefresh;
        private static Texture2D iconActionAnims;
        private static Texture2D iconAction2Pass;
        private static Texture2D iconActionAnimPlayer;
        private static Texture2D iconActionAvatarAlign;
        private static Texture2D iconSettings;        

        // SerializeField is used to ensure the view state is written to the window 
        // layout file. This means that the state survives restarting Unity as long as the window
        // is not closed. If the attribute is omitted then the state is still serialized/deserialized.
        [SerializeField] TreeViewState treeViewState;

        //The TreeView is not serializable, so it should be reconstructed from the tree data.
        CharacterTreeView characterTreeView;        

        public static void StoreBackScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.IsValid() && !string.IsNullOrEmpty(currentScene.path))
            {                
                backScenePath = currentScene.path;
            }
        }

        public static void GoBackScene()
        {
            if (!string.IsNullOrEmpty(backScenePath) && File.Exists(backScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Scene backScene = EditorSceneManager.OpenScene(backScenePath);
                if (backScene.IsValid())
                    backScenePath = null;
            }
        }

        private void SetContextCharacter(UnityEngine.Object obj)
        {
            SetContextCharacter(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
        }

        private void SetContextCharacter(string guid)
        {
            CharacterInfo oldCharacter = contextCharacter;

            if (contextCharacter == null || contextCharacter.guid != guid)
            {
                if (contextCharacter != null) contextCharacter.Release();
                contextCharacter = GetCharacterState(guid);
                
                CreateTreeView(oldCharacter != contextCharacter);

                if (Pipeline.isHDRP && contextCharacter.BuiltDualMaterialHair) characterTreeView.EnableMultiPass();
                else characterTreeView.DisableMultiPass();

                EditorPrefs.SetString("RL_Importer_Context_Path", contextCharacter.path);
            }            
        }        

        public static ImporterWindow Init(Mode windowMode, UnityEngine.Object characterObject)
        {                        
            Type hwt = Type.GetType("UnityEditor.SceneHierarchyWindow, UnityEditor.dll");
            ImporterWindow window = GetWindow<ImporterWindow>(windowTitle, hwt);
            window.minSize = new Vector2(300f, 500f);
            currentWindow = window;

            ClearAllData();
            window.SetActiveCharacter(characterObject, windowMode);
            window.InitData();                                
            window.Show();            

            return window;
        }        

        public void SetActiveCharacter(UnityEngine.Object obj, Mode mode)
        {
            if (Util.IsCC3Character(obj))
            {
                EditorPrefs.SetString("RL_Importer_Context_Path", AssetDatabase.GetAssetPath(obj));
            }

            ImporterWindow.mode = mode;
            EditorPrefs.SetString("RL_Importer_Mode", mode.ToString());
        }

        private void InitData()
        {            
            string[] folders = new string[] { "Assets", "Packages" };
            iconUnprocessed = Util.FindTexture(folders, "RLIcon_UnprocessedChar");
            iconBasic = Util.FindTexture(folders, "RLIcon_BasicChar");
            iconHQ = Util.FindTexture(folders, "RLIcon_HQChar");
            iconBaked = Util.FindTexture(folders, "RLIcon_BakedChar");
            iconMixed = Util.FindTexture(folders, "RLIcon_MixedChar");
            iconActionBake = Util.FindTexture(folders, "RLIcon_ActionBake");
            iconActionPreview = Util.FindTexture(folders, "RLIcon_ActionPreview");
            iconActionRefresh = Util.FindTexture(folders, "RLIcon_ActionRefresh");
            iconAction2Pass = Util.FindTexture(folders, "RLIcon_Action2Pass");
            iconActionAnims = Util.FindTexture(folders, "RLIcon_ActionAnims");
            iconActionAnimPlayer = Util.FindTexture(folders, "RLIcon_AnimPlayer");
            iconActionAvatarAlign = Util.FindTexture(folders, "RLIcon_AvatarAlign");
            iconSettings = Util.FindTexture(folders, "RLIcon_Settings");
            currentWindow = this;

            RefreshCharacterList();

            MakeStyle();
        }

        private void RefreshCharacterList()
        {
            if (validCharacters == null)
                validCharacters = new List<CharacterInfo>();
            else
                validCharacters.Clear();

            if (mode == Mode.none)
            {
                mode = Mode.multi;
                string modeValue = EditorPrefs.GetString("RL_Importer_Mode");                
                if (!string.IsNullOrEmpty(modeValue))
                    mode = (Mode)Enum.Parse(typeof(Mode), modeValue);                    
            }

            if (mode == Mode.single)
            {
                string editorPrefsContextPath = EditorPrefs.GetString("RL_Importer_Context_Path");
                if (!string.IsNullOrEmpty(editorPrefsContextPath) &&
                    File.Exists(editorPrefsContextPath))
                {
                    string guid = AssetDatabase.AssetPathToGUID(editorPrefsContextPath);
                    if (!string.IsNullOrEmpty(guid))
                        validCharacters.Add(new CharacterInfo(guid));
                }

                // fallback to multi mode
                if (validCharacters.Count == 0) mode = Mode.multi;
            }

            if (mode == Mode.multi)
            {
                List<string> validCharacterGUIDs = Util.GetValidCharacterGUIDS();
                foreach (string validGUID in validCharacterGUIDs)
                {
                    validCharacters.Add(new CharacterInfo(validGUID));
                }
            }
        }

        private void RestoreData()
        {
            if (validCharacters == null)
            {
                InitData();                
            }
        }

        private void RestoreSelection()
        {
            if (contextCharacter == null && validCharacters.Count > 0)
            {
                string editorPrefsContextPath = EditorPrefs.GetString("RL_Importer_Context_Path");
                if (!string.IsNullOrEmpty(editorPrefsContextPath))
                {
                    for (int i = 0; i < validCharacters.Count; i++)
                    {
                        if (validCharacters[i].path == editorPrefsContextPath)
                            SetContextCharacter(validCharacters[i].guid);
                    }
                }

                if (Selection.activeGameObject)
                {
                    string selectionPath = AssetDatabase.GetAssetPath(Selection.activeGameObject);
                    for (int i = 0; i < validCharacters.Count; i++)
                    {
                        if (validCharacters[i].path == selectionPath)
                            SetContextCharacter(validCharacters[i].guid);
                    }               
                }

                if (contextCharacter == null)
                    SetContextCharacter(validCharacters[0].guid);                
            }            
        }

        private CharacterInfo GetCharacterState(string guid)
        {            
            foreach (CharacterInfo s in validCharacters)
            {
                if (s.guid.Equals(guid)) return s;
            }

            return null;
        }        

        private void CreateTreeView(bool clearSelection = false)
        {
            if (contextCharacter != null)
            {
                // Check whether there is already a serialized view state (state 
                // that survived assembly reloading)
                if (treeViewState == null)
                {
                    treeViewState = new TreeViewState();
                }
                characterTreeView = new CharacterTreeView(treeViewState, contextCharacter.Fbx);

                characterTreeView.ExpandToDepth(2);
                if (clearSelection) characterTreeView.ClearSelection();
            }
        }
        
        private void OnGUI()
        {
            RestoreData();
            RestoreSelection();

            if (validCharacters == null || validCharacters.Count == 0)
            {
                GUILayout.Label("No CC-iC/iClone Characters detected!");
                return;
            }            

            float width = position.width - WINDOW_MARGIN;
            float height = position.height - WINDOW_MARGIN;
            float innerHeight = height - TOP_PADDING;                 

            Rect iconBlock = new Rect(0f, TOP_PADDING, ICON_WIDTH, innerHeight);
            Rect infoBlock = new Rect(iconBlock.xMax, TOP_PADDING, width - ICON_WIDTH - ACTION_WIDTH, INFO_HEIGHT);
            Rect optionBlock = new Rect(iconBlock.xMax, infoBlock.yMax, infoBlock.width, OPTION_HEIGHT);
            Rect actionBlock = new Rect(iconBlock.xMax + infoBlock.width, TOP_PADDING, ACTION_WIDTH, innerHeight);            
            Rect treeviewBlock = new Rect(iconBlock.xMax, optionBlock.yMax, infoBlock.width, height - optionBlock.yMax);
            Rect settingsBlock = new Rect(iconBlock.xMax, TOP_PADDING, width - ICON_WIDTH - ACTION_WIDTH, innerHeight);

            previewCharacterAfterGUI = false;
            refreshAfterGUI = false;
            buildAfterGUI = false;
            bakeAfterGUI = false;

            CheckDragAndDrop();

            OnGUIIconArea(iconBlock);            

            if (windowMode == ImporterWindowMode.Build)
                OnGUIInfoArea(infoBlock);

            if (windowMode == ImporterWindowMode.Build)
                OnGUIOptionArea(optionBlock);

            if (windowMode == ImporterWindowMode.Settings)
                OnGUISettingsArea(settingsBlock);

            OnGUIActionArea(actionBlock);

            if (windowMode == ImporterWindowMode.Build)
                OnGUITreeViewArea(treeviewBlock);

            // functions to run after the GUI has finished...

            if (previewCharacterAfterGUI)
            {
                StoreBackScene();

                PreviewScene.OpenPreviewScene(contextCharacter.Fbx);

                if (WindowManager.showTools) ShowAnimationPlayer();
                ResetAllSceneViewCamera();
            }
            else if (refreshAfterGUI)
            {
                RefreshCharacterList();
            }
            else if (buildAfterGUI)
            {
                BuildCharacter();
            }
            else if (bakeAfterGUI)
            {
                BakeCharacter();
            }
        }

        bool doubleClick = false;

        private void OnGUIIconArea(Rect iconBlock)
        {            
            GUILayout.BeginArea(iconBlock);

            Event e = Event.current;
            if (e.isMouse && e.type == EventType.MouseDown)
            {
                if (e.clickCount == 2) doubleClick = true;
                else doubleClick = false;
            }

            using (var iconScrollViewScope = new EditorGUILayout.ScrollViewScope(iconScrollView, GUILayout.Width(iconBlock.width - 10f), GUILayout.Height(iconBlock.height - 10f)))
            {
                iconScrollView = iconScrollViewScope.scrollPosition;
                GUILayout.BeginVertical();

                for (int idx = 0; idx < validCharacters.Count; idx++)
                {
                    CharacterInfo info = validCharacters[idx];
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(7f);
                    Texture2D iconTexture = iconUnprocessed;

                    if (info.bakeIsBaked)
                    {
                        if (info.BuiltBasicMaterials) iconTexture = iconMixed;
                        else if (info.BuiltHQMaterials) iconTexture = iconBaked;
                    }
                    else
                    {
                        if (info.BuiltBasicMaterials) iconTexture = iconBasic;
                        else if (info.BuiltHQMaterials) iconTexture = iconHQ;
                    }

                    if (GUILayout.Button(iconTexture,                        
                        GUILayout.Width(ICON_SIZE),
                        GUILayout.Height(ICON_SIZE))) 
                    {                        
                        SetContextCharacter(info.guid);
                        if (doubleClick)
                        {
                            previewCharacterAfterGUI = true;
                        }
                    }
                    
                    GUILayout.FlexibleSpace();                    
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();                    
                    GUILayout.FlexibleSpace();
                    string name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(info.guid));
                    GUILayout.Box(name, iconStyle, GUILayout.Width(ICON_SIZE));
                    GUILayout.FlexibleSpace();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();            
        }
        
        private void OnGUIInfoArea(Rect infoBlock)
        {            
            string importType = "Unprocessed";
            if (contextCharacter.BuiltBasicMaterials)
                importType = "Default Materials";
            if (contextCharacter.BuiltHQMaterials)
                importType = "High Quality Materials";
            if (contextCharacter.bakeIsBaked)
                importType += " + Baked";

            GUILayout.BeginArea(infoBlock);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(contextCharacter.name, boldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(contextCharacter.folder, labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("(" + contextCharacter.Generation.ToString() + ")", boldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(importType, boldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();            

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();            
        }

        private void OnGUIOptionArea(Rect optionBlock)
        {            
            GUILayout.BeginArea(optionBlock);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();            

            if (EditorGUILayout.DropdownButton(
                content: new GUIContent(contextCharacter.BasicMaterials ? "Basic Materials" : "High Quality Materials"),
                focusType: FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Basic Materials"), contextCharacter.BasicMaterials, MaterialOptionSelected, true);
                if (contextCharacter.CanHaveHighQualityMaterials)
                    menu.AddItem(new GUIContent("High Quality Materials"), contextCharacter.HQMaterials, MaterialOptionSelected, false);
                menu.ShowAsContext();
            }

            GUILayout.Space(1f);

            if (contextCharacter.BasicMaterials) GUI.enabled = false;
            if (EditorGUILayout.DropdownButton(                
                content: new GUIContent(contextCharacter.QualEyes.ToString() + " Eyes"),
                focusType: FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Basic Eyes"), contextCharacter.BasicEyes, EyeOptionSelected, CharacterInfo.EyeQuality.Basic);
                menu.AddItem(new GUIContent("Parallax Eyes"), contextCharacter.ParallaxEyes, EyeOptionSelected, CharacterInfo.EyeQuality.Parallax);
                if (Pipeline.isHDRP)
                    menu.AddItem(new GUIContent("Refractive (SSR) Eyes"), contextCharacter.RefractiveEyes, EyeOptionSelected, CharacterInfo.EyeQuality.Refractive);
                menu.ShowAsContext();
            }

            GUILayout.Space(1f);
            string hairType;
            switch(contextCharacter.QualHair)
            {
                case CharacterInfo.HairQuality.TwoPass: hairType = "Two Pass Hair"; break;
                case CharacterInfo.HairQuality.Coverage: hairType = "MSAA Coverage Hair"; break;
                default:
                case CharacterInfo.HairQuality.Default: hairType = "Single Pass Hair"; break;
            }
            if (EditorGUILayout.DropdownButton(
                content: new GUIContent(hairType),
                focusType: FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Single Pass Hair"), contextCharacter.DefaultHair, HairOptionSelected, CharacterInfo.HairQuality.Default);
                menu.AddItem(new GUIContent("Two Pass Hair"), contextCharacter.DualMaterialHair, HairOptionSelected, CharacterInfo.HairQuality.TwoPass);
                if (Importer.USE_AMPLIFY_SHADER && !Pipeline.isHDRP)
                    menu.AddItem(new GUIContent("MSAA Coverage Hair"), contextCharacter.CoverageHair, HairOptionSelected, CharacterInfo.HairQuality.Coverage);
                menu.ShowAsContext();
            }
            GUI.enabled = true;

            GUILayout.Space(8f);

            if (contextCharacter.BuiltBasicMaterials) GUI.enabled = false;
            if (EditorGUILayout.DropdownButton(
                content: new GUIContent(contextCharacter.BakeCustomShaders ? "Bake Custom Shaders":"Bake Default Shaders"),
                focusType: FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Default Shaders"), !contextCharacter.BakeCustomShaders, BakeShadersOptionSelected, false);
                menu.AddItem(new GUIContent("Custom Shaders"), contextCharacter.BakeCustomShaders, BakeShadersOptionSelected, true);                
                menu.ShowAsContext();
            }

            GUILayout.Space(1f);

            if (EditorGUILayout.DropdownButton(
                new GUIContent(contextCharacter.BakeSeparatePrefab ? "Bake Separate Prefab" : "Bake Overwrite Prefab"),
                FocusType.Passive                
                ))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Overwrite Prefab"), !contextCharacter.BakeSeparatePrefab, BakePrefabOptionSelected, false);
                menu.AddItem(new GUIContent("Separate Baked Prefab"), contextCharacter.BakeSeparatePrefab, BakePrefabOptionSelected, true);
                menu.ShowAsContext();
            }
            GUI.enabled = true;

            GUILayout.Space(8f);

            //
            // BUILD BUTTON
            //
            GUIContent buildContent;
            if (contextCharacter.BasicMaterials)
                buildContent = new GUIContent("Build Materials", "Setup materials to use the default shaders.");
            else
                buildContent = new GUIContent("Build Materials", "Setup materials to use the high quality shaders.");

            if (GUILayout.Button(buildContent,                
                GUILayout.Height(BUTTON_HEIGHT)))
            {
                buildAfterGUI = true;
            }
            
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();            
        }
        
        private void OnGUIActionArea(Rect actionBlock)
        {            
            GUILayout.BeginArea(actionBlock);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            if (false && !string.IsNullOrEmpty(backScenePath) && File.Exists(backScenePath))
            {               
                if (GUILayout.Button(new GUIContent("<", "Go back to the last valid scene."), 
                    GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
                {
                    GoBackScene();
                }

                GUILayout.Space(ACTION_BUTTON_SPACE);
            }                        

            if (GUILayout.Button(new GUIContent(iconActionPreview, "View the current character in a preview scene."), 
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                previewCharacterAfterGUI = true;
            }

            GUILayout.Space(ACTION_BUTTON_SPACE);

            if (mode == Mode.multi)
            {
                if (GUILayout.Button(new GUIContent(iconActionRefresh, "Reload the character list, for after adding or removing characters."), 
                    GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
                {
                    refreshAfterGUI = true;
                }

                GUILayout.Space(ACTION_BUTTON_SPACE);
            }

            GUILayout.Space(ACTION_BUTTON_SPACE + 11f);

            if (contextCharacter.BuiltBasicMaterials) GUI.enabled = false;
            if (GUILayout.Button(new GUIContent(iconActionBake, "Bake high quality materials down to compatible textures for the default shaders. i.e. HDRP/Lit, URP/Lut or Standard shader."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                bakeAfterGUI = true;
            }
            GUI.enabled = true;

            GUILayout.Space(ACTION_BUTTON_SPACE);

            if (contextCharacter.Unprocessed) GUI.enabled = false;
            if (GUILayout.Button(new GUIContent(iconActionAnims, "Process character animations and create a default animtor controller."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                RL.SetAnimationImport(contextCharacter, contextCharacter.Fbx);
            }
            GUI.enabled = true;
            
            /*
            GUILayout.Space(ACTION_BUTTON_SPACE);

            if (!contextCharacter.BuiltHQMaterials || contextCharacter.BuiltDualMaterialHair) GUI.enabled = false;
            if (GUILayout.Button(new GUIContent(iconAction2Pass, "Convert hair meshes to use two material passes. Two pass hair is generally higher quality, where the hair is first drawn opaque with alpha cutout and the remaing edges drawn in softer alpha blending, but can come at a performance cost."), 
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                contextCharacter.DualMaterialHair = true;
                MeshUtil.Extract2PassHairMeshes(Util.FindCharacterPrefabAsset(contextCharacter.Fbx));
                contextCharacter.Write();

                ShowPreviewCharacter();

                TrySetMultiPass(true);
            }
            GUI.enabled = true;
            */

            GUILayout.Space(ACTION_BUTTON_SPACE * 2f + 11f);

            if (contextCharacter == null) GUI.enabled = false;
            if (GUILayout.Button(new GUIContent(iconActionAnimPlayer, "Show animation preview player."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                if (WindowManager.showTools)
                {
                    /*
                    AnimationClip firstClip = Util.GetFirstAnimationClipFromCharacter(contextCharacter.Fbx);
                    if (AnimPlayerIMGUI.animationClip != firstClip)
                    {
                        AnimPlayerIMGUI.UpdatePlayerClip(firstClip);
                    }
                    else
                    {
                        showAnimPlayer = false;
                        HideAnimationPlayer();
                        ResetAllSceneViewCamera();
                    }*/
                    WindowManager.showTools = false;
                    HideAnimationPlayer();
                    ResetAllSceneViewCamera();
                }
                else
                {
                    WindowManager.showTools = true;
                    ShowAnimationPlayer();
                    ResetAllSceneViewCamera();
                }
            }
            GUI.enabled = true;

            /* Not just yet...
            GUILayout.Space(ACTION_BUTTON_SPACE);

            if (contextCharacter == null) GUI.enabled = false;
            if (GUILayout.Button(new GUIContent(iconActionAvatarAlign, "Avatar Alignment."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                
            }
            GUI.enabled = true;
            */

            GUILayout.FlexibleSpace();

            GUIContent settingsIconGC;
            if (windowMode != ImporterWindowMode.Settings)
                settingsIconGC = new GUIContent(iconSettings, "Settings.");
            else
                settingsIconGC = EditorGUIUtility.IconContent("back@2x");
            if (GUILayout.Button(settingsIconGC, 
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                if (windowMode != ImporterWindowMode.Settings)
                    windowMode = ImporterWindowMode.Settings;
                else
                    windowMode = ImporterWindowMode.Build;
            }

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();            
        }
        
        private void OnGUITreeViewArea(Rect treeviewBlock)
        {            
            GUILayout.BeginArea(treeviewBlock);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (contextCharacter != null)
            {
                characterTreeView.OnGUI(new Rect(0, 0, treeviewBlock.width, treeviewBlock.height - 16f));                
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            characterTreeView.selectLinked = GUILayout.Toggle(characterTreeView.selectLinked, "Select Linked");
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndArea();            
        }




        private void OnGUISettingsArea(Rect settingsBlock)
        {
            GUILayout.BeginArea(settingsBlock);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();            
            GUILayout.FlexibleSpace();
            GUILayout.Label("Settings", boldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(TITLE_SPACE);

            if (!Pipeline.isHDRP)
            {
                Importer.USE_AMPLIFY_SHADER = GUILayout.Toggle(Importer.USE_AMPLIFY_SHADER,
                    new GUIContent("Use Amplify Shaders", "Use the more advanced Amplify shaders where possible. " +
                    "Amplify shaders are capable of subsurface scattering effects, and anisotropic hair lighting in the URP and Build-in 3D pipelines."));
                GUILayout.Space(ROW_SPACE);
            }

            Importer.RECONSTRUCT_FLOW_NORMALS = GUILayout.Toggle(Importer.RECONSTRUCT_FLOW_NORMALS,
                new GUIContent("Reconstruct Flow Map Normals", "Rebuild missing Normal maps from Flow Maps in hair materials. " +
                "Reconstructed Normals add extra detail to the lighting models."));
            GUILayout.Space(ROW_SPACE);

            Importer.REBAKE_BLENDER_UNITY_MAPS = GUILayout.Toggle(Importer.REBAKE_BLENDER_UNITY_MAPS,
                new GUIContent("Rebake Blender Unity Maps", "Always re-bake the blender to unity Diffuse+Alpha, HDRP Mask and Metallic+Gloss maps. " +
                "Otherwise subsequent material rebuilds will try to re-use existing bakes. Only needed if the source textures are changed."));
            GUILayout.Space(ROW_SPACE);

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }




        private void EyeOptionSelected(object sel)
        {            
            contextCharacter.QualEyes = (CharacterInfo.EyeQuality)sel;
        }

        private void HairOptionSelected(object sel)
        {
            contextCharacter.QualHair = (CharacterInfo.HairQuality)sel;
        }

        private void MaterialOptionSelected(object sel)
        {
            if ((bool)sel)
                contextCharacter.BuildQuality = MaterialQuality.Default;
            else
                contextCharacter.BuildQuality = MaterialQuality.High;
        }

        private void BakeShadersOptionSelected(object sel)
        {
            contextCharacter.BakeCustomShaders = (bool)sel;
        }

        private void BakePrefabOptionSelected(object sel)
        {
            contextCharacter.BakeSeparatePrefab = (bool)sel;
        }

        public static void TrySetMultiPass(bool state)
        {
            ImporterWindow window = ImporterWindow.currentWindow;

            if (window && window.characterTreeView != null)
            {
                if (Pipeline.isHDRP && contextCharacter.BuiltDualMaterialHair)
                {
                    if (state)
                        window.characterTreeView.EnableMultiPass();
                    else
                        window.characterTreeView.DisableMultiPass();
                    return;
                }

                window.characterTreeView.DisableMultiPass();
            }                       
        }


        private GameObject ImportCharacter(CharacterInfo info)
        {
            Importer import = new Importer(info);            
            return import.Import();
        }
        
        private static void ClearAllData()
        {
            if (contextCharacter != null) contextCharacter.Release();
            contextCharacter = null;            

            if (validCharacters != null) validCharacters.Clear();
            validCharacters = null;
            
            logStyle = null;
            mainStyle = null;
            buttonStyle = null;
            labelStyle = null;
            boldStyle = null;

            iconUnprocessed = null;
            iconBasic = null;
            iconHQ = null;
            iconBaked = null;

            currentWindow = null;
        }

        private void OnDestroy()
        {            
            ClearAllData();            
        }        

        private static void MakeStyle()
        {
            logStyle = new GUIStyle();
            logStyle.wordWrap = true;
            logStyle.fontStyle = FontStyle.Italic;
            logStyle.normal.textColor = Color.grey;

            mainStyle = new GUIStyle();
            mainStyle.wordWrap = false;
            mainStyle.fontStyle = FontStyle.Normal;
            mainStyle.normal.textColor = Color.white;

            iconStyle = new GUIStyle();
            iconStyle.wordWrap = false;
            iconStyle.fontStyle = FontStyle.Normal;
            iconStyle.normal.textColor = Color.white;
            iconStyle.alignment = TextAnchor.MiddleCenter;

            boldStyle = new GUIStyle();
            boldStyle.alignment = TextAnchor.UpperLeft;
            boldStyle.wordWrap = false;
            boldStyle.fontStyle = FontStyle.Bold;
            boldStyle.normal.textColor = Color.white;

            labelStyle = new GUIStyle();
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.wordWrap = false;
            labelStyle.fontStyle = FontStyle.Normal;
            labelStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle();
            buttonStyle.wordWrap = false;
            buttonStyle.fontStyle = FontStyle.Normal;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
        }        

        public void CheckDragAndDrop()
        {
            switch (Event.current.type)
            {               
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;                    
                    break;
                    
                case EventType.DragPerform:

                    UnityEngine.Object[] refs = DragAndDrop.objectReferences;
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        UnityEngine.Object obj = DragAndDrop.objectReferences[0];
                        if (Util.IsCC3Character(obj))
                            SetContextCharacter(obj);                        
                    }
                    DragAndDrop.AcceptDrag();                    
                    break;                    
            }
        }      
        
        bool ShowPreviewCharacter()
        {
            GameObject fbx = contextCharacter.Fbx;

            PreviewScene ps = PreviewScene.GetPreviewScene();            

            if (ps.IsValid)
            {
                bool animationMode = AnimationMode.InAnimationMode();
                if (animationMode) AnimationMode.StopAnimationMode();

                ps.ShowPreviewCharacter(fbx);

                if (animationMode) AnimationMode.StartAnimationMode();
            }            

            return ps.IsValid;
        }

        bool UpdatePreviewCharacter(GameObject prefabAsset)
        {
            PreviewScene ps = PreviewScene.GetPreviewScene();            

            if (ps.IsValid)
            {
                bool animationMode = AnimationMode.InAnimationMode();
                if (animationMode) AnimationMode.StopAnimationMode();

                ps.UpdatePreviewCharacter(prefabAsset);

                if (animationMode) AnimationMode.StartAnimationMode();
            }            

            return ps.IsValid;
        }

        private void BuildCharacter()
        {
            Util.LogInfo("Doing: Building materials...");

            // refresh the character info for any Json changes
            contextCharacter.Refresh();

            // default to high quality if never set before
            if (contextCharacter.BuildQuality == MaterialQuality.None)
                contextCharacter.BuildQuality = MaterialQuality.High;

            // import and build the materials from the Json data
            GameObject prefabAsset = ImportCharacter(contextCharacter);
            contextCharacter.Write();

            // refresh the tree view with the new data
            CreateTreeView(true);

            // enable / disable multipass material selection (HDRP only)
            if (Pipeline.isHDRP && contextCharacter.HQMaterials && contextCharacter.BuiltDualMaterialHair) characterTreeView.EnableMultiPass();
            else characterTreeView.DisableMultiPass();

            // update the character in the preview scene with the new prefab asset
            if (prefabAsset)
            {
                if (UpdatePreviewCharacter(prefabAsset))
                {
                    if (WindowManager.showTools)
                    {
                        ShowAnimationPlayer();
                    }
                }
            }

            Repaint();            
        }

        private void BakeCharacter()
        {
            if (contextCharacter.HQMaterials)
            {
                ComputeBake baker = new ComputeBake(contextCharacter.Fbx, contextCharacter);
                GameObject bakedAsset = baker.BakeHQ();

                contextCharacter.bakeIsBaked = true;
                contextCharacter.Write();

                if (bakedAsset)
                {
                    ShowBakedCharacter(bakedAsset);
                }
            }
        }

        bool ShowBakedCharacter(GameObject bakedAsset)
        {
            PreviewScene ps = PreviewScene.GetPreviewScene();            

            if (ps.IsValid)
            {
                bool animationMode = AnimationMode.InAnimationMode();
                if (animationMode) AnimationMode.StopAnimationMode();

                ps.ShowBakedCharacter(bakedAsset);

                if (animationMode) AnimationMode.StartAnimationMode();
            }            

            return ps.IsValid;
        }

        public void HideAnimationPlayer()
        {            
            if (AnimPlayerGUI.IsPlayerShown())
            {
                AnimPlayerGUI.ResetFace();
                AnimPlayerGUI.DestroyPlayer();
            }

            WindowManager.showTools = false;
        }

        public void ShowAnimationPlayer() 
        {            
            PreviewScene ps = PreviewScene.GetPreviewScene();

            if (ps.IsValid)
            {
                if (AnimPlayerGUI.IsPlayerShown())
                {
                    AnimPlayerGUI.SetCharacter(ps, contextCharacter.Fbx);
                }
                else
                {
                    AnimPlayerGUI.CreatePlayer(ps, contextCharacter.Fbx);
                }

                WindowManager.showTools = true;
            }
        }

        public static void ResetAllSceneViewCamera()
        {
            PreviewScene ps = PreviewScene.GetPreviewScene();

            if (ps.IsValid)
            {
                GameObject obj = ps.GetPreviewCharacter();
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);

                if (root)
                {
                    GameObject hips = MeshUtil.FindCharacterBone(root, "CC_Base_Spine02");
                    if (!hips) hips = MeshUtil.FindCharacterBone(root, "Spine02");
                    GameObject head = MeshUtil.FindCharacterBone(root, "CC_Base_Head");
                    if (!head) head = MeshUtil.FindCharacterBone(root, "head");
                    if (hips && head)
                    {
                        Vector3 lookAt = (hips.transform.position + head.transform.position) / 2f;
                        Quaternion lookBackRot = new Quaternion();
                        Vector3 euler = lookBackRot.eulerAngles;
                        euler.y = -180f;
                        lookBackRot.eulerAngles = euler;                        

                        foreach (SceneView sv in SceneView.sceneViews)
                        {
                            sv.LookAt(lookAt, lookBackRot, 0.5f);
                        }
                    }
                }
            }
        }
    }
}
