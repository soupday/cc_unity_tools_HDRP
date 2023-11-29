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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.IO;

namespace Reallusion.Import
{
    public class MassProcessingWindow : EditorWindow
    {
        public static MassProcessingWindow massProcessingWindow;
        enum WindowMode { standard, extended }
        public enum DragBarId { a, b }
        public enum SortType { ascending, descending }
        public enum FilterType { all, processed, unprocessed }
        private static WindowMode windowMode;
        public static SortType windowSortType;
        public static FilterType windowFilterType;

        const float WINDOW_MARGIN = 2f;
        const float TOP_PADDING = 2f;
        const float SETTINGS_TOP_PADDING = 20f;
        const float INITIAL_PROC_LIST_WIDTH = 360f;
        const float INITIAL_PROC_FLAGS_WIDTH = 60f;
        const float PROC_LIST_MIN_W = 180f;
        const float PROC_FLAGS_MIN_W = 30f;
        const float PROC_CTRL_HEIGHT = 48f;
        const float SEARCH_BAR_HEIGHT = 20f;
        const float INITIAL_PROC_LIST_HEIGHT = 480f;//500f;
        const float PROC_LIST_MIN_H = 250f;
        const float INITIAL_SETTINGS_WIDTH = 220f;
        const float SETTINGS_MIN_W = 75f;
        const float DRAG_BAR_WIDTH = 2f;
        const float DRAG_HANDLE_PADDING = 4f;
        const float ICON_SIZE = 64f;
        const float ICON_WIDTH_DETAIL = 140f;
        const float ICON_SIZE_SMALL = 25f;
        const float ICON_SIZE_MID = 42f;
        const float ICON_DETAIL_MARGIN = 2f;
        const float LIST_MEMBER_LEFT_MARGIN = 6f;
        private float PROC_LIST_WIDTH = INITIAL_PROC_LIST_WIDTH;
        private float PROC_FLAGS_WIDTH = INITIAL_PROC_FLAGS_WIDTH;
        private float SETTINGS_WIDTH = INITIAL_SETTINGS_WIDTH;
        private bool dragging = false;
        private bool repaintDelegated = false;
        private Vector2 listScrollPosition;
        private Styles windowStyles;

        private Texture2D iconUnprocessed;
        private Texture2D iconBasic;
        private Texture2D iconHQ;
        private Texture2D iconBaked;
        private Texture2D iconMixed;
        private Texture2D iconListUnchecked;
        private Texture2D iconListChecked;
        private Texture2D iconListRemove;
        private Texture2D iconSettingsShown;
        private Texture2D iconSettings;
        private Texture2D iconSettingsShownChanged;
        private Texture2D iconSettingsChanged;
        private Texture2D iconSortList;
        private Texture2D iconStartProcessing;
        private Texture2D iconFilterEdit;
        private Texture2D iconFilterRemove;
        private Texture2D iconRefreshList;

        Rect prev = new Rect();

        private bool initDone = false;                
        private ImporterWindow importerWindow;
        public List<CharacterInfo> workingList;
        List<CharacterListDisplay> displayList;        
        CharacterInfo characterSettings;
        private bool isMassSelected = false;
        private string searchString = string.Empty;
        List<CharacterInfo> buildQueue;

        [MenuItem("Reallusion/Processing Tools/Batch Processing", priority = 400)]
        public static void ATInitAssetProcessing()
        {
            massProcessingWindow = OpenProcessingWindow();
        }

        [MenuItem("Reallusion/Processing Tools/Batch Processing", true)]
        public static bool ValidateATInitAssetProcessing()
        {
            if (ImporterWindow.Current != null && !EditorWindow.HasOpenInstances<MassProcessingWindow>())
                return true;
            else
                return false;
        }

        public static MassProcessingWindow OpenProcessingWindow()
        {
            //window = EditorWindow.GetWindow<MassProcessingWindow>();
            MassProcessingWindow window = ScriptableObject.CreateInstance<MassProcessingWindow>();
            bool windowAsUtility = true;
            windowMode = WindowMode.standard;
            windowSortType = SortType.ascending;

            if (windowAsUtility)
                window.ShowUtility(); // non dockable window
            else
                window.Show(); // dockable window

            window.minSize = GetMinSize();
            //initial window dimensions
            Rect centerPosition = GetRectToCenterWindow(INITIAL_PROC_LIST_WIDTH, INITIAL_PROC_LIST_HEIGHT);
            window.position = centerPosition;
            window.titleContent = new GUIContent("CC/iC Importer - Batch Processing");
            return window;
        }

        private void InitData()
        {
            SetWindowSize();
            importerWindow = ImporterWindow.Current;

            string[] folders = new string[] { "Assets", "Packages" };
            iconUnprocessed = Util.FindTexture(folders, "RLIcon_UnprocessedChar");
            iconBasic = Util.FindTexture(folders, "RLIcon_BasicChar");
            iconHQ = Util.FindTexture(folders, "RLIcon_HQChar");
            iconBaked = Util.FindTexture(folders, "RLIcon_BakedChar");
            iconMixed = Util.FindTexture(folders, "RLIcon_MixedChar");
            iconListUnchecked = Util.FindTexture(folders, "RLIcon_ListUnchecked");
            iconListChecked = Util.FindTexture(folders, "RLIcon_ListChecked");
            iconListRemove = Util.FindTexture(folders, "RLIcon_ListRemove");
            iconSettingsShown = Util.FindTexture(folders, "RLIcon_BuildSettingsShown");
            iconSettings = Util.FindTexture(folders, "RLIcon_BuildSettings");
            iconSettingsShownChanged = Util.FindTexture(folders, "RLIcon_BuildSettingsShown_changed");
            iconSettingsChanged = Util.FindTexture(folders, "RLIcon_BuildSettings_changed");
            iconSortList = Util.FindTexture(folders, "RLIcon_SortList");
            iconRefreshList = Util.FindTexture(folders, "RLIcon_RefreshList");
            iconFilterEdit = Util.FindTexture(folders, "RLIcon_FilterEdit");
            iconFilterRemove = Util.FindTexture(folders, "RLIcon_FilterRemove");
            iconStartProcessing = Util.FindTexture(folders, "RLIcon_StartProcessing");
            initDone = true;
        }

