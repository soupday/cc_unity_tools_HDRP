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

using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using ColliderSettings = Reallusion.Import.ColliderManager.ColliderSettings;
using System.Linq;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using System.Collections;
using UnityEditor.ShortcutManagement;

namespace Reallusion.Import
{
    [CustomEditor(typeof(ColliderManager))]
    public class ColliderManagerEditor : Editor
    {
        private bool drawAllGizmos = true;
        private bool resetAfterGUI = false;
        private bool recallAfterGUI = false;
        private Styles colliderManagerStyles;

        private ColliderManager colliderManager;
        private ColliderSettings currentCollider;
        private Texture2D editModeEnable, editModeDisable, magicaIcon;
        private Color baseBackground;

        const float LABEL_WIDTH = 80f;
        const float GUTTER = 40f;
        const float BUTTON_WIDTH = 160f;

        [SerializeField] private ColliderManager.GizmoState cachedGizmoState;
        [SerializeField] private bool editMode = false;
        [SerializeField] private bool activeEdit = false;
        public static bool EditMode => Current != null && Current.editMode;
        public static ColliderManagerEditor Current { get; private set; }

        private bool enableStatusDirty = false;

        public static string CURRENT_COLLIDER_NAME
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Current_Collider_Name"))
                    return EditorPrefs.GetString("RL_Current_Collider_Name");
                return "";
            }

