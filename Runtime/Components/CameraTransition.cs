
//using Coty.Camera.Interfaces;
//using UnityEngine;

//namespace Coty.Camera.Components
//{
//    /// <summary>
//    /// Camera transition component for smooth interpolation between camera states.
//    /// Handles various easing functions and transition types.
//    /// </summary>
//    [System.Serializable]
//    public class CameraTransitionComponent
//    {
//        #region Serialized Fields

//        [Header("Transition Settings")]
//        [SerializeField] private CameraTransition _transitionType = CameraTransition.Smooth;
//        [SerializeField] private float _defaultDuration = 1f;
//        [SerializeField] private AnimationCurve _customCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

//        #endregion

//        #region Properties

//        public bool IsTransitioning { get; private set; }
//        public float Progress { get; private set; }
//        public CameraTransition TransitionType { get; set; }

//        #endregion

//        #region Private Fields

//        private float _duration;
//        private float _timer;

//        // Start state
//        private Vector3 _startPosition;
//        private Quaternion _startRotation;
//        private float _startFov;

//        // Target state
//        private Vector3 _targetPosition;
//        private Quaternion _targetRotation;
//        private float _targetFov;

//        // Current interpolated state
//        private Vector3 _currentPosition;
//        private Quaternion _currentRotation;
//        private float _currentFov;

//        #endregion

//        #region Transition Control

//        public void StartTransition(
//            Vector3 fromPosition, Quaternion fromRotation, float fromFov,
//            Vector3 toPosition, Quaternion toRotation, float toFov,
//            CameraTransition transitionType, float duration = -1f)
//        {
//            // Store start state
//            _startPosition = fromPosition;
//            _startRotation = fromRotation;
//            _startFov = fromFov;

//            // Store target state
//            _targetPosition = toPosition;
//            _targetRotation = toRotation;
//            _targetFov = toFov;

//            // Setup transition
//            _transitionType = transitionType;
//            _duration = duration > 0f ? duration : GetDefaultDuration(transitionType);
//            _timer = 0f;
//            Progress = 0f;
//            IsTransitioning = true;

//            // Initialize current state
//            _currentPosition = _startPosition;
//            _currentRotation = _startRotation;
//            _currentFov = _startFov;
//        }

//        public void UpdateTransition(float deltaTime)
//        {
//            if (!IsTransitioning) return;

//            _timer += deltaTime;
//            Progress = Mathf.Clamp01(_timer / _duration);

//            if (Progress >= 1f)
//            {
//                // Transition complete
//                _currentPosition = _targetPosition;
//                _currentRotation = _targetRotation;
//                _currentFov = _targetFov;
//                IsTransitioning = false;
//                Progress = 1f;
//            }
//            else
//            {
//                // Interpolate based on transition type
//                var easedProgress = ApplyEasing(Progress);

//                _currentPosition = Vector3.Lerp(_startPosition, _targetPosition, easedProgress);
//                _currentRotation = Quaternion.Slerp(_startRotation, _targetRotation, easedProgress);
//                _currentFov = Mathf.Lerp(_startFov, _targetFov, easedProgress);
//            }
//        }

//        public void StopTransition()
//        {
//            IsTransitioning = false;
//            Progress = 1f;
//            _timer = 0f;
//        }

//        public void CompleteTransition()
//        {
//            if (!IsTransitioning) return;

//            _currentPosition = _targetPosition;
//            _currentRotation = _targetRotation;
//            _currentFov = _targetFov;
//            IsTransitioning = false;
//            Progress = 1f;
//        }

//        #endregion

//        #region State Getters

//        public Vector3 GetCurrentPosition() => _currentPosition;
//        public Quaternion GetCurrentRotation() => _currentRotation;
//        public float GetCurrentFov() => _currentFov;

//        public (Vector3 position, Quaternion rotation, float fov) GetCurrentState()
//        {
//            return (_currentPosition, _currentRotation, _currentFov);
//        }

//        #endregion

//        #region Easing Functions

//        private float ApplyEasing(float t)
//        {
//            switch (_transitionType)
//            {
//                case CameraTransition.Instant:
//                    return 1f;

//                case CameraTransition.Smooth:
//                    return t;

//                case CameraTransition.EaseIn:
//                    return EaseIn(t);

//                case CameraTransition.EaseOut:
//                    return EaseOut(t);

//                case CameraTransition.EaseInOut:
//                    return EaseInOut(t);

//                case CameraTransition.Bounce:
//                    return Bounce(t);

//                default:
//                    return t;
//            }
//        }

