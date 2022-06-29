using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFBufferView {

        [Required] public int buffer;
        [Required] public int byteLength;
        public int byteOffset = 0;
        public int byteStride = -1;
        /// <summary> OpenGL buffer target </summary>
        public int target = -1;
        public string name;
        public class ImportResult
        {
            public Stream stream;
            public int byteOffset;
            public int byteLength;
            public int? byteStride;
        }
        public static ImportResult[] Import(List<GLTFBufferView> bufferViews, GLTFBuffer.ImportResult[] bufferRes)
        {
            var results = new ImportResult[bufferViews.Count];
            Parallel.ForEach(Enumerable.Range(0, bufferViews.Count), i =>
            {
                var buffer = bufferRes[bufferViews[i].buffer];

                results[i]= new ImportResult();
                results[i].stream = buffer.stream;
                results[i].byteOffset = bufferViews[i].byteOffset;
                results[i].byteOffset += (int)buffer.startOffset;
                results[i].byteLength = bufferViews[i].byteLength;
                results[i].byteStride = bufferViews[i].byteStride;
            });
            return results;

        }



    }
}

