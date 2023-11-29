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
using UnityEngine.UIElements;
using UnityEditor;
using System;
using Object = UnityEngine.Object;
using System.Linq;

namespace Reallusion.Import
{
    
    public class LodSelectionWindow : EditorWindow
    {       
        public static LodSelectionWindow Current { get; private set; }

        private string mainUxmlName = "RL_LodToolWindowUI";
        private string uxmlExt = ".uxml";

        private VisualTreeAsset mainUxmlAsset;
        //private VisualTreeAsset listAUxmlAsset;

        private Button createButton;
        private TextField nameField;

        //private Dictionary<string, string> modelDict;
        private List<GridModel> modelList;

        private VisualElement main;
        private Vector2 scrollPos;
        private string nameHint;

        private Texture2D iconLODComplex;
        private Texture2D iconLODMedium;
        private Texture2D iconLODSimple;
        private Texture2D iconLODNone;
        private GUIStyle boldStyle;
        private GUIStyle countStyle;

        private static float iconSize = 128f;
        private static float boxW = iconSize + 8f;
        private static float boxH = iconSize + 42f;        

        [MenuItem("Assets/Reallusion/LOD Combiner", false, priority = 2020)]
        public static void InitTool()
        {
            InitLodSelector();
        }

        public static LodSelectionWindow InitLodSelector()
        {
            LodSelectionWindow window = GetWindow<LodSelectionWindow>("LOD Combining Tool");

            string path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
            if (AssetDatabase.IsValidFolder(path))
                window.BuildModelPrefabDict(path);
            else
                window.BuildModelPrefabDict(Selection.objects);

            window.minSize = new Vector2(boxW * 3f + 8f, boxH * 2f + 24f);
            window.Show();            

            return window;
        }

        private void OnDestroy()
        {
            createButton.clicked -= CreateButtonCallback;
            Current = null;
        }


        private void CreateGUI()
        {
            Current = this;

            mainUxmlAsset = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(GetAssetPath(mainUxmlName, uxmlExt), typeof(VisualTreeAsset));            

            VisualElement root = rootVisualElement;
            mainUxmlAsset.CloneTree(root);

            main = root.Q<VisualElement>("main-view");
            createButton = root.Q<Button>("create-button");
            nameField = root.Q<TextField>("name-field");            
            
            createButton.clicked += CreateButtonCallback;

            IMGUIContainer container = new IMGUIContainer(ContainerGUI);
            container.style.flexGrow = 1;
            main.Add(container);

            string[] folders = new string[] { "Assets", "Packages" };

            iconLODComplex = Util.FindTexture(folders, "RLIcon_LODComplex");
            iconLODMedium = Util.FindTexture(folders, "RLIcon_LODMedium");
            iconLODSimple = Util.FindTexture(folders, "RLIcon_LODSimple");
            iconLODNone = Util.FindTexture(folders, "RLIcon_LODNone");

            boldStyle = new GUIStyle();
            boldStyle.alignment = TextAnchor.MiddleCenter;
            boldStyle.wordWrap = false;
            boldStyle.fontStyle = FontStyle.Bold;
            boldStyle.normal.textColor = Color.white;

            countStyle = new GUIStyle();
            countStyle.alignment = TextAnchor.MiddleCenter;
            countStyle.wordWrap = false;
            countStyle.fontStyle = FontStyle.Normal;
            countStyle.normal.textColor = Color.Lerp(new Color(0.506f, 0.745f, 0.063f), Color.white, 0.333f);
        }
        
