using Aetheriaum.CameraSystem.Interfaces;
using UnityEngine;

namespace Coty.Camera.Components
{
    /// <summary>
    /// Camera orbit component for managing spherical coordinate camera movement.
    /// Handles smooth orbit mechanics around a target with input-based control.
    /// </summary>
    [System.Serializable]
    public class CameraOrbitComponent
    {
        #region Serialized Fields

        [Header("Orbit Settings")]
        [SerializeField] private float _horizontalAngle = 0f;
        [SerializeField] private float _verticalAngle = 20f;
        [SerializeField] private float _distance = 8f;

        [Header("Constraints")]
        [SerializeField] private float _minVerticalAngle = -30f;
        [SerializeField] private float _maxVerticalAngle = 60f;
        //[SerializeField] private float _minDistance = 2f;
        //[SerializeField] private float _maxDistance = 15f;

        [Header("Smoothing")]
        [SerializeField] private float _rotationSmoothTime = 0.1f;
        [SerializeField] private float _distanceSmoothTime = 0.3f;

        private ICameraSettings _settings;

        #endregion

        #region Properties

        public float HorizontalAngle
        {
            get => _horizontalAngle;
            set => _horizontalAngle = value % 360f;
        }

        public float VerticalAngle
        {
            get => _verticalAngle;
            set => _verticalAngle = Mathf.Clamp(value, _minVerticalAngle, _maxVerticalAngle);
        }

        public float Distance
        {
            get => _distance;
            set => _distance = _settings != null ?
                Mathf.Clamp(value, _settings.MinDistance, _settings.MaxDistance) : value;
        }
        public void SetSettings(ICameraSettings settings)
        {
            _settings = settings;

            // Apply settings constraints immediately
            if (_settings != null)
            {
                _distance = Mathf.Clamp(_distance, _settings.MinDistance, _settings.MaxDistance);
                _targetDistance = Mathf.Clamp(_targetDistance, _settings.MinDistance, _settings.MaxDistance);
            }
        }

        public Vector2 Angles => new Vector2(_horizontalAngle, _verticalAngle);

        #endregion

        #region Private Fields

        private float _targetDistance;
        private Vector2 _targetAngles;
        private Vector2 _angleVelocity;
        private float _distanceVelocity;

        private bool _isInitialized = false;

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (_isInitialized) return;

            _targetDistance = _distance;
            _targetAngles = new Vector2(_horizontalAngle, _verticalAngle);
            _isInitialized = true;
        }

        public void Initialize(float horizontalAngle, float verticalAngle, float distance)
        {
            _horizontalAngle = horizontalAngle;
            _verticalAngle = Mathf.Clamp(verticalAngle, _minVerticalAngle, _maxVerticalAngle);
            _distance = _settings != null ? Mathf.Clamp(distance, _settings.MinDistance, _settings.MaxDistance) : distance;

            Initialize();
        }

        #endregion

        #region Input Handling

        public void HandleInput(Vector2 inputDelta, float zoomDelta, ICameraSettings settings)
        {
            if (!_isInitialized || settings == null) return;

            // Store settings reference
            if (_settings != settings)
            {
                SetSettings(settings);
            }

            // Apply input to target angles
            var sensitivity = settings.LookSensitivity;
            _targetAngles.x += inputDelta.x * sensitivity;
            _targetAngles.y += inputDelta.y * sensitivity * (settings.InvertY ? -1f : 1f);

            // Clamp vertical angle
            _targetAngles.y = Mathf.Clamp(_targetAngles.y, _minVerticalAngle, _maxVerticalAngle);
            _targetAngles.x = _targetAngles.x % 360f;

            // Apply zoom using settings constraints
            if (Mathf.Abs(zoomDelta) > 0.01f)
            {
                _targetDistance -= zoomDelta * settings.ZoomSpeed;
                _targetDistance = Mathf.Clamp(_targetDistance, settings.MinDistance, settings.MaxDistance);
            }
        }

        #endregion

        #region Update

        public void UpdateOrbit(float deltaTime)
        {
            if (!_isInitialized) return;

            // Smooth angle interpolation
            var currentAngles = new Vector2(_horizontalAngle, _verticalAngle);
            var smoothedAngles = Vector2.SmoothDamp(currentAngles, _targetAngles, ref _angleVelocity, _rotationSmoothTime);

            _horizontalAngle = smoothedAngles.x;
            _verticalAngle = smoothedAngles.y;

            // Smooth distance interpolation
            _distance = Mathf.SmoothDamp(_distance, _targetDistance, ref _distanceVelocity, _distanceSmoothTime);
        }

        #endregion

        #region Position Calculation

