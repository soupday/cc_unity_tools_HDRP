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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reallusion.Import
{
    public class ImporterFeaturesWindow : EditorWindow
    {
        static ImporterFeaturesWindow importerFeaturesWindow = null;
        static long lastClosedTime;
        private CharacterInfo contextCharacter;
        private CharacterInfo originalCharacter;
        bool massProcessingValidate = false;
        bool flagChanged = false;

        private ImporterWindow importerWindow;
        private Styles windowStyles;
        private float DROPDOWN_WIDTH = 260f;
        private float INITIAL_DROPDOWN_HEIGHT = 160f;
        private float LABEL_WIDTH = 200f;
        private float SECTION_INDENT = 8f;
        private float SUB_SECTION_INDENT = 18f;

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
            hideFlags = HideFlags.DontSave;
        }

        void OnDisable()
        {            
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            importerFeaturesWindow = null;
        }

        void MassProcessingWindowValidate()
        {
            if (MassProcessingWindow.massProcessingWindow != null)
            {
                if (originalCharacter != null)
                {
                    CharacterInfo workingCharacter = MassProcessingWindow.massProcessingWindow.workingList.Where(x => x.guid == contextCharacter.guid).FirstOrDefault();
                    workingCharacter.settingsChanged = contextCharacter.ShaderFlags != originalCharacter.ShaderFlags;
                    MassProcessingWindow.massProcessingWindow.FilterDisplayedList();
                }
            }
        }

        public static bool ShowAtPosition(Rect buttonRect, CharacterInfo contextChar = null)
        {
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < lastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (importerFeaturesWindow == null)
                    importerFeaturesWindow = ScriptableObject.CreateInstance<ImporterFeaturesWindow>();
                else
                {
                    importerFeaturesWindow.Cancel();
                    return false;
                }

                importerFeaturesWindow.Init(buttonRect, contextChar);
                return true;
            }
            return false;
        }

        void Init(Rect buttonRect, CharacterInfo contextChar = null)
        {
            // Has to be done before calling Show / ShowWithMode
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);

            importerWindow = ImporterWindow.Current;

            if (contextChar != null)
            {
                contextCharacter = contextChar;
                originalCharacter = ImporterWindow.ValidCharacters.Where(x => x.guid == contextChar.guid).FirstOrDefault();
                massProcessingValidate = true;
            }
            else
            {
                contextCharacter = importerWindow.Character;
            }
            
            Vector2 windowSize = new Vector2(DROPDOWN_WIDTH, INITIAL_DROPDOWN_HEIGHT);
            ShowAsDropDown(buttonRect, windowSize);
        }

        void Cancel()
        {
            Close();
            GUI.changed = true;
            GUIUtility.ExitGUI();
        }

        public class Styles
        {
            public GUIStyle listEvenBg;
            public GUIStyle listOddBg;
            public GUIStyle listLabel;

            public Styles()
            {
                listEvenBg = new GUIStyle("ObjectPickerResultsOdd");
                listEvenBg.fontStyle = FontStyle.Normal;

                listOddBg = new GUIStyle("ObjectPickerResultsEven");
                listOddBg.fontStyle = FontStyle.Normal;

                listLabel = new GUIStyle("label");
                listLabel.fontSize = 12;
                listLabel.fontStyle = FontStyle.Italic;
            }
        }

        void OnGUI()
        {
            if (windowStyles == null) windowStyles = new Styles();
            int line = 0; // used to determine the background tint of alternate lines to avoid a block of solid color

            flagChanged = false;

            GUILayout.BeginVertical();
            // manipulate the "[Flags]enum ShaderFeatures" with condidions on what flags are available
            // due to pipleine version and available add-ons such as magica cloth or dynamic bone
            // much more flexible than EditorGUILayout.EnumFlagsField

            DrawLabelLine(line++, "Material Shader Features:");
            
            if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.Tessellation, "", SECTION_INDENT))
                flagChanged = true;

            if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.WrinkleMaps, "", SECTION_INDENT))
                flagChanged = true;
            
            DrawLabelLine(line++, "Character Physics:");

            // Cloth Physics
            if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.ClothPhysics, "Enable Cloth Physics", SECTION_INDENT))
                flagChanged = true;

            if (importerWindow.MagicaCloth2Available) // cloth alternatives available so enable non default selections
            {
                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.ClothPhysics))
                {
                    if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.UnityClothPhysics, "Unity Cloth", SUB_SECTION_INDENT, CharacterInfo.clothGroup))
                        flagChanged = true;

                    if (importerWindow.MagicaCloth2Available)
                    {
                        if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.MagicaCloth, "Magica Cloth 2", SUB_SECTION_INDENT, CharacterInfo.clothGroup))
                            flagChanged = true;
                    }
                }
            }

            // Hair Physics
            if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.HairPhysics, "Enable Hair Physics", SECTION_INDENT))
                flagChanged = true;

            if (importerWindow.DynamicBoneAvailable || importerWindow.MagicaCloth2Available) // cloth/bone alternatives available so enable non default selections
            {
                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.HairPhysics))
                {
                    if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics, "Unity Hair Physics", SUB_SECTION_INDENT, CharacterInfo.hairGroup))
                        flagChanged = true;

                    if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.MagicaClothHairPhysics, "Magica Cloth 2 Hair Physics", SUB_SECTION_INDENT, CharacterInfo.hairGroup))
                        flagChanged = true;

                    /*
                    if (importerWindow.DynamicBoneAvailable)
                    {
                        if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.SpringBoneHair, "Dynamic Bone Springbones", SUB_SECTION_INDENT, CharacterInfo.hairGroup))
                            flagChanged = true;
                    }
                    if (importerWindow.MagicaCloth2Available)
                    {
                        if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.MagicaBone, "Magica Bone Springbones", SUB_SECTION_INDENT, CharacterInfo.hairGroup))
                            flagChanged = true;
                    }
                    */
                }
            }

            // Spring Bone Physics
            if (importerWindow.DynamicBoneAvailable || importerWindow.MagicaCloth2Available)
            {
                if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.SpringBonePhysics, "Enable Spring Bone Physics", SECTION_INDENT))
                    flagChanged = true;

                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.SpringBonePhysics))
                {
                    if (importerWindow.MagicaCloth2Available)
                    {
                        if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.MagicaBone, "Magica Bone Springbones", SUB_SECTION_INDENT, CharacterInfo.springGroup))
                            flagChanged = true;
                    }
                    if (importerWindow.DynamicBoneAvailable)
                    {
                        if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.SpringBoneHair, "Dynamic Bone Springbones", SUB_SECTION_INDENT, CharacterInfo.springGroup))
                            flagChanged = true;
                    }                    
                }
            }

            DrawLabelLine(line++, "");

            if (Event.current.type == EventType.Repaint)
            {
                minSize = new Vector2(DROPDOWN_WIDTH, GUILayoutUtility.GetLastRect().yMax);
            }

            if (massProcessingValidate && flagChanged)
            {
                MassProcessingWindowValidate();
            }
        }

        private bool DrawFlagSelectionLine(int line, CharacterInfo.ShaderFeatureFlags flag, string overrideLabel = "", float indent = 0f, CharacterInfo.ShaderFeatureFlags [] radioGroup = null)
        {
            GUILayout.BeginHorizontal(GetLineStyle(line));
            GUILayout.Space(indent);
            bool flagVal = contextCharacter.ShaderFlags.HasFlag(flag);
            GUILayout.Label(new GUIContent(string.IsNullOrEmpty(overrideLabel) ? flag.ToString() : overrideLabel, ""), GUILayout.Width(LABEL_WIDTH));
            EditorGUI.BeginChangeCheck();
            flagVal = GUILayout.Toggle(flagVal, "");
            if (EditorGUI.EndChangeCheck())
            {
                if (radioGroup != null)
                    SetFeatureFlagInGroup(flag, radioGroup);
                else
                    SetFeatureFlag(flag, flagVal);

                return true;
            }
            GUILayout.EndHorizontal();
            return false;
        }

        private void DrawLabelLine(int line, string label)
        {
            GUILayout.BeginHorizontal(GetLineStyle(line));
            GUILayout.Label(label, windowStyles.listLabel);
            GUILayout.EndHorizontal();
        }

        private GUIStyle GetLineStyle(int itemIndex)
        {
            if (windowStyles == null) windowStyles = new Styles();

            return itemIndex % 2 > 0 ? windowStyles.listEvenBg : windowStyles.listOddBg;
        }

        private void SetFeatureFlag(CharacterInfo.ShaderFeatureFlags flag, bool value)
        {
            if (value)
            {
                if (!contextCharacter.ShaderFlags.HasFlag(flag))
                {
                    contextCharacter.ShaderFlags |= flag; // toggle changed to ON => bitwise OR to add flag
                    contextCharacter.EnsureDefaultsAreSet(flag);
                }
            }
            else
            {
                if (contextCharacter.ShaderFlags.HasFlag(flag))
                {
                    contextCharacter.ShaderFlags ^= flag; // toggle changed to OFF => bitwise XOR to remove flag

                    // if  the group flag is being unset then all the 'radio group' entries should be unset too
                    switch (flag)
                    {
                        case CharacterInfo.ShaderFeatureFlags.ClothPhysics:
                            {
                                foreach (CharacterInfo.ShaderFeatureFlags groupFlag in CharacterInfo.clothGroup)
                                {
                                    if (contextCharacter.ShaderFlags.HasFlag(groupFlag))
                                        contextCharacter.ShaderFlags ^= groupFlag;
                                }

                                /*
                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.MagicaCloth))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.MagicaCloth;

                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.UnityClothPhysics))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.UnityClothPhysics;
                                */
                                break;
                            }
                        case CharacterInfo.ShaderFeatureFlags.HairPhysics:
                            {
                                foreach (CharacterInfo.ShaderFeatureFlags groupFlag in CharacterInfo.hairGroup)
                                {
                                    if (contextCharacter.ShaderFlags.HasFlag(groupFlag))
                                        contextCharacter.ShaderFlags ^= groupFlag;
                                }

                                /*
                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.MagicaBone))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.MagicaBone;

                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.SpringBoneHair))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.SpringBoneHair;

                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics;
                                */
                                break;
                            }
                        case CharacterInfo.ShaderFeatureFlags.SpringBonePhysics:
                            {
                                foreach (CharacterInfo.ShaderFeatureFlags groupFlag in CharacterInfo.springGroup)
                                {
                                    if (contextCharacter.ShaderFlags.HasFlag(groupFlag))
                                        contextCharacter.ShaderFlags ^= groupFlag;
                                }

                                break;
                            }
                    }
                }
            }
        }

        private void SetFeatureFlagInGroup(CharacterInfo.ShaderFeatureFlags flag, CharacterInfo.ShaderFeatureFlags[] radioGroup)
        {
            foreach (CharacterInfo.ShaderFeatureFlags groupFlag in radioGroup)
            {
                SetFeatureFlag(groupFlag, groupFlag.Equals(flag));
            }
        }        
    }
}
