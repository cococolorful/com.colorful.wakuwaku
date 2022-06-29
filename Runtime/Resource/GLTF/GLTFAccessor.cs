using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFAccessor
    {
        public int bufferView = -1;
        public int byteOffset = 0;
        [Required] public string type;
        [Required] public int componentType;
        [Required] public int count;
        public float[] min;
        public float[] max;
        public Sparse sparse;

        [Serializable]
        public class Sparse
        {
            [Required] public int count;
            [Required] public Indices indices;
            [Required] public Values values;

            // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/README.md#values
            [Serializable]
            public class Values
            {
                [Required] public int bufferView;
                public int byteOffset = 0;
            }

            // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/README.md#indices
            [Serializable]
            public class Indices
            {
                [Required] public int bufferView;
                [Required] public int componentType;
                public int byteOffset = 0;
            }
        }

        public class ImportResult
        {
            private const float byteNormalize = 1f / byte.MaxValue;
            private const float shortNormalize = 1f / short.MaxValue;
            private const float ushortNormalize = 1f / ushort.MaxValue;
            private const float intNormalize = 1f / int.MaxValue;
            private const float uintNormalize = 1f / uint.MaxValue;

            public GLTFBufferView.ImportResult bufferView;
            public int? byteStride;
            public int count;
            public GLType componentType;
            public AccessorType type;
            public int byteOffset;
            public Sparse sparse;
            public class Sparse
            {
                public int count;
                public Indices indices;
                public Values values;

                public class Values
                {
                    public GLTFBufferView.ImportResult bufferView;
                    public int byteOffset = 0;
                }

                public class Indices
                {
                    public GLTFBufferView.ImportResult bufferView;
                    public GLType componentType;
                    public int byteOffset = 0;
                }
            }
            public Func<BufferedBinaryReader, int> GetIntReader(GLType componentType)
            {
                Func<BufferedBinaryReader, int> readMethod;
                switch (componentType)
                {
                    case GLType.BYTE:
                        return x => x.ReadSByte();
                    case GLType.UNSIGNED_BYTE:
                        return readMethod = x => x.ReadByte();
                    case GLType.FLOAT:
                        return readMethod = x => (int)x.ReadSingle();
                    case GLType.SHORT:
                        return readMethod = x => x.ReadInt16();
                    case GLType.UNSIGNED_SHORT:
                        return readMethod = x => x.ReadUInt16();
                    case GLType.UNSIGNED_INT:
                        return readMethod = x => (int)x.ReadUInt32();
                    default:
                        Debug.LogWarning("No componentType defined");
                        return readMethod = x => x.ReadInt32();
                }
            }

            public Func<BufferedBinaryReader, float> GetFloatReader(GLType componentType)
            {
                Func<BufferedBinaryReader, float> readMethod;
                switch (componentType)
                {
                    case GLType.BYTE:
                        return x => x.ReadSByte();
                    case GLType.UNSIGNED_BYTE:
                        return readMethod = x => x.ReadByte();
                    case GLType.FLOAT:
                        return readMethod = x => x.ReadSingle();
                    case GLType.SHORT:
                        return readMethod = x => x.ReadInt16();
                    case GLType.UNSIGNED_SHORT:
                        return readMethod = x => x.ReadUInt16();
                    case GLType.UNSIGNED_INT:
                        return readMethod = x => x.ReadUInt32();
                    default:
                        Debug.LogWarning("No componentType defined");
                        return readMethod = x => x.ReadSingle();
                }
            }

            public Func<BufferedBinaryReader, float> GetNormalizedFloatReader(GLType componentType)
            {
                Func<BufferedBinaryReader, float> readMethod;
                switch (componentType)
                {
                    case GLType.BYTE:
                        return x => x.ReadSByte();
                    case GLType.UNSIGNED_BYTE:
                        return readMethod = x => x.ReadByte() * byteNormalize;
                    case GLType.FLOAT:
                        return readMethod = x => x.ReadSingle();
                    case GLType.SHORT:
                        return readMethod = x => x.ReadInt16() * shortNormalize;
                    case GLType.UNSIGNED_SHORT:
                        return readMethod = x => x.ReadUInt16() * ushortNormalize;
                    case GLType.UNSIGNED_INT:
                        return readMethod = x => x.ReadUInt32() / uintNormalize;
                    default:
                        Debug.LogWarning("No componentType defined");
                        return readMethod = x => x.ReadSingle();
                }
            }
            public Matrix4x4[] ReadMatrix4x4()
            {
                if (!ValidateAccessorType(type, AccessorType.MAT4)) return new Matrix4x4[count];

                Func<BufferedBinaryReader, float> floatReader = GetFloatReader(componentType);

                Matrix4x4[] m = new Matrix4x4[count];
                if (bufferView != null)
                {
                    BufferedBinaryReader reader = new BufferedBinaryReader(bufferView.stream, 1024);
                    reader.Position = bufferView.byteOffset + byteOffset;
                    int byteSkip = byteStride.HasValue ? byteStride.Value - GetComponentSize() : 0;
                    for (int i = 0; i < count; i++)
                    {
                        m[i].m00 = floatReader(reader);
                        m[i].m01 = floatReader(reader);
                        m[i].m02 = floatReader(reader);
                        m[i].m03 = floatReader(reader);
                        m[i].m10 = floatReader(reader);
                        m[i].m11 = floatReader(reader);
                        m[i].m12 = floatReader(reader);
                        m[i].m13 = floatReader(reader);
                        m[i].m20 = floatReader(reader);
                        m[i].m21 = floatReader(reader);
                        m[i].m22 = floatReader(reader);
                        m[i].m23 = floatReader(reader);
                        m[i].m30 = floatReader(reader);
                        m[i].m31 = floatReader(reader);
                        m[i].m32 = floatReader(reader);
                        m[i].m33 = floatReader(reader);
                        reader.Skip(byteSkip);
                    }
                }
                if (sparse != null)
                {
                    Func<BufferedBinaryReader, int> indexIntReader = GetIntReader(sparse.indices.componentType);
                    BufferedBinaryReader indexReader = new BufferedBinaryReader(sparse.indices.bufferView.stream, 1024);
                    indexReader.Position = sparse.indices.bufferView.byteOffset + sparse.indices.byteOffset;
                    int[] indices = new int[sparse.count];
                    for (int i = 0; i < sparse.count; i++)
                    {
                        indices[i] = indexIntReader(indexReader);
                    }
                    BufferedBinaryReader valueReader = new BufferedBinaryReader(sparse.values.bufferView.stream, 1024);
                    indexReader.Position = sparse.values.bufferView.byteOffset + sparse.values.byteOffset;
                    for (int i = 0; i < sparse.count; i++)
                    {
                        int index = indices[i];
                        m[index].m00 = floatReader(valueReader);
                        m[index].m01 = floatReader(valueReader);
                        m[index].m02 = floatReader(valueReader);
                        m[index].m03 = floatReader(valueReader);
                        m[index].m10 = floatReader(valueReader);
                        m[index].m11 = floatReader(valueReader);
                        m[index].m12 = floatReader(valueReader);
                        m[index].m13 = floatReader(valueReader);
                        m[index].m20 = floatReader(valueReader);
                        m[index].m21 = floatReader(valueReader);
                        m[index].m22 = floatReader(valueReader);
                        m[index].m23 = floatReader(valueReader);
                        m[index].m30 = floatReader(valueReader);
                        m[index].m31 = floatReader(valueReader);
                        m[index].m32 = floatReader(valueReader);
                        m[index].m33 = floatReader(valueReader);
                    }
                }
                return m;
            }

            public Vector4[] ReadVec4(bool normalize = false)
            {
                if (!ValidateAccessorType(type, AccessorType.VEC4)) return new Vector4[count];

                Func<BufferedBinaryReader, float> floatReader = normalize ? GetNormalizedFloatReader(componentType) : GetFloatReader(componentType);

                Vector4[] v = new Vector4[count];
                if (bufferView != null)
                {
                    BufferedBinaryReader reader = new BufferedBinaryReader(bufferView.stream, 1024);
                    reader.Position = bufferView.byteOffset + byteOffset;
                    int byteSkip = byteStride.HasValue ? byteStride.Value - GetComponentSize() : 0;
                    for (int i = 0; i < count; i++)
                    {
                        v[i].x = floatReader(reader);
                        v[i].y = floatReader(reader);
                        v[i].z = floatReader(reader);
                        v[i].w = floatReader(reader);
                        reader.Skip(byteSkip);
                    }
                }
                if (sparse != null)
                {
                    Func<BufferedBinaryReader, int> indexIntReader = GetIntReader(sparse.indices.componentType);
                    BufferedBinaryReader indexReader = new BufferedBinaryReader(sparse.indices.bufferView.stream, 1024);
                    indexReader.Position = sparse.indices.bufferView.byteOffset + sparse.indices.byteOffset;
                    int[] indices = new int[sparse.count];
                    for (int i = 0; i < sparse.count; i++)
                    {
                        indices[i] = indexIntReader(indexReader);
                    }
                    BufferedBinaryReader valueReader = new BufferedBinaryReader(sparse.values.bufferView.stream, 1024);
                    indexReader.Position = sparse.values.bufferView.byteOffset + sparse.values.byteOffset;
                    for (int i = 0; i < sparse.count; i++)
                    {
                        int index = indices[i];
                        v[index].x = floatReader(valueReader);
                        v[index].y = floatReader(valueReader);
                        v[index].z = floatReader(valueReader);
                        v[index].w = floatReader(valueReader);
                    }
                }
                return v;
            }

            public Color[] ReadColor()
            {
                if (!ValidateAccessorTypeAny(type, AccessorType.VEC3, AccessorType.VEC4)) return new Color[count];

                Func<BufferedBinaryReader, float> floatReader = GetNormalizedFloatReader(componentType);

                Color[] c = new Color[count];
                if (bufferView != null)
                {
                    BufferedBinaryReader reader = new BufferedBinaryReader(bufferView.stream, 1024);
                    reader.Position = bufferView.byteOffset + byteOffset;
                    int byteSkip = byteStride.HasValue ? byteStride.Value - GetComponentSize() : 0;
                    if (type == AccessorType.VEC3)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            c[i].r = floatReader(reader);
                            c[i].g = floatReader(reader);
                            c[i].b = floatReader(reader);
                            reader.Skip(byteSkip);
                        }
                    }
                    else if (type == AccessorType.VEC4)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            c[i].r = floatReader(reader);
                            c[i].g = floatReader(reader);
                            c[i].b = floatReader(reader);
                            c[i].a = floatReader(reader);
                            reader.Skip(byteSkip);
                        }
                    }
                }
                if (sparse != null)
                {
                    Func<BufferedBinaryReader, int> indexIntReader = GetIntReader(sparse.indices.componentType);
                    BufferedBinaryReader indexReader = new BufferedBinaryReader(sparse.indices.bufferView.stream, 1024);
                    indexReader.Position = sparse.indices.bufferView.byteOffset + sparse.indices.byteOffset;
                    int[] indices = new int[sparse.count];
                    for (int i = 0; i < sparse.count; i++)
                    {
                        indices[i] = indexIntReader(indexReader);
                    }
                    BufferedBinaryReader valueReader = new BufferedBinaryReader(sparse.values.bufferView.stream, 1024);
                    indexReader.Position = sparse.values.bufferView.byteOffset + sparse.values.byteOffset;
                    if (type == AccessorType.VEC3)
                    {
                        for (int i = 0; i < sparse.count; i++)
                        {
                            int index = indices[i];
                            c[index].r = floatReader(valueReader);
                            c[index].g = floatReader(valueReader);
                            c[index].b = floatReader(valueReader);
                        }
                    }
                    else if (type == AccessorType.VEC4)
                    {
                        for (int i = 0; i < sparse.count; i++)
                        {
                            int index = indices[i];
                            c[index].r = floatReader(valueReader);
                            c[index].g = floatReader(valueReader);
                            c[index].b = floatReader(valueReader);
                            c[index].a = floatReader(valueReader);
                        }
                    }
                }
                return c;
            }

            public Vector3[] ReadVec3(bool normalize = false)
            {
                if (!ValidateAccessorType(type, AccessorType.VEC3)) return new Vector3[count];

                Func<BufferedBinaryReader, float> floatReader = normalize ? GetNormalizedFloatReader(componentType) : GetFloatReader(componentType);

                Vector3[] v = new Vector3[count];
                if (bufferView != null)
                {
                    BufferedBinaryReader reader = new BufferedBinaryReader(bufferView.stream, 1024);
                    reader.Position = bufferView.byteOffset + byteOffset;
                    int byteSkip = byteStride.HasValue ? byteStride.Value - GetComponentSize() : 0;
                    for (int i = 0; i < count; i++)
                    {
                        v[i].x = floatReader(reader);
                        v[i].y = floatReader(reader);
                        v[i].z = floatReader(reader);
                        reader.Skip(byteSkip);
                    }
                }
                if (sparse != null)
                {
                    Func<BufferedBinaryReader, int> indexIntReader = GetIntReader(sparse.indices.componentType);
                    BufferedBinaryReader indexReader = new BufferedBinaryReader(sparse.indices.bufferView.stream, 1024);
                    indexReader.Position = sparse.indices.bufferView.byteOffset + sparse.indices.byteOffset;
                    int[] indices = new int[sparse.count];
                    for (int i = 0; i < sparse.count; i++)
                    {
                        indices[i] = indexIntReader(indexReader);
                    }
                    BufferedBinaryReader valueReader = new BufferedBinaryReader(sparse.values.bufferView.stream, 1024);
                    valueReader.Position = sparse.values.bufferView.byteOffset + sparse.values.byteOffset;
                    for (int i = 0; i < sparse.count; i++)
                    {
                        int index = indices[i];
                        v[index].x = floatReader(valueReader);
                        v[index].y = floatReader(valueReader);
                        v[index].z = floatReader(valueReader);
                    }
                }
                return v;
            }

            public Vector2[] ReadVec2(bool normalize = false)
            {
                if (!ValidateAccessorType(type, AccessorType.VEC2)) return new Vector2[count];

                Func<BufferedBinaryReader, float> floatReader = normalize ? GetNormalizedFloatReader(componentType) : GetFloatReader(componentType);

                Vector2[] v = new Vector2[count];
                if (bufferView != null)
                {
                    BufferedBinaryReader reader = new BufferedBinaryReader(bufferView.stream, 1024);
                    reader.Position = bufferView.byteOffset + byteOffset;
                    int byteSkip = byteStride.HasValue ? byteStride.Value - GetComponentSize() : 0;
                    for (int i = 0; i < count; i++)
                    {
                        v[i].x = floatReader(reader);
                        v[i].y = floatReader(reader);
                        reader.Skip(byteSkip);
                    }
                }
                if (sparse != null)
                {
                    Func<BufferedBinaryReader, int> indexIntReader = GetIntReader(sparse.indices.componentType);
                    BufferedBinaryReader indexReader = new BufferedBinaryReader(sparse.indices.bufferView.stream, 1024);
                    indexReader.Position = sparse.indices.bufferView.byteOffset + sparse.indices.byteOffset;
                    int[] indices = new int[sparse.count];
                    for (int i = 0; i < sparse.count; i++)
                    {
                        indices[i] = indexIntReader(indexReader);
                    }
                    BufferedBinaryReader valueReader = new BufferedBinaryReader(sparse.values.bufferView.stream, 1024);
                    indexReader.Position = sparse.values.bufferView.byteOffset + sparse.values.byteOffset;
                    for (int i = 0; i < sparse.count; i++)
                    {
                        int index = indices[i];
                        v[index].x = floatReader(valueReader);
                        v[index].y = floatReader(valueReader);
                    }
                }
                return v;
            }

            public float[] ReadFloat()
            {
                if (!ValidateAccessorType(type, AccessorType.SCALAR)) return new float[count];

                Func<BufferedBinaryReader, float> floatReader = GetFloatReader(componentType);

                float[] f = new float[count];
                if (bufferView != null)
                {
                    BufferedBinaryReader reader = new BufferedBinaryReader(bufferView.stream, 1024);
                    reader.Position = bufferView.byteOffset + byteOffset;
                    int byteSkip = byteStride.HasValue ? byteStride.Value - GetComponentSize() : 0;
                    for (int i = 0; i < count; i++)
                    {
                        f[i] = floatReader(reader);
                        reader.Skip(byteSkip);
                    }
                }
                if (sparse != null)
                {
                    Func<BufferedBinaryReader, int> indexIntReader = GetIntReader(sparse.indices.componentType);
                    BufferedBinaryReader indexReader = new BufferedBinaryReader(sparse.indices.bufferView.stream, 1024);
                    indexReader.Position = sparse.indices.bufferView.byteOffset + sparse.indices.byteOffset;
                    int[] indices = new int[sparse.count];
                    for (int i = 0; i < sparse.count; i++)
                    {
                        indices[i] = indexIntReader(indexReader);
                    }
                    BufferedBinaryReader valueReader = new BufferedBinaryReader(sparse.values.bufferView.stream, 1024);
                    indexReader.Position = sparse.values.bufferView.byteOffset + sparse.values.byteOffset;
                    for (int i = 0; i < sparse.count; i++)
                    {
                        int index = indices[i];
                        f[index] = floatReader(valueReader);
                    }
                }
                return f;
            }

            public int[] ReadInt()
            {
                if (!ValidateAccessorType(type, AccessorType.SCALAR)) return new int[count];

                Func<BufferedBinaryReader, int> intReader = GetIntReader(componentType);

                int[] v = new int[count];
                if (bufferView != null)
                {
                    BufferedBinaryReader reader = new BufferedBinaryReader(bufferView.stream, 1024);
                    reader.Position = bufferView.byteOffset + byteOffset;
                    int byteSkip = byteStride.HasValue ? byteStride.Value - GetComponentSize() : 0;
                    for (int i = 0; i < count; i++)
                    {
                        v[i] = intReader(reader);
                        reader.Skip(byteSkip);
                    }
                }
                if (sparse != null)
                {
                    Func<BufferedBinaryReader, int> indexIntReader = GetIntReader(sparse.indices.componentType);
                    BufferedBinaryReader indexReader = new BufferedBinaryReader(sparse.indices.bufferView.stream, 1024);
                    indexReader.Position = sparse.indices.bufferView.byteOffset + sparse.indices.byteOffset;
                    int[] indices = new int[sparse.count];
                    for (int i = 0; i < sparse.count; i++)
                    {
                        indices[i] = indexIntReader(indexReader);
                    }
                    BufferedBinaryReader valueReader = new BufferedBinaryReader(sparse.values.bufferView.stream, 1024);
                    indexReader.Position = sparse.values.bufferView.byteOffset + sparse.values.byteOffset;
                    for (int i = 0; i < sparse.count; i++)
                    {
                        int index = indices[i];
                        v[index] = intReader(valueReader);
                    }
                }
                return v;
            }

            /// <summary> Get the size of the attribute type, in bytes </summary>
            public int GetComponentSize()
            {
                return type.ComponentCount() * componentType.ByteSize();
            }

            public static bool ValidateByteStride(int byteStride)
            {
                if (byteStride >= 4 && byteStride <= 252 && byteStride % 4 == 0) return true;
                Debug.Log("ByteStride of " + byteStride + " is invalid. Ignoring.");
                return false;
            }

            private static bool ValidateAccessorType(AccessorType type, AccessorType expected)
            {
                if (type == expected) return true;
                else
                {
                    Debug.LogError("Type mismatch! Expected " + expected + " got " + type);
                    return false;
                }
            }

            public static bool ValidateAccessorTypeAny(AccessorType type, params AccessorType[] expected)
            {
                for (int i = 0; i < expected.Length; i++)
                {
                    if (type == expected[i]) return true;
                }
                Debug.Log("Type mismatch! Expected " + string.Join("or ", expected) + ", got " + type);
                return false;
            }
        }

        public static ImportResult[] Import(List<GLTFAccessor> accessors, GLTFBufferView.ImportResult[] bufferViewRes)
        {
            var results = new ImportResult[accessors.Count];

            for (int i = 0; i < accessors.Count; i++)
            {
                var accessor = accessors[i];
                if (accessor.bufferView == -1)
                {
                    Debug.Assert(false);
                }

                results[i] = new ImportResult();
                {
                    results[i].bufferView = bufferViewRes[accessor.bufferView];
                    results[i].byteOffset = accessor.byteOffset;
                    results[i].count = accessor.count;
                    results[i].componentType = (GLType)accessor.componentType;
                    results[i].type = (AccessorType)Enum.Parse(typeof(AccessorType), accessor.type);
                    if (accessor.sparse != null)
                    {
                        if (accessor.sparse.indices == null || accessor.sparse.values == null)
                        {
                            accessor.sparse = null;
                            continue;
                        }
                        results[i].sparse = new ImportResult.Sparse();
                        {
                            results[i].sparse.count = accessor.sparse.count;
                            results[i].sparse.indices = new ImportResult.Sparse.Indices()
                            {
                                bufferView = bufferViewRes[accessor.sparse.indices.bufferView],
                                byteOffset = accessor.sparse.indices.byteOffset,
                                componentType = (GLType)accessor.sparse.indices.componentType
                            };
                            results[i].sparse.values = new ImportResult.Sparse.Values()
                            {
                                bufferView = bufferViewRes[accessor.sparse.values.bufferView],
                                byteOffset = accessor.sparse.values.byteOffset
                            };
                        };
                    }

                };
            }
            Parallel.ForEach(Enumerable.Range(0, accessors.Count), i =>
            {


            });
            return results;
        }
    }
}

