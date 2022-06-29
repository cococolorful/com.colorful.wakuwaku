using System;
using System.Collections.Generic;
using UnityEngine;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFMaterial
    {
        public string name;
        public PbrMetalRoughness pbrMetallicRoughness;
        public TextureInfo normalTexture;
        public TextureInfo occlusionTexture;
        public TextureInfo emissiveTexture;
        public float[] emissiveFactor = new float[] { 0, 0, 0 };
        public string alphaMode = "OPAQUE";
        public float alphaCutoff = 0.5f;
        public bool doubleSided = false;

        [Serializable]
        public class PbrMetalRoughness
        {
            [Required] public float[] baseColorFactor = new float[] { 1, 1, 1, 1 };
            public TextureInfo baseColorTexture;
            public float metallicFactor = 1f;
            public float roughnessFactor = 1f;
            public TextureInfo metallicRoughnessTexture;
        }
        [Serializable]
        public class TextureInfo
        {
            [Required] public int index = -1;
            public int texCoord = 0;
            public float scale = 1;
        }

        public class ImportResult
        {
            public Material material;
        }

        public static ImportResult[] Import(List<GLTFMaterial> gLTFMaterials, GLTFTexture.ImportResult[] textureRes)
        {
            ImportResult[] results = new ImportResult[gLTFMaterials.Count];
            for (int i = 0; i < gLTFMaterials.Count; i++)
            {
                var material = gLTFMaterials[i];
                results[i] = new ImportResult();

                
                //if (material.doubleSided)
                //{
                //    results[i].material = new Material(Shader.Find("wakuwaku/DefaultLitDoubleSide"));
                //}
                //else
                {
                    results[i].material = new Material(Shader.Find("wakuwaku/MetalWorkFlow"));
                }

                if (material.pbrMetallicRoughness.baseColorTexture != null && material.pbrMetallicRoughness.baseColorTexture.index != -1)
                    results[i].material.SetTexture("_BaseColorTex", textureRes[material.pbrMetallicRoughness.baseColorTexture.index].cache);
                if(material.pbrMetallicRoughness.metallicRoughnessTexture != null && material.pbrMetallicRoughness.metallicRoughnessTexture.index != -1)
                    results[i].material.SetTexture("_OcclusionMetallicRoughnessTexture", textureRes[material.pbrMetallicRoughness.metallicRoughnessTexture.index].cache);
                results[i].material.SetVector("_BaseColorFactor",
                    new Vector4(material.pbrMetallicRoughness.baseColorFactor[0],
                    material.pbrMetallicRoughness.baseColorFactor[1],
                    material.pbrMetallicRoughness.baseColorFactor[2],
                    material.pbrMetallicRoughness.baseColorFactor[3])); 
                results[i].material.SetVector("_OcclusionMetallicRoughnessFactor",
                    new Vector4(1,
                    material.pbrMetallicRoughness.metallicFactor,
                    material.pbrMetallicRoughness.roughnessFactor,
                    1));
                results[i].material.name = gLTFMaterials[i].name;
                
            }
            return results;
        }
    }
}