        public Vector3 CalculatePosition(Vector3 targetPosition)
        {
            if (!_isInitialized) return targetPosition;

            // Convert spherical coordinates to cartesian
            var horizontalRadians = _horizontalAngle * Mathf.Deg2Rad;
            var verticalRadians = _verticalAngle * Mathf.Deg2Rad;

            var horizontalDistance = _distance * Mathf.Cos(verticalRadians);
            var verticalOffset = _distance * Mathf.Sin(verticalRadians);

            var offset = new Vector3(
                horizontalDistance * Mathf.Sin(horizontalRadians),
                verticalOffset,
                horizontalDistance * Mathf.Cos(horizontalRadians)
            );

            return targetPosition - offset;
        }

        public Quaternion CalculateRotation(Vector3 cameraPosition, Vector3 targetPosition)
        {
            var direction = (targetPosition - cameraPosition).normalized;
            return Quaternion.LookRotation(direction);
        }

        #endregion

        #region Configuration

        public void SetConstraints(float minVerticalAngle, float maxVerticalAngle, float minDistance, float maxDistance)
        {
            _minVerticalAngle = minVerticalAngle;
            _maxVerticalAngle = maxVerticalAngle;

            // Update settings if available
            if (_settings != null)
            {
                _settings.MinDistance = minDistance;
                _settings.MaxDistance = maxDistance;
            }

            // Reapply constraints
            _verticalAngle = Mathf.Clamp(_verticalAngle, _minVerticalAngle, _maxVerticalAngle);
            _targetAngles.y = Mathf.Clamp(_targetAngles.y, _minVerticalAngle, _maxVerticalAngle);
            _distance = _settings != null ?
                Mathf.Clamp(_distance, _settings.MinDistance, _settings.MaxDistance) :
                Mathf.Clamp(_distance, minDistance, maxDistance);
            _targetDistance = _settings != null ?
                Mathf.Clamp(_targetDistance, _settings.MinDistance, _settings.MaxDistance) :
                Mathf.Clamp(_targetDistance, minDistance, maxDistance);
        }

        public void SetSmoothingTimes(float rotationSmoothTime, float distanceSmoothTime)
        {
            _rotationSmoothTime = Mathf.Max(0.01f, rotationSmoothTime);
            _distanceSmoothTime = Mathf.Max(0.01f, distanceSmoothTime);
        }

        #endregion

        #region Direct Control

        public void SetAngles(float horizontal, float vertical, bool instant = false)
        {
            _horizontalAngle = horizontal % 360f;
            _verticalAngle = Mathf.Clamp(vertical, _minVerticalAngle, _maxVerticalAngle);

            if (instant)
            {
                _targetAngles = new Vector2(_horizontalAngle, _verticalAngle);
                _angleVelocity = Vector2.zero;
            }
            else
            {
                _targetAngles = new Vector2(_horizontalAngle, _verticalAngle);
            }
        }

        public void SetDistance(float distance, bool instant = false)
        {
            _distance = _settings != null ? Mathf.Clamp(distance, _settings.MinDistance, _settings.MaxDistance) : distance;

            if (instant)
            {
                _targetDistance = _distance;
                _distanceVelocity = 0f;
            }
            else
            {
                _targetDistance = _distance;
            }
        }

        public void SetOrbitInstant(float horizontal, float vertical, float distance)
        {
            SetAngles(horizontal, vertical, true);
            SetDistance(distance, true);
        }

        #endregion

        #region Auto-Rotation

        public void ApplyAutoRotation(Vector3 targetForward, float speed, float deltaTime)
        {
            if (!_isInitialized) return;

            // Calculate desired angle based on target's forward direction
            var targetAngle = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg + 180f;

            // Smooth rotation towards target's back
            var angleDifference = Mathf.DeltaAngle(_targetAngles.x, targetAngle);
            _targetAngles.x += angleDifference * speed * deltaTime * 0.01f;
        }

        #endregion

        #region Debug

        public void DrawDebugInfo(Vector3 targetPosition)
        {
            if (!_isInitialized) return;

            var cameraPosition = CalculatePosition(targetPosition);

            // Draw orbit sphere
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPosition, _distance);

            // Draw camera position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraPosition, 0.2f);

            // Draw line from target to camera
            Gizmos.color = Color.white;
            Gizmos.DrawLine(targetPosition, cameraPosition);
        }

        #endregion

        #region Reset

        public void Reset()
        {
            _horizontalAngle = 0f;
            _verticalAngle = 20f;
            _distance = 8f;
            _targetDistance = _distance;
            _targetAngles = new Vector2(_horizontalAngle, _verticalAngle);
            _angleVelocity = Vector2.zero;
            _distanceVelocity = 0f;
        }

        #endregion
    }
}