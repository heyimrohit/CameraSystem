using Aetheriaum.CameraSystem.Interfaces;
using UnityEngine;

namespace Aetheriaum.CameraSystem.Settings
{
    /// <summary>
    /// Complete implementation of camera settings with all required properties
    /// </summary>
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Camera/Camera Settings")]
    public class CameraSettings : ScriptableObject, ICameraSettings
    {
        [Header("Movement")]
        [SerializeField] private float _followSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 3f;
        [SerializeField] private float _damping = 0.1f;

        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minDistance = 2f;
        [SerializeField] private float _maxDistance = 10f;

        [Header("Field of View")]
        [SerializeField] private float _defaultFieldOfView = 60f;
        [SerializeField] private float _minFieldOfView = 30f;
        [SerializeField] private float _maxFieldOfView = 90f;

        [Header("Input")]
        [SerializeField] private float _lookSensitivity = 2f;
        [SerializeField] private bool _invertY = false;

        [Header("Positioning")]
        [SerializeField] private float _heightOffset = 1.5f;

        [Header("Collision")]
        [SerializeField] private bool _collisionDetection = true;
        [SerializeField] private LayerMask _collisionLayers = -1;
        [SerializeField] private float _collisionBuffer = 0.1f;
        [SerializeField] private float _collisionRadius = 0.2f;

        #region ICameraSettings Implementation

        public float FollowSpeed
        {
            get => _followSpeed;
            set => _followSpeed = value;
        }

        public float RotationSpeed
        {
            get => _rotationSpeed;
            set => _rotationSpeed = value;
        }

        public float ZoomSpeed
        {
            get => _zoomSpeed;
            set => _zoomSpeed = value;
        }

        public float MinDistance
        {
            get => _minDistance;
            set => _minDistance = value;
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

        public float DefaultFieldOfView
        {
            get => _defaultFieldOfView;
            set => _defaultFieldOfView = value;
        }

        public float MinFieldOfView
        {
            get => _minFieldOfView;
            set => _minFieldOfView = value;
        }

        public float MaxFieldOfView
        {
            get => _maxFieldOfView;
            set => _maxFieldOfView = value;
        }

        public float LookSensitivity
        {
            get => _lookSensitivity;
            set => _lookSensitivity = value;
        }

        public bool InvertY
        {
            get => _invertY;
            set => _invertY = value;
        }

        public float HeightOffset
        {
            get => _heightOffset;
            set => _heightOffset = value;
        }

        public bool CollisionDetection
        {
            get => _collisionDetection;
            set => _collisionDetection = value;
        }

        public LayerMask CollisionLayers
        {
            get => _collisionLayers;
            set => _collisionLayers = value;
        }

        public float CollisionBuffer
        {
            get => _collisionBuffer;
            set => _collisionBuffer = value;
        }

        public float CollisionRadius
        {
            get => _collisionRadius;
            set => _collisionRadius = value;
        }

        public float Damping
        {
            get => _damping;
            set => _damping = value;
        }

        #endregion

        /// <summary>
        /// Create default camera settings in code
        /// </summary>
        public static CameraSettings CreateDefault()
        {
            var settings = CreateInstance<CameraSettings>();
            settings._followSpeed = 5f;
            settings._rotationSpeed = 3f;
            settings._zoomSpeed = 2f;
            settings._minDistance = 1.5f;
            settings._maxDistance = 8f;
            settings._defaultFieldOfView = 60f;
            settings._minFieldOfView = 30f;
            settings._maxFieldOfView = 90f;
            settings._lookSensitivity = 2f;
            settings._invertY = false;
            settings._heightOffset = 1.5f;
            settings._collisionDetection = true;
            settings._collisionLayers = -1;
            settings._collisionBuffer = 0.1f;
            settings._collisionRadius = 0.2f;
            settings._damping = 0.1f;
            return settings;
        }

        /// <summary>
        /// Validate settings values
        /// </summary>
        private void OnValidate()
        {
            _followSpeed = Mathf.Max(0.1f, _followSpeed);
            _rotationSpeed = Mathf.Max(0.1f, _rotationSpeed);
            _zoomSpeed = Mathf.Max(0.1f, _zoomSpeed);
            _minDistance = Mathf.Max(0.1f, _minDistance);
            _maxDistance = Mathf.Max(_minDistance + 0.1f, _maxDistance);
            _defaultFieldOfView = Mathf.Clamp(_defaultFieldOfView, 10f, 170f);
            _minFieldOfView = Mathf.Clamp(_minFieldOfView, 10f, _defaultFieldOfView);
            _maxFieldOfView = Mathf.Clamp(_maxFieldOfView, _defaultFieldOfView, 170f);
            _lookSensitivity = Mathf.Max(0.1f, _lookSensitivity);
            _heightOffset = Mathf.Max(0f, _heightOffset);
            _collisionBuffer = Mathf.Max(0f, _collisionBuffer);
            _collisionRadius = Mathf.Max(0.01f, _collisionRadius);
            _damping = Mathf.Max(0.01f, _damping);
        }
    }
}