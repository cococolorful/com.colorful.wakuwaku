using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using wakuwaku.Core;
using wakuwaku.Function.WRenderPipeline.LightDetail;

namespace wakuwaku.Function.WRenderPipeline
{

    namespace LightDetail
    {
        struct LightModel
        {
            public Mesh Mesh;
            public int SubMeshIdx;
            public Texture2D EmissiveTex;
            public Vector3 emissive_factor;
            public Transform transform;

        }
        struct LightBlasInfo
        {
            public int IndexOffset;
            public int IndexCount;
        }
        struct EmissiveVertex
        {
            public Vector3 position;
            public Vector3 normal;
        }
        public struct PathTracingLightOut
        {
            public float scale; //
            public float temperature;
            public Vector3 Position;
            public Vector3 Normal;

            public Vector3 Dimensions;
            public float Attenuation;
            public float FalloffExponent;
            public float RectLightBarnCosAngle;
            public float RectLightBarnLength;
            public int IESTextureSlice;
            public int LightType;

        }

        public struct PathTracingLight
        {
            public int shape_id;
            public Vector3 radiance_or_irradiance;
            public float area;
            public int to_world_id;
        }
        class Node
        {
            public Vector3 boundMin;
            public float flux;
            public Vector3 boundMax;
            public int ID;
        } 
    }
    public class Scene
    {
        public static Scene Instance {get { if (instance == null) Initialize(); return instance; }}
        static Scene instance;
        public  static void Initialize()
        {
            Debug.Log("RenderSceneManager Initialize");
            
            instance = new Scene();
            //BuildScene();
        }

        
        public void BuildScene()
        {
            CollectSceneData();
            CreateRasterMesh();
        }
        public Scene()
        {

            //if(Application.isPlaying)
            //{
            //    CollectSceneData();
            //    //CreateMeshGroup();
            //    //CreateMeshInstanceData();
            //    CreateRasterMesh();
            //}
            BuildMeshBVH();

            BuildLight();
        }

        struct MeshInstance
        {
            public int instace_id;
            public int material_id;
            
            public int vbuffer_offset;
            public int ibuffer_offset;
        }

        private void CreateMeshInstanceData()
        {
            throw new NotImplementedException();
        }

        struct MeshGroup
        {
            public List<MeshDescriptor> non_instanced_meshes;
            public List<MeshDescriptor> instanced_meshes;
        }

        private void CreateMeshGroup()
        {
            // non-instanced meshes are all placed in the same group
            // instanced mesh has one group per mesh
            // m_mesh_instances = null;
            //throw new NotImplementedException();
        }

        class SceneData
        {
            // Raytracing
            public RayTracingAccelerationStructure scene_bvh;

            // Instance transforms
            public ComputeBuffer world_matrices;
            public List<Matrix4x4> world_mat_cpu;
            
            public ComputeBuffer inverse_transpose_world_matrices;
            public List<Matrix4x4> inverse_transpose_world_mat_cpu;

            public ComputeBuffer prev_world_matrices;
            public ComputeBuffer inverse_transpose_prev_world_matrices;

            // Instance


            // Camera
            public Matrix4x4 camera_view_matrix;
            public Matrix4x4 camera_prev_view_matrix;
            public Matrix4x4 camera_proj_matrix;
            public Matrix4x4 camera_view_proj_matrix;
            public Matrix4x4 camera_inv_view_proj_matrix;
            public Matrix4x4 camera_view_proj_no_jitter_matrix;
            public Matrix4x4 camera_prev_view_proj_no_jitter_matrix;
            public Matrix4x4 camera_proj_no_jitter_matrix;

            public Vector3 g_camera_pos_world;
            public float g_camera_near_z;

            public Vector3 g_camera_right;
            public float g_camera_jitter_x;

            public Vector3 g_camera_up;
            public float g_camera_jitter_y;

