using Aetheriaum.CameraSystem.Interfaces;
using UnityEngine;

namespace Coty.Camera.Components
{
    /// <summary>
    /// Camera collision detection component preventing camera clipping through geometry.
    /// Implements smooth camera adjustment when obstacles are detected.
    /// </summary>
    [System.Serializable]
    public class CameraCollisionComponent : ICameraCollision
    {
        #region Serialized Fields

        [Header("Collision Settings")]
        [SerializeField] private bool _isEnabled = true;
        [SerializeField] private LayerMask _collisionLayers = -1;
        [SerializeField] private float _collisionBuffer = 0.2f;
        [SerializeField] private float _collisionRadius = 0.1f;
        [SerializeField] private float _adjustmentSpeed = 5f;

        [Header("Debug")]
        [SerializeField] private bool _debugDraw = false;

        #endregion

        #region Properties

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public LayerMask CollisionLayers
        {
            get => _collisionLayers;
            set => _collisionLayers = value;
        }

        public float CollisionBuffer
        {
            get => _collisionBuffer;
            set => _collisionBuffer = Mathf.Max(0f, value);
        }

        public float CollisionRadius
        {
            get => _collisionRadius;
            set => _collisionRadius = Mathf.Max(0.01f, value);
        }

        public float AdjustmentSpeed
        {
            get => _adjustmentSpeed;
            set => _adjustmentSpeed = Mathf.Max(0.1f, value);
        }

        #endregion

        #region Private Fields

        private Vector3 _lastValidPosition;
        private bool _hasValidPosition = false;

        #endregion

        #region ICameraCollision Implementation

        public Vector3 CheckCollision(Vector3 desiredPosition, Vector3 targetPosition)
        {
            if (!_isEnabled)
                return desiredPosition;

            // Check for collision between target and desired camera position
            var directionToCamera = (desiredPosition - targetPosition).normalized;
            var maxDistance = Vector3.Distance(targetPosition, desiredPosition);

            // Perform sphere cast from target towards desired camera position
            if (Physics.SphereCast(
                targetPosition,
                _collisionRadius,
                directionToCamera,
                out RaycastHit hit,
                maxDistance,
                _collisionLayers))
            {
                // Collision detected, adjust position
                var adjustedDistance = Mathf.Max(0f, hit.distance - _collisionBuffer);
                var adjustedPosition = targetPosition + directionToCamera * adjustedDistance;

                _lastValidPosition = adjustedPosition;
                _hasValidPosition = true;

                return adjustedPosition;
            }

            // No collision, use desired position
            _lastValidPosition = desiredPosition;
            _hasValidPosition = true;

            return desiredPosition;
        }

        public bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            if (!_isEnabled)
                return true;

            var direction = (to - from).normalized;
            var distance = Vector3.Distance(from, to);

            // Check if there's anything blocking the line of sight
            return !Physics.SphereCast(
                from,
                _collisionRadius,
                direction,
                out _,
                distance,
                _collisionLayers);
        }

        public Vector3 GetClosestValidPosition(Vector3 desiredPosition, Vector3 targetPosition)
        {
            if (!_isEnabled)
                return desiredPosition;

            // Try the desired position first
            if (HasLineOfSight(targetPosition, desiredPosition))
                return desiredPosition;

            // If desired position is blocked, try to find a valid position
            var directionToCamera = (desiredPosition - targetPosition).normalized;
            var maxDistance = Vector3.Distance(targetPosition, desiredPosition);

            // Sample positions at different distances
            var sampleCount = 10;
            for (int i = sampleCount - 1; i >= 0; i--)
            {
                var testDistance = (maxDistance / sampleCount) * i;
                var testPosition = targetPosition + directionToCamera * testDistance;

                if (HasLineOfSight(targetPosition, testPosition))
                {
                    return testPosition;
                }
            }

            // If no valid position found, return last known valid position or target position
            return _hasValidPosition ? _lastValidPosition : targetPosition;
        }