        private float batchTimer = 0f;
        public void BatchUpdateTimer()
        {
            if (batchTimer > 0f)
            {
                if (Time.realtimeSinceStartup > batchTimer)
                {
                    batchTimer = 0f;
                    EditorApplication.update -= BatchUpdateTimer;
                    BatchBuildNextQueueCharacter();
                }
            }
            else
            {
                batchTimer = 0f;
                EditorApplication.update -= BatchUpdateTimer;
            }
        }

        public void BatchQueueNextBuild(float delay)
        {
            EditorApplication.update -= BatchUpdateTimer;
            Selection.activeObject = null;

            if (buildQueue == null || buildQueue.Count == 0 || ImporterWindow.Current == null)
            {
                Util.LogInfo("Done batch processing!");
                batchTimer = 0f;
                // reset the window and displayed list at the end of the build process
                ResetSettings();
            }
            else
            {
                Util.LogInfo("Building: " + buildQueue[0].name + " (" + buildQueue.Count + " remaining) in " + delay + "s");
                batchTimer = Time.realtimeSinceStartup + delay;
                EditorApplication.update += BatchUpdateTimer;
            }
        }

        public void BatchBuildNextQueueCharacter()
        {
            if (ImporterWindow.Current == null) 
            {
                buildQueue = null;
                return;
            }

            if (buildQueue == null || buildQueue.Count == 0) return;

            CharacterInfo batchCharacter = buildQueue[0];

            CharacterInfo character = ImporterWindow.ValidCharacters.Where(t => t.guid == batchCharacter.guid).FirstOrDefault();
            if (character != null)
            {
                Util.LogInfo("Batch Queue Processing: " + character.name);
                
                character.CopySettings(batchCharacter);

                // default to high quality if never set before
                if (character.BuildQuality == MaterialQuality.None)
                    character.BuildQuality = MaterialQuality.High;

                // refresh the character info for any Json changes
                character.Refresh();                

                // import and build the materials from the Json data
                Importer import = new Importer(character);
                GameObject prefab = import.Import(true);
                character.Write();
                character.Release();
            }

            buildQueue.Remove(batchCharacter);

            BatchQueueNextBuild(1f);

            EditorApplication.delayCall += ProcessingRefresh;
        }

        private void ProcessingRefresh()
        {
            importerWindow.RefreshCharacterList();
            FilterDisplayedList();
        }

        private void ResetSettings()
        {
            ResetWindow();
            workingList = BuildCharacterInfoList();
            windowSortType = SortType.ascending;
            windowFilterType = FilterType.all;
            isMassSelected = false;
            searchString = string.Empty;
            GUI.FocusControl("");
            FilterDisplayedList();
        }

        private bool IsInFilteredDisplayList(CharacterInfo character)
        {
            foreach (CharacterListDisplay cld in displayList)
            {
                if (cld.guid == character.guid) return true;
            }

            return false;
        }

        public void BeginMassProcessing()
        {
            // add a delayed call to refresh the char list in the importer window and the batch window
            EditorApplication.delayCall += ProcessingRefresh;
            buildQueue = new List<CharacterInfo>();

            foreach (CharacterInfo character in workingList)
            {
                // all individual settings are stored in CharacterInfoList character (base class CharacterInfo)
                if (character.selectedInList && IsInFilteredDisplayList(character))
                {
                    // process character.
                    if (!buildQueue.Contains(character))
                    {
                        character.FixCharSettings();
                        buildQueue.Add(character);
                    }
                }
            }

            BatchQueueNextBuild(1f);
        }

        public static Vector2 GetMinSize()
        {
            float minWidth = PROC_LIST_MIN_W;
            if (windowMode == WindowMode.extended) minWidth += SETTINGS_MIN_W;

            return new Vector2(minWidth, PROC_LIST_MIN_H);
        }

        public void SetWindowSize()
        {
            Rect pos = this.position;
            switch (windowMode)
            {
                case WindowMode.standard: { pos.width = PROC_LIST_WIDTH + WINDOW_MARGIN; break; }
                case WindowMode.extended: { pos.width = PROC_LIST_WIDTH + DRAG_BAR_WIDTH + SETTINGS_WIDTH + WINDOW_MARGIN; break; }
            }
            this.position = pos;
            Repaint();
        }        

        public class CharacterListDisplay
        {
            public string guid;
            public string displayName;
            public bool selectedInList;
            public bool settingsChanged;
            public bool bakeIsBaked;
            public bool BuiltBasicMaterials;
            public bool BuiltHQMaterials;

            public CharacterListDisplay(string guidString, List<CharacterInfo> masterList)
            {
                CharacterInfo info = masterList.Where(t => t.guid == guidString).FirstOrDefault();
                if (info != null)
                {
                    guid = guidString;
                    displayName = info.name;
                    selectedInList = info.selectedInList;
                    settingsChanged = info.settingsChanged;
                    bakeIsBaked = info.bakeIsBaked;
                    BuiltBasicMaterials = info.BuiltBasicMaterials;
                    BuiltHQMaterials = info.BuiltHQMaterials;
                }
                else
                {
                    guid = "0000";
                    displayName = "Cannot Display Name";
                    selectedInList = false;
                    settingsChanged = false;
                    bakeIsBaked = false;
                    BuiltBasicMaterials = false;
                    BuiltHQMaterials = false;
                }
            }
        }

