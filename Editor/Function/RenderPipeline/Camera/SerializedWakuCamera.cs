using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace wakuwaku.Function.WRenderPipeline
{
    class SerializedWakuCamera : ISerializedCamera
    {
        public SerializedObject serializedObject { get; }

        public SerializedObject serializedAdditionalDataObject { get; }
        public CameraEditor.Settings baseCameraSettings { get; }

        public SerializedProperty projectionMatrixMode => throw new System.NotImplementedException();

        // Common properties
        public SerializedProperty dithering => throw new System.NotImplementedException();

        public SerializedProperty stopNaNs => throw new System.NotImplementedException();

        public SerializedProperty allowDynamicResolution => throw new System.NotImplementedException();

        public SerializedProperty volumeLayerMask => throw new System.NotImplementedException();

        public SerializedProperty clearDepth => throw new System.NotImplementedException();

        public SerializedProperty antialiasing => throw new System.NotImplementedException();

        // Colorful specific properties
        public SerializedProperty render_graph;
        //public SerializedProperty frame_buffer_count;
        public SerializedWakuCamera(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            var additional_camera_data = (serializedObject.targetObject as Camera).GetComponent<WakuAdditionalCameraData>();
            serializedAdditionalDataObject = new SerializedObject(additional_camera_data);

            render_graph = serializedAdditionalDataObject.Find((WakuAdditionalCameraData d) => d.render_graph);
            //frame_buffer_count = serializedAdditionalDataObject.Find((WakuAdditionalCameraData d) => d.frame_buffer_count);

        }
        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
        }


        public void Refresh()
        {
        }

        public void Update()
        {
            serializedObject.Update();
            serializedAdditionalDataObject.Update();
        }
    }
}

