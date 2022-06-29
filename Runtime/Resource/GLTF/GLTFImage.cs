using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFImage
    {
        public string uri;
        /// <summary> Either "image/jpeg" or "image/png" </summary>
        public string mimeType;
        public int bufferView = -1;
        public string name;

        public class ImportResult
        {
            public byte[] bytes;
            public string path;

            public ImportResult(byte[] bytes, string relativePath = null)
            {
                this.bytes = bytes;
                this.path = relativePath;
            }

            public Texture2D CreateTexture()
            {
                if (!string.IsNullOrEmpty(path))
                {

#if UNITY_EDITOR
                    // Load textures from asset database if we can
                    Texture2D assetTexture = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
                    if (assetTexture == null)
                    {
                        var str = Application.dataPath;
                        string relativePath = Path.GetRelativePath(str, path);
                        relativePath = "Assets/" + relativePath;
                        assetTexture = UnityEditor.AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D)) as Texture2D;
                    }
                    return assetTexture;
#endif
                }
                return null;
            }
        }

        public static ImportResult[] Import(List<GLTFImage> gLTFImages, string directoryRoot, GLTFBufferView.ImportResult[] bufferViews)
        {
            ImportResult[] results = new ImportResult[gLTFImages.Count];

            Parallel.ForEach(Enumerable.Range(0, results.Length), i =>
            {
                var image = gLTFImages[i];
                var fullUri = directoryRoot + gLTFImages[i].uri;
                if (!string.IsNullOrEmpty(image.uri))
                {
                    if (File.Exists(fullUri))
                    {
                        byte[] bytes = File.ReadAllBytes(fullUri);
                        results[i] = new ImportResult(bytes, fullUri);
                    } else if (image.uri.StartsWith("data:"))
                    {
                        throw new NotImplementedException();
                    } else
                        throw new NotImplementedException();
                }
                else  // bufferView is defined
                {
                    Debug.Assert(image.bufferView != -1 && !string.IsNullOrEmpty(image.mimeType));
                    throw new NotImplementedException();

                }
            });
            return results;
        }
    }
}