        public List<CharacterInfo> BuildCharacterInfoList()
        {
            List<CharacterInfo> output = new List<CharacterInfo>();

            if (ImporterWindow.ValidCharacters?.Count > 0)
            {
                foreach (Reallusion.Import.CharacterInfo c in ImporterWindow.ValidCharacters)
                {
                    output.Add(new CharacterInfo(c.guid));
                }
            }

            return output;
        }

        public List<CharacterListDisplay> BuildCharacterListDisplay(List<CharacterInfo> masterList, SortType sortType, FilterType filterType)
        {
            List<CharacterListDisplay> output = new List<CharacterListDisplay>();

            if (masterList != null && masterList.Count > 0)
            {
                List<CharacterInfo> processingList = masterList.ToList();

                IEnumerable<CharacterInfo> query = new List<CharacterInfo>();

                switch (sortType)
                {
                    case SortType.ascending:
                        {
                            query = from character in processingList
                                    orderby character.name ascending //.Substring(0, 1) ascending
                                    select character;
                            break;
                        }
                    case SortType.descending:
                        {
                            query = from character in processingList
                                    orderby character.name descending //.Substring(0, 1) descending
                                    select character;
                            break;
                        }
                }

                foreach (CharacterInfo c in query)
                {
                    bool searchMatch = false;

                    if (string.IsNullOrEmpty(searchString))
                    {
                        searchMatch = true;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(c.name))
                        {
                            bool contains = c.name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;
                            if (contains)
                            {
                                searchMatch = true;
                            }
                        }
                    }

                    CharacterListDisplay newInfo = new CharacterListDisplay(c.guid, masterList);
                    CharacterInfo originalInfo = (CharacterInfo)workingList.Where(t => t.guid.Equals(c.guid)).FirstOrDefault();
                    if (originalInfo != null)
                    {
                        newInfo.selectedInList = originalInfo.selectedInList;
                    }

                    switch (filterType)
                    {
                        case FilterType.all:
                            {
                                if (searchMatch) 
                                    output.Add(newInfo);
                                break;
                            }
                        case FilterType.processed:
                            {
                                if (searchMatch)
                                {
                                    if (newInfo.BuiltBasicMaterials || newInfo.BuiltHQMaterials) output.Add(newInfo);
                                }
                                break;

                            }
                        case FilterType.unprocessed:
                            {
                                if (searchMatch)
                                {
                                    if (!newInfo.BuiltBasicMaterials && !newInfo.BuiltHQMaterials) output.Add(newInfo);
                                }
                                break;
                            }
                    }
                }
            }

            return output;
        }

        public void FilterDisplayedList()
        {
            displayList = BuildCharacterListDisplay(workingList, windowSortType, windowFilterType);
            Repaint();
        }

        public List<CharacterInfo> SortAndFilterCharacterInfoList(SortType sortType, FilterType filterType)
        {
            List<CharacterInfo> output = new List<CharacterInfo>();

            if (ImporterWindow.ValidCharacters.Count > 0)
            {
                List<CharacterInfo> processingList = ImporterWindow.ValidCharacters.ToList();

                IEnumerable<CharacterInfo> query = new List<CharacterInfo>();

                switch (sortType)
                {
                    case SortType.ascending:                        
                        query = from character in processingList
                                orderby character.name.Substring(0, 1) ascending
                                select character;
                        break;                        
                    case SortType.descending:
                    default:                        
                        query = from character in processingList
                                orderby character.name.Substring(0, 1) descending
                                select character;
                        break;                        
                }

                foreach (Reallusion.Import.CharacterInfo c in query)
                {
                    CharacterInfo newInfo = new CharacterInfo(c.guid);
                    CharacterInfo originalInfo = (CharacterInfo)workingList.Where(t => t.guid.Equals(c.guid)).FirstOrDefault();
                    if (originalInfo != null)
                    {
                        newInfo.selectedInList = originalInfo.selectedInList;
                    }

                    switch (filterType)
                    {                        
                        case FilterType.processed:                            
                            if (newInfo.BuiltBasicMaterials || newInfo.BuiltHQMaterials) output.Add(newInfo);
                            break;
                        case FilterType.unprocessed:
                            if (!newInfo.BuiltBasicMaterials && !newInfo.BuiltHQMaterials) output.Add(newInfo);
                            break;
                        case FilterType.all:
                        default:
                            output.Add(newInfo);
                            break;
                    }
                }
            }
            return output;
        }

        private void OnGUI()
        {
            if (ImporterWindow.Current == null)
            {
                CloseWindow();
                return;
            }
            if (!initDone) InitData();
            if (windowStyles == null) windowStyles = new Styles();
            if (workingList == null) workingList = BuildCharacterInfoList();
            if (displayList == null) FilterDisplayedList();

            float width = position.width - WINDOW_MARGIN;
            float height = position.height - WINDOW_MARGIN;
            float innerHeight = height - TOP_PADDING;
            float listHeight = height - TOP_PADDING * 2 - PROC_CTRL_HEIGHT - SEARCH_BAR_HEIGHT;

            if (windowMode == WindowMode.standard) PROC_LIST_WIDTH = width;

            Rect controlsRect = new Rect(0f, TOP_PADDING, PROC_LIST_WIDTH + WINDOW_MARGIN, PROC_CTRL_HEIGHT);
            Rect nameFilterRect = new Rect(0f, controlsRect.yMax, PROC_LIST_WIDTH + WINDOW_MARGIN, SEARCH_BAR_HEIGHT);
            Rect listRect = new Rect(0f, nameFilterRect.yMax + TOP_PADDING, PROC_LIST_WIDTH + WINDOW_MARGIN, listHeight);
            Rect extendedDragBarRect = new Rect(listRect.xMax, TOP_PADDING, DRAG_BAR_WIDTH, innerHeight);
            Rect extendedSettingsRect = new Rect(extendedDragBarRect.xMax, TOP_PADDING, width - PROC_LIST_WIDTH, innerHeight);

            if (windowMode == WindowMode.extended) SETTINGS_WIDTH = width - PROC_LIST_WIDTH - DRAG_BAR_WIDTH;

            //TestShowArea(controlsRect, Color.red);
            //TestShowArea(listRect, Color.magenta);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            ONGUIControlsArea(controlsRect);
            OnGUINameFlterArea(nameFilterRect);
            OnGUIDetailWorkingDisplayListArea(listRect);
            EditorGUI.EndDisabledGroup();

            if (windowMode == WindowMode.extended)
            {
                OnGUIGeneralDragBarArea(extendedDragBarRect);
                OnGUIExtendedSettingsArea(extendedSettingsRect);
            }
        }

