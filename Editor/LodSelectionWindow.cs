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
        [MenuItem("Assets/Lod Tools/Selection Grid Window", false, 100)]
        public static void InitTool()
        {                        
            InitLodSelector();
        }

        public static void InitLodSelector()
        {
            LodSelectionWindow window = GetWindow<LodSelectionWindow>("LodSelectionGridWindow");

            string path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
            if (AssetDatabase.IsValidFolder(path))
                window.BuildModelPrefabDict(path);
            else
                window.BuildModelPrefabDict(Selection.objects);

            window.minSize = new Vector2(300f, 300f);
        }        

        private string mainUxmlName = "rl-lod-array-main-grid";
        //private string listBoxUxmlName = "rl-lod-array-list-box";
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

        private void OnDestroy()
        {
            createButton.clicked -= CreateButtonCallback;
        }


        private void CreateGUI()
        {
            mainUxmlAsset = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(GetAssetPath(mainUxmlName, uxmlExt), typeof(VisualTreeAsset));
            //listAUxmlAsset = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(GetAssetPath(listBoxUxmlName, uxmlExt), typeof(VisualTreeAsset));

            VisualElement root = rootVisualElement;
            mainUxmlAsset.CloneTree(root);

            main = root.Q<VisualElement>("main-view");
            createButton = root.Q<Button>("create-button");
            nameField = root.Q<TextField>("name-field");            
            
            createButton.clicked += CreateButtonCallback;

            IMGUIContainer container = new IMGUIContainer(ContainerGUI);
            container.style.flexGrow = 1;
            main.Add(container);
        }

        float boxW = 108f;
        float boxH = 125f;
        private void ContainerGUI()
        {
            GUIStyle boxStyle = new GUIStyle(EditorStyles.miniButton);
            boxStyle.margin = new RectOffset(1, 1, 1, 1);
            
            //boxStyle.normal.background = TextureColor(Color.red);
            boxStyle.fixedHeight = 100f;
            boxStyle.fixedWidth = 100f;

            GUIStyle selectedBoxStyle = new GUIStyle(boxStyle);
            selectedBoxStyle.normal.background = TextureColor(Color.green * 0.85f);

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
                if (GUILayout.Button(new GUIContent(model.Icon, ""), model.Selected ? selectedBoxStyle : boxStyle))
                {
                    model.Selected = !model.Selected;
                }
                //GUILayout.FlexibleSpace();
                GUILayout.Label(model.Name);
                GUILayout.EndVertical();
                GUILayout.EndArea();
                //GUI.Button(boxRect, new GUIContent(EditorGUIUtility.IconContent("HumanTemplate Icon").image, pwd), boxStyle);
                //GUI.Box(boxRect, new GUIContent("X", "x"));
                boxRect = GetNextBox(boxRect, xNum);
                //Debug.Log(model.Value);
            }

            GUI.EndScrollView();

            return;            
        }

        void CreateButtonCallback()
        {
            Object[] objects = new Object[modelList.Count];
            for (int i = 0; i < modelList.Count; i++)
            {
                objects[i] = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(modelList[i].Guid));
            }

            WindowManager.HideAnimationPlayer(true);
            Lodify l = new Lodify();
            GameObject lodPrefab = l.MakeLODPrefab(objects, nameField.text);
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

            foreach (string guid in results)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = Path.GetFileNameWithoutExtension(path);

                GameObject o = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                Object src = Util.FindRootPrefabAsset(o);

                if (Util.IsCC3Character(src))
                {
                    //modelDict.Add(guid, assetName);
                    GridModel g = new GridModel();
                    g.Guid = guid;
                    g.Name = assetName;
                    //g.Icon = (Texture2D)AssetDatabase.GetCachedIcon(path);
                    g.Icon = AssetPreview.GetAssetPreview(o);
                    if (Selection.objects.ToList().Contains(o))
                        g.Selected = true;
                    g.Tris = Lodify.CountPolys(o);
                    if (g.Tris > largest)
                    {
                        largest = g.Tris;
                        nameHint = o.name;
                    }
                    modelList.Add(g);
                }
            }

            nameField.SetValueWithoutNotify(nameHint + "_LOD");
        }

        private void BuildModelPrefabDict(Object[] objects)
        {            
            modelList = new List<GridModel>();
            int largest = 0;

            foreach (Object obj in objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.iEndsWith(".prefab"))
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    string assetName = Path.GetFileNameWithoutExtension(path);

                    GameObject o = (GameObject)AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                    Object src = Util.FindRootPrefabAsset(o);

                    if (Util.IsCC3Character(src))
                    {
                        //modelDict.Add(guid, assetName);
                        GridModel g = new GridModel();
                        g.Guid = guid;
                        g.Name = assetName;
                        //g.Icon = (Texture2D)AssetDatabase.GetCachedIcon(path);
                        g.Icon = AssetPreview.GetAssetPreview(o);
                        if (Selection.objects.ToList().Contains(o))
                            g.Selected = true;
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

            nameField.SetValueWithoutNotify(nameHint + "_LOD");
        }        

        private Editor MakeEditor(string guid)
        {
            Object o = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object));
            return Editor.CreateEditor(o);
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
                    Debug.Log("Found " + name);
                    return path;
                }
            }
            Debug.LogWarning("Asset " + name + " NOT found.");
            return "no";
        }

        public class GridModel
        {
            public string Guid { get; set; }
            public string Name { get; set; }
            public Texture2D Icon { get; set; }
            public bool Selected { get; set; }
            public int Tris { get; set; }
            public GridModel()
            {
                Guid = "";
                Name = "";
                Icon = (Texture2D)EditorGUIUtility.IconContent("HumanTemplate Icon").image;
                Selected = false;
                Tris = 0;
            }
        }
    }
}
