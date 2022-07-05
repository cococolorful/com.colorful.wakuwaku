using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

using UnityEngine;
using UnityEditor.AssetImporters;
using wakuwaku.Function.WRenderPipeline;

namespace wakuwaku.Resource.GLTF
{
    [CustomEditorForRenderPipeline(typeof(GLTFImporter),typeof(WRenderPipelineAsset))]
    public class GLTFImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical();

            var s = serializedObject.FindProperty("WithoutSubmesh");
            EditorGUILayout.PropertyField(s);
            EditorGUILayout.EndVertical();
            base.ApplyRevertGUI();
        }
    }
}