            public Vector3 g_camera_forward;
            public float g_camera_far_z;
            public void Apply()
            {
                Shader.SetGlobalBuffer("g_scene_world_matrices", world_matrices);
                Shader.SetGlobalBuffer("g_scene_inverse_transpose_world_matrices", inverse_transpose_world_matrices);

                Shader.SetGlobalMatrix("g_camera_view_proj_matrix", camera_view_proj_matrix);

                Shader.SetGlobalVector("g_camera_pos_world", g_camera_pos_world);
                Shader.SetGlobalVector("g_camera_right", g_camera_right);
                Shader.SetGlobalVector("g_camera_up", g_camera_up);
                Shader.SetGlobalVector("g_camera_forward", g_camera_forward);
                Shader.SetGlobalFloat("g_camera_near_z", g_camera_near_z); 
                Shader.SetGlobalFloat("g_camera_far_z", g_camera_far_z);
            }

            ~SceneData()
            {
                if(world_matrices !=null)
                    world_matrices.Dispose();
                if (inverse_transpose_world_matrices != null)
                    inverse_transpose_world_matrices.Dispose();
            }
        }

        public void ApplyCamera(Camera camera)
        {
            m_scene_gpu_data.camera_view_matrix = camera.worldToCameraMatrix;
            m_scene_gpu_data.camera_view_proj_matrix = camera.worldToCameraMatrix * camera.projectionMatrix;


//             var p1 = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.nearClipPlane));
//             var p2= camera.ViewportToWorldPoint(new Vector3(0, 1, camera.nearClipPlane));
//             var p3 = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.nearClipPlane));
// 
//             var right = (p2 - p1).normalized;
//             var up = (p3 - p2).normalized;
//             var forward = camera.transform.forward;

            m_scene_gpu_data.g_camera_forward = camera.transform.forward;
            m_scene_gpu_data.g_camera_right = camera.transform.right;
            m_scene_gpu_data.g_camera_up = camera.transform.up;

            m_scene_gpu_data.g_camera_near_z = camera.nearClipPlane;
            m_scene_gpu_data.g_camera_far_z = camera.farClipPlane;
            m_scene_gpu_data.g_camera_pos_world = camera.transform.position;
            m_scene_gpu_data.Apply();
        }
        SceneData m_scene_gpu_data;
        public class MeshDescriptor
        {
            public Mesh mesh;
            public List<GameObject> instances;
            public uint base_vertex_location;
            public uint start_index_location;
        }
        public List<MeshDescriptor> m_mesh_instances;

        public class MeshInstanceData
        {
            public int mesh_id;
            public int material_id;
            public int instance_id;
            public Material material;
            public MeshRenderer renderer;
        }
        public List<MeshInstanceData> InstancesData;

        List<Material> m_materials;
        Dictionary<Material, int> m_material2id;
        // how many mesh and how many instance and how many material and how many light

        public Mesh m_raster_mesh;

