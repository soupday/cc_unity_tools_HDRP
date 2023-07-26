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
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Reallusion.Import
{
    public class CharacterTreeView : UnityEditor.IMGUI.Controls.TreeView
    {
        public GameObject objectInContext;
        private string assetPath;
        public List<Object> objList;
        public List<int>[] linkedIndices;
        public IList<int> selectedIndices;
        private bool enableMultiPassMaterials;

        public const int NUM_LINKED_INDICES = 7;
        public const int LINKED_INDEX_SKIN = 0;
        public const int LINKED_INDEX_CORNEA = 1;
        public const int LINKED_INDEX_EYE_OCCLUSION = 2;
        public const int LINKED_INDEX_TEARLINE = 3;
        public const int LINKED_INDEX_TEETH = 4;
        public const int LINKED_INDEX_HAIR = 5;
        public const int LINKED_INDEX_EYE = 6;        

        public CharacterTreeView(TreeViewState treeViewState, GameObject obj) : base(treeViewState)
        {
            //Force Treeview to reload its data (will force BuildRoot and BuildRows to be called)
            objectInContext = obj;
            assetPath = AssetDatabase.GetAssetPath(obj);
            objList = new List<Object>();
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            //indicies
            int mDepth = -1;//root level
            int mId = 0;
            
            objList.Clear();
            objList.Add(null);
            var root = new TreeViewItem { id = mId++, depth = mDepth, displayName = "Root" };

            var allItems = new List<TreeViewItem>();

            linkedIndices = new List<int>[NUM_LINKED_INDICES];
            for (int i = 0; i < linkedIndices.Length; i++)
                linkedIndices[i] = new List<int>();            

            mDepth = 0;//base level        

            objList.Add(objectInContext);
            allItems.Add(new TreeViewItem { id = mId++, depth = mDepth, displayName = objectInContext.name, icon = (Texture2D)EditorGUIUtility.IconContent("Avatar Icon").image });

            //applicable objects
            DoThing(objectInContext.transform, allItems, ref mId);

            SetupParentsAndChildrenFromDepths(root, allItems);            
            return root;
        }

        void DoThing(Transform transform, List<TreeViewItem> allItems, ref int mId)
        {
            int mDepth = 0;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>();

            //applicable objects
            foreach (Renderer renderer in renderers)
            {
                Transform child = renderer.transform;

                mDepth = 1;//1st tier

                objList.Add(child);
                allItems.Add(new TreeViewItem { id = mId++, depth = mDepth, displayName = child.name, icon = (Texture2D)EditorGUIUtility.IconContent("Mesh Icon").image });

                foreach (Material m in child.gameObject.GetComponent<Renderer>().sharedMaterials)
                {
                    if (!m) continue;

                    mDepth = 2;//2nd tier

                    string sourceName = Util.GetSourceMaterialName(assetPath, m);
                    string shaderName = Util.GetShaderName(m);
                    int linkedIndex = Util.GetLinkedMaterialIndex(sourceName, shaderName);
                    if (linkedIndex >= 0)
                        linkedIndices[linkedIndex].Add(mId);

                    objList.Add(m);
                    allItems.Add(new TreeViewItem { id = mId++, depth = mDepth, displayName = m.name, icon = (Texture2D)EditorGUIUtility.IconContent("Material Icon").image });

                    int props = m.shader.GetPropertyCount();
                    for (int i = 0; i < props; i++)
                    {
                        int flagValue = (int)m.shader.GetPropertyFlags(i);
                        int checkBit = 0x00000001; //bit for UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector
                        int flagHasBit = (flagValue & checkBit);

                        if (flagHasBit == 0 && m.shader.GetPropertyType(i).Equals(UnityEngine.Rendering.ShaderPropertyType.Texture))
                        {
                            if (m.GetTexture(m.shader.GetPropertyName(i)) != null)
                            {
                                mDepth = 3;//3rd tier  

                                objList.Add(m.GetTexture(m.shader.GetPropertyName(i)));
                                allItems.Add(new TreeViewItem { id = mId++, depth = mDepth, displayName = m.shader.GetPropertyDescription(i), icon = (Texture2D)EditorGUIUtility.IconContent("Image Icon").image });
                            }
                        }
                    }
                }
            }
        }

        public void EnableMultiPass()
        {
            if (Pipeline.isHDRP)
                enableMultiPassMaterials = true;
            else
                enableMultiPassMaterials = false;
        }

        public void DisableMultiPass()
        {
            enableMultiPassMaterials = false;
        }

        public void ClearSelection()
        {
            IList<int> list = new int[] { 0 };
            SetSelection(list);
        }

        public void SelectLinked(int index)
        {            
            if (ImporterWindow.SELECT_LINKED)
            {
                for (int i = 0; i < linkedIndices.Length; i++)
                {
                    if (linkedIndices[i].Contains(index))
                    {
                        SetSelection(linkedIndices[i]); //, TreeViewSelectionOptions.FireSelectionChanged);
                    }
                }
            }
        }

        private bool TrySelectMultiPassMaterial(int index, List<Object> selectedObjects)
        {
            if (enableMultiPassMaterials)
            {
                if (objList[index].GetType() == typeof(Material))
                {
                    Material m = objList[index] as Material;
                    if (m && m.shader.name.iContains(Pipeline.SHADER_HQ_HAIR))
                    {
                        if (Util.GetMultiPassMaterials(m, out Material firstPass, out Material secondPass))
                        {
                            selectedObjects.Add(firstPass);
                            selectedObjects.Add(secondPass);
                            return true;
                        }                 
                    }
                }                
            }
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 1)
            {                
                SelectLinked(selectedIds[0]);
            }

            if (HasSelection())
            {                
                selectedIndices = GetSelection();
                List<Object> selectedObjects = new List<Object>(selectedIndices.Count);
                if (selectedIndices.Count > 1)
                {                    
                    foreach (int i in selectedIndices)
                    {
                        if (!TrySelectMultiPassMaterial(i, selectedObjects))
                        {
                            selectedObjects.Add(objList[i]);
                        }
                    }
                    Selection.objects = selectedObjects.ToArray();
                }
                else
                {
                    int i = selectedIndices[0];
                    if (TrySelectMultiPassMaterial(i, selectedObjects))
                    {
                        Selection.objects = selectedObjects.ToArray();
                    }
                    else
                    {
                        Selection.activeObject = objList[i];
                    }
                }
            }
        }

        private void ExpandToDepth(TreeViewItem item, int depth, int maxDepth)
        {
            if (depth <= maxDepth)
                SetExpanded(item.id, true);
            else
                SetExpanded(item.id, false);

            if (item.children != null)
            {
                foreach (TreeViewItem child in item.children)
                {
                    ExpandToDepth(child, depth + 1, maxDepth);
                }
            }
        }        

        public void ExpandToDepth(int maxDepth)
        {
            ExpandToDepth(rootItem, 0, maxDepth);            
        }        

        public void Release()
        {
            objList.Clear();
        }
    }
}