using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

using UnityEngine;
using UnityEditor.AssetImporters;

namespace wakuwaku.Resource.GLTF
{
    [ScriptedImporter(1,"gltf")]
    public class GLTFImporter : ScriptedImporter
    {
        public bool WithoutSubmesh = false;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string filepath = ctx.assetPath;
            string sceneName = null;
            
            string directoryRoot = filepath != null ? Directory.GetParent(filepath).ToString() + "/" : null;

            try
            {
                sceneName = Path.GetFileNameWithoutExtension(ctx.assetPath);

                GameObject gltfScene = new GameObject(sceneName);

                string json = File.ReadAllText(ctx.assetPath);
                GLTFObject s = JsonUtility.FromJson<GLTFObject>(json);
                CheckExtensionSupport(ref s.extensionsRequired);
                var bufferRes = GLTFBuffer.Import(s.buffers, ctx.assetPath);
                var bufferViewRes = GLTFBufferView.Import(s.bufferViews, bufferRes);
                var accessorRes = GLTFAccessor.Import(s.accessors, bufferViewRes);
                var imageRes = GLTFImage.Import(s.images, directoryRoot, bufferViewRes);
                var textureRes = GLTFTexture.Import(s.textures, imageRes);
                var materialRes = GLTFMaterial.Import(s.materials, textureRes);
                var meshRes = GLTFMesh.Import(s.meshes, accessorRes, materialRes, WithoutSubmesh);
                var nodeRes = GLTFNode.Import(s.nodes, meshRes,s.extensions, gltfScene, WithoutSubmesh);
                // Save asset
                SaveToAsset(gltfScene, ctx);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        void CheckExtensionSupport(ref List<string> extensionsUsed)
        {
            HashSet<string> extensionsSupported = new HashSet<string> {
                "KHR_lights_punctual"};
            foreach (var item in extensionsUsed)
            {
                if(!extensionsSupported.Contains(item))
                {
                    Debug.LogError("not support " + item);
                }
            }
         }
        private void SaveToAsset(GameObject root, AssetImportContext ctx)
        {
//             ctx.AddObjectToAsset(ctx.assetPath, gltfScene);
            ctx.AddObjectToAsset("main", root);
            ctx.SetMainObject(root);

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            AddMeshes(filters, skinnedRenderers, ctx);
            AddMaterials(renderers, skinnedRenderers, ctx);
/*            AddAnimations(animations, ctx, settings.animationSettings);*/
        }
        public static void AddMeshes(MeshFilter[] filters, SkinnedMeshRenderer[] skinnedRenderers, AssetImportContext ctx)
        {
            HashSet<Mesh> visitedMeshes = new HashSet<Mesh>();
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (visitedMeshes.Contains(mesh)) continue;
                ctx.AddAsset(mesh.name, mesh);
                visitedMeshes.Add(mesh);
            }
            //             for (int i = 0; i < skinnedRenderers.Length; i++)
            //             {
            //                 Mesh mesh = skinnedRenderers[i].sharedMesh;
            //                 if (visitedMeshes.Contains(mesh)) continue;
            //                 ctx.AddObjectToAsset(mesh.name, mesh);
            //                 visitedMeshes.Add(mesh);
            //             }
        }
        public static void AddMaterials(MeshRenderer[] renderers, SkinnedMeshRenderer[] skinnedRenderers, AssetImportContext ctx)
        {
            HashSet<Material> visitedMaterials = new HashSet<Material>();
            HashSet<Texture2D> visitedTextures = new HashSet<Texture2D>();
            for (int i = 0; i < renderers.Length; i++)
            {
                foreach (Material mat in renderers[i].sharedMaterials)
                {
                    /*if (mat == GLTFMaterial.defaultMaterial) continue;*/
                    if (visitedMaterials.Contains(mat)) continue;
                    if (string.IsNullOrEmpty(mat.name)) mat.name = "material" + visitedMaterials.Count;
                    ctx.AddAsset(mat.name, mat);
                    visitedMaterials.Add(mat);

                    // Add textures
                    foreach (Texture2D tex in mat.AllTextures())
                    {
                        // Dont add asset textures
                        //if (images[i].isAsset) continue;
                        if (visitedTextures.Contains(tex)) continue;
                        if (AssetDatabase.Contains(tex)) continue;
                        if (string.IsNullOrEmpty(tex.name)) tex.name = "texture" + visitedTextures.Count;
                        ctx.AddAsset(tex.name, tex);
                        visitedTextures.Add(tex);
                    }
                }
            }
            //             for (int i = 0; i < skinnedRenderers.Length; i++)
            //             {
            //                 foreach (Material mat in skinnedRenderers[i].sharedMaterials)
            //                 {
            //                     if (visitedMaterials.Contains(mat)) continue;
            //                     if (string.IsNullOrEmpty(mat.name)) mat.name = "material" + visitedMaterials.Count;
            //                     ctx.AddAsset(mat.name, mat);
            //                     visitedMaterials.Add(mat);
            // 
            //                     // Add textures
            //                     foreach (Texture2D tex in mat.AllTextures())
            //                     {
            //                         // Dont add asset textures
            //                         //if (images[i].isAsset) continue;
            //                         if (visitedTextures.Contains(tex)) continue;
            //                         if (AssetDatabase.Contains(tex)) continue;
            //                         if (string.IsNullOrEmpty(tex.name)) tex.name = "texture" + visitedTextures.Count;
            //                         ctx.AddAsset(tex.name, tex);
            //                         visitedTextures.Add(tex);
            //                     }
            //                 }
            //             }
        }
       

    }
    public static class AssetUtility
    {
        public static void AddAsset(this AssetImportContext ctx, string identifier, UnityEngine.Object obj)
        {
#if UNITY_2018_2_OR_NEWER
            ctx.AddObjectToAsset(identifier, obj);
#else
			ctx.AddSubAsset(identifier, obj);
#endif
        }
        public static IEnumerable<Texture2D> AllTextures(this Material mat)
        {
            int[] ids = mat.GetTexturePropertyNameIDs();
            for (int i = 0; i < ids.Length; i++)
            {
                Texture2D tex = mat.GetTexture(ids[i]) as Texture2D;
                if (tex != null) yield return tex;
            }
        }
    }
}



