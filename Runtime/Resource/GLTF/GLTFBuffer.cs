using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFBuffer
    {
        [Required] public int byteLength;
        public string uri;
        public string name;


        public class ImportResult : IDisposable
        {
            public Stream stream;
            public long startOffset;

            public void Dispose()
            {
                stream.Dispose();
            }
            //public long startOffset;


        }
        ImportResult Import(string filepath/*, byte[] bytefile, long binChunkStart*/)
        {
            string directoryRoot = Directory.GetParent(filepath).ToString() + "/";
            var fullUrl = directoryRoot + uri;
            Debug.Assert(File.Exists(fullUrl));

            ImportResult result = new ImportResult();
            // Load URI
            result.stream = File.OpenRead(fullUrl);
            result.startOffset = result.stream.Length - byteLength; // TODO:why
            Debug.Assert(result.startOffset >= 0);
            return result;
        }

        public static ImportResult[] Import(List<GLTFBuffer> gLTFBuffers, string filepath)
        {
            var results = new ImportResult[gLTFBuffers.Count];
            var p = Parallel.ForEach(Enumerable.Range(0, gLTFBuffers.Count), i =>
            {
                results[i] = gLTFBuffers[i].Import(filepath);
            });
            return results;
        }

    }
}
