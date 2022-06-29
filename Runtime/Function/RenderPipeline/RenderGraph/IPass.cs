using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using ComputeBufferHandle = UnityEngine.Experimental.Rendering.RenderGraphModule.ComputeBufferHandle;
using RendererListHandle = UnityEngine.Experimental.Rendering.RenderGraphModule.RendererListHandle;
using TextureHandle = UnityEngine.Experimental.Rendering.RenderGraphModule.TextureHandle;
namespace wakuwaku.Function.WRenderPipeline
{
    public abstract class IPass
    {
        public enum PinType { Read, Write, ReadWrite };

        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
        public class PortPinAttribute : Attribute
        {
            public PinType pinType { private set; get; } = PinType.Write;

            public PortPinAttribute(PinType pinType)
            {
                this.pinType = pinType;

            }
         }

        public bool enableAsyncCompute  = false;
        public bool allowPassCulling = true;

        protected IPass()
        {
            var fieldInfos = RGReflectionUtil.GetPublicField(this.GetType());
            __inputs = fieldInfos.Item1;
            __outputs = fieldInfos.Item2;
            __publicSeriParams = fieldInfos.Item3;
        }

        public abstract void Execute(
   UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph renderGraph,
   Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset
   );

        #region  RenderGraph Aux
        // Aux operation
        protected UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph m_renderGraph;

        protected TextureHandle CreateTexture(
           in UnityEngine.Experimental.Rendering.RenderGraphModule.TextureDesc desc)
        {
            return m_renderGraph.CreateTexture(desc);
        }
        protected  TextureHandle ImportTexture(RTHandle rt)
        {
            return m_renderGraph.ImportTexture(rt);
        }

        protected  TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            return m_renderGraph.ImportBackbuffer(rt);
        }

        protected  RendererListHandle CreateRendererList(in RendererListDesc desc)
        {
            return m_renderGraph.CreateRendererList(in desc);
        }

        protected  ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
        {
            return m_renderGraph.ImportComputeBuffer(computeBuffer);
        }

        protected  ComputeBufferHandle CreateComputeBuffer(in UnityEngine.Experimental.Rendering.RenderGraphModule.ComputeBufferDesc desc)
        {
            return m_renderGraph.CreateComputeBuffer(in desc);
        }

        protected  ComputeBufferHandle CreateComputeBuffer(in  ComputeBufferHandle computeBuffer)
        {
            return m_renderGraph.CreateComputeBuffer(in computeBuffer);
        }
 #endregion

        #region RenderGraphBuilder Aux
         protected UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraphBuilder m_builder;
        #endregion

        [HideInInspector]
        public Rect __position;

        [NonSerialized]
        public List<FieldInfo> __inputs;
        [NonSerialized]
        public List<FieldInfo> __outputs;
        [NonSerialized]
        public List<FieldInfo> __publicSeriParams;
        [NonSerialized]
        public List<FieldInfo> __renderList;
    }

    public abstract class IRenderPass<Derived> : IPass  where Derived : class,new ()
     {

        public override void Execute(
    UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph renderGraph,
    Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset
    )
        {
            m_renderGraph = renderGraph;
            using (m_builder = m_renderGraph.AddRenderPass<Derived>(nameof(Derived), out var DerivedData))
            {
                m_builder.EnableAsyncCompute(enableAsyncCompute);
                m_builder.AllowPassCulling(allowPassCulling);
                
                AllocateWriteResource(camera, context, asset);

                foreach (var input in __inputs)
                {
                    if (input.FieldType == typeof(TextureHandle))
                    {
                        m_builder.ReadTexture((TextureHandle)input.GetValue(this));
                    }
                    else if (input.FieldType == typeof(ComputeBufferHandle))
                    {
                        m_builder.ReadComputeBuffer((ComputeBufferHandle)input.GetValue(this));
                    }else if(input.FieldType == typeof(RendererListHandle))
                    {
                        m_builder.UseRendererList((RendererListHandle)input.GetValue(this));
                    }
                }
                foreach (var output in __outputs)
                {
                    if (output.FieldType == typeof(TextureHandle))
                    {
                        m_builder.WriteTexture((TextureHandle)output.GetValue(this));
                    }
                    else if (output.FieldType == typeof(ComputeBufferHandle))
                    {
                        m_builder.WriteComputeBuffer((ComputeBufferHandle)output.GetValue(this));
                    }
                    else if (output.FieldType == typeof(RendererListHandle))
                    {
                        m_builder.UseRendererList((RendererListHandle)output.GetValue(this));
                    }
                }

                m_builder.SetRenderFunc(
                (Derived DerivedData, UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraphContext ctx) =>
                {
                    ctx.cmd.name = DerivedData.ToString();
                    Execute(ctx);
                });
            }
        }

        protected abstract void AllocateWriteResource(Camera camera, ScriptableRenderContext context, RenderPipelineAsset asset);
        protected abstract void Execute(
            UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraphContext ctx)
        ;

        
    }
}
