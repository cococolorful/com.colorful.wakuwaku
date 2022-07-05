using System;
using System.Collections.Generic;
using UnityEngine;
using wakuwaku.Function;
using wakuwaku.Function.WRenderPipeline;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFNode 
    {
        public string name;
        /// <summary> Indices of child nodes </summary>
        public int[] children;
        /// <summary> Local TRS </summary>

        /// <summary> Local position </summary>
        public float[] translation = new float[] {0,0,0};
        /// <summary> Local rotation </summary>
        public float[] rotation = new float[] {0,0,0,1};
        /// <summary> Local scale </summary>
        public float[] scale = new float[] {1,1,1};
        public int mesh = -1;
        public int skin = -1;
        public int camera = -1;
        public int weights = -1;

        public Extensions extensions;
        [Serializable]
        public class Extensions
        {
            [Serializable]
            public class KHR_lights_punctual_Extension
            {
                public int light;
            }
            public KHR_lights_punctual_Extension KHR_lights_punctual;
        }

        public class ImportResult
        {

            public int parent = -1;
            public int[] children;
            
            public Transform transform;
        }
        public static ImportResult[] Import(List<GLTFNode>  gLTFNodes, GLTFMesh.ImportResult[]  meshes,GLTFExtension gLTFExtensions,GameObject root,bool without_submesh)
        {
            List<ImportResult> results = new List<ImportResult>(gLTFNodes.Count);
            for (int i = 0; i < gLTFNodes.Count; i++)
            {
                results.Add(new ImportResult());
            }
            // Initialize
            for (int i = 0; i < gLTFNodes.Count; i++)
            {
                results[i] = new ImportResult()
                {
                    children = gLTFNodes[i].children,
                    transform = new GameObject().transform,
                };
                results[i].transform.SetPositionAndRotation(new Vector3(gLTFNodes[i].translation[0], gLTFNodes[i].translation[1], gLTFNodes[i].translation[2]),
                    new Quaternion(gLTFNodes[i].rotation[0], gLTFNodes[i].rotation[1], gLTFNodes[i].rotation[2], gLTFNodes[i].rotation[3]));

                results[i].transform.gameObject.name = gLTFNodes[i].name;
                if (results[i].transform.gameObject.name == "")
                    results[i].transform.gameObject.name = "wakuwaku" + results[i].transform.GetInstanceID();

                if (gLTFNodes[i].mesh != -1)
                {
                    if (meshes[gLTFNodes[i].mesh].materials.Length > 1 && without_submesh) // exist submesh,split it to child node
                    {

                        List<int> children;
                        if (results[i].children != null)
                            children = new List<int>(results[i].children);
                        else
                            children = new List<int>();
                        for (int j = 0; j < meshes[gLTFNodes[i].mesh].mesh.Length; j++)
                        {
                            var child = new ImportResult() { children = null, transform = new GameObject().transform };

                            MeshFilter mf = child.transform.gameObject.AddComponent<MeshFilter>();
                            mf.sharedMesh = meshes[gLTFNodes[i].mesh].mesh[j];

                            MeshRenderer mr = child.transform.gameObject.AddComponent<MeshRenderer>();
                            mr.sharedMaterial = meshes[gLTFNodes[i].mesh].materials[j];

                            child.transform.gameObject.AddComponent<MeshRendererAdditionalData>();
                            child.transform.SetParent(results[i].transform,false) ;
                            Debug.Assert(child.transform.localEulerAngles == Vector3.zero);
                            //child.transform.set
                            children.Add(results.Count);
                            results.Add(child);
                        }
                        results[i].children = children.ToArray();
                    }else
                    {
                        MeshFilter mf = results[i].transform.gameObject.AddComponent<MeshFilter>();
                        mf.sharedMesh = meshes[gLTFNodes[i].mesh].mesh[0];

                        MeshRenderer mr = results[i].transform.gameObject.AddComponent<MeshRenderer>();
                        mr.sharedMaterials = meshes[gLTFNodes[i].mesh].materials;

                        results[i].transform.gameObject.AddComponent<MeshRendererAdditionalData>();
                    }
                }

                if(gLTFNodes[i].extensions != null &&
                    gLTFNodes[i].extensions.KHR_lights_punctual != null)
                {
                    Debug.LogError("TODO: add Light");
                    var lightCmp = results[i].transform.gameObject.AddComponent<Light>();
                    //results[i].transform.gameObject.AddComponent<ColLight>();

                    var lightData = gLTFExtensions.KHR_lights_punctual.lights[gLTFNodes[i].extensions.KHR_lights_punctual.light];
                    if(lightData.type == "point")
                    {
                        lightCmp.type = LightType.Point;
                    }
                    else if (lightData.type == "directional")
                    {
                        lightCmp.type = LightType.Directional;
                    }
                    else if (lightData.type == "spot")
                    {
                        lightCmp.type = LightType.Spot;
                        lightCmp.innerSpotAngle = lightData.spot.innerConeAngle;
                        lightCmp.spotAngle = lightData.spot.outerConeAngle;
                    }
                    else
                    {
                        Debug.LogError("Unknown Light Type in node " + gLTFNodes[i].name);
                    }

                    lightCmp.color = new Color(lightData.color[0], lightData.color[1], lightData.color[2]);
                    lightCmp.intensity = lightData.intensity;
                }
            }

            // Set up hierarchy
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].children != null)
                {
                    foreach (var idx in results[i].children)
                    {
                        results[idx].parent = i;
                    }
                }
            }

            void ApplyTRSRecursive(int i)
            {
                
                if (results[i].children != null)
                    foreach (var child in results[i].children)
                    {
                        results[child].transform.SetParent(results[i].transform);
                        ApplyTRSRecursive(child);
                    }
            }
            // Apply TRS
            for (int i = 0; i < results.Count; i++)
            {
                if(results[i].parent == -1)
                {
                    results[i].transform.SetParent(root.transform);
                    ApplyTRSRecursive(i);
                }
            }
            return results.ToArray();

        }
    }
}