        private void TestShowArea(Rect area, Color color)
        {
            GUIStyle gUIStyle = GUI.skin.box;
            gUIStyle.normal.background = TextureColor(color);
            GUILayout.BeginArea(area, gUIStyle);
            GUILayout.EndArea();
        }

        private void ONGUIControlsArea(Rect controlsRect)
        {
            GUILayout.BeginArea(controlsRect, GUI.skin.box);
            GUILayout.BeginHorizontal();
            string buttonText = windowSortType == SortType.ascending ? "Sort Descending" : "Sort Ascending";

            if (GUILayout.Button(new GUIContent(iconSortList, buttonText), GUILayout.Width(ICON_SIZE_MID),
                                     GUILayout.Height(ICON_SIZE_MID)))
            {
                SortType sorting;
                if (windowSortType == SortType.ascending)
                    sorting = SortType.descending;
                else
                    sorting = SortType.ascending;

                windowSortType = sorting;
                FilterDisplayedList();
            }

            if (GUILayout.Button(new GUIContent(iconFilterEdit, "Change List Filter"), GUILayout.Width(ICON_SIZE_MID),
                                     GUILayout.Height(ICON_SIZE_MID)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Display All"), windowFilterType == FilterType.all, FilterOptionSelected, FilterType.all);
                menu.AddItem(new GUIContent("Show only unprocessed"), windowFilterType == FilterType.unprocessed, FilterOptionSelected, FilterType.unprocessed);
                menu.AddItem(new GUIContent("Show only processed"), windowFilterType == FilterType.processed, FilterOptionSelected, FilterType.processed);
                menu.ShowAsContext();
            }

            if (GUILayout.Button(new GUIContent(iconFilterRemove, "Remove all filters"), GUILayout.Width(ICON_SIZE_MID),
                                     GUILayout.Height(ICON_SIZE_MID)))
            {
                windowSortType = SortType.ascending;
                windowFilterType = FilterType.all;
                FilterDisplayedList();
            }
            
            if (GUILayout.Button(new GUIContent(iconRefreshList, "Reset list and all settings."),
                                     GUILayout.Width(ICON_SIZE_MID),
                                     GUILayout.Height(ICON_SIZE_MID)))
            {
                ResetSettings();
                /*
                ResetWindow();
                workingList = BuildCharacterInfoList();
                windowSortType = SortType.ascending;
                windowFilterType = FilterType.all;
                isMassSelected = false;
                searchString = string.Empty;
                GUI.FocusControl("");
                FilterDisplayedList();
                */
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(iconStartProcessing, "Begin mass processing of selected items."),
                                     GUILayout.Width(ICON_SIZE_MID),
                                     GUILayout.Height(ICON_SIZE_MID)))
            {
                ResetWindow();
                BeginMassProcessing();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        
        private void OnGUINameFlterArea(Rect areaRect)
        {
            Rect selectAllBoxRect = new Rect(areaRect.x, areaRect.y, 20f, areaRect.height);
            Rect nameFilterRect = new Rect(selectAllBoxRect.xMax, areaRect.y, areaRect.width - selectAllBoxRect.width, areaRect.height);
            Texture2D massSelectedIcon = isMassSelected ? iconListChecked : iconListUnchecked;

            GUILayout.BeginArea(selectAllBoxRect);

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(); 
            GUILayout.Space(LIST_MEMBER_LEFT_MARGIN);
            GUILayout.BeginVertical(); 
            GUILayout.FlexibleSpace();
            GUILayout.Box(massSelectedIcon, new GUIStyle(),
                            GUILayout.Width(16f),
                            GUILayout.Height(16f));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndArea();

            if (HandleListClick(selectAllBoxRect))
            {
                isMassSelected = !isMassSelected;
                ToggleSelectAllDisplayed();
                Repaint();
            }

            GUILayout.BeginArea(nameFilterRect);

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            //GUILayout.Space(1f);

            GUILayout.BeginHorizontal(); // horizontal container for image and label
            GUILayout.Space(10f);
            EditorGUI.BeginChangeCheck();
            searchString = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField);

            if (EditorGUI.EndChangeCheck())
            {
                FilterDisplayedList();
            }

            // The TextField does not update until it loses focus, so clearing the string wont clear the text field
            // was: winbtn_win_close_h - not available in 2023.1
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_clear"), EditorStyles.toolbarButton, GUILayout.Width(22)))
            {
                searchString = string.Empty;
                GUI.FocusControl("");
                FilterDisplayedList();
            } 

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        private void OnGUIDetailWorkingDisplayListArea(Rect iconBlock)
        {
            windowStyles.FixTexturesOnDomainReload();
            float rowHeight = ICON_SIZE_SMALL + 2 * ICON_DETAIL_MARGIN;

            Rect boxRect = new Rect(0f, 0f, PROC_LIST_WIDTH - PROC_FLAGS_WIDTH - 4f, rowHeight);
            Rect flagsBoxRect = new Rect(boxRect.xMax + 2f, 0f, PROC_FLAGS_WIDTH, rowHeight);
            Rect posRect = new Rect(iconBlock);
            Rect viewRect = new Rect(0f, 0f, PROC_LIST_WIDTH - 14f, rowHeight * (displayList.Count + 0.5f));

            listScrollPosition = GUI.BeginScrollView(posRect, listScrollPosition, viewRect, false, false);
            for (int idx = 0; idx < displayList.Count; idx++)
            {
                CharacterListDisplay info = displayList[idx];
                CharacterInfo importerWindowInfo = ImporterWindow.ValidCharacters.Where(t => t.guid == info.guid).FirstOrDefault();
                Texture2D iconTexture = iconUnprocessed;
                string name = "";
                if (importerWindowInfo != null)
                {
                    name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(importerWindowInfo.guid));
                    if (importerWindowInfo.bakeIsBaked)
                    {
                        if (importerWindowInfo.BuiltBasicMaterials) iconTexture = iconMixed;
                        else if (importerWindowInfo.BuiltHQMaterials) iconTexture = iconBaked;
                    }
                    else
                    {
                        if (importerWindowInfo.BuiltBasicMaterials) iconTexture = iconBasic;
                        else if (importerWindowInfo.BuiltHQMaterials) iconTexture = iconHQ;
                    }
                }
                else
                {
                    name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(info.guid));
                }

                float heightDelta = ICON_SIZE_SMALL + 2 * ICON_DETAIL_MARGIN;
                boxRect.y = idx * heightDelta;
                flagsBoxRect.y = idx * heightDelta;
                GUILayout.BeginArea(boxRect);

                GUILayout.BeginVertical(idx % 2 == 0 ? windowStyles.fakeButtonContext : windowStyles.fakeButton);
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal(); // horizontal container for image and label
                GUILayout.Space(LIST_MEMBER_LEFT_MARGIN);
                GUILayout.BeginVertical(); // vertical container for checkbox
                GUILayout.FlexibleSpace();
                GUILayout.Box(info.selectedInList ? iconListChecked : iconListUnchecked, new GUIStyle(),
                GUILayout.Width(16f),
                GUILayout.Height(16f));
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.BeginVertical(); // vertical container for image
                GUILayout.FlexibleSpace();

                GUILayout.Box(iconTexture, new GUIStyle(),
                    GUILayout.Width(ICON_SIZE_SMALL),
                    GUILayout.Height(ICON_SIZE_SMALL));
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical(); // vertical container for image

                GUILayout.BeginVertical(); // vertical container for label
                GUILayout.FlexibleSpace();
                GUILayout.Label(name, windowStyles.nameTextStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical(); // vertical container for label

                GUILayout.FlexibleSpace(); // fill horizontal for overall left-justify

                GUILayout.EndHorizontal(); // horizontal container for image and label

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical(); //(fakeButton)

                GUILayout.EndArea();

                if (HandleListClick(boxRect))
                {
                    info.selectedInList = !info.selectedInList;
                    CharacterInfo workingChar = workingList.Where(t => t.guid == info.guid).FirstOrDefault();
                    if (workingChar != null)
                    {
                        workingChar.selectedInList = info.selectedInList;
                    }
                    Repaint();
                }

                GUILayout.BeginArea(flagsBoxRect);

                GUILayout.BeginVertical(idx % 2 == 0 ? windowStyles.fakeButtonContext : windowStyles.fakeButton);

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();

                Texture2D displayIcon;
                if (characterSettings == null)
                {
                    displayIcon = info.settingsChanged == true ? iconSettingsChanged : iconSettings;
                }

                else
                {
                    displayIcon = info.guid == characterSettings.guid ? info.settingsChanged == true ? iconSettingsShownChanged : iconSettingsShown : info.settingsChanged == true ? iconSettingsChanged : iconSettings;
                }
                GUILayout.Box(displayIcon, new GUIStyle(),
                        GUILayout.Width(ICON_SIZE_SMALL),
                        GUILayout.Height(ICON_SIZE_SMALL));

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.EndVertical();

                GUILayout.EndArea();
                if (HandleListClick(flagsBoxRect))
                {
                    if (characterSettings != null)
                    {
                        if (info.guid == characterSettings.guid)
                        {
                            windowMode = WindowMode.standard;
                            characterSettings = null;
                        }
                        else
                        {
                            characterSettings = workingList.Where(t => t.guid == info.guid).FirstOrDefault();
                            if (windowMode == WindowMode.standard)
                                windowMode = WindowMode.extended;
                        }
                    }
                    else
                    {
                        characterSettings = workingList.Where(t => t.guid == info.guid).FirstOrDefault();
                        if (windowMode == WindowMode.standard)
                            windowMode = WindowMode.extended;
                    }
                    SetWindowSize();
                    Repaint();
                }
            }
            GUI.EndScrollView();
        }

        private void OnGUIExtendedSettingsArea(Rect optionBlock)
        {
            GUILayout.BeginArea(optionBlock);
            if (characterSettings != null)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(SETTINGS_TOP_PADDING);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(characterSettings.name, windowStyles.boldStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10f);

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical();

                if (characterSettings.Generation == BaseGeneration.Unknown)
                {
                    if (EditorGUILayout.DropdownButton(
                        content: new GUIContent("Rig Type: " + characterSettings.UnknownRigType.ToString()),
                        focusType: FocusType.Passive))
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Rig Type: None"), characterSettings.UnknownRigType == CharacterInfo.RigOverride.None, RigOptionSelected, CharacterInfo.RigOverride.None);
                        menu.AddItem(new GUIContent("Rig Type: Humanoid"), characterSettings.UnknownRigType == CharacterInfo.RigOverride.Humanoid, RigOptionSelected, CharacterInfo.RigOverride.Humanoid);
                        menu.AddItem(new GUIContent("Rig Type: Generic"), characterSettings.UnknownRigType == CharacterInfo.RigOverride.Generic, RigOptionSelected, CharacterInfo.RigOverride.Generic);
                        menu.ShowAsContext();
                    }

                    GUILayout.Space(1f);
                }

                if (EditorGUILayout.DropdownButton(
                    content: new GUIContent(characterSettings.BasicMaterials ? "Basic Materials" : "High Quality Materials"),
                    focusType: FocusType.Passive))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Basic Materials"), characterSettings.BasicMaterials, MaterialOptionSelected, true);
                    if (characterSettings.CanHaveHighQualityMaterials)
                        menu.AddItem(new GUIContent("High Quality Materials"), characterSettings.HQMaterials, MaterialOptionSelected, false);
                    menu.ShowAsContext();
                }

