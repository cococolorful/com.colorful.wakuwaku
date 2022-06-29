using wakuwaku.Resource.GLTF;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;



public class BufferedBinaryReader : IDisposable
{
    private readonly Stream stream;
    private readonly byte[] buffer;
    private readonly int bufferSize;
    private int bufferOffset;
    private int bufferedBytes;
    private int byteStride;

    private Bit2Converter bit2Converter;
    private Bit4Converter bit4Converter;

    public long Position { get { return stream.Position + bufferOffset; } set { stream.Position = value; bufferedBytes = 0; bufferOffset = 0; } }

    public BufferedBinaryReader(Stream stream, int bufferSize)
    {
        this.stream = stream;
        this.bufferSize = bufferSize;
        buffer = new byte[bufferSize];
        bufferOffset = 0;
        bufferedBytes = 0;
    }

    private void FillBuffer(int byteCount)
    {
        int unreadBytes = bufferedBytes - bufferOffset;

        if (unreadBytes < byteCount)
        {
            // If not enough bytes left in buffer
            if (unreadBytes != 0)
            {
                // If buffer still has unread bytes, move them to start of buffer
                Buffer.BlockCopy(buffer, bufferOffset, buffer, 0, unreadBytes);
            }

            bufferedBytes = stream.Read(buffer, unreadBytes, bufferSize - unreadBytes) + unreadBytes;
            bufferOffset = 0;
        }
    }

    public byte ReadByte()
    {
        FillBuffer(1);
        return buffer[bufferOffset++];
    }

    public sbyte ReadSByte()
    {
        FillBuffer(1);
        return (sbyte)buffer[bufferOffset++];
    }

    public ushort ReadUInt16()
    {
        FillBuffer(sizeof(ushort));
        return bit2Converter.Read(buffer, ref bufferOffset).@ushort;
    }

    public short ReadInt16()
    {
        FillBuffer(sizeof(short));
        return bit2Converter.Read(buffer, ref bufferOffset).@short;
    }

    public uint ReadUInt32()
    {
        FillBuffer(sizeof(uint));
        return bit4Converter.Read(buffer, ref bufferOffset).@uint;
    }

    public int ReadInt32()
    {
        FillBuffer(sizeof(int));
        return bit4Converter.Read(buffer, ref bufferOffset).@int;
    }

    public float ReadSingle()
    {
        FillBuffer(sizeof(float));
        return bit4Converter.Read(buffer, ref bufferOffset).@float;
    }

