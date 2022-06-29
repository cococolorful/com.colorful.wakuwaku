using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFTexture
    {
        public int sampler = -1;
        public int source = -1;
        public string name;

        public class ImportResult
        {
            private GLTFImage.ImportResult image;
            public Texture2D cache;

            /// <summary> Constructor </summary>
            public ImportResult(GLTFImage.ImportResult image)
            {
                this.image = image;
                this.cache = image.CreateTexture();
            }

        }

        public static ImportResult[] Import(List<GLTFTexture> textures, GLTFImage.ImportResult[] imageRes)
        {
            var results = new ImportResult[textures.Count];

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i].source != -1)
                    results[i] = new ImportResult(imageRes[textures[i].source]);
                else
                {
                    // default texture
                    throw new NotImplementedException();
                }
            }
            Parallel.ForEach(Enumerable.Range(0, textures.Count), i =>
            {

            });
            return results;

        }
    }
}