            set
            {
                EditorPrefs.SetString("RL_Current_Collider_Name", value);
            }
        }

        private void OnEnable()
        {
            Current = this;
            colliderManager = (ColliderManager)target;
            InitIcons();
            UpdateClothShortcutList();
            //SyncEnableStatus();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void OnDisable()
        {
            //
        }

        private void InitCurrentCollider(string name = null)
        {
            currentCollider = null;

            if (colliderManager.settings.Length > 0)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    foreach (ColliderSettings cs in colliderManager.settings)
                    {
                        if (cs.name == name)
                        {
                            currentCollider = cs;
                            return;
                        }
                    }
                }

                currentCollider = colliderManager.settings[0];
            }
        }

        private void CreateAbstractColliders()
        {
            Physics.CreateAbstractColliders(colliderManager, out colliderManager.abstractedCapsuleColliders);//, out colliderManager.genericColliderList);
        }

        private void InitIcons()
        {
            editModeEnable = Util.FindTexture(new string[] { "Assets", "Packages" }, "RL_Edit_Enable");
            editModeDisable = Util.FindTexture(new string[] { "Assets", "Packages" }, "RL_Edit_Disable");
            //magicaIcon = Util.FindTexture(new string[] { "Assets", "Packages" }, "icon-collider");
            //if (magicaIcon == null)
            //{
            magicaIcon = (Texture2D)EditorGUIUtility.IconContent("CircleCollider2D Icon").image;
            //}
            colliderManager.currentEditType = ColliderManager.ColliderType.Unknown;
            colliderManager.magicaCloth2Available = Physics.MagicaCloth2IsAvailable();
            colliderManager.dynamicBoneAvailable = Physics.DynamicBoneIsAvailable();
        }

        public class Styles
        {
            public GUIStyle sceneLabelText;
            public GUIStyle objectLabelText;
            public GUIStyle normalButton;
            public GUIStyle currentButton;

            public Styles()
            {
                sceneLabelText = new GUIStyle();
                sceneLabelText.normal.textColor = Color.cyan;
                sceneLabelText.fontSize = 18;

                objectLabelText = new GUIStyle();
                objectLabelText.normal.textColor = Color.red;
                objectLabelText.fontSize = 12;

                normalButton = new GUIStyle(GUI.skin.button);
                currentButton = new GUIStyle(GUI.skin.button);
                currentButton.normal.background = TextureColor(new Color(0.3f, 0.3f, 0.63f, 0.5f));
            }
        }

        private void OnSceneGUI()
        {
            //CatchKeyEvents();

            if (colliderManagerStyles == null) colliderManagerStyles = new Styles();

            string selectedName = "";

            if (colliderManager.abstractedCapsuleColliders != null)
            {
                foreach (ColliderManager.AbstractCapsuleCollider c in colliderManager.abstractedCapsuleColliders)
                {
                    // Color drawCol = c == colliderManager.selectedAbstractCapsuleCollider ? Color.red : new Color(0.60f, 0.9f, 0.60f);
                    Color drawCol = c == colliderManager.selectedAbstractCapsuleCollider ? Color.red : Color.cyan;
                    if (colliderManager.selectedAbstractCapsuleCollider == c)
                    {
                        selectedName = c.name;
                        //small floating annotation near the collider                        
                        Handles.Label(c.transform.position + Vector3.up * 0.1f + Vector3.left * 0.1f, c.name, colliderManagerStyles.objectLabelText);
                        if (c.isEnabled)
                            DrawWireCapsule(c.transform.position, c.transform.rotation, c.radius, c.height, c.axis, drawCol);
                        switch (colliderManager.manipulator)
                        {
                            case ColliderManager.ManipulatorType.position:
                                {
                                    Vector3 targetPosition = c.transform.position;
                                    EditorGUI.BeginChangeCheck();
                                    targetPosition = Handles.PositionHandle(targetPosition, c.transform.rotation);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        if (!ColliderManager.AbstractCapsuleCollider.IsNullOrEmpty(colliderManager.mirrorImageAbstractCapsuleCollider))
                                        {
                                            Vector3 delta = c.transform.parent.InverseTransformPoint(targetPosition) - c.transform.parent.InverseTransformPoint(c.transform.position);
                                            Quaternion inv = Quaternion.Inverse(c.transform.localRotation);
                                            Vector3 diff = inv * delta;
                                            colliderManager.UpdateColliderFromAbstract(c.transform.localPosition, c.transform.localRotation);
                                        }
                                        c.transform.position = targetPosition;
                                    }
                                    break;
                                }
                            case ColliderManager.ManipulatorType.rotation:
                                {
                                    Quaternion targetRotation = c.transform.rotation;
                                    Quaternion currentLocalRotation = c.transform.localRotation;
                                    EditorGUI.BeginChangeCheck();
                                    targetRotation = Handles.RotationHandle(targetRotation, c.transform.position);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Quaternion targetLocalRotation = Quaternion.Inverse(c.transform.parent.rotation) * targetRotation;
                                        if (!ColliderManager.AbstractCapsuleCollider.IsNullOrEmpty(colliderManager.mirrorImageAbstractCapsuleCollider))
                                        {
                                            Vector3 rDiff = targetLocalRotation.eulerAngles - currentLocalRotation.eulerAngles;
                                            colliderManager.UpdateColliderFromAbstract(c.transform.localPosition, targetLocalRotation);
                                        }
                                        c.transform.rotation = targetRotation;
                                    }

                                    break;
                                }
                            case ColliderManager.ManipulatorType.scale:
                                {
                                    Handles.color = Color.green;
                                    EditorGUI.BeginChangeCheck();
                                    float h = c.height;
                                    float r = c.radius;

                                    Vector3 hDir, rDir, r2Dir;
                                    if (c.axis == ColliderManager.ColliderAxis.y)
                                    {
                                        hDir = c.transform.up;
                                        rDir = c.transform.forward;
                                        r2Dir = c.transform.right;
                                    }
                                    else if (c.axis == ColliderManager.ColliderAxis.z)
                                    {
                                        hDir = c.transform.forward;
                                        rDir = c.transform.up;
                                        r2Dir = c.transform.right;
                                    }
                                    else // x
                                    {
                                        hDir = c.transform.right;
                                        rDir = c.transform.forward;
                                        r2Dir = c.transform.up;
                                    }
                                    hDir *= h * 0.5f;
                                    rDir *= r;
                                    r2Dir *= r;
                                    Vector3 hDelta = SceneView.lastActiveSceneView.camera.WorldToViewportPoint(c.transform.position + hDir) -
                                                     SceneView.lastActiveSceneView.camera.WorldToViewportPoint(c.transform.position);
                                    Vector3 rDelta = SceneView.lastActiveSceneView.camera.WorldToViewportPoint(c.transform.position + rDir) -
                                                     SceneView.lastActiveSceneView.camera.WorldToViewportPoint(c.transform.position);
                                    Vector3 r2Delta = SceneView.lastActiveSceneView.camera.WorldToViewportPoint(c.transform.position + r2Dir) -
                                                      SceneView.lastActiveSceneView.camera.WorldToViewportPoint(c.transform.position);
                                    if (Mathf.Abs(r2Delta.x) > Mathf.Abs(rDelta.x))
                                    {
                                        rDelta = r2Delta;
                                        rDir = r2Dir;
                                    }

                                    float hSign = 1f;
                                    float rSign = 1f;
                                    if (Mathf.Abs(hDelta.x) > Mathf.Abs(hDelta.y)) hSign = -Mathf.Sign(hDelta.x);
                                    else hSign = -Mathf.Sign(hDelta.y);
                                    if (Mathf.Abs(rDelta.x) > Mathf.Abs(rDelta.y)) rSign = Mathf.Sign(rDelta.x);
                                    else rSign = Mathf.Sign(rDelta.y);
                                    Vector3 hPos = c.transform.position - (hDir * hSign);
                                    h = Handles.ScaleValueHandle(h, hPos,
                                                                 c.transform.rotation * Quaternion.Euler(90, 0, 0),
                                                                 HandleUtility.GetHandleSize(hPos),
                                                                 Handles.DotHandleCap, 1);

                                    Handles.DrawWireArc(c.transform.position,
                                                        c.transform.up,
                                                        -c.transform.right,
                                                        180,
                                                        r);

                                    Vector3 rPos = c.transform.position + (rDir * rSign);
                                    r = Handles.ScaleValueHandle(r, rPos,
                                                                 c.transform.rotation,
                                                                 HandleUtility.GetHandleSize(rPos),
                                                                 Handles.DotHandleCap, 1);

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        c.radius = r;
                                        c.height = h;
                                        colliderManager.UpdateColliderFromAbstract(c.transform.localPosition, c.transform.localRotation);
                                    }
                                    break;
                                }
                        }
                    }
                    else
                    {
                        if (Selection.objects.Contains(colliderManager.gameObject))
                        {
                            drawAllGizmos = false;
                            if (colliderManager.mirrorImageAbstractCapsuleCollider == c)
                                DrawWireCapsule(c.transform.position, c.transform.rotation, c.radius, c.height, c.axis, Color.magenta);
                        }
                        else
                            drawAllGizmos = true;

                        if (drawAllGizmos)
                        {
                            if (c.isEnabled)
                            {
                                if (colliderManager.mirrorImageAbstractCapsuleCollider == c) drawCol = Color.magenta;
                                DrawWireCapsule(c.transform.position, c.transform.rotation, c.radius, c.height, c.axis, drawCol);
                            }
                        }
                    }
                }
                // always writes screen text when in edit mode or a collider is selected for editing
                if (activeEdit || editMode)
                {
                    //large fixed text on the scene view 
                    Handles.BeginGUI();
                    string lockString = ActiveEditorTracker.sharedTracker.isLocked ? "Locked" : "Unlocked";
                    string modeString = colliderManager.manipulatorArray[(int)colliderManager.manipulator];
                    string displayString = "Inspector Status: " + lockString + "\nSelected Collider: " + selectedName + "\nMode: " + modeString;

                    GUI.Label(new Rect(55, 7, 1000, 1000), displayString, colliderManagerStyles.sceneLabelText);
                    Handles.EndGUI();
                }
            }
        }

        public void CatchKeyEvents()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyUp)
            {

            }

            if (e.type == EventType.KeyDown)
            {

            }
        }

        public void SyncMode()
        {
            switch (Tools.current)
            {
                case Tool.Move:
                    {
                        if (colliderManager.manipulator != ColliderManager.ManipulatorType.position)
                        {
                            colliderManager.manipulator = ColliderManager.ManipulatorType.position;
                        }
                        break;
                    }
                case Tool.Rotate:
                    {
                        if (colliderManager.manipulator != ColliderManager.ManipulatorType.rotation)
                        {
                            colliderManager.manipulator = ColliderManager.ManipulatorType.rotation;
                        }
                        break;
                    }
                case Tool.Scale:
                    {
                        if (colliderManager.manipulator != ColliderManager.ManipulatorType.scale)
                        {
                            colliderManager.manipulator = ColliderManager.ManipulatorType.scale;
                        }
                        break;
                    }
            }
        }

        public void SyncEnableStatus()
        {
            if (colliderManager.clothMeshes != null && colliderManager.clothMeshes.Length > 0)
            {
                //Debug.Log("Syncing UnityCloth Enabled Status");
                foreach (ColliderManager.EnableStatusGameObject clothMesh in colliderManager.clothMeshes)
                {
                    Cloth c = clothMesh.GameObject.GetComponent<Cloth>();
                    if (c != null) {
                        clothMesh.EnabledStatus = c.enabled;
                    }
                }
            }

            if (colliderManager.magicaCloth2Available)
            {
                if (colliderManager.magicaClothMeshes != null && colliderManager.magicaClothMeshes.Length > 0)
                {
                    //Debug.Log("Syncing Magica Cloth Enabled Status");
                    foreach (ColliderManager.EnableStatusGameObject clothMesh in colliderManager.magicaClothMeshes)
                    {
                        clothMesh.EnabledStatus = Physics.GetMagicaComponentEnableStatus(clothMesh.GameObject);
                    }
                }
            }
        }

        private void DrawClothShortcutUpdate()
        {
            if (GUILayout.Button("Refresh"))
            {
                UpdateClothShortcutList();
            }
        }
        public void UpdateClothShortcutList()
        {
            if (colliderManager.magicaCloth2Available)
            {
                Type clothType = Physics.GetTypeInAssemblies("MagicaCloth2.MagicaCloth");
                if (clothType != null)
                {
                    Component[] magicaClothInstances = colliderManager.gameObject.GetComponentsInChildren(clothType);
                    List<ColliderManager.EnableStatusGameObject> magicaList = new List<ColliderManager.EnableStatusGameObject>();
                    foreach(Component magicaComponent in magicaClothInstances)
                    {
                        GameObject g = magicaComponent.gameObject;
                        magicaList.Add(new ColliderManager.EnableStatusGameObject(g, Physics.GetMagicaComponentEnableStatus(g)));
                    }
                    colliderManager.magicaClothMeshes = magicaList.ToArray();
                }
            }

            Component[] unityClothInstances = colliderManager.gameObject.GetComponentsInChildren<Cloth>();
            List<ColliderManager.EnableStatusGameObject> nativeList = new List<ColliderManager.EnableStatusGameObject>();
            foreach (Component clothComponent in unityClothInstances)
            {
                GameObject g = clothComponent.gameObject;
                nativeList.Add(new ColliderManager.EnableStatusGameObject(g, Physics.GetComponentEnabled(clothComponent)));
            }
            colliderManager.clothMeshes = nativeList.ToArray();
        }

        public override void OnInspectorGUI()
        {
            //CatchKeyEvents();
            SyncMode();

            if (colliderManager.abstractedCapsuleColliders == null ||
                colliderManager.abstractedCapsuleColliders.Count == 0)
            {
                CreateAbstractColliders();
            }
            if (editModeEnable == null) InitIcons();
            if (colliderManagerStyles == null) colliderManagerStyles = new Styles();

            baseBackground = GUI.backgroundColor;
            base.OnInspectorGUI();


            DrawShortCutSave();


            DrawEditAssistBlock();
            //DrawColliderSetSelector();
            if (editMode)
            {
                DrawColliderSelectionBlock();
                DrawStoreControls();
            }

            if (colliderManager.clothMeshes != null && colliderManager.clothMeshes.Length > 0)
            {
                DrawUnityClothShortcuts();
            }

            if (colliderManager.magicaClothMeshes != null && colliderManager.magicaClothMeshes.Length > 0)
            {
                DrawMagicaClothShortcuts();
            }

            //DrawClothShortcutUpdate();

            if (resetAfterGUI)
            {
                // optional: deselect the collider for editing
                bool deSelectChar = false;
                if (deSelectChar)
                {
                    DeSelectColliderForEdit();
                }

                // reset the collider to the cached values
                colliderManager.ResetColliderFromCache();

                SceneView.RepaintAll();
                resetAfterGUI = false;
            }

            if (recallAfterGUI)
            {
                PhysicsSettingsStore.RecallAbstractColliderSettings(colliderManager, false);
                recallAfterGUI = false;
            }
        }

        private void DrawEditAssistBlock()
        {
            GUILayout.Space(10f);
            GUILayout.Label("Collider Edit Mode", EditorStyles.boldLabel);
            GUILayout.Space(10f);
            GUI.backgroundColor = editMode ? Color.Lerp(baseBackground, Color.green, 0.9f) : baseBackground;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = baseBackground;

            GUILayout.BeginVertical();
            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10f);

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();

            // Icons from <a target="_blank" href="https://icons8.com/icon/1CDroSc0Up0D/wrench">Wrench</a> icon by <a target="_blank" href="https://icons8.com">Icons8</a>

            editMode = ActiveEditorTracker.sharedTracker.isLocked;
            //string lookIcon = locked ? "d_SceneViewVisibility" : "ViewToolOrbit";
            //Texture2D lookIconImage = (Texture2D)EditorGUIUtility.IconContent(lookIcon).image;
            Texture2D lookIconImage = editMode ? editModeDisable : editModeEnable;
            if (GUILayout.Button(new GUIContent(lookIconImage, (editMode ? "EXIT from" : "ENTER") + " Collider Edit Mode.\n" + (editMode ? "This will UNLOCK the inspctor and reselect the character - drawing all the default gizmos" : "This will LOCK the inspector and deselect the character - showing only the gizmos of editable colliders and preventing loss of focus on the character.")), GUILayout.Width(48f), GUILayout.Height(48f)))
            {
                if (!editMode)
                {
                    SetEditAssistMode();
                }
                else
                {
                    UnSetEditAssistMode();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUIStyle wrap = new GUIStyle(GUI.skin.button);
            wrap.wordWrap = true;
            string editModeInactiveText = "Collider Edit Mode allows convenient editing of the phycics colliders. This will LOCK the inspector to the character and an only draw the gizmos for the editable colliders.";  //edit removed: This will provide a less cluttered view and avoid loss of character focus causing issues.
            string editModeActiveText = "Collider Edit Mode is currently ACTIVE: The inspector is currently locked to the character. Click the button to deactivate.";
            EditorGUILayout.HelpBox(editMode ? editModeActiveText : editModeInactiveText, MessageType.Info, true);

            GUILayout.Space(10f);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.EndVertical(); //(EditorStyles.helpBox);
        }

        public void SetEditAssistMode()
        {
            CreateAbstractColliders();
            Tools.hidden = true;
            editMode = true;
            if (colliderManager != null)
            {
                if (colliderManager.selectedAbstractCapsuleCollider != null)
                {
                    if (colliderManager.selectedAbstractCapsuleCollider.transform != null)
                        FocusPosition(colliderManager.selectedAbstractCapsuleCollider.transform.position);
                }
            }
            Selection.activeObject = colliderManager.gameObject;
            ActiveEditorTracker.sharedTracker.isLocked = editMode;
            //ActiveEditorTracker.sharedTracker.ForceRebuild();
            SetGizmos();
            Selection.activeObject = null;
            SceneView.RepaintAll();
            if (EditorApplication.isPlaying)
            {
                WindowManager.GrabLastSceneFocus();
            }
        }

        public void UnSetEditAssistMode()
        {
            Tools.hidden = false;
            // optional: deselect the collider for editing
            bool deSelectChar = false;
            if (deSelectChar)
            {
                DeSelectColliderForEdit();
            }

            editMode = false;
            ActiveEditorTracker.sharedTracker.isLocked = false;
            //ActiveEditorTracker.sharedTracker.ForceRebuild();
            ResetGizmos();
            Selection.activeObject = colliderManager.gameObject;
            SceneView.RepaintAll();
        }

        private void DrawColliderSetSelector()
        {
            GUILayout.Space(10f);
            GUILayout.Label("Collider Type Select", EditorStyles.boldLabel);
            GUILayout.Space(10f);
            GUI.backgroundColor = editMode ? Color.Lerp(baseBackground, Color.green, 0.9f) : baseBackground;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = baseBackground;

            GUILayout.BeginHorizontal(); // controls + text
            GUILayout.Space(10f);

            GUILayout.BeginVertical(); // controls group for vertical centering
            GUILayout.Space(10f);  // enforce a minimum upper border
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(); // controls group horizontal layout
            float iconSize = 36f;

            bool active = colliderManager.currentEditType.HasFlag(ColliderManager.ColliderType.UnityEngine);
            GUI.backgroundColor = active ? Color.Lerp(baseBackground, Color.blue, 0.35f) : baseBackground;
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("CapsuleCollider Icon").image, "Native UnityEngine colliders"), GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
            {
                if (active)
                    colliderManager.currentEditType ^= ColliderManager.ColliderType.UnityEngine;
                else
                    colliderManager.currentEditType |= ColliderManager.ColliderType.UnityEngine;
            }
            GUI.backgroundColor = baseBackground;

            GUILayout.Space(4f);

            active = colliderManager.currentEditType.HasFlag(ColliderManager.ColliderType.MagicaCloth2);
            GUI.backgroundColor = active ? Color.Lerp(baseBackground, Color.blue, 0.35f) : baseBackground;
            if (GUILayout.Button(new GUIContent(magicaIcon, "Magica Cloth 2 colliders"), GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
            {
                if (active)
                    colliderManager.currentEditType ^= ColliderManager.ColliderType.MagicaCloth2;
                else
                    colliderManager.currentEditType |= ColliderManager.ColliderType.MagicaCloth2;
            }
            GUI.backgroundColor = baseBackground;

            GUILayout.Space(4f);

            active = colliderManager.currentEditType.HasFlag(ColliderManager.ColliderType.DynamicBone);
            GUI.backgroundColor = active ? Color.Lerp(baseBackground, Color.blue, 0.35f) : baseBackground;
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("FixedJoint Icon").image, "Dynamic Bone colliders"), GUILayout.Width(iconSize), GUILayout.Height(iconSize)))
            {
                if (active)
                    colliderManager.currentEditType ^= ColliderManager.ColliderType.DynamicBone;
                else
                    colliderManager.currentEditType |= ColliderManager.ColliderType.DynamicBone;
            }
            GUI.backgroundColor = baseBackground;

            GUILayout.Space(4f);
            GUILayout.EndHorizontal(); // controls group horizontal layout

            GUILayout.FlexibleSpace();
            GUILayout.Space(10f); // enforce a minimum lower border
            GUILayout.EndVertical(); // controls group for vertical centering


            GUILayout.BeginVertical(); // text for vertical centering
            GUILayout.FlexibleSpace();

            string labelText = WriteLabelText(colliderManager.currentEditType);

            GUILayout.Label(labelText);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical(); // text for vertical centering
            GUILayout.FlexibleSpace(); // left justify the text area
            GUILayout.EndHorizontal(); // controls + text

            GUILayout.EndVertical(); // (EditorStyles.helpBox);
        }

        private string WriteLabelText(ColliderManager.ColliderType source)
        {
            string labelText = "";
            bool newlineNeeded = false;

            if (source.HasFlag(ColliderManager.ColliderType.UnityEngine))
            {
                labelText += "Native UnityEngine";
                newlineNeeded = true;
            }

            if (source.HasFlag(ColliderManager.ColliderType.MagicaCloth2))
            {
                if (newlineNeeded) labelText += "\n";
                labelText += "Magica Cloth 2";
                newlineNeeded = true;
            }

            if (source.HasFlag(ColliderManager.ColliderType.DynamicBone))
            {
                if (newlineNeeded) labelText += "\n";
                labelText += "DynamicBone";
                newlineNeeded = true;
            }

            return labelText;
        }

        private void DrawColliderSelectionBlock()
        {
            GUILayout.Space(10f);
            GUILayout.Label("Adjust Colliders", EditorStyles.boldLabel);
            GUILayout.Space(10f);
            GUI.backgroundColor = editMode ? Color.Lerp(baseBackground, Color.green, 0.9f) : baseBackground;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = baseBackground;

            GUILayout.BeginVertical();
            GUILayout.Space(10f);
            if (colliderManager.abstractedCapsuleColliders != null)
            {
                foreach (ColliderManager.AbstractCapsuleCollider c in colliderManager.abstractedCapsuleColliders)
                {
                    bool active = (c == colliderManager.selectedAbstractCapsuleCollider);
                    GUILayout.BeginVertical();
                    GUILayout.Space(active ? 1f : 0f);
                    GUILayout.BeginHorizontal();

                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginDisabledGroup(!c.isEnabled);
                    GUI.backgroundColor = active ? Color.Lerp(baseBackground, Color.blue, 0.35f) : baseBackground;
                    if (GUILayout.Button(c.name, GUILayout.MaxWidth(250f)))
                    //if (GUILayout.Button(c.name, (active ? colliderManagerStyles.currentButton : colliderManagerStyles.normalButton), GUILayout.MaxWidth(250f)))
                    {
                        SelectColliderForEdit(c);
                    }
                    GUI.backgroundColor = baseBackground;
                    EditorGUI.EndDisabledGroup();
                    // off button
                    GUILayout.Space(4f);
                    EditorGUI.BeginChangeCheck();
                    c.isEnabled = GUILayout.Toggle(c.isEnabled, "");
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (c.isEnabled)
                        {
                            c.transform.gameObject.SetActive(true);
                            if (colliderManager.transformSymmetrically)
                            {
                                ColliderManager.AbstractCapsuleCollider m = DetermineMirrorImageCollider(c);
                                if (m != null)
                                {
                                    m.isEnabled = true;
                                    m.transform.gameObject.SetActive(true);
                                }
                            }
                        }
                        else
                        {
                            c.transform.gameObject.SetActive(false);
                            if (colliderManager.transformSymmetrically)
                            {
                                ColliderManager.AbstractCapsuleCollider m = DetermineMirrorImageCollider(c);
                                if (m != null)
                                {
                                    m.isEnabled = false;
                                    m.transform.gameObject.SetActive(false);
                                }
                            }
                            DeSelectColliderForEdit();
                        }
                        SceneView.RepaintAll();
                    }
                    // end of off button

                    GUILayout.FlexibleSpace();

                    GUILayout.EndHorizontal();

                    if (active)
                    {
                        GUILayout.Space(0f);
                        DrawEditModeControls();
                    }

                    GUILayout.EndVertical();
                    if (active)
                        GUILayout.Space(1f);


                }
            }
            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10f);

            EditorGUI.BeginChangeCheck();
            colliderManager.transformSymmetrically = GUILayout.Toggle(colliderManager.transformSymmetrically, new GUIContent("Symmetrical Transformation"));
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
                if (!ColliderManager.AbstractCapsuleCollider.IsNullOrEmpty(colliderManager.selectedAbstractCapsuleCollider))
                {
                    colliderManager.mirrorImageAbstractCapsuleCollider = DetermineMirrorImageCollider(colliderManager.selectedAbstractCapsuleCollider);
                    FocusPosition(colliderManager.selectedAbstractCapsuleCollider.transform.position);
                }
            }
            GUILayout.Space(10f);
            EditorGUI.BeginChangeCheck();
            colliderManager.frameSymmetryPair = GUILayout.Toggle(colliderManager.frameSymmetryPair, new GUIContent("Frame Pair"));
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
                if (!ColliderManager.AbstractCapsuleCollider.IsNullOrEmpty(colliderManager.selectedAbstractCapsuleCollider))
                {
                    colliderManager.mirrorImageAbstractCapsuleCollider = DetermineMirrorImageCollider(colliderManager.selectedAbstractCapsuleCollider);
                    FocusPosition(colliderManager.selectedAbstractCapsuleCollider.transform.position);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }

        private void SelectColliderForEdit(ColliderManager.AbstractCapsuleCollider c)
        {
            //SetGizmos();
            activeEdit = true;
            if (!SceneView.lastActiveSceneView.drawGizmos && !editMode)
                SceneView.lastActiveSceneView.drawGizmos = true;
            colliderManager.selectedAbstractCapsuleCollider = c;
            colliderManager.mirrorImageAbstractCapsuleCollider = DetermineMirrorImageCollider(c);
            colliderManager.CacheCollider(colliderManager.selectedAbstractCapsuleCollider, colliderManager.mirrorImageAbstractCapsuleCollider);

            if (AnimPlayerGUI.IsPlayerShown())
            {
                AnimPlayerGUI.ForbidTracking();
            }

            FocusPosition(colliderManager.selectedAbstractCapsuleCollider.transform.position);

            SceneView.RepaintAll();
        }

        private void DeSelectColliderForEdit()
        {
            if (AnimPlayerGUI.IsPlayerShown())
            {
                AnimPlayerGUI.AllowTracking();
            }

            colliderManager.selectedAbstractCapsuleCollider = null;
            colliderManager.mirrorImageAbstractCapsuleCollider = null;
            activeEdit = false;

            SceneView.RepaintAll();
        }

        public void DrawEditModeControls()
        {
            // centralize controls
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // button strip to match selection button width
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(270f));
            GUILayout.Space(10f);

            GUI.backgroundColor = colliderManager.manipulator == ColliderManager.ManipulatorType.position ? Color.Lerp(baseBackground, Color.blue, 0.35f) : baseBackground;
            //GUIStyle style = (colliderManager.manipulator == ColliderManager.ManipulatorType.position ? colliderManagerStyles.currentButton : colliderManagerStyles.normalButton);
            //if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_MoveTool on").image, "Transform position tool"), style, GUILayout.Width(30f)))
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_MoveTool on").image, "Transform position tool"), GUILayout.Width(30f)))
            {
                colliderManager.manipulator = ColliderManager.ManipulatorType.position;
                Tools.current = Tool.Move;
                //SceneView.RepaintAll();
            }
            GUI.backgroundColor = baseBackground;

            GUI.backgroundColor = colliderManager.manipulator == ColliderManager.ManipulatorType.rotation ? Color.Lerp(baseBackground, Color.blue, 0.35f) : baseBackground;
            //style = (colliderManager.manipulator == ColliderManager.ManipulatorType.rotation ? colliderManagerStyles.currentButton : colliderManagerStyles.normalButton);
            //if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_RotateTool On").image, "Transform rotation tool"), style, GUILayout.Width(30f)))
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_RotateTool On").image, "Transform rotation tool"), GUILayout.Width(30f)))
            {
                colliderManager.manipulator = ColliderManager.ManipulatorType.rotation;
                Tools.current = Tool.Rotate;
                //SceneView.RepaintAll();
            }
            GUI.backgroundColor = baseBackground;

            GUI.backgroundColor = colliderManager.manipulator == ColliderManager.ManipulatorType.scale ? Color.Lerp(baseBackground, Color.blue, 0.35f) : baseBackground;
            //style = (colliderManager.manipulator == ColliderManager.ManipulatorType.scale ? colliderManagerStyles.currentButton : colliderManagerStyles.normalButton);
            //if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("ScaleTool On").image, "Transform scale tool"), style, GUILayout.Width(30f)))
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("ScaleTool On").image, "Transform scale tool"), GUILayout.Width(30f)))
            {
                colliderManager.manipulator = ColliderManager.ManipulatorType.scale;
                Tools.current = Tool.Scale;
                //SceneView.RepaintAll();
            }
            GUI.backgroundColor = baseBackground;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_TreeEditor.Trash").image, "Undo Changes"), colliderManagerStyles.normalButton, GUILayout.Width(30f)))
            {
                resetAfterGUI = true;
            }

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_TreeEditor.Refresh").image, "Reset Collider To Default"), colliderManagerStyles.normalButton, GUILayout.Width(30f)))
            {
                colliderManager.ResetSingleAbstractCollider(PhysicsSettingsStore.RecallAbstractColliderSettings(colliderManager, true), colliderManager.selectedAbstractCapsuleCollider.name, colliderManager.transformSymmetrically);
            }

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_clear").image, "Only Deselect Collider"), colliderManagerStyles.normalButton, GUILayout.Width(30f)))
            {
                DeSelectColliderForEdit();
            }
            GUILayout.Space(10f);
            GUILayout.EndHorizontal();

            // off button
            GUILayout.Space(24f);
            // end of off button

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public void DrawStoreControls()
        {
            GUILayout.Space(10f);
            GUILayout.Label("Save and Recall Colliders", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            GUI.backgroundColor = editMode ? Color.Lerp(baseBackground, Color.green, 0.9f) : baseBackground;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = baseBackground;
            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.Lerp(baseBackground, Color.red, 0.25f);
            GUIContent saveLabel = new GUIContent("Save Settings", "Save the current collider settings to disk - this will overwrite any previously saved settings");
            if (GUILayout.Button(saveLabel, GUILayout.Width(120f), GUILayout.MinWidth(100f)))
            {
                PhysicsSettingsStore.SaveAbstractColliderSettings(colliderManager, colliderManager.abstractedCapsuleColliders);
            }
            GUI.backgroundColor = baseBackground;
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.Lerp(baseBackground, Color.yellow, 0.25f);
            GUIContent recallLabel = new GUIContent("Recall Settings", "Recall any previously saved collider settings");
            if (GUILayout.Button(recallLabel, GUILayout.Width(120f), GUILayout.MinWidth(100f)))
            {
                colliderManager.ResetAbstractColliders(PhysicsSettingsStore.RecallAbstractColliderSettings(colliderManager, false));
                Repaint();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = baseBackground;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (Application.isPlaying) GUI.enabled = false;
            GUI.backgroundColor = Color.Lerp(baseBackground, Color.magenta, 0.25f);
            GUIContent resetLabel = new GUIContent("Reset All to Defaults", "This will retrieve and apply the default collider layout to the character.");
            if (GUILayout.Button(resetLabel, GUILayout.Width(BUTTON_WIDTH)))
            {
                colliderManager.ResetAbstractColliders(PhysicsSettingsStore.RecallAbstractColliderSettings(colliderManager, true));
                Repaint();
                SceneView.RepaintAll();
            }
            GUI.enabled = true;
            GUI.backgroundColor = baseBackground;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            /*
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (Application.isPlaying) GUI.enabled = false;
            GUI.backgroundColor = Color.Lerp(baseBackground, Color.cyan, 0.25f);
            GUIContent applyLabel = new GUIContent("Save Changes to Prefab", "Save ALL changes to the character prefab.\nThis includes the current collider settings.");
            if (GUILayout.Button(applyLabel, GUILayout.Width(BUTTON_WIDTH)))
            {
                CommitPrefab(colliderManager);
            }
            GUI.enabled = true;
            GUI.backgroundColor = baseBackground;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            */
            GUILayout.Space(10f);

            GUILayout.EndVertical();// (EditorStyles.helpBox);
        }

        public void CommitPrefab(Object obj)
        {
            WindowManager.HideAnimationPlayer(true);
            WindowManager.HideAnimationRetargeter(true);

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (prefabRoot)
            {
                // save prefab asset
                PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
                Debug.Log("ALL changes have been comitted to the character prefab.");
                enableStatusDirty = false;
            }
        }

        public void DrawUnityClothShortcuts()
        {
            GUILayout.Space(10f);
            GUILayout.Label("Available Unity Cloth Meshes", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            GUI.backgroundColor = editMode ? Color.Lerp(baseBackground, Color.green, 0.9f) : baseBackground;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = baseBackground;
            GUILayout.Space(10f);

            GUILayout.BeginVertical();

            GUI.backgroundColor = Color.Lerp(baseBackground, Color.blue, 0.20f);
            if (colliderManager.clothMeshes != null)
            {
                foreach (ColliderManager.EnableStatusGameObject clothMesh in colliderManager.clothMeshes)
                {
                    if (clothMesh.GameObject != null)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(clothMesh.GameObject.name, GUILayout.Width(160f)))
                        {
                            Selection.activeObject = clothMesh.GameObject;
                        }
                        GUILayout.Space(4f);
                        GUILayout.BeginVertical();
                        GUILayout.Space(4f);
                        EditorGUI.BeginChangeCheck();
                        clothMesh.EnabledStatus = GUILayout.Toggle(clothMesh.EnabledStatus, "");
                        if (EditorGUI.EndChangeCheck())
                        {
                            Cloth cloth = clothMesh.GameObject.GetComponent<Cloth>();
                            if (cloth != null)
                                cloth.enabled = clothMesh.EnabledStatus;
                            /*
                            WeightMapper weightMapper = clothMesh.GameObject.GetComponent<WeightMapper>();
                            if (weightMapper != null)
                                weightMapper.enabled = clothMesh.EnabledStatus;
                            */
                            enableStatusDirty = true;
                        }
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(4f);
                    }
                }
            }
            GUI.backgroundColor = baseBackground;
            GUILayout.EndVertical();
            GUILayout.Space(10f);
            GUILayout.EndVertical();// (EditorStyles.helpBox);
        }

        public void DrawMagicaClothShortcuts()
        {
            GUILayout.Space(10f);
            GUILayout.Label("Available Magica Cloth Meshes", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            GUI.backgroundColor = editMode ? Color.Lerp(baseBackground, Color.green, 0.9f) : baseBackground;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = baseBackground;
            GUILayout.Space(10f);

            GUILayout.BeginVertical();

            GUI.backgroundColor = Color.Lerp(baseBackground, Color.green, 0.25f);
            if (colliderManager.magicaClothMeshes != null)
            {
                foreach (ColliderManager.EnableStatusGameObject clothMesh in colliderManager.magicaClothMeshes)
                {
                    if (clothMesh.GameObject != null)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(clothMesh.GameObject.name, GUILayout.Width(160f)))
                        {
                            Selection.activeObject = clothMesh.GameObject;
                        }
                        GUILayout.Space(4f);
                        GUILayout.BeginVertical();
                        GUILayout.Space(4f);
                        EditorGUI.BeginChangeCheck();
                        clothMesh.EnabledStatus = GUILayout.Toggle(clothMesh.EnabledStatus, "");
                        if (EditorGUI.EndChangeCheck())
                        {
                            Physics.SetMagicaComponentEnableStatus(clothMesh.GameObject, clothMesh.EnabledStatus);
                            enableStatusDirty = true;
                        }
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(4f);
                    }
                }
            }
            GUI.backgroundColor = baseBackground;
            GUILayout.EndVertical();
            GUILayout.Space(10f);
            GUILayout.EndVertical();// (EditorStyles.helpBox);
        }

        private void DrawShortCutSave()
        {
            GUILayout.Space(10f);
            GUILayout.Label("Save Prefab", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            GUI.backgroundColor = editMode ? Color.Lerp(baseBackground, Color.green, 0.9f) : baseBackground;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = baseBackground;
            GUILayout.Space(10f);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (Application.isPlaying) GUI.enabled = false;
            GUI.backgroundColor = enableStatusDirty ? Color.Lerp(baseBackground, Color.cyan, 0.35f) : Color.Lerp(baseBackground, Color.cyan, 0.15f);
            string toolTip = "Save ALL changes to the character prefab." + (enableStatusDirty ? "\nChanges have been made to the enable status of the cloth components.\nThese can be saved to the prefab here." : "");
            GUIContent applyLabel = new GUIContent("Save Changes to Prefab", toolTip);
            if (GUILayout.Button(applyLabel, GUILayout.Width(BUTTON_WIDTH)))
            {
                //save to prefab
                CommitPrefab(colliderManager);
            }
            GUI.enabled = true;
            GUI.backgroundColor = baseBackground;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(10f);
            GUILayout.EndVertical();// (EditorStyles.helpBox);
        }

        // see: https://forum.unity.com/threads/drawing-capsule-gizmo.354634/#post-4100557
        public static void DrawWireCapsule(Vector3 _pos, Quaternion _rot, float _radius, float _height, ColliderManager.ColliderAxis _axis, Color _color = default(Color))
        {
			if (_axis == ColliderManager.ColliderAxis.z)
				_rot = _rot * Quaternion.AngleAxis(90f, Vector3.right);
            if (_color != default(Color))
                Handles.color = _color;
            Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, _rot, Handles.matrix.lossyScale);
            using (new Handles.DrawingScope(angleMatrix))
            {
                var pointOffset = (_height - (_radius * 2)) / 2;

                //draw sideways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, _radius);
                Handles.DrawLine(new Vector3(0, pointOffset, -_radius), new Vector3(0, -pointOffset, -_radius));
                Handles.DrawLine(new Vector3(0, pointOffset, _radius), new Vector3(0, -pointOffset, _radius));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, _radius);
                //draw frontways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, _radius);
                Handles.DrawLine(new Vector3(-_radius, pointOffset, 0), new Vector3(-_radius, -pointOffset, 0));
                Handles.DrawLine(new Vector3(_radius, pointOffset, 0), new Vector3(_radius, -pointOffset, 0));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, _radius);
                //draw center
                Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, _radius);
                Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, _radius);

            }
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

        public void FocusPosition(Vector3 pos)
        {
			Bounds framingBounds;
            float mult = 0.35f;
            
            if (colliderManager.transformSymmetrically && colliderManager.frameSymmetryPair && !ColliderManager.AbstractCapsuleCollider.IsNullOrEmpty(colliderManager.mirrorImageAbstractCapsuleCollider))
            {
                Vector3 diff = colliderManager.mirrorImageAbstractCapsuleCollider.transform.position + colliderManager.selectedAbstractCapsuleCollider.transform.position;
                Vector3 mid = diff / 2;
                float mag = diff.magnitude;

                if (mag > 2)
                    mult = mag * 0.15f;
                else
                    mult = mag * 0.4f;

                framingBounds = new Bounds(mid, Vector3.one * mult);
            }
            else
                framingBounds = new Bounds(pos, Vector3.one * mult);

            SceneView.lastActiveSceneView.Frame(framingBounds, false);
            //SceneView.lastActiveSceneView.rotation = Quaternion.Euler(180f, 0f, 180f);
        }

        private ColliderManager.AbstractCapsuleCollider DetermineMirrorImageCollider(ColliderManager.AbstractCapsuleCollider collider)
        {
            if (!colliderManager.transformSymmetrically) { return null; }

            if (colliderManager.DetermineMirrorImageColliderName(collider.name, out string mirrorName, out colliderManager.selectedMirrorPlane))
                return colliderManager.abstractedCapsuleColliders.Find(x => x.name == mirrorName);
            else
                return null;
        }

        public void SetGizmos()
		{
            // turn on gizmo display (if off) and in 2022.1 or above can supress the drawing
            // of certain gizmos and icons for a cleaner scene
            cachedGizmoState = new ColliderManager.GizmoState();
            //ColliderManager.GizmoState state = colliderManager.cachedGizmoState;
            cachedGizmoState.gizmosEnabled = SceneView.lastActiveSceneView.drawGizmos;
            if (!cachedGizmoState.gizmosEnabled)
				SceneView.lastActiveSceneView.drawGizmos = true;

#if UNITY_2022_1_OR_NEWER
			colliderManager.hasGizmoUtility = true;
            bool gizmoState = false;
			bool iconState = false;
            Component[] components = colliderManager.GetComponentsInChildren<Component>();
			List<Type> usedTypes = new List<Type>();
            foreach (var component in components)
            {
                // we only need to set the GizmoInfo once per Type so can discard further instances of that Type
                if (!usedTypes.Contains(component.GetType()))
				{
                    usedTypes.Add(component.GetType());
                    if (GizmoUtility.TryGetGizmoInfo(component.GetType(), out GizmoInfo info))
					{
						if (colliderManager.gizmoNames.Contains(info.name))
						{
							if (info.hasGizmo)
							{
								gizmoState = info.gizmoEnabled;
								info.gizmoEnabled = false;
							}

							if (info.hasIcon)
							{
								iconState = info.iconEnabled;
								info.iconEnabled = false;
							}
							GizmoUtility.ApplyGizmoInfo(info);

							if (info.name == "CapsuleCollider") { cachedGizmoState.capsuleEnabled = gizmoState; }
							else if (info.name == "Cloth") { cachedGizmoState.clothEnabled = gizmoState; }
							else if (info.name == "SphereCollider") { cachedGizmoState.sphereEnabled = gizmoState; }
							else if (info.name == "BoxCollider") { cachedGizmoState.boxEnabled = gizmoState; }
							else if (info.name == "MagicaCapsuleCollider") { cachedGizmoState.magicaCapsuleEnabled = gizmoState; cachedGizmoState.magicaCapsuleIconEnabled = iconState; }
							else if (info.name == "MagicaCloth") { cachedGizmoState.magicaClothEnabled = gizmoState; cachedGizmoState.magicaClothIconEnabled = iconState; }
							else if (info.name == "MagicaSphereCollider") { cachedGizmoState.magicaSphereEnabled = gizmoState; cachedGizmoState.magicaSphereIconEnabled = iconState; }
							else if (info.name == "MagicaPlaneCollider") { cachedGizmoState.magicaPlaneEnabled = gizmoState; cachedGizmoState.magicaPlaneIconEnabled = iconState; }
						}
					} 
				}
            }
#endif
            PhysicsSettingsStore.SaveGizmoState(colliderManager, cachedGizmoState);
		}

        public void ResetGizmos()
		{
            cachedGizmoState = PhysicsSettingsStore.RecallGizmoState(colliderManager);
            if (cachedGizmoState == null) return;
             //ColliderManager.GizmoState state = colliderManager.cachedGizmoState;
            SceneView.lastActiveSceneView.drawGizmos = cachedGizmoState.gizmosEnabled;

#if UNITY_2022_1_OR_NEWER
            bool gizmoState = false;
            bool iconState = false;
            Component[] components = colliderManager.GetComponentsInChildren<Component>();
            List<Type> usedTypes = new List<Type>();
			foreach (var component in components)
			{
				if (!usedTypes.Contains(component.GetType()))
				{
					usedTypes.Add(component.GetType());
					if (GizmoUtility.TryGetGizmoInfo(component.GetType(), out GizmoInfo info))
					{
						if (colliderManager.gizmoNames.Contains(info.name))
						{
							if (info.name == "CapsuleCollider") { gizmoState = cachedGizmoState.capsuleEnabled; }
							else if (info.name == "Cloth") { gizmoState = cachedGizmoState.clothEnabled; }
							else if (info.name == "SphereCollider") { gizmoState = cachedGizmoState.sphereEnabled; }
							else if (info.name == "BoxCollider") { gizmoState = cachedGizmoState.boxEnabled; }
							else if (info.name == "MagicaCapsuleCollider") { gizmoState = cachedGizmoState.magicaCapsuleEnabled; iconState = cachedGizmoState.magicaCapsuleIconEnabled; }
							else if (info.name == "MagicaCloth") { gizmoState = cachedGizmoState.magicaClothEnabled; iconState = cachedGizmoState.magicaClothIconEnabled; }
							else if (info.name == "MagicaSphereCollider") { gizmoState = cachedGizmoState.magicaSphereEnabled; iconState = cachedGizmoState.magicaSphereIconEnabled; }
							else if (info.name == "MagicaPlaneCollider") { gizmoState = cachedGizmoState.magicaPlaneEnabled; iconState = cachedGizmoState.magicaPlaneIconEnabled; }

							if (info.hasGizmo)
							{
								info.gizmoEnabled = gizmoState;
							}

							if (info.hasIcon)
							{
								info.iconEnabled = iconState;
							}
							GizmoUtility.ApplyGizmoInfo(info);
						}
					}
				}
			}
#endif
        }
    }
}






