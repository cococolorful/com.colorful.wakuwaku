using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace wakuwaku.Function.WRenderPipeline
{
    public class RGSearchWindowProvider : ScriptableObject ,ISearchWindowProvider
    {
        private RenderGraphView SampleGraphView;
        private EditorWindow _editorWindow;
        public void Init(RenderGraphView sampleGraphView,EditorWindow editorWindow)
        {
            this.SampleGraphView = sampleGraphView;
            _editorWindow = editorWindow;   
        }
        List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>();
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Create Pass")));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(IPass))))
                    {
                        entries.Add(new SearchTreeEntry(new GUIContent(type.Name)) { level = 1, userData = type });
                    }
                }
            }

            return entries;
        }

        bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var type = searchTreeEntry.userData as System.Type;

            var windowRoot = _editorWindow.rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, context.screenMousePosition - _editorWindow.position.position);
            var graphMousePosition = SampleGraphView.contentViewContainer.WorldToLocal(windowMousePosition);

            SampleGraphView.AddPassFromType(type, graphMousePosition);
            return true;
        }
    }
}
