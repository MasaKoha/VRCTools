using System;
using System.Collections.Generic;
using UnityEngine;

namespace Masakoha.VRCTools.BackupBlendShapes.Editor
{
    [Serializable]
    internal class SkinnedMeshData
    {
        public SkinnedMeshRenderer renderer;
    }

    [Serializable]
    internal sealed class AvatarInfo
    {
        public List<BlendShape> blendShapes;
    }

    [Serializable]
    internal sealed class BlendShape
    {
        public string key;
        public float weight;
    }
}