        public void DrawDebugInfo()
        {
            if (!_debugDraw || !_hasValidPosition)
                return;

            // Draw collision sphere at last valid position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_lastValidPosition, _collisionRadius);

            // Draw collision buffer
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lastValidPosition, _collisionRadius + _collisionBuffer);
        }

        #endregion

        #region Advanced Collision Methods

        /// <summary>
        /// Check collision with smooth interpolation from current position
        /// </summary>
        public Vector3 CheckCollisionSmooth(Vector3 currentPosition, Vector3 desiredPosition, Vector3 targetPosition, float deltaTime)
        {
            if (!_isEnabled)
                return desiredPosition;

            var collisionCheckedPosition = CheckCollision(desiredPosition, targetPosition);

            // Smooth interpolation to avoid jarring camera movements
            return Vector3.Lerp(currentPosition, collisionCheckedPosition, _adjustmentSpeed * deltaTime);
        }

        /// <summary>
        /// Perform multi-sample collision check for better accuracy
        /// </summary>
        public Vector3 CheckCollisionMultiSample(Vector3 desiredPosition, Vector3 targetPosition, int samples = 5)
        {
            if (!_isEnabled || samples <= 1)
                return CheckCollision(desiredPosition, targetPosition);

            var direction = (desiredPosition - targetPosition).normalized;
            var maxDistance = Vector3.Distance(targetPosition, desiredPosition);
            var sampleStep = maxDistance / samples;

            // Check from target towards desired position
            for (int i = samples; i > 0; i--)
            {
                var testDistance = sampleStep * i;
                var testPosition = targetPosition + direction * testDistance;

                if (!Physics.CheckSphere(testPosition, _collisionRadius, _collisionLayers))
                {
                    // Found valid position
                    var adjustedPosition = targetPosition + direction * Mathf.Max(0f, testDistance - _collisionBuffer);
                    _lastValidPosition = adjustedPosition;
                    _hasValidPosition = true;
                    return adjustedPosition;
                }
            }

            // All positions blocked, return closest safe position
            return targetPosition + direction * _collisionBuffer;
        }

        /// <summary>
        /// Check collision in multiple directions to find alternative positions
        /// </summary>
        public Vector3 CheckCollisionWithAlternatives(Vector3 desiredPosition, Vector3 targetPosition)
        {
            if (!_isEnabled)
                return desiredPosition;

            // Try the desired position first
            var primaryResult = CheckCollision(desiredPosition, targetPosition);

            // If the result is significantly different from desired, try alternatives
            if (Vector3.Distance(primaryResult, desiredPosition) > _collisionBuffer * 2f)
            {
                var originalDirection = (desiredPosition - targetPosition).normalized;
                var distance = Vector3.Distance(targetPosition, desiredPosition);

                // Try slight variations in direction
                var alternativeDirections = new Vector3[]
                {
                    Quaternion.AngleAxis(15f, Vector3.up) * originalDirection,
                    Quaternion.AngleAxis(-15f, Vector3.up) * originalDirection,
                    Quaternion.AngleAxis(15f, Vector3.right) * originalDirection,
                    Quaternion.AngleAxis(-15f, Vector3.right) * originalDirection
                };

                foreach (var direction in alternativeDirections)
                {
                    var alternativePosition = targetPosition + direction * distance;
                    if (HasLineOfSight(targetPosition, alternativePosition))
                    {
                        _lastValidPosition = alternativePosition;
                        _hasValidPosition = true;
                        return alternativePosition;
                    }
                }
            }

            return primaryResult;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Configure collision settings
        /// </summary>
        public void Configure(LayerMask layers, float buffer, float radius, float speed)
        {
            _collisionLayers = layers;
            _collisionBuffer = buffer;
            _collisionRadius = radius;
            _adjustmentSpeed = speed;
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            _isEnabled = true;
            _collisionLayers = -1;
            _collisionBuffer = 0.2f;
            _collisionRadius = 0.1f;
            _adjustmentSpeed = 5f;
            _hasValidPosition = false;
        }

        #endregion
    }
}