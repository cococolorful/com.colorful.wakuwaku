using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace wakuwaku.Function.WRenderPipeline
{

    public class RenderGraphView : GraphView,IEdgeConnectorListener
    {
        RGSearchWindowProvider m_RGSearchWindowProvider;
        EditorWindow m_EditorWindow;
        WRenderGraph m_RenderGraph;
        public RenderGraphView(EditorWindow editorWindow, WRenderGraph renderGraph) : base()
        {
            m_RenderGraph = renderGraph;

            m_EditorWindow = editorWindow;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            Insert(0, new GridBackground());

            this.AddManipulator(new SelectionDragger());

            m_RGSearchWindowProvider = ScriptableObject.CreateInstance<RGSearchWindowProvider>();
            m_RGSearchWindowProvider.Init(this,m_EditorWindow);

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_RGSearchWindowProvider);
            };

            //
            graphViewChanged = (GraphViewChange change) =>
            {
                if (change.elementsToRemove != null)
                {
                    foreach (var ele in change.elementsToRemove)
                    {
                        if (ele as RGNodeView != null)
                        {
                            RemovePass(ele as RGNodeView);

                            // remove attached edges in render graph view
                            Refresh();
                        }
                        else if (ele as Edge != null)
                        {
                            RemoveEdge(ele as Edge);
                        }
                    }
                }
               
                return change;
            };
        }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            foreach (var port in ports.ToList())
            {
                if (startAnchor.node == port.node ||
                    startAnchor.direction == port.direction ||
                    startAnchor.portType != port.portType)
                {
                    continue;
                }

                compatiblePorts.Add(port);
            }
            return compatiblePorts;
        }

        public void AddPassFromType(Type renderPassType,Vector2 pos)
        {
            if (renderPassType == null)
                throw new ArgumentNullException(nameof(renderPassType));

            RGNodeView rgNode = new RGNodeView(m_RenderGraph, Activator.CreateInstance(renderPassType) as IPass, this);
            rgNode.SetPosition(new Rect(pos, Vector2.zero));
            AddPass(rgNode);
        }

        private void AddPass(RGNodeView node)
        {
            m_RenderGraph.AddPass(node.userData as IPass);

            AddElement(node);
        }
        private void RemovePass(RGNodeView node)
        {
            m_RenderGraph.RemovePass(node.userData as IPass);

            RemoveElement(node);
        }
        private void AddEdge(Edge edgeView)
        {
            if (edgeView.input.connected) // target port has connected
            {
                RemoveEdge(edgeView);
            }

            var input_node = edgeView.input.node as RGNodeView;
            var output_node = edgeView.output.node as RGNodeView;

            var input = new WRenderGraph.Port()
            {
                 portName = edgeView.input.portName,
                 pass = input_node.userData as IPass,
            };
            var output = new WRenderGraph.Port()
            {
                portName = edgeView.output.portName,
                pass = output_node.userData as IPass,
            };
            if (m_RenderGraph.AddEdge(input,output))
            {
                edgeView.output.Connect(edgeView);
                edgeView.input.Connect(edgeView);
                AddElement(edgeView);
            }
        }
        private void RemoveEdge(Edge edgeView)
        {
            WRenderGraph.Port port = new WRenderGraph.Port()
            {
                pass = edgeView.input.node.userData as IPass,
                portName = edgeView.input.portName,
            };
            m_RenderGraph.RemoveEdge(port);

            // remove edge in render graph view
            var old_edge_enum = edgeView.input.connections.GetEnumerator();
            old_edge_enum.MoveNext();
            var old_edge = old_edge_enum.Current;
            old_edge.input.Disconnect(old_edge);
            old_edge.output.Disconnect(old_edge);
            RemoveElement(old_edge);
        }

        public void OnDrop(GraphView graphView, Edge edgeView)
        {
            AddEdge(edgeView);
        }

        public void Refresh()
        {
            if (m_RenderGraph == null)
                return;
            var eles = graphElements.ToList();
            foreach (var ele in eles)
            {
                RemoveElement(ele);
            }

            Dictionary<IPass, RGNodeView> pass2View = new Dictionary<IPass, RGNodeView>();
            foreach (var pass in m_RenderGraph.passes)
            {
                RGNodeView rgNode = new RGNodeView(m_RenderGraph, pass, this);

                rgNode.SetPosition(pass.__position);
                AddElement(rgNode);

                pass2View.Add(pass,rgNode);
            }

            m_RenderGraph.TopoTravese(passContext =>
            {
                foreach (var edge in passContext.Item2) // input edge
                {
                    Edge edgeView = new Edge();
                    foreach (var inputPort in pass2View[edge.input.pass].inputPorts)
                    {
                        if (edge.input.portName == inputPort.portName)
                        {
                            edgeView.input = inputPort;
                            break;
                        }
                    }
                    foreach (var outputPort in pass2View[edge.output.pass].outputPorts)
                    {
                        if (edge.output.portName == outputPort.portName)
                        {
                            edgeView.output = outputPort;
                            break;
                        }
                    }

                    edgeView.input.Connect(edgeView);
                    edgeView.output.Connect(edgeView);
                    AddElement(edgeView);
                }
            });
        }
        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            
            Debug.Log(position);
            position = m_EditorWindow.position.position + position;
            SearchWindow.Open(new SearchWindowContext(position), m_RGSearchWindowProvider);
        }

        public void SetGraphInfo(WRenderGraph bRenderGraph)
        {
            m_RenderGraph = bRenderGraph;
        }
    }





}
