using System.Collections.Generic;
using Manager.GameManager;
using UnityEngine;

namespace Cameras
{
    // Trees between the camera and the player or the boss stop being drawn, so a canopy never hides the fight
    public class ForestOcclusionFader : MonoBehaviour
    {
        [SerializeField] private float _castRadius = 4f;
        [SerializeField] private float _castLength = 120f;
        [SerializeField] private float _targetHeight = 1.2f;

        private readonly RaycastHit[] _hits = new RaycastHit[48];
        private readonly HashSet<ForestTree> _hiddenTrees = new HashSet<ForestTree>();
        private readonly HashSet<ForestTree> _occludingTrees = new HashSet<ForestTree>();
        private readonly List<ForestTree> _treesToShow = new List<ForestTree>();

        private void LateUpdate()
        {
            _occludingTrees.Clear();
            if (GameManager.Instance.player)
                CollectOccluders(GameManager.Instance.player.transform);

            if (GameManager.Instance.ActiveBoss)
                CollectOccluders(GameManager.Instance.ActiveBoss.transform);

            foreach (ForestTree tree in _occludingTrees)
                if (_hiddenTrees.Add(tree))
                    tree.SetHidden(true);

            _treesToShow.Clear();
            foreach (ForestTree tree in _hiddenTrees)
                if (!_occludingTrees.Contains(tree))
                    _treesToShow.Add(tree);

            foreach (ForestTree tree in _treesToShow)
            {
                _hiddenTrees.Remove(tree);
                if (tree)
                    tree.SetHidden(false);
            }
        }

        private void OnDisable()
        {
            foreach (ForestTree tree in _hiddenTrees)
                if (tree)
                    tree.SetHidden(false);

            _hiddenTrees.Clear();
        }

        private void CollectOccluders(Transform target)
        {
            Vector3 viewDirection = transform.forward;
            Vector3 origin = target.position + Vector3.up * _targetHeight - viewDirection * _castLength;
            int hitCount = Physics.SphereCastNonAlloc(origin, _castRadius, viewDirection, _hits, _castLength - 1f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                ForestTree tree = _hits[i].collider.GetComponentInParent<ForestTree>();
                if (tree)
                    _occludingTrees.Add(tree);
            }
        }
    }
}
