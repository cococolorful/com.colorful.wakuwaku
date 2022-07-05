using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace wakuwaku.Function.WRenderPipeline
{
    public class RGWindowView : EditorWindow
    {
        RenderGraphView m_RenderGraphView ;
        WRenderGraph m_RenderGraph ;
        [MenuItem("wakuwaku/OpenRenderGraph Editor")]
        public static RGWindowView Create()
        {
            return GetWindow<RGWindowView>("RenderGraphEditor");
        }
        private void OnEnable()
        {
            try
            {
                var toolbar = new IMGUIContainer(() =>
                {
                    GUILayout.BeginHorizontal(EditorStyles.toolbar);


                    if (GUILayout.Button("Load", EditorStyles.toolbarButton))
                    {
                        Load();
                    }

                    if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                    {
                        Save();
                    }
                    if (GUILayout.Button("Save as", EditorStyles.toolbarButton))
                    {
                        SaveAS();
                    }
                    if (GUILayout.Button("Compile", EditorStyles.toolbarButton))
                    {
                        m_RenderGraph.Compile();
                        Save();
                     }
                    GUILayout.Label("current scene graph : " + m_RenderGraph.name);

                    GUILayout.EndHorizontal();

                });
                rootVisualElement.Add(toolbar);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                throw;
            }

           
            if(m_RenderGraph == null)
            {
                m_RenderGraph = ScriptableObject.CreateInstance<WRenderGraph>();
                m_RenderGraph.name = "span by window view";
            }
            
            m_RenderGraphView = new RenderGraphView(this, m_RenderGraph) { style = { flexGrow = 1 } };
            rootVisualElement.Add(m_RenderGraphView);
            m_RenderGraphView.Refresh();
            this.Focus();
        }

        
        private void Update()
        {
            //SceneView.A += view =>
            //{
            //    var e = Event.current;
            //    if (e != null && e.keyCode != KeyCode.None)
            //        Debug.Log("Key pressed in editor: " + e.keyCode);
            //};
            Repaint(); //TODO:切换为当scene发生Rendering的时候重绘
        }
        public void Load(string path)
        {
            WRenderGraph info;
            try
            {
                info = AssetDatabase.LoadAssetAtPath<WRenderGraph>(path);
                if (info == null) throw new System.Exception("Null");
            }
            catch
            {
                Debug.LogError(string.Format("Load Render Graph Info at \"{0}\" failed!", path));
                return;
            }
            m_RenderGraph = info;
            m_RenderGraphView.SetGraphInfo(m_RenderGraph);


            m_RenderGraphView.Refresh();
        }
        public void Load()
        {
            FileUtil.OpenFileName openFileName = new FileUtil.OpenFileName();
            openFileName.structSize = Marshal.SizeOf(openFileName);
            openFileName.templateName = "*.asset";
            openFileName.filter = "RG(*.asset)\0*.asset";
            openFileName.file = new string(new char[256]);
            openFileName.maxFile = openFileName.file.Length;
            openFileName.fileTitle = new string(new char[64]);
            openFileName.maxFileTitle = openFileName.fileTitle.Length;
            openFileName.initialDir = Application.dataPath.Replace('/', '\\');
            openFileName.title = "Load RG";
            openFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;

            if (FileUtil.GetOpenFileName(openFileName))
            {
                string path = openFileName.file.Substring(openFileName.file.IndexOf("Assets"));
                path = path.Replace('\\', '/');
                if (!path.Contains(".asset"))
                {
                    path += ".asset";
                }
                Load(path);
            }
            
            m_RenderGraphView.Refresh();
        }

        public void Save()
        {
            m_RenderGraphView.Refresh();
            if (AssetDatabase.Contains(m_RenderGraph))
            {
                EditorUtility.SetDirty(m_RenderGraph);
            }else
            {
                SaveAS();
            }

            AssetDatabase.Refresh();
        }
        public void SaveAS()
        {
            FileUtil.OpenFileName openFileName = new FileUtil.OpenFileName();
            openFileName.structSize = Marshal.SizeOf(openFileName);
            openFileName.templateName = "*.asset";
            openFileName.filter = "RG\0*.asset";
            openFileName.file = new string(new char[256]);
            openFileName.maxFile = openFileName.file.Length;
            openFileName.fileTitle = new string(new char[64]);
            openFileName.maxFileTitle = openFileName.fileTitle.Length;
            openFileName.initialDir = Application.dataPath.Replace('/', '\\');
            openFileName.title = "Save RG";
            openFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008 | 0x00000002;

            if (FileUtil.GetSaveFileName(openFileName))
            {
                string path = openFileName.file.Substring(openFileName.file.IndexOf("Assets"));
                path = path.Replace('\\', '/');
                if (!path.Contains(".asset"))
                {
                    path += ".asset";
                }

                if (m_RenderGraph == null)
                    m_RenderGraph = CreateInstance<WRenderGraph>();
                AssetDatabase.CreateAsset(m_RenderGraph, path);
            }

            AssetDatabase.Refresh();
        }
    }

    public class FileUtil
    {

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public System.IntPtr dlgOwner = System.IntPtr.Zero;
            public System.IntPtr instance = System.IntPtr.Zero;
            public System.String filter = null;
            public System.String customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public System.String file = null;
            public int maxFile = 0;
            public System.String fileTitle = null;
            public int maxFileTitle = 0;
            public System.String initialDir = null;
            public System.String title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public System.String defExt = null;
            public System.IntPtr custData = System.IntPtr.Zero;
            public System.IntPtr hook = System.IntPtr.Zero;
            public System.String templateName = null;
            public System.IntPtr reservedPtr = System.IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [System.Runtime.InteropServices.DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool GetOpenFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName ofn);

        [System.Runtime.InteropServices.DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool GetSaveFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName ofn);
    }
}