        void CreateRasterMesh()
        {
            m_raster_mesh = new Mesh();
            m_raster_mesh.name = "Raster Scene Mesh";
            
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector4> tangents= new List<Vector4>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> indices = new List<int>();

            for (int i = 0; i < m_mesh_instances.Count; i++)
            {
                m_mesh_instances[i].base_vertex_location = (uint)vertices.Count;
                vertices.AddRange(m_mesh_instances[i].mesh.vertices);
                normals.AddRange(m_mesh_instances[i].mesh.normals);
                tangents.AddRange(m_mesh_instances[i].mesh.tangents);
                uvs.AddRange(m_mesh_instances[i].mesh.uv);

                m_mesh_instances[i].start_index_location = (uint)indices.Count;
                indices.AddRange(m_mesh_instances[i].mesh.triangles);

            }

            m_raster_mesh.SetVertices(vertices);
            m_raster_mesh.SetNormals(normals);
            m_raster_mesh.SetUVs(0,uvs);
            m_raster_mesh.SetTangents(tangents);
            m_raster_mesh.SetTriangles(indices,0);
            m_raster_mesh.RecalculateBounds();
            //m_raster_mesh.UploadMeshData(true);

            commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[m_draw_count];
            uint instance_id = 0;
            foreach (var item in m_mesh_instances)
            {
                foreach (var instance in item.instances)
                {
                    commandData[instance_id].instanceCount = 1;
                    commandData[instance_id].indexCountPerInstance = (uint)item.mesh.GetIndexCount(0);
                    commandData[instance_id].startIndex = (uint)item.start_index_location;
                    commandData[instance_id].baseVertexIndex = (uint)item.base_vertex_location;
                    commandData[instance_id].startInstance = (uint)instance_id;
                    ++instance_id;
                }
            }
            
            if (commandBuf != null)
                commandBuf.Dispose();
            commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, m_draw_count, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            commandBuf.SetData(commandData);
        }
        private void CollectSceneData()
        {
            m_mesh_instances = new List<MeshDescriptor>();
            m_material2id = new Dictionary<Material, int>();
            m_materials = new List<Material>();

            Dictionary<Mesh, int> mesh2mesh_desc_id = new Dictionary<Mesh, int>();
            foreach (var item in GameObject.FindObjectsOfType<MeshRendererAdditionalData>())
            {
                if (item.gameObject.activeInHierarchy == false)
                    continue;
                
                if(mesh2mesh_desc_id.TryGetValue(item.GetComponent<MeshFilter>().sharedMesh,out var idx) == false)
                {
                    idx = m_mesh_instances.Count;
                    mesh2mesh_desc_id.Add(item.GetComponent<MeshFilter>().sharedMesh, idx);
                    m_mesh_instances.Add(new MeshDescriptor() { instances = new List<GameObject>() ,mesh = item.GetComponent<MeshFilter>().sharedMesh });
                }
                m_mesh_instances[idx].instances.Add(item.gameObject);

                var material = item.GetComponent<MeshRenderer>().sharedMaterial;
                if (m_material2id.ContainsKey(material) == false)
                {
                    m_material2id.Add(material, m_materials.Count);
                    m_materials.Add(material);
                }
            }

            InstancesData = new List<MeshInstanceData>();

            // instance data
            m_draw_count = 0;
            foreach (var item in m_mesh_instances)
            {
                foreach (var instance in item.instances)
                {
                    InstancesData.Add(new MeshInstanceData()
                    {
                        instance_id = m_draw_count,
                        material_id = m_material2id[instance.GetComponent<MeshRenderer>().sharedMaterial],
                        material = instance.GetComponent<MeshRenderer>().sharedMaterial,
                         renderer = instance.GetComponent<MeshRenderer>(),
                    });
                    ++m_draw_count;
                }
            }
        

            m_scene_gpu_data = new SceneData();
            m_scene_gpu_data.world_mat_cpu = new List<Matrix4x4>();
            m_scene_gpu_data.inverse_transpose_world_mat_cpu = new List<Matrix4x4>();

            
            foreach (var item in m_mesh_instances)
            {
                foreach (var instance in item.instances)
                {
                    m_scene_gpu_data.world_mat_cpu.Add(instance.transform.localToWorldMatrix);
                    m_scene_gpu_data.inverse_transpose_world_mat_cpu.Add(instance.transform.worldToLocalMatrix.transpose);
                    
                }
            }
            unsafe
            {
                m_scene_gpu_data.world_matrices = new ComputeBuffer(m_scene_gpu_data.world_mat_cpu.Count, sizeof(Matrix4x4));

                m_scene_gpu_data.world_matrices.SetData(m_scene_gpu_data.world_mat_cpu);

                m_scene_gpu_data.inverse_transpose_world_matrices = new ComputeBuffer(m_scene_gpu_data.inverse_transpose_world_mat_cpu.Count, sizeof(Matrix4x4));
                m_scene_gpu_data.inverse_transpose_world_matrices.SetData(m_scene_gpu_data.inverse_transpose_world_mat_cpu);
            }
        }

        ~Scene()
        {
            _SceneBVH.Dispose();
            _SceneBVH = null;

            compressed_index_buffer_GPU.Dispose();
            compressed_vertex_buffer_GPU.Dispose();
            compressed_to_world_buffer_GPU.Dispose();
            light_buffer_GPU.Dispose();
        }

        public GraphicsBuffer commandBuf;
        GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
        int m_draw_count;

        public void Rasterize(Material material)
        {
            m_scene_gpu_data.Apply();
            RenderParams rp = new RenderParams(material);
            rp.worldBounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));