                GUILayout.Space(1f);

                if (characterSettings.BasicMaterials) GUI.enabled = false;
                if (EditorGUILayout.DropdownButton(
                    content: new GUIContent(characterSettings.QualEyes.ToString() + " Eyes"),
                    focusType: FocusType.Passive))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Basic Eyes"), characterSettings.BasicEyes, EyeOptionSelected, CharacterInfo.EyeQuality.Basic);
                    menu.AddItem(new GUIContent("Parallax Eyes"), characterSettings.ParallaxEyes, EyeOptionSelected, CharacterInfo.EyeQuality.Parallax);
                    if (Pipeline.isHDRP)
                        menu.AddItem(new GUIContent("Refractive (SSR) Eyes"), characterSettings.RefractiveEyes, EyeOptionSelected, CharacterInfo.EyeQuality.Refractive);
                    menu.ShowAsContext();
                }

                GUILayout.Space(1f);
                string hairType;
                switch (characterSettings.QualHair)
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
                    menu.AddItem(new GUIContent("Single Pass Hair"), characterSettings.DefaultHair, HairOptionSelected, CharacterInfo.HairQuality.Default);
                    menu.AddItem(new GUIContent("Two Pass Hair"), characterSettings.DualMaterialHair, HairOptionSelected, CharacterInfo.HairQuality.TwoPass);
                    if (Importer.USE_AMPLIFY_SHADER && !Pipeline.isHDRP)
                        menu.AddItem(new GUIContent("MSAA Coverage Hair"), characterSettings.CoverageHair, HairOptionSelected, CharacterInfo.HairQuality.Coverage);
                    menu.ShowAsContext();
                }
                // /*
                bool showDebugEnumPopup = false;
                if (showDebugEnumPopup)
                {
                    int features = 2;
                    if (Pipeline.isHDRP12) features++; // tessellation
                    if (Pipeline.is3D || Pipeline.isURP) features++; // Amplify
                    EditorGUI.BeginChangeCheck();
                    if (features == 1)
                        characterSettings.ShaderFlags = (CharacterInfo.ShaderFeatureFlags)EditorGUILayout.EnumPopup(characterSettings.ShaderFlags);
                    else if (features > 1)
                        characterSettings.ShaderFlags = (CharacterInfo.ShaderFeatureFlags)EditorGUILayout.EnumFlagsField(characterSettings.ShaderFlags);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ValidateSettings(characterSettings);
                    }
                    GUI.enabled = true;
                }
                // */
                //////////////

                if (Event.current.type == EventType.Repaint)
                    prev = GUILayoutUtility.GetLastRect();

                if (EditorGUILayout.DropdownButton(
                    content: new GUIContent("Features"),
                    focusType: FocusType.Passive))
                {
                    ImporterFeaturesWindow.ShowAtPosition(new Rect(prev.x, prev.y + 20f, prev.width, prev.height), characterSettings);
                }
                //////////////
                ///
                GUILayout.Space(8f);

                if (characterSettings.BuiltBasicMaterials) GUI.enabled = false;
                if (EditorGUILayout.DropdownButton(
                    content: new GUIContent(characterSettings.BakeCustomShaders ? "Bake Custom Shaders" : "Bake Default Shaders"),
                    focusType: FocusType.Passive))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Default Shaders"), !characterSettings.BakeCustomShaders, BakeShadersOptionSelected, false);
                    menu.AddItem(new GUIContent("Custom Shaders"), characterSettings.BakeCustomShaders, BakeShadersOptionSelected, true);
                    menu.ShowAsContext();
                }

                GUILayout.Space(1f);

                if (EditorGUILayout.DropdownButton(
                    new GUIContent(characterSettings.BakeSeparatePrefab ? "Bake Separate Prefab" : "Bake Overwrite Prefab"),
                    FocusType.Passive
                    ))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Overwrite Prefab"), !characterSettings.BakeSeparatePrefab, BakePrefabOptionSelected, false);
                    menu.AddItem(new GUIContent("Separate Baked Prefab"), characterSettings.BakeSeparatePrefab, BakePrefabOptionSelected, true);
                    menu.ShowAsContext();
                }
                GUI.enabled = true;

                GUILayout.Space(12f);

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Apply to Selected", GUILayout.Width(140f), GUILayout.Height(32f)))
                {
                    CopyCurrentSettingsToSelected();
                }

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.EndVertical();

                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }

        private void CopyCurrentSettingsToSelected()
        {
            bool dirty = false;

            if (characterSettings != null)
            {
                characterSettings.FixCharSettings();

                foreach (CharacterInfo character in workingList)
                {                    
                    if (character != characterSettings && character.selectedInList)
                    {                        
                        character.CopySettings(characterSettings);
                        if (ValidateSettings(character))
                            dirty = true;
                    }
                }
            }            

            if (dirty)
            {
                FilterDisplayedList();
            }
        }

        private void ToggleSelectAllDisplayed()
        {
            foreach (CharacterListDisplay character in displayList)
            {
                character.selectedInList = isMassSelected;
                CharacterInfo workingListChar = workingList.Where(t => t.guid == character.guid).FirstOrDefault();
                if (workingListChar != null)
                {
                    workingListChar.selectedInList = isMassSelected;
                }
            }
        }

        public bool ValidateSettings(CharacterInfo characterSettings, bool refresh = true)
        {
            bool dirty = false;
            if (characterSettings != null)
            {
                // find the original settings in validCharacters
                CharacterInfo original = ImporterWindow.ValidCharacters.Where(x => x.guid == characterSettings.guid).FirstOrDefault();
                if (original != null)
                {
                    if (characterSettings.UnknownRigType != original.UnknownRigType) dirty = true;
                    if (characterSettings.BasicMaterials != original.BasicMaterials) dirty = true;
                    if (characterSettings.HQMaterials != original.HQMaterials) dirty = true;
                    if (characterSettings.BasicEyes != original.BasicEyes) dirty = true;
                    if (characterSettings.ParallaxEyes != original.ParallaxEyes) dirty = true;
                    if (characterSettings.RefractiveEyes != original.RefractiveEyes) dirty = true;
                    if (characterSettings.DefaultHair != original.DefaultHair) dirty = true;
                    if (characterSettings.DualMaterialHair != original.DualMaterialHair) dirty = true;
                    if (characterSettings.CoverageHair != original.CoverageHair) dirty = true;
                    if (characterSettings.ShaderFlags != original.ShaderFlags) dirty = true;
                    if (characterSettings.BakeCustomShaders != original.BakeCustomShaders) dirty = true;
                    if (characterSettings.BakeSeparatePrefab != original.BakeSeparatePrefab) dirty = true;
                }
            }
            characterSettings.settingsChanged = dirty;
            if (refresh) FilterDisplayedList();
            return dirty;
        }

        private void OnGUIGeneralDragBarArea(Rect dragBarRect)
        {
            Rect dragHandle = new Rect(dragBarRect.x, dragBarRect.y, DRAG_BAR_WIDTH + DRAG_HANDLE_PADDING, dragBarRect.height);
            EditorGUIUtility.AddCursorRect(dragHandle, MouseCursor.ResizeHorizontal);
            HandleMouseDrag(dragHandle);

            GUILayout.BeginArea(dragBarRect);
            GUILayout.BeginVertical(windowStyles.dragBarStyle);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void HandleMouseDrag(Rect container)
        {
            Event mouseEvent = Event.current;
            if (container.Contains(mouseEvent.mousePosition) || dragging)
            {
                if (mouseEvent.type == EventType.MouseDrag)
                {
                    dragging = true;

                    // original block
                    PROC_LIST_WIDTH += mouseEvent.delta.x;
                    if (PROC_LIST_WIDTH < PROC_LIST_MIN_W)
                        PROC_LIST_WIDTH = PROC_LIST_MIN_W;

                    RepaintOnUpdate();
                }

                if (mouseEvent.type == EventType.MouseUp)
                {
                    dragging = false;
                    RepaintOnceOnUpdate();
                }
            }
        }

        private bool HandleListClick(Rect container)
        {
            Event mouseEvent = Event.current;
            if (container.Contains(mouseEvent.mousePosition))
            {
                if (mouseEvent.type == EventType.MouseDown)
                {
                    if (mouseEvent.clickCount == 2)
                    {
                        //fakeButtonDoubleClick = true;
                    }
                    else
                    {
                        //fakeButtonDoubleClick = false;
                    }
                    return true;
                }
            }
            return false;
        }

        public class Styles
        {
            public GUIStyle iconStyle;
            public GUIStyle dragBarStyle;
            public GUIStyle fakeButton;
            public GUIStyle fakeButtonContext;
            public GUIStyle nameTextStyle;
            public GUIStyle boldStyle;
            public Texture2D dragTex, contextTex;

            public Styles()
            {
                iconStyle = new GUIStyle();
                iconStyle.wordWrap = false;
                iconStyle.fontStyle = FontStyle.Normal;
                iconStyle.normal.textColor = Color.white;
                iconStyle.alignment = TextAnchor.MiddleCenter;

                dragBarStyle = new GUIStyle();
                dragBarStyle.normal.background = dragTex;
                dragBarStyle.stretchHeight = true;
                dragBarStyle.stretchWidth = true;

                fakeButton = new GUIStyle();
                fakeButton.padding = new RectOffset(1, 1, 1, 1);
                fakeButton.stretchHeight = true;
                fakeButton.stretchWidth = true;

                fakeButtonContext = new GUIStyle();
                fakeButtonContext.name = "fakeButtonContext";
                fakeButtonContext.normal.background = contextTex;
                fakeButtonContext.padding = new RectOffset(1, 1, 1, 1);
                fakeButtonContext.stretchHeight = true;
                fakeButtonContext.stretchWidth = true;

                nameTextStyle = new GUIStyle();
                nameTextStyle.alignment = TextAnchor.MiddleLeft;
                nameTextStyle.wordWrap = false;
                nameTextStyle.fontStyle = FontStyle.Normal;
                nameTextStyle.normal.textColor = Color.white;

                boldStyle = new GUIStyle();
                boldStyle.alignment = TextAnchor.UpperLeft;
                boldStyle.wordWrap = false;
                boldStyle.fontStyle = FontStyle.Bold;
                boldStyle.normal.textColor = Color.white;

                FixTexturesOnDomainReload();
            }

            public void FixTexturesOnDomainReload()
            {
                if (!dragTex)
                {
                    dragTex = TextureColor(new Color(0f, 0f, 0f, 0.25f));
                    dragBarStyle.normal.background = dragTex;
                }
                if (!contextTex)
                {
                    contextTex = TextureColor(new Color(0.259f, 0.345f, 0.259f));
                    fakeButtonContext.normal.background = contextTex;
                }
            }
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

        public static Rect GetRectToCenterWindow(float width, float height)
        {
#if UNITY_2020_3_OR_NEWER
            Rect appRect = EditorGUIUtility.GetMainWindowPosition();            
#else
            Rect appRect = GetEditorApplicationWindowRect();
#endif

            if (appRect == new Rect())
            {
                return new Rect(100f, 100f, width, height);
            }

            float xOrigin = appRect.x + (appRect.width / 2f) - (width / 2f);
            float yOrigin = appRect.y + (appRect.height / 2f) - (height / 2f);

            return new Rect(xOrigin, yOrigin, width, height);
        }

        public static Rect GetEditorApplicationWindowRect()
        {
            // The editor application's position is stored in:
            // The current domain's assembly called UnityEditor.CoreModule (System.Reflection.Assembly)
            // Inside the CoreModule the defined type ContainerWindow (System.Reflection.TypeInfo)

            // All Unity application windows are objects of type ContainerWindow (as above)
            // Each window has a "position" 'property' and a "m_ShowMode" 'field'
            // Get a field object for "m_ShowMode" and a property object for "position"        
            // Iterate through the windows obtained with Resources.FindObjectsOfTypeAll
            // The main window has the field m_ShowMode == 4 (field object .GetValue(window))
            // The main window is obtained with property object .GetValue(window)

#if UNITY_2020_3_OR_NEWER
            System.Reflection.Assembly coreModuleAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(t => t.FullName.Contains("UnityEditor.CoreModule")).FirstOrDefault();
            if (coreModuleAssembly != null)
            {                
                System.Reflection.TypeInfo containerWindowTypeInfo = coreModuleAssembly.DefinedTypes.Where(t => t.FullName.Contains("ContainerWindow")).FirstOrDefault();
                if (containerWindowTypeInfo != null)
                {
                    var showModeField = containerWindowTypeInfo.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var positionProperty = containerWindowTypeInfo.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (showModeField != null && positionProperty != null)
                    {
                        var allContainerWindows = Resources.FindObjectsOfTypeAll(containerWindowTypeInfo);
                        foreach (var win in allContainerWindows)
                        {
                            var showmode = (int)showModeField.GetValue(win);
                            if (showmode == 4) // main window
                            {
                                var mainWindowPosition = (Rect)positionProperty.GetValue(win, null);
                                return mainWindowPosition;
                            }
                        }
                    }
                }
            }
#else
            //Unity 2019 ContainerWindow type is in the UnityEditor assembly
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (System.Reflection.TypeInfo t in assembly.DefinedTypes)
                {
                    if (t.FullName.iContains("ContainerWindow"))
                    {
                        var showModeField = t.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var positionProperty = t.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (showModeField != null && positionProperty != null)
                        {
                            var allContainerWindows = Resources.FindObjectsOfTypeAll(t);
                            foreach (var win in allContainerWindows)
                            {
                                var showmode = (int)showModeField.GetValue(win);
                                if (showmode == 4) // main window
                                {
                                    var mainWindowPosition = (Rect)positionProperty.GetValue(win, null);
                                    return mainWindowPosition;
                                }
                            }
                        }
                    }
                }
            }
#endif
            return new Rect(0f, 0f, 0f, 0f);  // something was null - return a new empty Rect
        }

        private void FilterOptionSelected(object sel)
        {
            windowFilterType = (FilterType)sel;
            FilterDisplayedList();
        }

        private void EyeOptionSelected(object sel)
        {
            characterSettings.QualEyes = (CharacterInfo.EyeQuality)sel;
            ValidateSettings(characterSettings);
        }

        private void RigOptionSelected(object sel)
        {
            characterSettings.UnknownRigType = (CharacterInfo.RigOverride)sel;
            ValidateSettings(characterSettings);
        }

        private void HairOptionSelected(object sel)
        {
            characterSettings.QualHair = (CharacterInfo.HairQuality)sel;
            ValidateSettings(characterSettings);
        }

        private void MaterialOptionSelected(object sel)
        {
            if ((bool)sel)
                characterSettings.BuildQuality = MaterialQuality.Default;
            else
                characterSettings.BuildQuality = MaterialQuality.High;
            ValidateSettings(characterSettings);
        }

        private void BakeShadersOptionSelected(object sel)
        {
            characterSettings.BakeCustomShaders = (bool)sel;
            ValidateSettings(characterSettings);
        }

        private void BakePrefabOptionSelected(object sel)
        {
            characterSettings.BakeSeparatePrefab = (bool)sel;
            ValidateSettings(characterSettings);
        }

        private void OnDisable()
        {
            ResetWindow();
            buildQueue = null;            
        }

        private void OnEnable()
        {
            initDone = false;  //if the window is open during play mode - then this forces a refresh of the character list after exiting play mode (currently the window is closed on entering play mode)
        }

        private void ResetWindow()
        {
            characterSettings = null;
            windowMode = WindowMode.standard;
            SetWindowSize();
        }

        private void CloseWindow()
        {
            ResetWindow();
            buildQueue = null;
            this.Close();
        }
    }
}