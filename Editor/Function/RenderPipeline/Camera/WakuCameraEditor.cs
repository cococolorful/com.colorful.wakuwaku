using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Graphs;
using UnityEngine;
using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

namespace wakuwaku.Function.WRenderPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(WRenderPipelineAsset))]
    public class WakuCameraEditor : CameraEditor
    {
        SerializedWakuCamera m_serialized_waku_camera;
        Camera m_camera;

        WakuAdditionalCameraData m_additional_camera_data;

        private new void OnEnable()
        {
            base.OnEnable();
            m_camera = target as Camera;
            m_additional_camera_data = m_camera.GetComponent<WakuAdditionalCameraData>();
            if (m_additional_camera_data == null)
                m_additional_camera_data = m_camera.gameObject.AddComponent<WakuAdditionalCameraData>();

            m_serialized_waku_camera = new SerializedWakuCamera(serializedObject);
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            //base.OnInspectorGUI();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.ObjectField(m_serialized_waku_camera.render_graph);
            m_additional_camera_data.frame_buffer_count = EditorGUILayout.IntField("frame_buffer_count",m_additional_camera_data.frame_buffer_count);
            m_additional_camera_data.use_native_render = EditorGUILayout.Toggle("use_native_render", m_additional_camera_data.use_native_render);
            EditorGUILayout.EndVertical();

            m_serialized_waku_camera.Apply();
        }

    }
}