        private void ContainerGUI()
        {
            GUIStyle boxStyle = new GUIStyle(EditorStyles.miniButton);
            boxStyle.margin = new RectOffset(1, 1, 1, 1);
            
            //boxStyle.normal.background = TextureColor(Color.red);
            boxStyle.fixedHeight = iconSize;
            boxStyle.fixedWidth = iconSize;

            GUIStyle selectedBoxStyle = new GUIStyle(boxStyle);
            selectedBoxStyle.normal.background = TextureColor(new Color(0.506f, 0.745f, 0.063f));
            GUIStyle selectedBakedBoxStyle = new GUIStyle(boxStyle);
            selectedBakedBoxStyle.normal.background = TextureColor(new Color(1.0f, 0.745f, 0.063f));

            Rect posRect = new Rect(0f, 0f, main.contentRect.width, main.contentRect.height);

            
            int xNum = (int)Math.Floor(main.contentRect.width / boxW);
            int total = modelList.Count; // modelDict.Count;
            int yNum = (int)Math.Ceiling((decimal)total / (decimal)xNum);

            float viewRectMaxHeight = yNum * boxH;
            Rect viewRect = new Rect(0f, 0f, main.contentRect.width - 16f, viewRectMaxHeight);
            
            scrollPos = GUI.BeginScrollView(posRect, scrollPos, viewRect);

            Rect boxRect = new Rect(0, 0, boxW, boxH);
            //foreach (KeyValuePair<string, string> model in modelDict)
            foreach (GridModel model in modelList)           
            {
                GUILayout.BeginArea(boxRect);
                GUILayout.BeginVertical();
                Texture2D icon;                
                if (model.Tris >= 50000) icon = iconLODComplex;
                else if (model.Tris >= 10000) icon = iconLODMedium;
                else if (model.Tris >= 500) icon = iconLODSimple;
                else icon = iconLODNone;
                if (GUILayout.Button(new GUIContent(icon, ""), 
                                     model.Selected ? 
                                        (model.Baked ? selectedBakedBoxStyle : selectedBoxStyle) : boxStyle))
                {
                    model.Selected = !model.Selected;
                    if (model.Selected)
                    {
                        if (model.Baked)
                        {
                            GridModel sgm = GetBakedSourceModel(model);
                            if (sgm != null) sgm.Selected = false;
                        }
                        else
                        {
                            GridModel bgm = GetBakedModel(model);
                            if (bgm != null) bgm.Selected = false;
                        }
                    }
                }
                //GUILayout.FlexibleSpace();
                GUILayout.Label(model.Name, boldStyle);
                string triCount;
                if (model.Tris >= 1000000) triCount = ((float)model.Tris / 1000000f).ToString("0.0") + "M";
                else if (model.Tris >= 1000) triCount = ((float)model.Tris / 1000f).ToString("0.0") + "K";
                else triCount = ((int)(model.Tris)).ToString();
                GUILayout.Label("(" + triCount + " Triangles)", countStyle);
                GUILayout.EndVertical();
                GUILayout.EndArea();
                //GUI.Button(boxRect, new GUIContent(EditorGUIUtility.IconContent("HumanTemplate Icon").image, pwd), boxStyle);
                //GUI.Box(boxRect, new GUIContent("X", "x"));
                boxRect = GetNextBox(boxRect, xNum);
            }

            if (modelList.Count == 0)
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No character prefabs detected in folder or selection.");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }

            GUI.EndScrollView();

            return;            
        }

        void CreateButtonCallback()
        {
            List<Object> objects = new List<Object>();            
            foreach (GridModel model in modelList)
            {
                if (model.Selected)
                {
                    objects.Add(model.GetAsset());
                }
            }

            WindowManager.HideAnimationPlayer(true);
            Lodify l = new Lodify();
            GameObject lodPrefab = l.MakeLODPrefab(objects.ToArray(), nameField.text);
            if (lodPrefab && WindowManager.IsPreviewScene)
            {
                WindowManager.previewScene.ShowPreviewCharacter(lodPrefab);
            }
            Selection.activeObject = lodPrefab;

            Close();
        }

        private Rect GetNextBox(Rect lastBox, int xMax)
        {
            Rect newBox = new Rect(0f, 0f, boxW, boxH);

            float newX = lastBox.x + lastBox.width;
            float newY = lastBox.y + lastBox.height;

            if ((newX + boxW) > (xMax * boxW))
            {
                newBox.x = 0f;
                newBox.y = newY;
            }
            else
            {
                newBox.x = newX;
                newBox.y = lastBox.y;
            }            
            return newBox;
        }

        Texture2D TextureColor(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void BuildModelPrefabDict(string folder)
        {            
            modelList = new List<GridModel>();
            string[] folders = new string[] { folder };
            string search = "t:Prefab";
            string[] results = AssetDatabase.FindAssets(search, folders);
            int largest = 0;
            const string bakedPathSuffix = Importer.BAKE_SUFFIX + ".prefab";

            foreach (string guid in results)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = Path.GetFileNameWithoutExtension(path);

                AssetToModelList(bakedPathSuffix, path, guid, assetName, ref largest);
            }

            nameField.SetValueWithoutNotify(nameHint + "_LOD");

            SortAndAutoSelect();
        }

