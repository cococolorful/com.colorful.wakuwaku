using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFMesh
    {
        public List<GLTFPrimitive> primitives;
        /// <summary> Morph target weights </summary>
        public List<float> weights;
        public string name;

        public class ImportResult
        {
            public Material[] materials;
            public Mesh[] mesh;
        }
        public static ImportResult[] Import(List<GLTFMesh> gltfMeshes, GLTFAccessor.ImportResult[] accessors,GLTFMaterial.ImportResult[] materialRes)
        {
            void ReadUVs(ref List<Vector2> uvs, GLTFAccessor.ImportResult[] accessors, int texcoord, int vertCount)
            {
                // If there are no valid texcoords
                if (texcoord == -1)
                {
                    // If there are already uvs, add some empty filler uvs so it still matches the vertex array
                    if (uvs != null) uvs.AddRange(new Vector2[vertCount - uvs.Count]);
                    return;
                }
                Vector2[] _uvs = accessors[texcoord].ReadVec2(true);
                FlipY(ref _uvs);
                if (uvs == null) uvs = new List<Vector2>(_uvs);
                else uvs.AddRange(_uvs);
            }

            void FlipY(ref Vector2[] uv)
            {
                for (int i = 0; i < uv.Length; i++)
                {
                    uv[i].y = 1 - uv[i].y;
                }
            }

            var results = new ImportResult[gltfMeshes.Count];
            for (int i = 0; i < gltfMeshes.Count; i++)
            {
                results[i] = new ImportResult();
                results[i].mesh = new Mesh[gltfMeshes[i].primitives.Count];
                results[i].materials = new Material[gltfMeshes[i].primitives.Count];
                {
                    if (gltfMeshes[i].primitives.Count == 0)
                    {
                        Debug.LogWarning("0 primitives in mesh");
                    }
                    else
                    {
                        
                        for (int j = 0; j < gltfMeshes[i].primitives.Count; j++)
                        {
                            results[i].mesh[j] = new Mesh();
                            var primitive = gltfMeshes[i].primitives[j];

                            if (primitive.attributes.POSITION != -1)
                            {
                                IEnumerable<Vector3> newVerts = accessors[primitive.attributes.POSITION].ReadVec3(true).Select(v => { v.x = -v.x; return v; });
                                results[i].mesh[j].SetVertices(new List<Vector3>(newVerts));                            
                            }

                            // Tris - (Invert all triangles. Instead of flipping each triangle, just flip the entire array. Much easier)
                            if (primitive.indices != -1)
                            {
                                Debug.Assert(primitive.mode == (int)RenderingMode.TRIANGLES, "only support triangle now");
                                results[i].mesh[j].SetTriangles(new List<int>(accessors[primitive.indices].ReadInt().Reverse()), 0);
                                //results[i].mesh[j].SetIndices(new List<int>(accessors[primitive.indices].ReadInt().Reverse()), MeshTopology.Triangles,1);
                                //submeshTrisMode.Add(primitive.mode);
                            }
                            /// Normals - (X points left in GLTF)
                            if (primitive.attributes.NORMAL != -1)
                            {
                                results[i].mesh[j].SetNormals(new List<Vector3>(accessors[primitive.attributes.NORMAL].ReadVec3(true).Select(v => { v.x = -v.x; return v; })));
                                
                            }
                            // Tangents - (X points left in GLTF)
                            if (primitive.attributes.TANGENT != -1)
                            {
                                results[i].mesh[j].SetTangents(new List<Vector4>(accessors[primitive.attributes.TANGENT].ReadVec4(true).Select(v => { v.y = -v.y; v.z = -v.z; return v; })));

                            }
                            
                            if (primitive.material != -1)
                            {
                                results[i].materials[j] = materialRes[primitive.material].material;
                            }
                            else
                            {
                                Debug.LogWarning("No material,Add default material");
                                
                            }
                            List<Vector2> uv1 = null;
                            ReadUVs(ref uv1, accessors, primitive.attributes.TEXCOORD_0, results[i].mesh[j].vertexCount);
                            results[i].mesh[j].SetUVs(0, uv1);


                            // final
                            results[i].mesh[j].RecalculateBounds();
                            if (results[i].mesh[j].normals.Count() == 0)
                            {
                                results[i].mesh[j].RecalculateNormals();
                            }
                            if(results[i].mesh[j].tangents.Count() == 0)
                            {
                                results[i].mesh[j].RecalculateTangents();
                            }
                            results[i].mesh[j].name = gltfMeshes[i].name == null ? "mesh" : gltfMeshes[i].name + j;
                        }
                    }
                }
            }

            return results;
        }


        [Serializable]
        public class GLTFPrimitive
        {
            [Required] public GLTFAttributes attributes;
            /// <summary> Rendering mode</summary>
            public int mode = (int)RenderingMode.TRIANGLES;
            public int indices = -1;
            public int material = -1;
            /// <summary> Morph targets </summary>
            public List<GLTFAttributes> targets;

            [Serializable]
            public class GLTFAttributes
            {
                public int POSITION = -1;
                public int NORMAL = -1;
                public int TANGENT = -1;
                public int COLOR_0 = -1;
                public int TEXCOORD_0 = -1;
                public int TEXCOORD_1 = -1;
                public int TEXCOORD_2 = -1;
                public int TEXCOORD_3 = -1;
                public int TEXCOORD_4 = -1;
                public int TEXCOORD_5 = -1;
                public int TEXCOORD_6 = -1;
                public int TEXCOORD_7 = -1;
                public int JOINTS_0 = -1;
                public int JOINTS_1 = -1;
                public int JOINTS_2 = -1;
                public int JOINTS_3 = -1;
                public int WEIGHTS_0 = -1;
                public int WEIGHTS_1 = -1;
                public int WEIGHTS_2 = -1;
                public int WEIGHTS_3 = -1;
            }
        }
    }
}

