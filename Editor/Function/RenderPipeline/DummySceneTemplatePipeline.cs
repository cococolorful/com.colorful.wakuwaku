using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.SceneManagement;
public class DummySceneTemplatePipeline : ISceneTemplatePipeline
{
    public void BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName)
    {
        if (sceneTemplateAsset)
        {
            Debug.Log($"Before Template Pipeline {sceneTemplateAsset.name} isAdditive: {isAdditive} sceneName: {sceneName}");
        }
        GameObject camera = new GameObject();
        camera.AddComponent<Camera>();
        camera.AddComponent<UnityEngine.Rendering.FreeCamera>();
        
        //camera.AddComponent<RenderEngineManager>();
        //
        //GameObject light = new GameObject();
        //light.AddComponent<ColLight>();
    }

    public void AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName)
    {
        if (sceneTemplateAsset)
        {
            Debug.Log($"After Template Pipeline {sceneTemplateAsset.name} scene: {scene} isAdditive: {isAdditive} sceneName: {sceneName}");
        }
    }

    public bool IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset)
    {

        return true;
    }
}