    public void Skip(int bytes)
    {
        FillBuffer(bytes);
        bufferOffset += bytes;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Bit2Converter
    {
        [FieldOffset(0)] public byte b0;
        [FieldOffset(1)] public byte b1;
        [FieldOffset(0)] public short @short;
        [FieldOffset(0)] public ushort @ushort;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bit2Converter Read(byte[] buffer, ref int bufferOffset)
        {
            b0 = buffer[bufferOffset++];
            b1 = buffer[bufferOffset++];
            return this;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Bit4Converter
    {
        [FieldOffset(0)] public byte b0;
        [FieldOffset(1)] public byte b1;
        [FieldOffset(2)] public byte b2;
        [FieldOffset(3)] public byte b3;
        [FieldOffset(0)] public float @float;
        [FieldOffset(0)] public int @int;
        [FieldOffset(0)] public uint @uint;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bit4Converter Read(byte[] buffer, ref int bufferOffset)
        {
            b0 = buffer[bufferOffset++];
            b1 = buffer[bufferOffset++];
            b2 = buffer[bufferOffset++];
            b3 = buffer[bufferOffset++];
            return this;
        }
    }

    public void Dispose()
    {
        stream.Close();
    }
}

namespace wakuwaku.Resource.GLTF
{
    public enum AlphaMode { OPAQUE, MASK, BLEND }
    public enum AccessorType { SCALAR, VEC2, VEC3, VEC4, MAT2, MAT3, MAT4 }
    public enum RenderingMode { POINTS = 0, LINES = 1, LINE_LOOP = 2, LINE_STRIP = 3, TRIANGLES = 4, TRIANGLE_STRIP = 5, TRIANGLE_FAN = 6 }
    public enum GLType { UNSET = -1, BYTE = 5120, UNSIGNED_BYTE = 5121, SHORT = 5122, UNSIGNED_SHORT = 5123, UNSIGNED_INT = 5125, FLOAT = 5126 }
    public enum Format { AUTO, GLTF, GLB }
    public enum CameraType { perspective, orthographic }
    public enum InterpolationMode { ImportFromFile = -1, LINEAR = 0, STEP = 1, CUBICSPLINE = 2 }

    public static class EnumExtensions
    {
        public static int ByteSize(this GLType gltype)
        {
            switch (gltype)
            {
                case GLType.BYTE:
                    return sizeof(sbyte);
                case GLType.UNSIGNED_BYTE:
                    return sizeof(byte);
                case GLType.SHORT:
                    return sizeof(short);
                case GLType.UNSIGNED_SHORT:
                    return sizeof(ushort);
                case GLType.FLOAT:
                    return sizeof(float);
                case GLType.UNSIGNED_INT:
                    return sizeof(uint);
                default:
                    Debug.LogError("GLType " + (int)gltype + " not supported!");
                    return 0;
            }
        }

        public static int ComponentCount(this AccessorType accessorType)
        {
            switch (accessorType)
            {
                case AccessorType.SCALAR:
                    return 1;
                case AccessorType.VEC2:
                    return 2;
                case AccessorType.VEC3:
                    return 3;
                case AccessorType.VEC4:
                    return 4;
                case AccessorType.MAT2:
                    return 4;
                case AccessorType.MAT3:
                    return 9;
                case AccessorType.MAT4:
                    return 16;
                default:
                    Debug.LogError("AccessorType " + accessorType + " not supported!");
                    return 0;
            }
        }
    }

    // TODO:Implement Required
    sealed class RequiredAttribute : Attribute
    {
    }
    //  
    //      public abstract class JsonConverter
    //      {
    //          public virtual bool CanRead => true;
    //  
    //          public virtual bool CanWrite => true;
    //  
    //          public abstract string WriteJson(object value);
    //  
    //          public abstract object ReadJson(string reader, /*Type objectType,*/ object existingValue);
    //  
    //          public abstract bool CanConvert(Type objectType);
    //      }
    //      public class Matrix4x4Converter : JsonConverter
    //      {
    //          public override bool CanConvert(Type objectType)
    //          {
    //              return objectType == typeof(Matrix4x4);
    //          }
    //  
    //          public override object ReadJson(string reader, object existingValue)
    //          {
    //              throw new NotImplementedException();
    //          }
    //  
    //          public override string WriteJson(object value)
    //          {
    //              Matrix4x4 m = (Matrix4x4)value;
    //  
    //              return JsonUtility.ToJson(m);
    //          }
    //      }

    [Serializable]
    public class GLTFObject
    {
        public int scene;
        [Required]
        public GLTFAsset asset;
        public List<GLTFScene> scenes;
        public List<GLTFNode> nodes;
        public List<GLTFMesh> meshes;
        //public List<GLTFAnimation> animations;
        public List<GLTFBuffer> buffers;
        public List<GLTFBufferView> bufferViews;
        public List<GLTFAccessor> accessors;
        //public List<GLTFSkin> skins;
        public List<GLTFTexture> textures;
        public List<GLTFImage> images;
        public List<GLTFMaterial> materials;
        public List<GLTFCamera> cameras;
        public List<string> extensionsUsed;
        public List<string> extensionsRequired;
        public GLTFExtension extensions;
        //         public GameObject Import()
        //         {
        //             GameObject gameObject = 
        //         }
    }
    [Serializable]
    public class GLTFExtension
    {
        [Serializable]
        public class KHR_lights_punctual_Extension
        {
            [Serializable]
            public class Light
            {
                public string name = "";
                public float[] color = new float[] { 1.0f, 1.0f, 1.0f };
                public float intensity = 1.0f;
                [Required]
                public string type;
                public float range = float.PositiveInfinity;
                [Serializable]
                public class SpotLight
                {
                    public float innerConeAngle = 0;
                    public float outerConeAngle = (float)(Math.PI / 4.0f);
                }
                public SpotLight spot;
            }

            public List<Light> lights;
        }
        public KHR_lights_punctual_Extension KHR_lights_punctual;
    }
    [Serializable]
    public class GLTFSkin
    {
    }
    [Serializable]
    public class GLTFAnimation
    {
    }
}
