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

        private static readonly string windowTitle = "CC3 Import Tool";        
        private static CharacterInfo contextCharacter;
        private static List<CharacterInfo> validCharacters;
        private static string backScenePath;
        private static Mode mode;
                        
        private Vector2 iconScrollView;
        private bool previewCharacterAfterGUI;
        private bool refreshAfterGUI;

        const float ICON_SIZE = 80f;
        const float WINDOW_MARGIN = 4f;
        const float TOP_PADDING = 16f;
        const float ACTION_BUTTON_WIDTH = 100f;        
        const float BUTTON_HEIGHT = 30f;
        const float FUNCTION_BUTTON_WIDTH = 100f;        
        const float INFO_HEIGHT = 90f;
        const float OPTION_HEIGHT = 60f;
        const float ACTION_HEIGHT = 76f;
        const float ICON_WIDTH = 120f;

        private static GUIStyle logStyle, mainStyle, buttonStyle, labelStyle, boldStyle;
        private static Texture2D iconUnprocessed;
        private static Texture2D iconBasic;
        private static Texture2D iconHQ;
        private static Texture2D iconBaked;

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

                EditorPrefs.SetString("RL_Importer_Context_Path", contextCharacter.path);
            }
        }        

        public static ImporterWindow Init(Mode windowMode, UnityEngine.Object characterObject)
        {                        
            Type hwt = Type.GetType("UnityEditor.SceneHierarchyWindow, UnityEditor.dll");
            ImporterWindow window = GetWindow<ImporterWindow>(windowTitle, hwt);
            window.minSize = new Vector2(300f, 500f);

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
            if (validCharacters == null) InitData();
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
                GUILayout.Label("No CC3/iClone Characters detected!");
                return;
            }            

            float width = position.width - WINDOW_MARGIN;
            float height = position.height - WINDOW_MARGIN;
            float innerHeight = height - TOP_PADDING;                 

            Rect iconBlock = new Rect(0f, TOP_PADDING, ICON_WIDTH, innerHeight);
            Rect infoBlock = new Rect(iconBlock.xMax, TOP_PADDING, width - iconBlock.width, INFO_HEIGHT);
            Rect optionBlock = new Rect(iconBlock.xMax, infoBlock.yMax, infoBlock.width, OPTION_HEIGHT);
            Rect actionBlock = new Rect(iconBlock.xMax, optionBlock.yMax, infoBlock.width, ACTION_HEIGHT);            
            Rect treeviewBlock = new Rect(iconBlock.xMax, actionBlock.yMax, infoBlock.width, height - actionBlock.yMax);

            previewCharacterAfterGUI = false;
            refreshAfterGUI = false;

            CheckDragAndDrop();

            OnGUIIconArea(iconBlock);            

            OnGUIInfoArea(infoBlock);

            OnGUIOptionArea(optionBlock);

            OnGUIActionArea(actionBlock);

            OnGUITreeViewArea(treeviewBlock);

            // creating a new preview scene in between GUI Layouts causes errors...
            if (previewCharacterAfterGUI)
            {
                StoreBackScene();
                Util.PreviewCharacter(contextCharacter.Fbx);
            }

            if (refreshAfterGUI)
            {
                RefreshCharacterList();
            }
        }

        private void OnGUIIconArea(Rect iconBlock)
        {            
            GUILayout.BeginArea(iconBlock);
            using (var iconScrollViewScope = new EditorGUILayout.ScrollViewScope(iconScrollView, GUILayout.Width(iconBlock.width - 10f), GUILayout.Height(iconBlock.height - 10f)))
            {
                iconScrollView = iconScrollViewScope.scrollPosition;
                GUILayout.BeginVertical();

                for (int idx = 0; idx < validCharacters.Count; idx++)
                {
                    CharacterInfo info = validCharacters[idx];
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    Texture2D iconTexture = iconUnprocessed;

                    if (info.bakeIsBaked) iconTexture = iconBaked;
                    else if (info.logType == CharacterInfo.ProcessingType.Basic) iconTexture = iconBasic;
                    else if (info.logType == CharacterInfo.ProcessingType.HighQuality) iconTexture = iconHQ;

                    if (GUILayout.Button(iconTexture,
                        //mainStyle, 
                        GUILayout.Width(ICON_SIZE),
                        GUILayout.Height(ICON_SIZE)))
                    {
                        SetContextCharacter(info.guid);
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    string name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(info.guid));
                    GUILayout.Box(name, mainStyle);
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
            if (contextCharacter.logType == CharacterInfo.ProcessingType.Basic)
                importType = "Default Materials";
            if (contextCharacter.logType == CharacterInfo.ProcessingType.HighQuality)
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

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (false && !string.IsNullOrEmpty(backScenePath) && File.Exists(backScenePath))
            {               
                if (GUILayout.Button("Back", GUILayout.Width(65f), GUILayout.Height(BUTTON_HEIGHT)))
                {
                    GoBackScene();
                }
            }
            else
            {
                GUILayout.Space(69f);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Preview", GUILayout.Width(65), GUILayout.Height(BUTTON_HEIGHT)))
            {
                previewCharacterAfterGUI = true;
            }

            GUILayout.FlexibleSpace();

            if (mode == Mode.multi)
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(65), GUILayout.Height(BUTTON_HEIGHT)))
                {
                    refreshAfterGUI = true;
                }                
            }
            else
            {
                GUILayout.Space(69f);
            }

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
            GUILayout.FlexibleSpace();

            if (!contextCharacter.CanHaveHighQualityMaterials) GUI.enabled = false;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            contextCharacter.qualRefractiveEyes = GUILayout.Toggle(contextCharacter.qualRefractiveEyes, "Eye - Refractive");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            contextCharacter.bakeCustomShaders = GUILayout.Toggle(contextCharacter.bakeCustomShaders, "Bake - Custom Shaders");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            contextCharacter.bakeSeparatePrefab = GUILayout.Toggle(contextCharacter.bakeSeparatePrefab, "Bake - To Separate Prefab");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
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

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Default", GUILayout.Width(ACTION_BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                Util.LogInfo("Doing: Connect Default Materials.");
                GameObject prefab = ImportCharacter(contextCharacter, MaterialQuality.Default);
                contextCharacter.logType = CharacterInfo.ProcessingType.Basic;
                contextCharacter.Write();
                CreateTreeView(true);

                if (contextCharacter.isLOD) Util.ReplacePreviewCharacter(contextCharacter.Fbx, prefab);
            }            
            GUILayout.FlexibleSpace();
            if (!contextCharacter.CanHaveHighQualityMaterials) GUI.enabled = false;
            if (GUILayout.Button("High Quality", GUILayout.Width(ACTION_BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                Util.LogInfo("Doing: Connect High Quality Materials.");
                GameObject prefab = ImportCharacter(contextCharacter, MaterialQuality.High);
                contextCharacter.logType = CharacterInfo.ProcessingType.HighQuality;
                contextCharacter.Write();
                CreateTreeView(true);

                if (contextCharacter.isLOD) Util.ReplacePreviewCharacter(contextCharacter.Fbx, prefab);
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (contextCharacter.logType != CharacterInfo.ProcessingType.HighQuality) GUI.enabled = false;
            if (GUILayout.Button("Bake", GUILayout.Width(FUNCTION_BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                if (contextCharacter.logType == CharacterInfo.ProcessingType.HighQuality)
                {     
                    ComputeBake baker = new ComputeBake(contextCharacter.Fbx, contextCharacter);
                    baker.BakeHQ();

                    contextCharacter.bakeIsBaked = true;
                    contextCharacter.Write();
                }
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (contextCharacter.logType == CharacterInfo.ProcessingType.None) GUI.enabled = false;
            if (GUILayout.Button("Animations", GUILayout.Width(FUNCTION_BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                RL.SetAnimationImport(contextCharacter, contextCharacter.Fbx);
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

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
            characterTreeView.selectLinked = GUILayout.Toggle(characterTreeView.selectLinked, "Select Linked");
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndArea();            
        }        

        private GameObject ImportCharacter(CharacterInfo info, MaterialQuality quality)
        {
            Importer import = new Importer(info);            
            import.SetQuality(quality);
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

    }
}
