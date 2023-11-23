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
        private float INITIAL_DROPDOWN_HEIGHT = 200f;
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

            //if (Pipeline.isHDRP12) -- HDRP12 tessellation
            //if (Pipeline.is3D || Pipeline.isURP) -- Amplify tessellation
            DrawLabelLine(line++, "Tessellation...");
            if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.Tessellation, "", SECTION_INDENT))
                flagChanged = true;

            DrawLabelLine(line++, "");

            // wrinkle maps            
            DrawLabelLine(line++, "Wrinkle Maps...");
            if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.WrinkleMaps, "", SECTION_INDENT))
                flagChanged = true;

            DrawLabelLine(line++, "");

            // cloth physics
            DrawLabelLine(line++, "Cloth Physics...");
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

            DrawLabelLine(line++, "");

            // hair physics
            DrawLabelLine(line++, "Hair Physics...");
            if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.HairPhysics, "Enable Hair Physics", SECTION_INDENT))
                flagChanged = true;

            if (importerWindow.DynamicBoneAvailable || importerWindow.MagicaCloth2Available) // cloth/bone alternatives available so enable non default selections
            {
                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.HairPhysics))
                {
                    if (DrawFlagSelectionLine(line++, CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics, "Unity Hair Physics", SUB_SECTION_INDENT, CharacterInfo.hairGroup))
                        flagChanged = true;

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

                    // since the section flag is being unset then all the entries should be unset too
                    switch(flag)
                    {
                        case CharacterInfo.ShaderFeatureFlags.ClothPhysics:
                            {
                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.MagicaCloth))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.MagicaCloth;

                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.UnityClothPhysics))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.UnityClothPhysics;

                                break;
                            }
                        case CharacterInfo.ShaderFeatureFlags.HairPhysics:
                            {
                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.MagicaBone))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.MagicaBone;

                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.SpringBoneHair))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.SpringBoneHair;

                                if (contextCharacter.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics))
                                    contextCharacter.ShaderFlags ^= CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics;

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
        /*
        private void EnsureDefaultsAreSet(CharacterInfo.ShaderFeatureFlags flag)
        {
            // if no alternatives are available or the flags are unset - then set unity physics as a default when activating cloth or hair physics
            switch (flag)
            {
                case CharacterInfo.ShaderFeatureFlags.ClothPhysics:
                    {
                        if (!importerWindow.MagicaCloth2Available)
                        {
                            SetFeatureFlag(CharacterInfo.ShaderFeatureFlags.UnityClothPhysics, true);                            
                        }

                        if (!importerWindow.Character.GroupHasFlagSet(CharacterInfo.clothGroup))
                        {
                            SetFeatureFlag(CharacterInfo.ShaderFeatureFlags.UnityClothPhysics, true);
                        }

                        break;
                    }
                case CharacterInfo.ShaderFeatureFlags.HairPhysics:
                    {
                        if (!importerWindow.MagicaCloth2Available && !importerWindow.DynamicBoneAvailable)
                        {
                            SetFeatureFlag(CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics, true);
                        }

                        if (!importerWindow.Character.GroupHasFlagSet(CharacterInfo.hairGroup))
                        {
                            SetFeatureFlag(CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics, true);
                        }

                        break;
                    }
            }
        }

        private bool GroupHasFlagSet(CharacterInfo.ShaderFeatureFlags[] group)
        {
            foreach (CharacterInfo.ShaderFeatureFlags groupFlag in group)
            {
                if (importerWindow.Character.ShaderFlags.HasFlag(groupFlag)) return true;
            }
            return false;
        }
        
        // 'radio groups' of mutually exclusive settings
        public CharacterInfo.ShaderFeatureFlags[] clothGroup =
        {
            CharacterInfo.ShaderFeatureFlags.UnityClothPhysics, // UnityEngine.Cloth instance
            CharacterInfo.ShaderFeatureFlags.MagicaCloth // MagicaCloth2 instance set to 'Mesh Cloth' mode
        };

        public CharacterInfo.ShaderFeatureFlags[] hairGroup =
        {
            CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics, // UnityEngine.Cloth instance for hair objects
            CharacterInfo.ShaderFeatureFlags.SpringBoneHair, // DynamicBone springbones
            CharacterInfo.ShaderFeatureFlags.MagicaBone // MagicaCloth2 instance set to 'Bone Cloth' mode for springbones
        };
        */
    }
}
