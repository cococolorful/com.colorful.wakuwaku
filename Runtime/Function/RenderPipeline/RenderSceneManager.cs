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
    public class RenderSceneManager
    {
        public static RenderSceneManager Instance {get { if (instance == null) Initialize(); return instance; }}
        static RenderSceneManager instance;
        public  static void Initialize()
        {
            Debug.Log("RenderSceneManager Initialize");
            Material s;
            
            instance = new RenderSceneManager();
        }

        public RenderSceneManager()
        {
            BuildMeshBVH();

            BuildLight();
        }
        ~RenderSceneManager()
        {
            _SceneBVH.Dispose();
            _SceneBVH = null;

            compressed_index_buffer_GPU.Dispose();
            compressed_vertex_buffer_GPU.Dispose();
            compressed_to_world_buffer_GPU.Dispose();
            light_buffer_GPU.Dispose();
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
