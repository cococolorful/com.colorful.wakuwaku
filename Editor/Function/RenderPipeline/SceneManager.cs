using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace wakuwaku.Function.WRenderPipeline
{
    public static class SceneManager
    {
        [MenuItem("wakuwaku/Build Render Scene")]
        static void Build()
        {
            Scene.Instance.BuildScene();
        }
    }
}