//        private float EaseIn(float t) => t * t;
//        private float EaseOut(float t) => 1f - (1f - t) * (1f - t);
//        private float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
//        private float Bounce(float t) => 1f - Mathf.Abs(Mathf.Cos(t * Mathf.PI * 2f)) * (1f - t);

//        #endregion

//        #region Custom Curve Support

//        public void SetCustomCurve(AnimationCurve curve)
//        {
//            _customCurve = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
//        }

//        private float ApplyCustomCurve(float t)
//        {
//            return _customCurve != null ? _customCurve.Evaluate(t) : t;
//        }

//        #endregion

//        #region Duration Management

//        private float GetDefaultDuration(CameraTransition transitionType)
//        {
//            switch (transitionType)
//            {
//                case CameraTransition.Instant:
//                    return 0f;
//                case CameraTransition.Smooth:
//                    return _defaultDuration;
//                case CameraTransition.EaseIn:
//                case CameraTransition.EaseOut:
//                case CameraTransition.EaseInOut:
//                    return _defaultDuration * 0.8f;
//                case CameraTransition.Bounce:
//                    return _defaultDuration * 1.2f;
//                default:
//                    return _defaultDuration;
//            }
//        }

//        public void SetDefaultDuration(float duration)
//        {
//            _defaultDuration = Mathf.Max(0.1f, duration);
//        }

//        #endregion

//        #region Advanced Transitions

//        /// <summary>
//        /// Start transition with custom parameters and callback
//        /// </summary>
//        public void StartAdvancedTransition(
//            Vector3 fromPosition, Quaternion fromRotation, float fromFov,
//            Vector3 toPosition, Quaternion toRotation, float toFov,
//            float duration, AnimationCurve customCurve = null)
//        {
//            if (customCurve != null)
//            {
//                SetCustomCurve(customCurve);
//                _transitionType = CameraTransition.Smooth; // Use smooth with custom curve
//            }

//            StartTransition(fromPosition, fromRotation, fromFov, toPosition, toRotation, toFov, _transitionType, duration);
//        }

//        /// <summary>
//        /// Transition with different speeds for position, rotation, and FOV
//        /// </summary>
//        public void StartSeparateTransition(
//            Vector3 fromPosition, Quaternion fromRotation, float fromFov,
//            Vector3 toPosition, Quaternion toRotation, float toFov,
//            float positionDuration, float rotationDuration, float fovDuration)
//        {
//            // For now, use the longest duration and handle separately in future updates
//            var maxDuration = Mathf.Max(positionDuration, rotationDuration, fovDuration);
//            StartTransition(fromPosition, fromRotation, fromFov, toPosition, toRotation, toFov, _transitionType, maxDuration);
//        }

//        #endregion

//        #region Transition Queries

//        /// <summary>
//        /// Get remaining transition time
//        /// </summary>
//        public float GetRemainingTime()
//        {
//            return IsTransitioning ? Mathf.Max(0f, _duration - _timer) : 0f;
//        }

//        /// <summary>
//        /// Get total transition duration
//        /// </summary>
//        public float GetDuration()
//        {
//            return _duration;
//        }

//        /// <summary>
//        /// Check if transition is in first half
//        /// </summary>
//        public bool IsInFirstHalf()
//        {
//            return IsTransitioning && Progress < 0.5f;
//        }

//        /// <summary>
//        /// Check if transition is in second half
//        /// </summary>
//        public bool IsInSecondHalf()
//        {
//            return IsTransitioning && Progress >= 0.5f;
//        }

//        #endregion

//        #region Debug

//        public void DrawDebugInfo()
//        {
//            if (!IsTransitioning) return;

//            // Draw start position
//            Gizmos.color = Color.red;
//            Gizmos.DrawWireSphere(_startPosition, 0.3f);

//            // Draw target position
//            Gizmos.color = Color.green;
//            Gizmos.DrawWireSphere(_targetPosition, 0.3f);

//            // Draw current position
//            Gizmos.color = Color.yellow;
//            Gizmos.DrawWireSphere(_currentPosition, 0.2f);

//            // Draw transition line
//            Gizmos.color = Color.white;
//            Gizmos.DrawLine(_startPosition, _targetPosition);
//        }

//        #endregion

//        #region Reset

//        public void Reset()
//        {
//            IsTransitioning = false;
//            Progress = 0f;
//            _timer = 0f;
//            _duration = _defaultDuration;
//            _transitionType = CameraTransition.Smooth;
//        }

//        #endregion
//    }

//    // Note: Update the title when using this component
//    // Change filename from CameraTransition.cs to CameraTransitionComponent.cs
//}