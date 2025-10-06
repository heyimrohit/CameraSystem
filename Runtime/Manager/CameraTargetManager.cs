using UnityEngine;
using System.Collections.Generic;
using System;

namespace Aetheriaum.CameraSystem.Manager
{
    public class CameraTargetManager
    {
        private Transform _currentTarget;
        private Transform _previousTarget;
        private readonly Dictionary<string, float> _targetWeights = new Dictionary<string, float>();

        public event Action<Transform, Transform> TargetChanged;

        public Transform CurrentTarget => _currentTarget;
        public Transform PreviousTarget => _previousTarget;

        public void SetTarget(Transform newTarget, float weight = 1f)
        {
            if (newTarget == null)
            {
                Debug.LogWarning("Attempting to set null camera target");
                return;
            }

            _previousTarget = _currentTarget;
            _currentTarget = newTarget;

            string targetId = newTarget.GetInstanceID().ToString();
            _targetWeights[targetId] = weight;

            TargetChanged?.Invoke(_previousTarget, _currentTarget);
        }

        public void ClearTarget()
        {
            _previousTarget = _currentTarget;
            _currentTarget = null;
            _targetWeights.Clear();

            TargetChanged?.Invoke(_previousTarget, null);
        }

        public bool HasTarget()
        {
            return _currentTarget != null && _currentTarget.gameObject.activeInHierarchy;
        }

        public Vector3 GetTargetPosition()
        {
            return HasTarget() ? _currentTarget.position : Vector3.zero;
        }

        public float GetTargetWeight(Transform target)
        {
            if (target == null) return 0f;
            string targetId = target.GetInstanceID().ToString();
            return _targetWeights.TryGetValue(targetId, out float weight) ? weight : 0f;
        }
    }
}