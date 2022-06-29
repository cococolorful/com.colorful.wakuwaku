using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    public class WRenderGraph : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// the data of the graph,which can easy to serialize
        /// </summary
        public List<IPass> passes = new List<IPass>();


        public struct Port
        {
            public IPass pass;
            public string portName;
        }
        public struct Edge
        {
            public Port input;
            public Port output;
        }
        [NonSerialized]
        public Dictionary<IPass, Tuple<List<Edge>, List<Edge>>> NodeRelation = new Dictionary<IPass, Tuple<List<Edge>, List<Edge>>>();


        /// <summary>
        /// related operation
//         /// </summary>
//         public int CurrentPassId() { return m_NextPassID; }
        public void AddPass<Pass>(Pass pass) where Pass : IPass
        {
            //pass.Init(this/*,pass.GetType()*/);
            passes.Add(pass);
            NodeRelation.Add(pass, new Tuple<List<Edge>, List<Edge>>(new List<Edge>(), new List<Edge>()));
        }
        public void RemovePass<Pass>(Pass pass) where Pass : IPass
        {
            passes.Remove(pass);

            NodeRelation.TryGetValue(pass, out var relations);
            foreach (var inputEdge in relations.Item1)
            {
                NodeRelation[inputEdge.output.pass].Item2.Remove(inputEdge);

            }
            foreach (var outputEdge in relations.Item2)
            {
                NodeRelation[outputEdge.input.pass].Item1.Remove(outputEdge);

            }
            NodeRelation.Remove(pass);
        }
        public bool AddEdge(Port inputPort, Port outputPort)
        {
            if (inputPort.pass == outputPort.pass)
            {
                return false;
            }
            if (IsParent(outputPort.pass, inputPort.pass))
            {
                return false;
            }

            Edge edge = new Edge() { input = inputPort,output = outputPort};
            NodeRelation[inputPort.pass].Item1.Add(edge);
            NodeRelation[outputPort.pass].Item2.Add(edge);

            return true;
        }
        public void RemoveEdge(Port inputPort)
        {
            if (NodeRelation.TryGetValue(inputPort.pass, out var tuple))
            {
                foreach (var inputEdge in tuple.Item1)
                {
                    if (inputEdge.input.pass == inputPort.pass && inputEdge.input.portName == inputPort.portName)
                    {
                        NodeRelation[inputEdge.output.pass].Item2.Remove(inputEdge);
                        NodeRelation[inputEdge.input.pass].Item1.Remove(inputEdge);
                        return;
                    }

                }
            }
        }
        public bool IsParent(IPass child, IPass bummyParent)
        {
            if (!NodeRelation.TryGetValue(child, out var relations) || relations.Item1.Count == 0)
                return false;
            foreach (var inputEdge in relations.Item1)
            {
                var parent = inputEdge.output; // current node real parent
                if (parent.pass == bummyParent)
                {
                    return true;

                }
                else if (IsParent(parent.pass, bummyParent))
                    return true;
            }
            return false;
        }

        // topological travel
        /// <summary>
        /// pass , inputs , outputs
        /// </summary>
        /// <param name="action"></param>
        public void TopoTravese(Action<Tuple<IPass, List<Edge>, List<Edge>>> action)
        {
            // find entry
            Dictionary<IPass, bool> keyValuePairs = new Dictionary<IPass, bool>();
            Queue<IPass> queue = new Queue<IPass>();
            foreach (var rel in NodeRelation)
            {
                if (rel.Value.Item1.Count == 0)
                    queue.Enqueue(rel.Key);
            }

            while (queue.Count != 0)
            {
                var inputEdge = queue.Dequeue();
                action(new Tuple<IPass, List<Edge>, List<Edge>>(inputEdge, NodeRelation[inputEdge].Item1, NodeRelation[inputEdge].Item2));

                foreach (var child in NodeRelation[inputEdge].Item2)
                {
                    if (keyValuePairs.TryGetValue(child.input.pass, out bool value) && value == true)
                    {
                        continue;
                    }
                    queue.Enqueue(child.input.pass);
                    keyValuePairs.Add(child.input.pass, true);
                }
            }

        }

        [Serializable]
        struct PassInfo
        {
            [Serializable]
            public struct Parameter
            {
                [SerializeField]
                public byte[] valueBytes;

                [SerializeField]
                public UnityEngine.Object objRef;
            }
            [SerializeField]
            public string typeStr;
            [SerializeField]
            public List<Parameter> parasBytes;
        }
        [SerializeField]
        private List<PassInfo> __IPassInfo;

        [Serializable]
        struct EdgeInfo
        {
            public int inputPassIdx;
            public int intputResourceIdx;

            public int outputPassIdx;
            public int outtputResourceIdx;
        }
        [SerializeField]
        private List<EdgeInfo> __EdgeInfo;
        public void OnBeforeSerialize()
        {
            EndFrame();
            Cleanup();
            
            __IPassInfo = new List<PassInfo>(passes.Count);
            foreach (var pass in passes)
            {
                PassInfo info;
                info.typeStr = pass.GetType().ToString();
                info.parasBytes = new List<PassInfo.Parameter>(pass.__publicSeriParams.Count);

                foreach (var paraField in pass.__publicSeriParams)
                {
                    PassInfo.Parameter parameter = new PassInfo.Parameter();
                    try
                    {
                        var value = paraField.GetValue(pass);
                        if (value != null)
                        {
                            if (RGReflectionUtil.IsEngineObject(paraField.FieldType))
                            {
                                parameter.objRef = value as UnityEngine.Object;
                            }else
                            {
                                MemoryStream stream = new MemoryStream();
                                new XmlSerializer(paraField.FieldType).Serialize(stream, value);
                                parameter.valueBytes = stream.GetBuffer();
                            }

                            
                        }
                        else
                        {
                            throw new Exception(string.Format("the value of {0} is null,it should exist a default value", paraField.Name)); 
                        }
                    }
                    catch
                    {
                        Debug.LogWarning(string.Format("serialize \"{0}: {1}\" failed!", info.typeStr, paraField.Name));
                    }
                    info.parasBytes.Add(parameter);
                }
                __IPassInfo.Add(info);

            }

            __EdgeInfo = new List<EdgeInfo>();
            foreach (var relation in NodeRelation)
            {
                foreach (var input in relation.Value.Item1)
                {
                    EdgeInfo edgeInfo;
                    edgeInfo.inputPassIdx = passes.FindIndex(pass => { return pass == input.input.pass; });
                    edgeInfo.intputResourceIdx = input.input.pass.__inputs.FindIndex(field => { 
                        return field.Name == input.input.portName; });
                    edgeInfo.outputPassIdx = passes.FindIndex(pass => { return pass == input.output.pass; });
                    edgeInfo.outtputResourceIdx = input.output.pass.__outputs.FindIndex(field => { 
                        return field.Name == input.output.portName; });

                    __EdgeInfo.Add(edgeInfo);
                }
            }
        }


        public void OnAfterDeserialize()
        {
            passes = new List<IPass>(__IPassInfo.Count);
            try
            {
                foreach (var passInfo in __IPassInfo)
                {
                    if (passInfo.typeStr == null)
                        throw new ArgumentNullException(nameof(passInfo));

                    var pass = Activator.CreateInstance(RGReflectionUtil.GetTypeFromName(passInfo.typeStr)) as IPass;
                    for (int i = 0; i < pass.__publicSeriParams.Count; i++)
                    {
                        var field = pass.__publicSeriParams[i];
                        try
                        {
                            if(RGReflectionUtil.IsEngineObject(field.FieldType))
                            {
                                if (passInfo.parasBytes[i].objRef != null)
                                    field.SetValue(pass, passInfo.parasBytes[i].objRef);
                            }
                            else
                            {
                                MemoryStream stream = new MemoryStream(passInfo.parasBytes[i].valueBytes);
                                field.SetValue(pass, new XmlSerializer(field.FieldType).Deserialize(stream));

                            }
                        }
                        catch
                        {
                            field.SetValue(pass, System.Activator.CreateInstance(field.FieldType));
                            Debug.LogWarning(string.Format("Load data \"{0}: {1}\" failed!", passInfo.typeStr, field.Name));
                        }
                    }
                    AddPass(pass);
                }

                foreach (var edgeInfo in __EdgeInfo)
                {
                    Port input, output;
                    input.pass = passes[edgeInfo.inputPassIdx];
                    input.portName = input.pass.__inputs[edgeInfo.intputResourceIdx].Name;
                    output.pass = passes[edgeInfo.outputPassIdx];
                    output.portName = output.pass.__outputs[edgeInfo.outtputResourceIdx].Name;

                    AddEdge(input, output);
                }
            }
            catch (Exception e)
            {

                __IPassInfo.Clear();
                __EdgeInfo.Clear();
                throw;
            }
        }


        ///////////////////////////////////////////////////////////
        /// 
        /// temp resource during the execute
        ///////////////////////////////////////////////////////////
    

        [NonSerialized]
        private UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph m_BuildInRG;

        public void Cleanup()
        {
            if (m_BuildInRG != null)
                m_BuildInRG.Cleanup();
            m_BuildInRG = null;
        }
        public void EndFrame()
        {
            if (m_BuildInRG != null)
                m_BuildInRG.EndFrame();
        }

        public class CompiledPassInfo
        {
            public bool isCulled;
            public IPass pass;
        }
        public struct CompiledFieldInfo
        {
            public IPass pass;
            public FieldInfo field;
        }
        public List<CompiledPassInfo> topoPasses;

        // 用来在Execute阶段遍历
        private List<IPass> excutePasses;
        private Dictionary<CompiledFieldInfo, CompiledFieldInfo> input2Output;
        private Dictionary<CompiledFieldInfo, CompiledPassInfo> field2Pass;
        public void Compile()
        {
            topoPasses = new List<CompiledPassInfo>();
            excutePasses = new List<IPass>();
            input2Output = new Dictionary<CompiledFieldInfo, CompiledFieldInfo>();
            field2Pass = new Dictionary<CompiledFieldInfo, CompiledPassInfo>();



            TopoTravese(passContext =>
            {

                //var fieldInfos = RGReflectionUtil.GetPublicField(passContext.Item1.GetType());
                var compiledPassInfo = new CompiledPassInfo() { isCulled = false, pass = passContext.Item1 };

                foreach (var inputField in compiledPassInfo.pass.__inputs)
                {
                    bool hasInput = false;
                    // find the inputEdge
                    foreach (var inputEdge in NodeRelation[compiledPassInfo.pass].Item1)
                    {
                        if (inputEdge.input.portName == inputField.Name ) // find
                        {
                            CompiledFieldInfo outputField = new CompiledFieldInfo();
                            outputField.pass = inputEdge.output.pass;

                            foreach (var outputFieldIt in inputEdge.output.pass.__outputs)
                            {
                                if(inputEdge.output.portName == outputFieldIt.Name )
                                {
                                    outputField.field = outputFieldIt;
                                    break;
                                }
                            }

                            if(outputField.field != null && field2Pass[outputField].isCulled == false)
                            {
                                hasInput = true;
                                input2Output.Add(new CompiledFieldInfo { pass = inputEdge.input.pass, field = inputField }, outputField);
                                break;
                            }
                        }
                    }
                    if(!hasInput) //存在inputPort但是没有资源输入的节点。直接cull掉
                    {
                        compiledPassInfo.isCulled = true;
                    }
                }
                if(!compiledPassInfo.isCulled)
                    excutePasses.Add(compiledPassInfo.pass);
                topoPasses.Add(compiledPassInfo);
                foreach (var fieldInfo in compiledPassInfo.pass.__outputs)
                {
                    field2Pass.Add(new CompiledFieldInfo { pass = compiledPassInfo.pass, field = fieldInfo},compiledPassInfo);
                }


            });
        }
        [NonSerialized]
        private int frameIdx = 0;
        public void Excute(ScriptableRenderContext context, Camera camera, RenderPipelineAsset asset)
        {
            if (topoPasses == null)
                Compile();
            if (m_BuildInRG == null)
                m_BuildInRG = new UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph(name);

            var cmd = CommandBufferPool.Get(camera + " : " + name);
            UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraphParameters renderGraphParameters = new UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraphParameters()
            {
                scriptableRenderContext = context,
                commandBuffer = cmd,
                currentFrameIndex = frameIdx++
            };
            try
            {
                using(m_BuildInRG.RecordAndExecute(renderGraphParameters))
                {
                    foreach (var pass in excutePasses)
                    {
                        foreach (var inputField in pass.__inputs)
                        {
                            var outputField = input2Output[new CompiledFieldInfo { field = inputField,pass = pass}]; // TODO:对于没有input的pass，未能正确cull
                            inputField.SetValue(pass, outputField.field.GetValue(field2Pass[outputField].pass));
                        }
                        
                        pass.Execute(m_BuildInRG, camera, context, asset);
                    }
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                context.Submit();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogWarning("If you use enableAsync, make sure that all commands executed by the pass can be asynchronous ");
            }
            
        }
    }


}
