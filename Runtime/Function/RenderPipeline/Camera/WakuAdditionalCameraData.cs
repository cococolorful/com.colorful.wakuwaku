using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace wakuwaku.Function.WRenderPipeline
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public unsafe class WakuAdditionalCameraData : MonoBehaviour
    {
        public Camera m_camera;

        int width,height;
        public bool use_native_render = false;

        public int idx;
        public int frame_buffer_count=2;
        public Texture2D []native_render_texture;
        public WRenderGraph render_graph;
        // Start is called before the first frame update


        [DllImport("NativeRenderer")]
        private static extern void OnSize(int width, int height, int frame_buffer_count, IntPtr[] color_handles);
        [DllImport("NativeRenderer")]
        private static extern void OnNativeRendererQuit();
        [DllImport("NativeRenderer")]
        private static extern void OnInitialize();

        [DllImport("NativeRenderer")]
        private static extern void EndAddRenderItem();
        [DllImport("NativeRenderer")]
        private static extern int AddRenderItem(int mesh_id, float* transform, int[] material_instance_ids, int material_count);

        [System.Runtime.InteropServices.DllImport("NativeRenderer")]
        private static extern int AddMesh(int mesh_id, IntPtr vertex_handle, int num_vectex_elements, IntPtr index_handlem, int num_index_elements, void* subMeshes, int subMesh_count);

        [System.Runtime.InteropServices.DllImport("NativeRenderer")]
        private static extern int AddTexture(int id, IntPtr texture_handle);
        [System.Runtime.InteropServices.DllImport("NativeRenderer")]
        private static extern int AddMaterial(int id, int albedo_id);
        static bool sigle = false ;


        private void Awake()
        {
            OnInitialize();

            Init();
        }

        private void OnDisable()
        {
            OnNativeRendererQuit();
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        struct NativeSubMesh
        {
            public int index_start;
            public int index_count;
        }
        public void Init()
        {
            m_camera = GetComponent<Camera>();
            Modify();

            AddMeshAndMaterial();
            EndAddRenderItem();
        }

        private static void AddMeshAndMaterial()
        {
            var mesh_components = GameObject.FindObjectsOfType<MeshFilter>();
            List<Mesh> meshes = new List<Mesh>();
            foreach (var item in mesh_components)
            {
                meshes.Add(item.mesh);
                var mesh_renderer = item.GetComponent<MeshRenderer>();

                if (mesh_renderer != null)
                {
                    int[] material_ids = new int[mesh_renderer.sharedMaterials.Length];
                    for (int i = 0; i < mesh_renderer.sharedMaterials.Length; i++)
                    {
                        AddTexture(mesh_renderer.sharedMaterials[i].GetTexture("_BaseColorTex").GetInstanceID(), mesh_renderer.sharedMaterials[i].GetTexture("_BaseColorTex").GetNativeTexturePtr());
                        AddMaterial(mesh_renderer.sharedMaterials[i].GetInstanceID(), mesh_renderer.sharedMaterials[i].GetTexture("_BaseColorTex").GetInstanceID());
                    
                        material_ids[i] = mesh_renderer.sharedMaterials[i].GetInstanceID();
                    }

                    var world = item.transform.localToWorldMatrix;
                    AddRenderItem(item.mesh.GetInstanceID(),(float*)UnsafeUtility.AddressOf(ref world), material_ids, mesh_renderer.sharedMaterials.Length);
                }
            }

            foreach (var mesh in meshes)
            {
                NativeSubMesh[] subMeshes = new NativeSubMesh[mesh.subMeshCount];
                for (int i = 0; i < subMeshes.Length; i++)
                {
                    subMeshes[i] = new NativeSubMesh();
                    var subMehs_desc = mesh.GetSubMesh(i);
                    subMeshes[i].index_start = subMehs_desc.indexStart;
                    subMeshes[i].index_count = subMehs_desc.indexCount;
                }

                unsafe
                {

                    if (mesh != null)
                    {
                        Debug.Log(mesh.GetInstanceID());
                        var id = AddMesh(mesh.GetInstanceID(), mesh.GetNativeVertexBufferPtr(0), mesh.vertexCount, mesh.GetNativeIndexBufferPtr(), mesh.triangles.Length, UnsafeUtility.AddressOf(ref subMeshes[0]), subMeshes.Length);
                        Debug.Log(id);
                    }


                }
            }
        }

        void Modify()
        {
            width = m_camera.pixelWidth;
            height = m_camera.pixelHeight;

            IntPtr[] color_handles = new IntPtr[frame_buffer_count];

            OnSize(width, height, frame_buffer_count, color_handles);
            native_render_texture = null;
            native_render_texture = new Texture2D[frame_buffer_count];
            for (int i = 0; i < frame_buffer_count; i++)
            {
                native_render_texture[i] = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBAFloat, true, true, color_handles[i]);
            }
        }
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.TextArea("test for funny");
            GUILayout.EndHorizontal();
        }
        // Update is called once per frame

        [DllImport("NativeRenderer")]
        private static extern int Render();

        void Update()
        {
            if(width != m_camera.pixelWidth || height != m_camera.pixelHeight)
            {
                Modify();
            }
            int frame_idx = Render();
            Debug.Log(frame_idx);
            idx = frame_idx;
            //Graphics.Blit(native_render_texture[frame_idx], m_camera.targetTexture);
        }
    }

}


