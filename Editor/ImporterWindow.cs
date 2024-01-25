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

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace Reallusion.Import
{
    [System.Serializable]
    public class ImporterWindow : EditorWindow
    {
        
        [SerializeField]
        private static bool sceneFocus = false;

        public static bool isSceneFocus { get { return sceneFocus; } }
        public static void SetSceneFocus(bool val)
        {
            sceneFocus = val;
        }

        public enum Mode { none, single, multi }

        private static readonly string windowTitle = "CC/iC Importer " + Pipeline.FULL_VERSION;
        private static CharacterInfo contextCharacter;
        private static List<CharacterInfo> validCharacters;
        private static string backScenePath;
        private static Mode mode;        
        public static ImporterWindow Current { get; private set; }        
        public CharacterInfo Character { get { return contextCharacter; } }
        public static List<CharacterInfo> ValidCharacters => validCharacters;

        private Vector2 iconScrollView;
        private bool previewCharacterAfterGUI;
        private bool refreshAfterGUI;
        private bool buildAfterGUI;
        private bool bakeAfterGUI;
        private bool bakeHairAfterGUI;
        private bool processAnimationsAfterGUI;
        private bool restoreHairAfterGUI;
        private bool physicsAfterGUI;
        public enum ImporterWindowMode { Build, Bake, Settings }
        private ImporterWindowMode windowMode = ImporterWindowMode.Build;

        const float ICON_SIZE = 64f;
        const float WINDOW_MARGIN = 4f;
        const float TOP_PADDING = 16f;
        const float ACTION_BUTTON_SIZE = 40f;
        const float WEE_BUTTON_SIZE = 32f;
        const float ACTION_BUTTON_SPACE = 4f;
        const float BUTTON_HEIGHT = 40f;
        const float INFO_HEIGHT = 80f;
        const float OPTION_HEIGHT = 170f;
        const float ACTION_HEIGHT = 76f;
        const float ICON_WIDTH = 100f; // re-purposed below for draggable width icon area
        const float ACTION_WIDTH = ACTION_BUTTON_SIZE + 12f;
        const float TITLE_SPACE = 12f;
        const float ROW_SPACE = 4f;
        const float MIN_SETTING_WIDTH = ACTION_WIDTH;

        // additions for draggable width icon area
        const float DRAG_BAR_WIDTH = 2f;
        const float DRAG_HANDLE_PADDING = 4f;        
        const float ICON_WIDTH_MIN = 100f;
        const float ICON_WIDTH_DETAIL = 140f;
        const float ICON_SIZE_SMALL = 25f;
        const float ICON_DETAIL_MARGIN = 2f;
        private float CURRENT_INFO_WIDTH = 0f;
        const float INFO_WIDTH_MIN = 0f;
        private bool dragging = false;
        private bool repaintDelegated = false;

        private Styles importerStyles;        
        //GUIStyle dragBarStyle;
        //GUIStyle nameTextStyle;
        //GUIStyle fakeButton;
        //GUIStyle fakeButtonContext;

        //private GUIStyle logStyle, mainStyle, buttonStyle, labelStyle, boldStyle, iconStyle;
        private Texture2D iconUnprocessed;
        private Texture2D iconBasic;
        private Texture2D iconHQ;
        private Texture2D iconBaked;
        private Texture2D iconMixed;
        private Texture2D iconActionBake;
        private Texture2D iconActionBakeOn;
        private Texture2D iconActionBakeHair;
        private Texture2D iconActionBakeHairOn;
        private Texture2D iconActionPreview;
        private Texture2D iconActionPreviewOn;
        private Texture2D iconActionRefresh;
        private Texture2D iconActionAnims;
        private Texture2D iconActionPhysics;
        private Texture2D iconActionLOD;
        private Texture2D iconAction2Pass;
        private Texture2D iconAlembic;
        private Texture2D iconActionAnimPlayer;
        private Texture2D iconActionAnimPlayerOn;
        private Texture2D iconActionAvatarAlign;
        private Texture2D iconActionAvatarAlignOn;
        private Texture2D iconSettings;
        private Texture2D iconSettingsOn;
        private Texture2D iconLighting;
        private Texture2D iconCamera;
        private Texture2D iconBuildMaterials;        

        // SerializeField is used to ensure the view state is written to the window 
        // layout file. This means that the state survives restarting Unity as long as the window
        // is not closed. If the attribute is omitted then the state is still serialized/deserialized.
        [SerializeField] TreeViewState treeViewState;

        //The TreeView is not serializable, so it should be reconstructed from the tree data.
        CharacterTreeView characterTreeView;

        private bool magicaCloth2Available;
        public bool MagicaCloth2Available { get { return magicaCloth2Available; } }

        private bool dynamicBoneAvailable;
        public bool DynamicBoneAvailable { get { return dynamicBoneAvailable; } }

        public static float ICON_AREA_WIDTH
        {
            get
            {                
                if (EditorPrefs.HasKey("RL_Importer_IconAreaWidth"))
                    return EditorPrefs.GetFloat("RL_Importer_IconAreaWidth");
                return ICON_WIDTH;
            }

            set
            {
                EditorPrefs.SetFloat("RL_Importer_IconAreaWidth", value);
            }
        }

        public static bool SELECT_LINKED
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Importer_SelectLinked"))
                    return EditorPrefs.GetBool("RL_Importer_SelectLinked");
                return true;
            }

            set
            {
                EditorPrefs.SetBool("RL_Importer_SelectLinked", value);
            }
        }

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
                contextCharacter.CheckGeneration();
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
            window.minSize = new Vector2(ACTION_WIDTH + ICON_WIDTH + MIN_SETTING_WIDTH + WINDOW_MARGIN, 500f);
            Current = window;

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
            CheckAvailableAddons();

            string[] folders = new string[] { "Assets", "Packages" };
            iconUnprocessed = Util.FindTexture(folders, "RLIcon_UnprocessedChar");
            iconBasic = Util.FindTexture(folders, "RLIcon_BasicChar");
            iconHQ = Util.FindTexture(folders, "RLIcon_HQChar");
            iconBaked = Util.FindTexture(folders, "RLIcon_BakedChar");
            iconMixed = Util.FindTexture(folders, "RLIcon_MixedChar");
            iconActionBake = Util.FindTexture(folders, "RLIcon_ActionBake");
            iconActionBakeOn = Util.FindTexture(folders, "RLIcon_ActionBake_Sel");
            iconActionBakeHair = Util.FindTexture(folders, "RLIcon_ActionBakeHair");
            iconActionBakeHairOn = Util.FindTexture(folders, "RLIcon_ActionBakeHair_Sel");
            iconActionPreview = Util.FindTexture(folders, "RLIcon_ActionPreview");
            iconActionPreviewOn = Util.FindTexture(folders, "RLIcon_ActionPreview_Sel");
            iconActionRefresh = Util.FindTexture(folders, "RLIcon_ActionRefresh");
            iconAction2Pass = Util.FindTexture(folders, "RLIcon_Action2Pass");
            iconAlembic = Util.FindTexture(folders, "RLIcon_Alembic");
            iconActionAnims = Util.FindTexture(folders, "RLIcon_ActionAnims");
            iconActionPhysics = Util.FindTexture(folders, "RLIcon_ActionPhysics");
            iconActionLOD = Util.FindTexture(folders, "RLIcon_ActionLOD");
            iconActionAnimPlayer = Util.FindTexture(folders, "RLIcon_AnimPlayer");
            iconActionAvatarAlign = Util.FindTexture(folders, "RLIcon_AvatarAlign");
            iconActionAnimPlayerOn = Util.FindTexture(folders, "RLIcon_AnimPlayer_Sel");
            iconActionAvatarAlignOn = Util.FindTexture(folders, "RLIcon_AvatarAlign_Sel");
            iconSettings = Util.FindTexture(folders, "RLIcon_Settings");
            iconSettingsOn = Util.FindTexture(folders, "RLIcon_Settings_Sel");
            iconLighting = Util.FindTexture(folders, "RLIcon_Lighting");
            iconCamera = Util.FindTexture(folders, "RLIcon_Camera");
            iconBuildMaterials = Util.FindTexture(folders, "RLIcon_ActionBuildMaterials");
            Current = this;

            RefreshCharacterList();
            
            if (titleContent.text != windowTitle) titleContent.text = windowTitle;
        }        

        private void PreviewCharacter()
        {
            StoreBackScene();

            PreviewScene ps = WindowManager.OpenPreviewScene(contextCharacter.Fbx);

            if (WindowManager.showPlayer)
                WindowManager.ShowAnimationPlayer();

            ResetAllSceneViewCamera();

            // lighting doesn't update correctly when first previewing a scene in HDRP
            EditorApplication.delayCall += ForceUpdateLighting;
        }

        public void RefreshCharacterList()
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
            if (importerStyles == null) importerStyles = new Styles();

            RestoreData();
            RestoreSelection();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            if (validCharacters == null || validCharacters.Count == 0)            
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No CC/iClone Characters detected!");                                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(20f);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(iconActionRefresh, "Reload the character list, for after adding or removing characters."),
                    GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
                {
                    EditorApplication.delayCall += RefreshCharacterList;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();                
                return;
            }
            EditorGUI.EndDisabledGroup();

            float width = position.width - WINDOW_MARGIN;
            float height = position.height - WINDOW_MARGIN;
            float innerHeight = height - TOP_PADDING;
            float optionHeight = OPTION_HEIGHT;
            //if (Pipeline.isHDRP12) optionHeight += 14f;
            if (contextCharacter.Generation == BaseGeneration.Unknown) optionHeight += 14f;
            optionHeight += 14f;
            
            if (width - ICON_AREA_WIDTH - ACTION_WIDTH < MIN_SETTING_WIDTH)
            {
                ICON_AREA_WIDTH = Mathf.Max(ICON_WIDTH, width - ACTION_WIDTH - MIN_SETTING_WIDTH);
            }            

            if (ICON_AREA_WIDTH > width - 51f) ICON_AREA_WIDTH = Mathf.Max(ICON_WIDTH, width - 51f);

            Rect iconBlock = new Rect(0f, TOP_PADDING, ICON_AREA_WIDTH, innerHeight);

            // additions for draggable width icon area
            Rect dragBar = new Rect(iconBlock.xMax, TOP_PADDING, DRAG_BAR_WIDTH, innerHeight);

            Rect infoBlock = new Rect(dragBar.xMax, TOP_PADDING, width - ICON_AREA_WIDTH - ACTION_WIDTH, INFO_HEIGHT);
            CURRENT_INFO_WIDTH = infoBlock.width;
            
            Rect optionBlock = new Rect(dragBar.xMax, infoBlock.yMax, infoBlock.width, optionHeight);
            Rect actionBlock = new Rect(dragBar.xMax + infoBlock.width, TOP_PADDING, ACTION_WIDTH, innerHeight);            
            Rect treeviewBlock = new Rect(dragBar.xMax, optionBlock.yMax, infoBlock.width, height - optionBlock.yMax);
            Rect settingsBlock = new Rect(dragBar.xMax, TOP_PADDING, width - ICON_AREA_WIDTH - ACTION_WIDTH, innerHeight);

            previewCharacterAfterGUI = false;
            refreshAfterGUI = false;
            buildAfterGUI = false;
            bakeAfterGUI = false;
            bakeHairAfterGUI = false;
            restoreHairAfterGUI = false;
            physicsAfterGUI = false;
            processAnimationsAfterGUI = false;

            CheckDragAndDrop();

            //OnGUIIconArea(iconBlock);
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            OnGUIFlexibleIconArea(iconBlock);
            OnGUIDragBarArea(dragBar);
            EditorGUI.EndDisabledGroup();

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
                EditorApplication.delayCall += PreviewCharacter;
            }
            else if (refreshAfterGUI)
            {
                EditorApplication.delayCall += RefreshCharacterList;
            }
            else if (buildAfterGUI)
            {
                EditorApplication.delayCall += BuildCharacter;
            }
            else if (bakeAfterGUI)
            {
                EditorApplication.delayCall += BakeCharacter;
            }
            else if (bakeHairAfterGUI)
            {
                EditorApplication.delayCall += BakeCharacterHair;
            }
            else if (restoreHairAfterGUI)
            {
                EditorApplication.delayCall += RestoreCharacterHair;
            }
            else if (physicsAfterGUI)
            {
                EditorApplication.delayCall += RebuildCharacterPhysics;
            }
            else if (processAnimationsAfterGUI)
            {
                EditorApplication.delayCall += ProcessAnimations;
            }
        }

        bool doubleClick = false;
                
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
            GUILayout.Label(contextCharacter.name, importerStyles.boldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(contextCharacter.folder, importerStyles.labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("(" + contextCharacter.Generation.ToString() + "/"
                                + contextCharacter.FaceProfile.expressionProfile + "/"
                                + contextCharacter.FaceProfile.visemeProfile
                            + ")", importerStyles.boldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(importType, importerStyles.boldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();            

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();            
        }
        Rect prev = new Rect();
        private void OnGUIOptionArea(Rect optionBlock)
        {            
            GUILayout.BeginArea(optionBlock);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            if (contextCharacter.Generation == BaseGeneration.Unknown)
            {                
                if (EditorGUILayout.DropdownButton(
                    content: new GUIContent("Rig Type: " + contextCharacter.UnknownRigType.ToString()),
                    focusType: FocusType.Passive))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Rig Type: None"), contextCharacter.UnknownRigType == CharacterInfo.RigOverride.None, RigOptionSelected, CharacterInfo.RigOverride.None);
                    menu.AddItem(new GUIContent("Rig Type: Humanoid"), contextCharacter.UnknownRigType == CharacterInfo.RigOverride.Humanoid, RigOptionSelected, CharacterInfo.RigOverride.Humanoid);                    
                    menu.AddItem(new GUIContent("Rig Type: Generic"), contextCharacter.UnknownRigType == CharacterInfo.RigOverride.Generic, RigOptionSelected, CharacterInfo.RigOverride.Generic);
                    menu.ShowAsContext();
                }

                GUILayout.Space(1f);
            }

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
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(1f);

            //if (contextCharacter.BasicMaterials) GUI.enabled = false;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || contextCharacter.BasicMaterials);
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

            // /*
            bool showDebugEnumPopup = false;
            if (showDebugEnumPopup)
            {
                int features = 2;
                if (Pipeline.isHDRP12) features++; // tessellation
                if (Pipeline.is3D || Pipeline.isURP) features++; // Amplify

                if (features == 1)
                {
                    contextCharacter.ShaderFlags = (CharacterInfo.ShaderFeatureFlags)EditorGUILayout.EnumPopup(contextCharacter.ShaderFlags);
                }
                else if (features > 1)
                {
                    EditorGUI.BeginChangeCheck();
                    contextCharacter.ShaderFlags = (CharacterInfo.ShaderFeatureFlags)EditorGUILayout.EnumFlagsField(contextCharacter.ShaderFlags);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if ((contextCharacter.ShaderFlags & CharacterInfo.ShaderFeatureFlags.SpringBoneHair) > 0 &&
                            (contextCharacter.ShaderFlags & CharacterInfo.ShaderFeatureFlags.HairPhysics) > 0)
                        {
                            contextCharacter.ShaderFlags -= CharacterInfo.ShaderFeatureFlags.SpringBoneHair;
                        }
                    }
                }
            }
            // */
            EditorGUI.EndDisabledGroup();
            //GUI.enabled = true;

            //////////////
            
            if (Event.current.type == EventType.Repaint)
                prev = GUILayoutUtility.GetLastRect();

            if (EditorGUILayout.DropdownButton(
                content: new GUIContent("Features"),
                focusType: FocusType.Passive))
            {                
               ImporterFeaturesWindow.ShowAtPosition(new Rect(prev.x, prev.y + 20f, prev.width, prev.height));
            }
            //////////////

            GUILayout.Space(8f);

            //if (contextCharacter.BuiltBasicMaterials) GUI.enabled = false;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || contextCharacter.BuiltBasicMaterials);
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
            EditorGUI.EndDisabledGroup();
            //GUI.enabled = true;

            GUILayout.Space(8f);

            //
            // BUILD BUTTON
            //
            GUIContent buildContent;
            if (contextCharacter.BasicMaterials)
                buildContent = new GUIContent("Build Materials", iconBuildMaterials, "Setup materials to use the default shaders.");
            else
                buildContent = new GUIContent("Build Materials", iconBuildMaterials, "Setup materials to use the high quality shaders.");

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            if (GUILayout.Button(buildContent,                
                GUILayout.Height(BUTTON_HEIGHT), GUILayout.Width(160f)))
            {
                buildAfterGUI = true;
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            GUILayout.EndArea();            
        }
        
        private void OnGUIActionArea(Rect actionBlock)
        {            
            GUILayout.BeginArea(actionBlock);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            if (false && !string.IsNullOrEmpty(backScenePath) && File.Exists(backScenePath))
            {               
                if (GUILayout.Button(new GUIContent("<", "Go back to the last valid scene."), 
                    GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
                {
                    GoBackScene();
                }

                GUILayout.Space(ACTION_BUTTON_SPACE);
            }                        


            if (GUILayout.Button(new GUIContent(WindowManager.IsPreviewScene ? iconActionPreviewOn : iconActionPreview, "View the current character in a preview scene."), 
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
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(ACTION_BUTTON_SPACE + 11f);

            //if (contextCharacter.BuiltBasicMaterials) GUI.enabled = false;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || contextCharacter.BuiltBasicMaterials);
            if (GUILayout.Button(new GUIContent(contextCharacter.bakeIsBaked ? iconActionBakeOn : iconActionBake, "Bake high quality materials down to compatible textures for the default shaders. i.e. HDRP/Lit, URP/Lut or Standard shader."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                bakeAfterGUI = true;
            }
            EditorGUI.EndDisabledGroup();
            //GUI.enabled = true;

            GUILayout.Space(ACTION_BUTTON_SPACE);

            
            if (contextCharacter.tempHairBake)
            {
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                if (GUILayout.Button(new GUIContent(iconActionBakeHairOn, "Restore original hair diffuse textures."),
                    GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
                {
                    restoreHairAfterGUI = true;
                }
                EditorGUI.EndDisabledGroup();
            }
            else //if (!contextCharacter.BuiltBasicMaterials && contextCharacter.HasColorEnabledHair())
            {
                //if (contextCharacter.BuiltBasicMaterials || !contextCharacter.HasColorEnabledHair()) GUI.enabled = false;
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || contextCharacter.BuiltBasicMaterials || !contextCharacter.HasColorEnabledHair());
                if (GUILayout.Button(new GUIContent(iconActionBakeHair, "Bake hair diffuse textures, to preview the baked results of the 'Enable Color' in the hair materials."),
                    GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
                {
                    bakeHairAfterGUI = true;
                }
                EditorGUI.EndDisabledGroup();
            }
            //GUI.enabled = true;

            GUILayout.Space(ACTION_BUTTON_SPACE);

            //if (contextCharacter.Unprocessed) GUI.enabled = false;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || contextCharacter.Unprocessed);
            if (GUILayout.Button(new GUIContent(iconActionAnims, "Process, extract and rename character animations and create a default animtor controller."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                processAnimationsAfterGUI = true;                
            }
            EditorGUI.EndDisabledGroup();
            //

            GUILayout.Space(ACTION_BUTTON_SPACE);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            if (GUILayout.Button(new GUIContent(iconActionPhysics, "Rebuilds the character physics."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                physicsAfterGUI = true;
            }
            EditorGUI.EndDisabledGroup();
            //GUI.enabled = true;

#if UNITY_ALEMBIC_1_0_7
            GUILayout.Space(ACTION_BUTTON_SPACE);
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            if (GUILayout.Button(new GUIContent(iconAlembic, "Process alembic animations with this character's materials."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                Alembic.ProcessAlembics(contextCharacter.Fbx, contextCharacter.name, contextCharacter.folder);
            }
            EditorGUI.EndDisabledGroup();
            //GUI.enabled = true;
#endif

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

            GUILayout.Space(ACTION_BUTTON_SPACE);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            if (GUILayout.Button(new GUIContent(iconActionLOD, "Run the LOD combining tool on the prefabs associated with this character."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                string prefabsFolder = contextCharacter.GetPrefabsFolder();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(prefabsFolder, typeof(Object)) as Object;
                LodSelectionWindow.InitTool();
            }
            EditorGUI.EndDisabledGroup();
            //GUI.enabled = true;

            GUILayout.Space(ACTION_BUTTON_SPACE * 2f + 11f);

            //if (contextCharacter == null) GUI.enabled = false;
            EditorGUI.BeginDisabledGroup(contextCharacter == null);
            if (GUILayout.Button(new GUIContent(AnimPlayerGUI.IsPlayerShown() ? iconActionAnimPlayerOn : iconActionAnimPlayer, "Show animation preview player."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                if (AnimPlayerGUI.IsPlayerShown())
                {
                    GameObject characterPrefab = WindowManager.GetSelectedOrPreviewCharacter();
                    WindowManager.HideAnimationPlayer(true);                                        
                    ResetAllSceneViewCamera(characterPrefab);
                }
                else
                {
                    GameObject characterPrefab = WindowManager.GetSelectedOrPreviewCharacter();
                    WindowManager.ShowAnimationPlayer();                                        
                    ResetAllSceneViewCamera(characterPrefab);
                }
            }
            EditorGUI.EndDisabledGroup();
            //GUI.enabled = true;

            GUILayout.Space(ACTION_BUTTON_SPACE);

            //if (contextCharacter == null) GUI.enabled = false;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || contextCharacter == null);
            if (GUILayout.Button(new GUIContent(AnimRetargetGUI.IsPlayerShown() ? iconActionAvatarAlignOn : iconActionAvatarAlign, "Animation Adjustment & Retargeting."),
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                if (AnimRetargetGUI.IsPlayerShown())
                {
                    WindowManager.HideAnimationRetargeter(true);                    
                }
                else
                {
                    if (AnimPlayerGUI.IsPlayerShown())
                        WindowManager.ShowAnimationRetargeter();
                }
            }
            //GUI.enabled = true;
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            GUILayout.Space(ACTION_BUTTON_SPACE);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (!WindowManager.IsPreviewScene) GUI.enabled = false;
            if (GUILayout.Button(new GUIContent(iconLighting, "Cycle Lighting."),
                GUILayout.Width(WEE_BUTTON_SIZE), GUILayout.Height(WEE_BUTTON_SIZE)))
            {
                PreviewScene.CycleLighting();
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(ACTION_BUTTON_SPACE);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (!WindowManager.IsPreviewScene) GUI.enabled = false;
            if (GUILayout.Button(new GUIContent(iconCamera, "Match main camera to scene view."),
                GUILayout.Width(WEE_BUTTON_SIZE), GUILayout.Height(WEE_BUTTON_SIZE)))
            {
                WindowManager.DoMatchSceneCameraOnce();
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            

            GUILayout.Space(ACTION_BUTTON_SPACE);
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            GUIContent settingsIconGC;
            if (windowMode != ImporterWindowMode.Settings)
                settingsIconGC = new GUIContent(iconSettings, "Settings.");
            else
                settingsIconGC = new GUIContent(iconSettingsOn, "Back.");
            if (GUILayout.Button(settingsIconGC, 
                GUILayout.Width(ACTION_BUTTON_SIZE), GUILayout.Height(ACTION_BUTTON_SIZE)))
            {
                if (windowMode != ImporterWindowMode.Settings)
                    windowMode = ImporterWindowMode.Settings;
                else
                    windowMode = ImporterWindowMode.Build;
            }
            EditorGUI.EndDisabledGroup();

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
            SELECT_LINKED = GUILayout.Toggle(SELECT_LINKED, "Select Linked");            
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndArea();            
        }

        private void OnGUISettingsArea(Rect settingsBlock)
        {
            if (EditorApplication.isPlaying)
            {
                windowMode = ImporterWindowMode.Build;
                return;
            }

            GUILayout.BeginArea(settingsBlock);
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();            
            GUILayout.FlexibleSpace();
            GUILayout.Label("Settings", importerStyles.boldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(TITLE_SPACE);

            if (Pipeline.isHDRP)
            {
                Importer.USE_DIGITAL_HUMAN_SHADER = GUILayout.Toggle(Importer.USE_DIGITAL_HUMAN_SHADER,
                    new GUIContent("Use Dual Specular Shaders", "Use Dual Specular shaders where possible. Dual specular shaders use the stack lit master node which is forward only. "+
                    "The dual specular shader setups are based principles used in the Heretic digital human shaders."));
                GUILayout.Space(ROW_SPACE);
            }
            else
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

            /*if (Pipeline.isHDRP)
            {
                Importer.USE_TESSELLATION_SHADER = GUILayout.Toggle(Importer.USE_TESSELLATION_SHADER,
                new GUIContent("Use Tessellation in Shaders", "Use tessellation enabled shaders where possible. " +
                "For HDRP 10 & 11 this means default shaders only (HDRP/LitTessellation). For HDRP 12 (Unity 2021.2+) all shader graph shaders can have tessellation enabled."));
                GUILayout.Space(ROW_SPACE);
            }*/

            Importer.ANIMPLAYER_ON_BY_DEFAULT = GUILayout.Toggle(Importer.ANIMPLAYER_ON_BY_DEFAULT,
                    new GUIContent("Animation Player On", "Always show the animation player when opening the preview scene."));
            GUILayout.Space(ROW_SPACE);

            Importer.USE_SELF_COLLISION = GUILayout.Toggle(Importer.USE_SELF_COLLISION,
                    new GUIContent("Use self collision", "Use the self collision distances from the Character Creator export."));
            GUILayout.Space(ROW_SPACE);

            GUILayout.Space(10f);
            GUILayout.BeginVertical(new GUIContent("", "Override mip-map bias for all textures setup for the characters."), importerStyles.labelStyle);
            GUILayout.Label("Mip-map Bias");
            GUILayout.Space(ROW_SPACE);
            GUILayout.BeginHorizontal();
            Importer.MIPMAP_BIAS = GUILayout.HorizontalSlider(Importer.MIPMAP_BIAS, -1f, 1f, GUILayout.Width(160f));
            GUILayout.Label(Importer.MIPMAP_BIAS.ToString("0.00"),
                            GUILayout.Width(40f));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(ROW_SPACE);

            GUILayout.Space(10f);
            GUILayout.BeginVertical(new GUIContent("", "When setting up the physics capsule and sphere colliders, shrink the radius by this amount. This can help resolve colliders pushing out cloth too much during simulation."), importerStyles.labelStyle);
            GUILayout.Label("Physics Collider Shrink");
            GUILayout.Space(ROW_SPACE);
            GUILayout.BeginHorizontal();
            Physics.PHYSICS_SHRINK_COLLIDER_RADIUS = GUILayout.HorizontalSlider(Physics.PHYSICS_SHRINK_COLLIDER_RADIUS, -2, 2f, GUILayout.Width(160f));
            GUILayout.Label(Physics.PHYSICS_SHRINK_COLLIDER_RADIUS.ToString("0.00"), 
                            GUILayout.Width(40f));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(ROW_SPACE);


            if (MagicaCloth2Available)
            {
                GUILayout.Space(10f);
                GUILayout.BeginVertical(new GUIContent("", "Set global values for Magica Cloth 2 proxy mesh reduction settings. NB these settings will only be applied the next time the character physics are built."), importerStyles.labelStyle);
                GUILayout.Label("Magica Cloth 2 - Reduction Settings");
                GUILayout.Space(ROW_SPACE);
                GUILayout.Label("Cloth Objects");
                GUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                GUILayout.Label("Simple Distance", GUILayout.Width(100f));
                Physics.CLOTHSIMPLEDISTANCE = (float)Math.Round(GUILayout.HorizontalSlider(Physics.CLOTHSIMPLEDISTANCE, 0f, 0.2f, GUILayout.Width(100f)), 3);
                GUILayout.Label(Physics.CLOTHSIMPLEDISTANCE.ToString("0.000"),
                                GUILayout.Width(40f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                GUILayout.Label("Shape Distance", GUILayout.Width(100f));
                Physics.CLOTHSHAPEDISTANCE = (float)Math.Round(GUILayout.HorizontalSlider(Physics.CLOTHSHAPEDISTANCE, 0f, 0.2f, GUILayout.Width(100f)), 3);
                GUILayout.Label(Physics.CLOTHSHAPEDISTANCE.ToString("0.000"),
                                GUILayout.Width(40f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Label("Hair Objects");
                GUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                GUILayout.Label("Simple Distance", GUILayout.Width(100f));
                Physics.HAIRSIMPLEDISTANCE = (float)Math.Round(GUILayout.HorizontalSlider(Physics.HAIRSIMPLEDISTANCE, 0f, 0.2f, GUILayout.Width(100f)), 3);
                GUILayout.Label(Physics.HAIRSIMPLEDISTANCE.ToString("0.000"),
                                GUILayout.Width(40f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                GUILayout.Label("Shape Distance", GUILayout.Width(100f));
                Physics.HAIRSHAPEDISTANCE = (float)Math.Round(GUILayout.HorizontalSlider(Physics.HAIRSHAPEDISTANCE, 0f, 0.2f, GUILayout.Width(100f)), 3);
                GUILayout.Label(Physics.HAIRSHAPEDISTANCE.ToString("0.000"),
                                GUILayout.Width(40f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(ROW_SPACE);
                GUILayout.EndVertical();
                GUILayout.BeginVertical(new GUIContent("", "Set the threshold for conversion of the PhysX weightmap into the 'Fixed/Moveable' system used by Magica Cloth 2.  When a very low value is set then any slight movement allowed by PhysX will also allow movement in Magica Cloth 2."), importerStyles.labelStyle);

                GUILayout.Label("Weightmap Threshold %", GUILayout.Width(140f));
                GUILayout.BeginHorizontal();
                GUILayout.Space(12f);
                Physics.MAGICA_WEIGHTMAP_THRESHOLD_PC = (float)Math.Round(GUILayout.HorizontalSlider(Physics.MAGICA_WEIGHTMAP_THRESHOLD_PC, 0f, 20f, GUILayout.Width(214f)), 2);
                GUILayout.Label(Physics.MAGICA_WEIGHTMAP_THRESHOLD_PC.ToString("0.00") + " %",
                                GUILayout.Width(50f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(ROW_SPACE);
            }

            /*
            GUILayout.Space(10f);
            GUILayout.BeginVertical(new GUIContent("", "When assigning weight maps, the system analyses the weights of the mesh to determine which colliders affect the cloth simulation.Only cloth weights above this threshold will be considered for collider detection. Note: This is the default value supplied to the WeightMapper component, it can be further modified there."), importerStyles.labelStyle);
            GUILayout.Label("Collider Detection Threshold");
            GUILayout.Space(ROW_SPACE);            
            GUILayout.BeginHorizontal();
            Physics.PHYSICS_WEIGHT_MAP_DETECT_COLLIDER_THRESHOLD = GUILayout.HorizontalSlider(Physics.PHYSICS_WEIGHT_MAP_DETECT_COLLIDER_THRESHOLD, 0f, 1f);
            GUILayout.Label(Physics.PHYSICS_WEIGHT_MAP_DETECT_COLLIDER_THRESHOLD.ToString("0.00"), 
                            GUILayout.Width(40f));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(ROW_SPACE);
            */

            GUILayout.Space(10f);
            string label = "Log Everything";
            if (Util.LOG_LEVEL == 0) label = "Log Errors Only";
            if (Util.LOG_LEVEL == 1) label = "Log Warnings and Errors";
            if (Util.LOG_LEVEL == 2) label = "Log Messages";
            if (EditorGUILayout.DropdownButton(
                content: new GUIContent(label),
                focusType: FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Log Errors Only"), Util.LOG_LEVEL == 0, LogOptionSelected, 0);
                menu.AddItem(new GUIContent("Log Warnings and Errors"), Util.LOG_LEVEL == 1, LogOptionSelected, 1);
                menu.AddItem(new GUIContent("Log Messages"), Util.LOG_LEVEL == 2, LogOptionSelected, 2);
                menu.AddItem(new GUIContent("Log Everything"), Util.LOG_LEVEL == 3, LogOptionSelected, 3);
                menu.ShowAsContext();
            }
            GUILayout.Space(ROW_SPACE);

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Reset Options", "Reset options to defaults."),
                GUILayout.Height(BUTTON_HEIGHT), GUILayout.Width(160f)))
            {
                ResetOptions();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(ROW_SPACE);

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
            GUILayout.EndArea();
        }

        private void LogOptionSelected(object sel)
        {
            Util.LOG_LEVEL = (int)sel;
        }

        private void EyeOptionSelected(object sel)
        {            
            contextCharacter.QualEyes = (CharacterInfo.EyeQuality)sel;
        }

        private void RigOptionSelected(object sel)
        {
            contextCharacter.UnknownRigType = (CharacterInfo.RigOverride)sel;
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
            ImporterWindow window = ImporterWindow.Current;

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
            GameObject prefab = import.Import();
            info.Write();
            return prefab;
        }
        
        private static void ClearAllData()
        {
            if (contextCharacter != null) contextCharacter.Release();
            contextCharacter = null;
            
            if (validCharacters != null)
            {
                foreach (CharacterInfo ci in validCharacters)
                {
                    ci.Release();
                }
                validCharacters.Clear();
                validCharacters = null;
            }

            if (Current && Current.characterTreeView != null)
            {
                ImporterWindow window = Current;
                window.characterTreeView.Release();
            }

            Current = null;            
        }

        private void OnDestroy()
        {            
            ClearAllData();            
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

        public bool UpdatePreviewCharacter(GameObject prefabAsset)
        {
            if (WindowManager.IsPreviewScene)
            {
                WindowManager.GetPreviewScene().UpdatePreviewCharacter(prefabAsset);
            }            

            return WindowManager.IsPreviewScene;
        }

        private void BuildCharacter()
        {
            Util.LogInfo("Building materials:");            

            // refresh the character info for any Json changes
            contextCharacter.Refresh();

            // default to high quality if never set before
            if (contextCharacter.BuildQuality == MaterialQuality.None)
                contextCharacter.BuildQuality = MaterialQuality.High;

            // import and build the materials from the Json data
            GameObject prefabAsset = ImportCharacter(contextCharacter);            

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
                    if (WindowManager.showPlayer) 
                        WindowManager.ShowAnimationPlayer();
                }
            }

            Repaint();            
        }

        private void BakeCharacter()
        {
            if (contextCharacter.HQMaterials)
            {
                Util.LogInfo("Baking materials:");

                WindowManager.HideAnimationPlayer(true);

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

        private void BakeCharacterHair()
        {
            if (contextCharacter.HQMaterials)
            {
                Util.LogInfo("Baking hair materials:");

                WindowManager.HideAnimationPlayer(true);

                ComputeBake baker = new ComputeBake(contextCharacter.Fbx, contextCharacter, "Hair");
                baker.BakeHQHairDiffuse();

                contextCharacter.tempHairBake = true;
                contextCharacter.Write();
            }
        }

        private void RestoreCharacterHair()
        {
            if (contextCharacter.HQMaterials)
            {
                Util.LogInfo("Restoring hair materials:");

                WindowManager.HideAnimationPlayer(true);

                ComputeBake baker = new ComputeBake(contextCharacter.Fbx, contextCharacter, "Hair");
                GameObject bakedAsset = baker.RestoreHQHair();

                contextCharacter.tempHairBake = false;
                contextCharacter.Write();
            }
        }

        bool ShowBakedCharacter(GameObject bakedAsset)
        {
            if (WindowManager.IsPreviewScene)
            {
                WindowManager.GetPreviewScene().ShowBakedCharacter(bakedAsset);
            }            

            return WindowManager.IsPreviewScene;
        } 
        
        void RebuildCharacterPhysics()
        {
            WindowManager.HideAnimationPlayer(true);
            WindowManager.HideAnimationRetargeter(true);

            GameObject prefabAsset = Physics.RebuildPhysics(contextCharacter);

            if (prefabAsset)
            {
                if (UpdatePreviewCharacter(prefabAsset))
                {
                    if (WindowManager.showPlayer)
                        WindowManager.ShowAnimationPlayer();
                }
            }

            Repaint();
        }

        void ProcessAnimations()
        {
            RL.DoAnimationImport(contextCharacter);
            GameObject characterPrefab = Util.FindCharacterPrefabAsset(contextCharacter.Fbx);
            if (characterPrefab == null)
            {
                Util.LogWarn("Could not find character prefab for retargeting, using FBX instead.");
                characterPrefab = contextCharacter.Fbx;
            }

            AnimRetargetGUI.GenerateCharacterTargetedAnimations(contextCharacter.path, characterPrefab, true);
            List<string> motionGuids = contextCharacter.GetMotionGuids();
            if (motionGuids.Count > 0)
            {
                //Avatar sourceAvatar = contextCharacter.GetCharacterAvatar();
                foreach (string motionGuid in motionGuids)
                {
                    string motionPath = AssetDatabase.GUIDToAssetPath(motionGuid);
                    AnimRetargetGUI.GenerateCharacterTargetedAnimations(motionPath, characterPrefab, true);
                }
            }
            int animationRetargeted = contextCharacter.DualMaterialHair ? 2 : 1;
            contextCharacter.animationRetargeted = animationRetargeted;
            contextCharacter.Write();
        }

        public static void ResetAllSceneViewCamera(GameObject targetOverride = null)
        {
            if (WindowManager.IsPreviewScene) 
            {
                GameObject obj;
                if (targetOverride) obj = targetOverride;
                else obj = WindowManager.GetPreviewScene().GetPreviewCharacter();                

                if (obj)
                {
                    GameObject root = Util.GetScenePrefabInstanceRoot(obj);

                    if (root)
                    {
                        //GameObject hips = MeshUtil.FindCharacterBone(root, "CC_Base_Spine02", "Spine02");
                        //GameObject head = MeshUtil.FindCharacterBone(root, "CC_Base_Head", "Head");
                        GameObject hips = MeshUtil.FindCharacterBone(root, "CC_Base_NeckTwist01", "NeckTwist01");
                        GameObject head = MeshUtil.FindCharacterBone(root, "CC_Base_Head", "Head");
                        if (hips && head)
                        {
                            Vector3 lookAt = (hips.transform.position + head.transform.position * 2f) / 3f;
                            Quaternion lookBackRot = new Quaternion();
                            Vector3 euler = lookBackRot.eulerAngles;
                            euler.y = -180f;
                            lookBackRot.eulerAngles = euler;

                            foreach (SceneView sv in SceneView.sceneViews)
                            {
                                sv.LookAt(lookAt, lookBackRot, 0.25f);
                            }
                        }
                    }
                }
            }
        }

        public static void ForceUpdateLighting()
        {
            PreviewScene.PokeLighting();
        }

        public static void ResetOptions()
        {
            Importer.MIPMAP_BIAS = 0f;
            Importer.RECONSTRUCT_FLOW_NORMALS = false;
            Importer.REBAKE_BLENDER_UNITY_MAPS = false;
            Importer.ANIMPLAYER_ON_BY_DEFAULT = false;
            Importer.USE_SELF_COLLISION = false;
            Importer.USE_AMPLIFY_SHADER = true;
            Importer.USE_DIGITAL_HUMAN_SHADER = false;
            Physics.PHYSICS_SHRINK_COLLIDER_RADIUS = 0.5f;
            Physics.PHYSICS_WEIGHT_MAP_DETECT_COLLIDER_THRESHOLD = 0.25f;
            
            Physics.CLOTHSIMPLEDISTANCE = Physics.CLOTHSHAPEDISTANCE_DEFAULT;
            Physics.CLOTHSHAPEDISTANCE = Physics.CLOTHSHAPEDISTANCE_DEFAULT;
            Physics.HAIRSIMPLEDISTANCE = Physics.HAIRSIMPLEDISTANCE_DEFAULT;
            Physics.HAIRSHAPEDISTANCE = Physics.HAIRSHAPEDISTANCE_DEFAULT;
            Physics.MAGICA_WEIGHTMAP_THRESHOLD_PC = Physics.MAGICA_WEIGHTMAP_THRESHOLD_PC_DEFAULT;

            Util.LOG_LEVEL = 0;
            ICON_AREA_WIDTH = ICON_WIDTH;
        }

        // additions for draggable width icon area

        private void OnGUIDragBarArea(Rect dragBar)
        {
            //Rect dragHandle = new Rect(dragBar.x - DRAG_HANDLE_PADDING, dragBar.y, 2 * DRAG_HANDLE_PADDING, dragBar.height);
            Rect dragHandle = new Rect(dragBar.x, dragBar.y, DRAG_BAR_WIDTH + DRAG_HANDLE_PADDING, dragBar.height);
            EditorGUIUtility.AddCursorRect(dragHandle, MouseCursor.ResizeHorizontal);
            HandleMouseDrag(dragHandle);

            GUILayout.BeginArea(dragBar);
            GUILayout.BeginVertical(importerStyles.dragBarStyle);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void OnGUIFlexibleIconArea(Rect iconBlock)
        {            
            if (ICON_AREA_WIDTH > ICON_WIDTH_DETAIL)
            {
                OnGUIDetailIconArea(iconBlock); // detail view icon area layout
            }
            else
            {
                OnGUILargeIconArea(iconBlock); // adapted original icon area layaout
            }            
        }

        // adapted original icon area layaout
        private void OnGUILargeIconArea(Rect iconBlock)
        {
            GUILayout.BeginArea(iconBlock);

            Event e = Event.current;
            if (e.isMouse && e.type == EventType.MouseDown)
            {
                if (e.clickCount == 2) doubleClick = true;
                else doubleClick = false;
            }

            using (var iconScrollViewScope = new EditorGUILayout.ScrollViewScope(iconScrollView, GUILayout.Width(iconBlock.width - 1f), GUILayout.Height(iconBlock.height - 10f)))
            {
                iconScrollView = iconScrollViewScope.scrollPosition;
                GUILayout.BeginVertical();

                for (int idx = 0; idx < validCharacters.Count; idx++)
                {
                    CharacterInfo info = validCharacters[idx];                    
                    Texture2D iconTexture = iconUnprocessed;
                    string name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(info.guid));
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

                    Color background = GUI.backgroundColor;
                    Color tint = background;
                    if (contextCharacter == info)
                        tint = Color.green;
                    GUI.backgroundColor = Color.Lerp(background, tint, 0.25f);

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical();
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

                    GUI.backgroundColor = background;

                    GUILayout.Space(2f);

                    GUILayout.Box(name, importerStyles.iconStyle, GUILayout.Width(ICON_SIZE));
                    GUILayout.Space(2f);
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }

        // detail view icon area layout
        private void OnGUIDetailIconArea(Rect iconBlock)
        {
            importerStyles.FixMeh();

            GUILayout.Space(TOP_PADDING);

            float rowHeight = ICON_SIZE_SMALL + 2 * ICON_DETAIL_MARGIN;

            Rect boxRect = new Rect(0f, 0f, ICON_AREA_WIDTH - 4f, rowHeight);
            Rect posRect = new Rect(iconBlock);
            Rect viewRect = new Rect(0f, 0f, ICON_AREA_WIDTH - 14f, rowHeight * validCharacters.Count);

            iconScrollView = GUI.BeginScrollView(posRect, iconScrollView, viewRect, false, false);
            for (int idx = 0; idx < validCharacters.Count; idx++)
            {
                CharacterInfo info = validCharacters[idx];
                Texture2D iconTexture = iconUnprocessed;
                string name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(info.guid));
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
                
                float heightDelta = ICON_SIZE_SMALL + 2 * ICON_DETAIL_MARGIN;
                boxRect.y = idx * heightDelta;

                GUILayout.BeginArea(boxRect);
                                
                GUILayout.BeginVertical(contextCharacter == info ? importerStyles.fakeButtonContext : importerStyles.fakeButton);
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal(); // horizontal container for image and label

                GUILayout.BeginVertical(); // vertical container for image
                GUILayout.FlexibleSpace();

                GUILayout.Box(iconTexture, new GUIStyle(),
                    GUILayout.Width(ICON_SIZE_SMALL),
                    GUILayout.Height(ICON_SIZE_SMALL));
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical(); // vertical container for image

                GUILayout.BeginVertical(); // vertical container for label
                GUILayout.FlexibleSpace();                
                GUILayout.Label(name, importerStyles.nameTextStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical(); // vertical container for label

                GUILayout.FlexibleSpace(); // fill horizontal for overall left-justify

                GUILayout.EndHorizontal(); // horizontal container for image and label

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical(); //(fakeButton)

                GUILayout.EndArea();

                if (HandleListClick(boxRect))
                {
                    RepaintOnUpdate();
                    SetContextCharacter(info.guid);
                    if (fakeButtonDoubleClick)
                    {
                        previewCharacterAfterGUI = true;
                    }
                }                
            }
            GUI.EndScrollView();
        }

        private void HandleMouseDrag(Rect container)
        {
            Event mouseEvent = Event.current;
            if (container.Contains(mouseEvent.mousePosition) || dragging)
            {
                if (mouseEvent.type == EventType.MouseDrag)
                {                    
                    dragging = true;
                    ICON_AREA_WIDTH += mouseEvent.delta.x;
                    if (ICON_AREA_WIDTH < ICON_WIDTH_MIN)
                        ICON_AREA_WIDTH = ICON_WIDTH_MIN;

                    //float INFO_WIDTH_CALC = position.width - WINDOW_MARGIN - ICON_WIDTH - ACTION_WIDTH;
                    if (CURRENT_INFO_WIDTH < INFO_WIDTH_MIN)
                        ICON_AREA_WIDTH = position.width - WINDOW_MARGIN - ACTION_WIDTH - INFO_WIDTH_MIN;

                    RepaintOnUpdate();
                }

                if (mouseEvent.type == EventType.MouseUp)
                {                    
                    dragging = false;

                    RepaintOnUpdate();
                }
            }
        }        

        private bool fakeButtonDoubleClick = false;

        private bool HandleListClick(Rect container)
        {            
            Event mouseEvent = Event.current;
            if (container.Contains(mouseEvent.mousePosition))
            {
                if (mouseEvent.type == EventType.MouseDown)
                {
                    if (mouseEvent.clickCount == 2)
                    {
                        fakeButtonDoubleClick = true;
                    }
                    else
                        fakeButtonDoubleClick = false;
                    return true;
                }                
            }
            return false;
        }

        void RepaintOnUpdate()
        {
            if (!repaintDelegated)
            {
                repaintDelegated = true;
                EditorApplication.update -= RepaintOnceOnUpdate;
                EditorApplication.update += RepaintOnceOnUpdate;
            }
        }
        
        void RepaintOnceOnUpdate()
        {
            Repaint();
            EditorApplication.update -= RepaintOnceOnUpdate;
            repaintDelegated = false;
        }

        public static Texture2D TextureColor(Color color)
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }



        public class Styles
        {
            public GUIStyle logStyle;
            public GUIStyle mainStyle;
            public GUIStyle buttonStyle;
            public GUIStyle labelStyle;
            public GUIStyle boldStyle;
            public GUIStyle iconStyle;
            public GUIStyle dragBarStyle;
            public GUIStyle nameTextStyle;
            public GUIStyle fakeButton;
            public GUIStyle fakeButtonContext;
            public Texture2D dragTex, contextTex;

            public Styles()
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

                //color textures for the area styling
                               

                dragBarStyle = new GUIStyle();
                dragBarStyle.normal.background = dragTex;
                dragBarStyle.stretchHeight = true;
                dragBarStyle.stretchWidth = true;

                nameTextStyle = new GUIStyle();
                nameTextStyle.alignment = TextAnchor.MiddleLeft;
                nameTextStyle.wordWrap = false;
                nameTextStyle.fontStyle = FontStyle.Normal;
                nameTextStyle.normal.textColor = Color.white;

                fakeButton = new GUIStyle();
                //fakeButton.normal.background = nonContextTex;
                fakeButton.padding = new RectOffset(1, 1, 1, 1);
                fakeButton.stretchHeight = true;
                fakeButton.stretchWidth = true;

                fakeButtonContext = new GUIStyle();
                fakeButtonContext.name = "fakeButtonContext";
                fakeButtonContext.normal.background = contextTex;
                fakeButtonContext.padding = new RectOffset(1, 1, 1, 1);
                fakeButtonContext.stretchHeight = true;
                fakeButtonContext.stretchWidth = true;

                FixMeh();
            }

            public void FixMeh()
            {
                if (!dragTex)
                {                    
                    dragTex = TextureColor(new Color(0f,0f,0f,0.25f));
                    dragBarStyle.normal.background = dragTex;                    
                }
                if (!contextTex)
                {                    
                    contextTex = TextureColor(new Color(0.259f, 0.345f, 0.259f));
                    fakeButtonContext.normal.background = contextTex;
                }
            }
        }

        private void CheckAvailableAddons()
        {
            // init simple bools for the GUI to use to avoid repeatedly iterating through 
            // AppDomain.CurrentDomain.GetAssemblies() -- ALWAYS make these checks before any reflection code
            dynamicBoneAvailable = Physics.DynamicBoneIsAvailable();
            magicaCloth2Available = Physics.MagicaCloth2IsAvailable();
        }

    }
}
