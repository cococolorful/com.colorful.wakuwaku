using UnityEditor;

namespace wakuwaku.Function.WRenderPipeline
{
     [CustomEditor(typeof(WakuAdditionalCameraData))]
     [CustomEditorForRenderPipeline(typeof(WakuAdditionalCameraData), typeof(WRenderPipelineAsset))]
     public class WakuAdditionalCameraDataEditor : Editor
     {
         public override void OnInspectorGUI()
         {
         
         }
 
     }
}
