using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace wakuwaku.Function.WRenderPipeline
{
    public static class CommandExtension
    {
        public static void RasterizeScene(this CommandBuffer cmd, string passname)
        {
            foreach (var item in Scene.Instance.InstancesData)
            {
                //Debug.Log(item.instance_id);
                //cmd.SetGlobalInt("instanceID", item.instance_id);
                //item.renderer.sharedMaterial.SetInt("instanceID", item.instance_id);
                cmd.DrawRenderer(item.renderer, item.renderer.sharedMaterial,0, 0);

                //RenderParams renderParams = new RenderParams(); 
                //renderParams.material =item.material;
                //
                //Graphics.RenderMesh(renderParams,Scene.Instance.m_mesh_instances[item.mesh_id].mesh,0,Matrix4x4.identity);
                //Graphics.ExecuteCommandBuffer(cmd);
                //cmd.DrawMeshInstancedIndirect(Scene.Instance.m_raster_mesh, 0, item.material, shader_pass, Scene.Instance.commandBuf, item.instance_id * GraphicsBuffer.IndirectDrawIndexedArgs.size);

            }
            
            
        }

    }
}
