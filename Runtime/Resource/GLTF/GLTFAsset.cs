using System;

namespace wakuwaku.Resource.GLTF
{
    [Serializable]
    public class GLTFAsset 
    {
        /// <summary> A copyright message suitable for display to credit the content creator. </summary>
		public string copyright;
        /// <summary> Tool that generated this glTF model. Useful for debugging. </summary>
        public string generator;
        /// <summary> The glTF version that this asset targets. </summary>
        [Required] 
        public string version;
        /// <summary> The minimum glTF version that this asset targets. </summary>
        public string minVersion;
    }
}

