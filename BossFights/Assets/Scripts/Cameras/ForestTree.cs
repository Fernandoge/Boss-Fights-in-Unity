using UnityEngine;
using UnityEngine.Rendering;

namespace Cameras
{
    // Marks a tree that the camera may hide while it is in the way; hidden trees still cast their shadow
    public class ForestTree : MonoBehaviour
    {
        private Renderer[] _renderers;
        private bool _isHidden;

        public void SetHidden(bool isHidden)
        {
            if (_isHidden == isHidden)
                return;

            _isHidden = isHidden;
            if (_renderers == null)
                _renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer treeRenderer in _renderers)
                treeRenderer.shadowCastingMode = isHidden ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        }
    }
}
