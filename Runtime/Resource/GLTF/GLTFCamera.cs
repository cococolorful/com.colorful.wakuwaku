using System;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFCamera
    {
        public Orthographic orthographic;
        public Perspective perspective;
        [Required] public CameraType type;
        public string name;
        [Serializable]
        public class Orthographic
        {
            [Required] public float xmag;
            [Required] public float ymag;
            [Required] public float zfar;
            [Required] public float znear;
        }

        [Serializable]
        public class Perspective
        {
            public float? aspectRatio;
            [Required] public float yfov;
            public float? zfar;
            [Required] public float znear;
        }
    }
}