        private void BuildModelPrefabDict(Object[] objects)
        {
            modelList = new List<GridModel>();
            int largest = 0;
            const string bakedPathSuffix = Importer.BAKE_SUFFIX + ".prefab";

            foreach (Object obj in objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                string guid = AssetDatabase.AssetPathToGUID(path);
                string assetName = Path.GetFileNameWithoutExtension(path);

                AssetToModelList(bakedPathSuffix, path, guid, assetName, ref largest);
            }

            nameField.SetValueWithoutNotify(nameHint + "_LOD");            

            SortAndAutoSelect();
        }

        private void AssetToModelList(string bakedPathSuffix, string path, string guid, string assetName, ref int largest)
        {
            if (path.iEndsWith(".prefab"))
            {
                bool baked = false;
                if (path.iEndsWith(bakedPathSuffix))
                {
                    path = path.Substring(0, path.Length - bakedPathSuffix.Length) + ".prefab";
                    baked = true;
                }

                GameObject o = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                Object src = Util.FindRootPrefabAsset(o);                
                
                if (Util.IsCC3Character(src))
                {                    
                    GridModel g = new GridModel();
                    g.Guid = guid;
                    g.Name = assetName;                    
                    g.Icon = AssetPreview.GetAssetPreview(o);
                    g.Selected = true;
                    g.Baked = baked;
                    g.Tris = Lodify.CountPolys(o);
                    if (g.Tris > largest)
                    {
                        largest = g.Tris;
                        nameHint = o.name;
                    }
                    modelList.Add(g);
                }
            }            
        }

        private void SortAndAutoSelect()
        {
            modelList = modelList.OrderByDescending(o => o.Tris).ToList();             

            // select everything
            foreach (GridModel gm in modelList) gm.Selected = true;

            // deselect all baked source models
            foreach (GridModel gm in modelList)
            {
                if (gm.Baked)
                {
                    GridModel sgm = GetBakedSourceModel(gm);
                    if (sgm != null) sgm.Selected = false;
                }
            }
        }

        private GridModel GetBakedModel(GridModel sgm)
        {
            string name = sgm.Name;
            if (name.iEndsWith(Importer.BAKE_SUFFIX)) return sgm;

            string bakedName = name + Importer.BAKE_SUFFIX;
            foreach (GridModel bgm in modelList)
            {
                if (bgm.Name == bakedName) return bgm;
            }            

            return null;
        }

        private GridModel GetBakedSourceModel(GridModel bgm)
        {
            string bakedName = bgm.Name;
            if (bakedName.iEndsWith(Importer.BAKE_SUFFIX))
            {
                string name = bakedName.Substring(0, bakedName.Length - Importer.BAKE_SUFFIX.Length);
                foreach (GridModel sgm in modelList)
                {
                    if (sgm.Name == name) return sgm;
                }

                return null;
            }
            else
            {
                return bgm;
            }
        }        

        private string GetAssetPath(string name, string extension)
        {
            string[] folders = new string[] { "Assets", "Packages" };
            string search = name;
            string ext = extension;
            string[] results = AssetDatabase.FindAssets(search, folders);

            foreach (string guid in results)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetExtension(path).Equals(ext, System.StringComparison.InvariantCultureIgnoreCase))
                {                    
                    return path;
                }
            }

            Debug.LogError("Asset " + name + " NOT found.");
            return "no";
        }

        public class GridModel
        {
            public string Guid { get; set; }
            public string Name { get; set; }
            public Texture2D Icon { get; set; }
            public bool Selected { get; set; }
            public int Tris { get; set; }
            public bool Baked { get; set; }

            public GridModel()
            {
                Guid = "";
                Name = "";
                Icon = (Texture2D)EditorGUIUtility.IconContent("HumanTemplate Icon").image;
                Selected = false;
                Tris = 0;
                Baked = false;
            }

            public string GetPath()
            {
                return AssetDatabase.GUIDToAssetPath(Guid);
            }

            public Object GetAsset()
            {
                string path = GetPath();
                return AssetDatabase.LoadAssetAtPath<Object>(path);
            }
        }
    }
}