            Graphics.RenderMeshIndirect(rp, m_raster_mesh, commandBuf, m_draw_count);
            
        }
        public void BuildLight()
        {
            IntegrateEmissiveTriangles();
        }

        public static void Clear()
        {
            instance = null;
        }
        private void BuildMeshBVH()
        {
            _SceneBVH = new RayTracingAccelerationStructure();
            foreach (var item in GameObject.FindObjectsOfType<MeshRendererAdditionalData>())
            {
                var render = item.GetComponent<MeshRenderer>();
                RayTracingSubMeshFlags[] subMeshFlags = new RayTracingSubMeshFlags[render.sharedMaterials.Length];

                Array.Fill<RayTracingSubMeshFlags>(subMeshFlags, RayTracingSubMeshFlags.Enabled);
                _SceneBVH.AddInstance(render, subMeshFlags);
                //_SceneBVH.upda
            }
            _SceneBVH.Build();
            Debug.Log("Ray Tracing BVH finish");
        }
        
        public void IntegrateEmissiveTriangles()
        {
            /// <summary>
            /// 计算满二叉树的高度，其中，这个二叉树的的叶子节点>=numLeafs并且节点数最少。
            /// </summary>
            /// <param name="numLeafs"></param>
            /// <returns></returns>
            int CalculateTreeLevels(int numLeafs)
            {
                return Mathf.CeilToInt(Mathf.Log(numLeafs, 2)) + 1;
            }

            uint BitExpansion(uint x)
            {
                x = (x | x << 16) & 0x30000ff;
                x = (x | x << 8) & 0x300f00f;
                x = (x | x << 4) & 0x30c30c3;
                x = (x | x << 2) & 0x9249249;
                return x;
            }
            Dictionary<LightModel, LightBlasInfo> model2blas = new Dictionary<LightModel, LightBlasInfo>(); ;


            if (model2blas == null)
                model2blas = new Dictionary<LightModel, LightBlasInfo>();
            model2blas.Clear();

            // Probably mesh light
            var pMeshLights = GameObject.FindObjectsOfType<MeshRendererAdditionalData>();

            int sumIndexCount = 0;
            // Add real mesh light into collect
            foreach (var light in pMeshLights)
            {
                var Mesh = light.GetComponent<MeshFilter>().sharedMesh;
                if (Mesh == null) continue;
                var materials = light.GetComponent<MeshRenderer>().sharedMaterials;

                for (int i = 0; i < Math.Min(materials.Length, Mesh.subMeshCount); i++)
                {
                    var material = materials[i];

                    if (material != null && material.shader.name == "wakuwaku/Diffuse")
                    {
                        var emissive_factor = material.GetVector("_EmissiveFactor");
                        // a mesh light must have a emissive texture
                        if (emissive_factor != Vector4.zero)
                        {
                            var tex = material.GetTexture("_EmissiveTex");
                            if (tex == null)
                                tex = Texture2D.whiteTexture;

                            LightModel lightModel = new LightModel()
                            {
                                Mesh = Mesh,
                                SubMeshIdx = i,
                                emissive_factor = emissive_factor,
                                EmissiveTex = tex as Texture2D,
                                transform = light.transform,
                            };



                            if (!model2blas.TryGetValue(lightModel, out var blasInfo))
                            {
                                blasInfo.IndexOffset = sumIndexCount;
                                blasInfo.IndexCount = (int)lightModel.Mesh.GetIndexCount(i);
                                sumIndexCount += blasInfo.IndexCount;

                                model2blas.Add(lightModel, blasInfo);
                            }
                        }
                    }
                }
            }

            if (sumIndexCount == 0)
            {
                Debug.Log("No light in the scene");
                //light_buffer_GPU
                //Shader.SetGlobalBuffer("g_compressed_index_buffer", compressed_index_buffer_GPU);
                //Shader.SetGlobalBuffer("g_compressed_vertex_buffer", compressed_vertex_buffer_GPU);
                //Shader.SetGlobalBuffer("g_compressed_to_world_buffer", compressed_to_world_buffer_GPU);
                //Shader.SetGlobalBuffer("g_light_buffer", light_buffer_GPU);
                return;
            }
            Debug.Assert(sumIndexCount % 3 == 0);

            ComputeBuffer triangleRadiance = new ComputeBuffer((int)(sumIndexCount / 3), 4 * 3);
            {
                Vector3Int[] array = new Vector3Int[(int)(sumIndexCount / 3)];
                triangleRadiance.name = "triangleRadiance";
                triangleRadiance.SetData(array);
            }

            ComputeBuffer triangleSumOfTexel = new ComputeBuffer((int)(sumIndexCount / 3), 4);
            {
                uint[] array = new uint[(int)(sumIndexCount / 3)];
                triangleSumOfTexel.name = "triangleSumOfTexel";
                triangleSumOfTexel.SetData(array);
            }


            Material emissive_integration = new UnityEngine.Material(Shader.Find("wakuwaku/EmissiveIntegration"));
            foreach (var item in model2blas)
            {
                var cmd = CommandBufferPool.Get("IntegrateEmissiveTriangles: " + item.Key.Mesh.name + item.Key.SubMeshIdx);
                emissive_integration.SetBuffer("_triangleRadianceList", triangleRadiance);
                emissive_integration.SetBuffer("_triangleNumOfTexels", triangleSumOfTexel);
                emissive_integration.SetInt("_triOffset", (int)(item.Value.IndexOffset / 3));

                cmd.SetRandomWriteTarget(1, triangleRadiance);
                cmd.SetRandomWriteTarget(2, triangleSumOfTexel);
                emissive_integration.SetTexture("_EmissionTex", item.Key.EmissiveTex);
                cmd.SetViewport(new Rect(0, 0, 2 * item.Key.EmissiveTex.width, 2 * item.Key.EmissiveTex.height));
                cmd.DrawMesh(item.Key.Mesh, Matrix4x4.identity, emissive_integration, item.Key.SubMeshIdx);
                UnityEngine.Graphics.ExecuteCommandBuffer(cmd);
            }

            T[] GetDate<T>(ComputeBuffer computeBuffer)
            {
                T[] ret = new T[computeBuffer.count];
                computeBuffer.GetData(ret);
                return ret;
            }
            var retRadianceBuffer = GetDate<Vector3Int>(triangleRadiance);
            var retNumOfTexelBuffer = GetDate<uint>(triangleSumOfTexel);

            List<EmissiveVertex> compressedVertexBuffer = new List<EmissiveVertex>();
            List<int> compressedIndexBuffer = new List<int>();

            List<PathTracingLight> pathTracingLights = new List<PathTracingLight>();
            // 
            //             List<float> compressed_area_buffer = new List<float>();
            //             List<Vector3> compressed_radiance_buffer = new List<Vector3>();
            List<Matrix4x4> compressed_to_world_buffer = new List<Matrix4x4>();
            Dictionary<LightModel, LightBlasInfo> model2blasModified = new Dictionary<LightModel, LightBlasInfo>();


            int numTotalBLASNode = 0;
            int numTotalLeafs = 0;
            foreach (var item in model2blas)
            {
                //                 LightBlasInfo newBlasInfo;
                //                 newBlasInfo.IndexOffset = compressedIndexBuffer.Count;

                Dictionary<EmissiveVertex, int> vertex2Index = new Dictionary<EmissiveVertex, int>();
                var indices = item.Key.Mesh.GetIndices(item.Key.SubMeshIdx);

                int numTriangles = (int)(item.Value.IndexCount / 3);
                for (uint triId = 0; triId < numTriangles; triId++)
                {
                    var indexInTriangle = item.Value.IndexOffset / 3 + triId;
                    Vector3 L = new Vector3(retRadianceBuffer[indexInTriangle].x, retRadianceBuffer[indexInTriangle].y, retRadianceBuffer[indexInTriangle].z) / 255.0f;
                    Vector3 averageL = L / retNumOfTexelBuffer[indexInTriangle];
                    averageL.Scale(item.Key.emissive_factor);
                    if (averageL.magnitude > 0)
                    {
                        Vector3[] p = new Vector3[3];
                        for (int vertId = 0; vertId < 3; vertId++)
                        {
                            int v = indices[triId * 3 + vertId];

                            EmissiveVertex vertex;
                            vertex.position = item.Key.Mesh.vertices[v];
                            vertex.normal = item.Key.Mesh.normals[v];

                            if (!vertex2Index.TryGetValue(vertex, out var index))
                            {
                                compressedVertexBuffer.Add(vertex);
                                index = (int)(compressedVertexBuffer.Count - 1);
                                vertex2Index.Add(vertex, index);
                            }
                            compressedIndexBuffer.Add(index);

                            p[vertId] = vertex.position;
                        }

                        float area = 0.5f * Vector3.Cross(p[2] - p[0], p[1] - p[0]).magnitude;

                        // float flux = Mathf.PI * area * averageL;
                        pathTracingLights.Add(
                            new PathTracingLight()
                            {
                                area = area,
                                radiance_or_irradiance = averageL,
                                shape_id = (compressedIndexBuffer.Count / 3) - 1,
                                to_world_id = compressed_to_world_buffer.Count
                            });
                    }
                }

                compressed_to_world_buffer.Add(item.Key.transform.localToWorldMatrix);

                //                 newBlasInfo.IndexCount = (compressedIndexBuffer.Count - newBlasInfo.IndexOffset);
                //                 model2blasModified.Add(item.Key, newBlasInfo);
                // 
                //                 int numTriangle = (int)(newBlasInfo.IndexCount / 3);
                //                 int numTreeLevels = CalculateTreeLevels(numTriangle);
                //                 int numTreeLeafs = 1 << (numTreeLevels - 1);
                // 
                // 
                //                 numTotalBLASNode += 2 * numTreeLeafs;
            }

            model2blas = model2blasModified;
            triangleRadiance.Release();
            triangleSumOfTexel.Release();

            // no light in the scene
            if (compressedIndexBuffer.Count == 0)
            {
                return;
            }

            if (/*IsUniformSample()*/true)
            {
                void Reset(ComputeBuffer buffer)
                {
                    if (buffer != null)
                        buffer.Release();
                }
                Reset(compressed_index_buffer_GPU);
                Reset(compressed_vertex_buffer_GPU);
                Reset(compressed_to_world_buffer_GPU);
                Reset(light_buffer_GPU);
                unsafe
                {
                    compressed_index_buffer_GPU = new ComputeBuffer(compressedIndexBuffer.Count / 3, 3 * sizeof(uint));
                    compressed_index_buffer_GPU.SetData(compressedIndexBuffer);

                    //                     compressed_radiance_buffer_GPU = new ComputeBuffer(compressed_radiance_buffer.Count, sizeof(Vector3));
                    //                     compressed_radiance_buffer_GPU.SetData(compressed_radiance_buffer);

                    compressed_vertex_buffer_GPU = new ComputeBuffer(compressedVertexBuffer.Count, sizeof(EmissiveVertex));
                    compressed_vertex_buffer_GPU.SetData(compressedVertexBuffer);

                    compressed_to_world_buffer_GPU = new ComputeBuffer(compressed_to_world_buffer.Count, sizeof(Matrix4x4));
                    compressed_to_world_buffer_GPU.SetData(compressed_to_world_buffer);

                    light_buffer_GPU = new ComputeBuffer(pathTracingLights.Count, sizeof(PathTracingLight));
                    light_buffer_GPU.SetData(pathTracingLights);
                }
                compressed_index_buffer_GPU.name = "compressed_index_buffer_GPU";
                compressed_vertex_buffer_GPU.name = "compressed_vertex_buffer_GPU";
                compressed_to_world_buffer_GPU.name = "compressed_to_world_buffer_GPU";
                light_buffer_GPU.name = "light_buffer_GPU";
                Shader.SetGlobalBuffer("g_compressed_index_buffer", compressed_index_buffer_GPU);
                Shader.SetGlobalBuffer("g_compressed_vertex_buffer", compressed_vertex_buffer_GPU);
                Shader.SetGlobalBuffer("g_compressed_to_world_buffer", compressed_to_world_buffer_GPU);
                Shader.SetGlobalBuffer("g_light_buffer", light_buffer_GPU);
            }
            else // bvh
            {
                Node[] BLAS = new Node[numTotalBLASNode];

                for (int i = 0; i < numTotalBLASNode; i++)
                    BLAS[i] = new Node();

                Node[] BLASRoots = new Node[model2blas.Count];
                // populate BLAS triangles
                {
                    int BLASOffset = 0;
                    foreach (var meshInfo in model2blas)
                    {
                        int numBLASTriangles = (int)(meshInfo.Value.IndexCount / 3);
                        int numTreeLevels = CalculateTreeLevels(numBLASTriangles);
                        int numTreeLeafs = 1 << (numTreeLevels - 1);

                        //Tuple<int, Node>[] LocalBLAS = new Tuple<int, Node>[numBLASTriangles];

                        List<Tuple<int, Node>> LocalBLAS = new List<Tuple<int, Node>>(numBLASTriangles);
                        Bounds BLASbound = new Bounds();

                        int meshIndexOffset = (int)meshInfo.Value.IndexOffset;
                        for (int triId = 0; triId < numBLASTriangles; triId++)
                        {
                            int i0 = compressedIndexBuffer[meshIndexOffset + 3 * triId];
                            int i1 = compressedIndexBuffer[meshIndexOffset + 3 * triId + 1];
                            int i2 = compressedIndexBuffer[meshIndexOffset + 3 * triId + 2];
                            Vector3 p0 = compressedVertexBuffer[i0].position;
                            Vector3 p1 = compressedVertexBuffer[i1].position;
                            Vector3 p2 = compressedVertexBuffer[i2].position;

                            float flux = 0;// compressedFluxBuffer[meshIndexOffset / 3 + triId];
                            Bounds bbox = new Bounds();
                            bbox.Encapsulate(p0);
                            bbox.Encapsulate(p1);
                            bbox.Encapsulate(p2);

                            BLASbound.Encapsulate(bbox);

                            Node node = new Node();
                            node.boundMin = bbox.min;
                            node.boundMax = bbox.max;
                            node.flux = flux;
                            node.ID = meshIndexOffset + 3 * triId;

                            LocalBLAS.Add(new Tuple<int, Node>(-1, node));
                        }

                        // generate sort keys
                        const int quantLevel = 32; //must <= 1024
                        for (int i = 0; i < numBLASTriangles; i++)
                        {
                            // center of bbox
                            Vector3 normPos = Vector3.zero;
                            for (int k = 0; k < 3; k++)
                            {
                                normPos[k] = ((0.5f * (LocalBLAS[i].Item2.boundMax + LocalBLAS[i].Item2.boundMin)) - BLASbound.min)[k] / BLASbound.size[k];
                            }
                            Vector3 normPos1 = ((0.5f * (LocalBLAS[i].Item2.boundMax + LocalBLAS[i].Item2.boundMin)) - BLASbound.min).Div(BLASbound.size);
                            Debug.Assert(normPos == normPos1);
                            uint quantX = BitExpansion(Math.Min(Math.Max(0u, (uint)(normPos.x * quantLevel)), (uint)quantLevel - 1));
                            uint quantY = BitExpansion(Math.Min(Math.Max(0u, (uint)(normPos.y * quantLevel)), (uint)quantLevel - 1));
                            uint quantZ = BitExpansion(Math.Min(Math.Max(0u, (uint)(normPos.z * quantLevel)), (uint)quantLevel - 1));
                            uint mortonCode = quantX * 4 + quantY * 2 + quantZ;
                            LocalBLAS[i] = new Tuple<int, Node>((int)mortonCode, LocalBLAS[i].Item2);
                        }

                        // sort it
                        LocalBLAS.Sort((Tuple<int, Node> l, Tuple<int, Node> r) =>
                        {
                            return l.Item1.CompareTo(r.Item1);
                        });

                        BLASOffset += numTreeLeafs;
                        for (int triID = 0; triID < numBLASTriangles; triID++)
                        {
                            BLAS[BLASOffset + triID] = LocalBLAS[triID].Item2;
                        }

                        // fill bogus lights
                        for (int triID = numBLASTriangles; triID < numTreeLeafs; triID++)
                        {
                            BLAS[BLASOffset + triID] = new Node { boundMin = Vector3.one * 1e8f/*Mathf.Infinity*/, boundMax = Vector3.one * -1e8f/*Mathf.NegativeInfinity*/, flux = 0 };
                        }

                        BLASOffset += numTreeLeafs;
                    }
                }

                // generate BLAS
                {
                    int BLASOffset = 0;
                    int modelId = 0;
                    foreach (var meshInfo in model2blas)
                    {
                        int numBLASTriangles = (meshInfo.Value.IndexCount / 3);
                        int numTreeLevels = CalculateTreeLevels(numBLASTriangles);
                        int numTreeLeafs = 1 << (numTreeLevels - 1);

                        // build level by level
                        for (int level = 1; level < numTreeLevels; level++)
                        {
                            int numLevelLights = numTreeLeafs >> level;
                            int levelStart = numLevelLights;
                            for (int levelLightId = 0; levelLightId < numLevelLights; levelLightId++)
                            {
                                int nodeid = levelStart + levelLightId;
                                int nodeAddr = BLASOffset + nodeid;
                                int firstChildId = nodeid << 1;
                                int secondChildId = firstChildId + 1;
                                int firstChildAddr = BLASOffset + firstChildId;
                                int secondChildAddr = BLASOffset + secondChildId;
                                BLAS[nodeAddr].flux = BLAS[firstChildAddr].flux + BLAS[secondChildAddr].flux;
                                Bounds temp_parent = new Bounds();
                                Bounds tempFirstChild = new Bounds(0.5f * (BLAS[firstChildAddr].boundMin + BLAS[firstChildAddr].boundMax),
                                    (BLAS[firstChildAddr].boundMin.GreaterThen(BLAS[firstChildAddr].boundMax)) ? Vector3.zero : BLAS[firstChildAddr].boundMax - BLAS[firstChildAddr].boundMin);

                                Bounds tempSecondChild = new Bounds(0.5f * (BLAS[secondChildAddr].boundMin + BLAS[secondChildAddr].boundMax),
                                    (BLAS[secondChildAddr].boundMin.GreaterThen(BLAS[secondChildAddr].boundMax)) ? Vector3.zero : BLAS[secondChildAddr].boundMax - BLAS[secondChildAddr].boundMin);

                                if (tempFirstChild.size != Vector3.zero)
                                    temp_parent.Encapsulate(tempFirstChild);

                                if (tempSecondChild.size != Vector3.zero)
                                    temp_parent.Encapsulate(tempSecondChild);

                                BLAS[nodeAddr].boundMin = temp_parent.min;
                                BLAS[nodeAddr].boundMax = temp_parent.max;
                            }
                        }
                        BLAS[BLASOffset + 1].ID = modelId;
                        BLASRoots[modelId++] = (BLAS[BLASOffset + 1]);

                        BLASOffset += 2 * numTreeLeafs;
                    }
                }

                unsafe
                {
                    //                     ComputeBuffer compressedIndexBufferGPU = new ComputeBuffer(compressedIndexBuffer.Count, sizeof(uint));
                    //                     compressedIndexBufferGPU.SetData(compressedIndexBuffer);
                    // 
                    //                     ComputeBuffer compressed_radiance_buffer_GPU = new ComputeBuffer(compressed_radiance_buffer.Count, sizeof(float));
                    //                     compressed_radiance_buffer_GPU.SetData(compressed_radiance_buffer);
                    // 
                    //                     ComputeBuffer compressedVertexBufferGPU = new ComputeBuffer(compressedVertexBuffer.Count, sizeof(EmissiveVertex));
                    //                     compressedVertexBufferGPU.SetData(compressedVertexBuffer);
                    // 
                    // 
                    //                     compressedVertexBufferGPU.Release();
                    //                     compressed_radiance_buffer_GPU.Release();
                    //                     compressedIndexBufferGPU.Release();
                }
            }
        }

        // mesh data
        public RayTracingAccelerationStructure SceneBVH { get { if (_SceneBVH == null) _SceneBVH = new RayTracingAccelerationStructure(); return _SceneBVH; } }
        private RayTracingAccelerationStructure _SceneBVH;

        // lights data
        public ComputeBuffer compressed_index_buffer_GPU;
        public ComputeBuffer compressed_vertex_buffer_GPU;
        public ComputeBuffer compressed_to_world_buffer_GPU;
        public ComputeBuffer light_buffer_GPU;

    }
}
