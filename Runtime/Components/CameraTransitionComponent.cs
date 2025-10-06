using Aetheriaum.CameraSystem.Interfaces;
using UnityEngine;

namespace Aetheriaum.CameraSystem.Components
{
    /// <summary>
    /// Camera transition component for smooth interpolation between camera states.
    /// Handles various easing functions and transition types.
    /// </summary>
    [System.Serializable]
    public class CameraTransitionComponent
    {
        #region Serialized Fields

        [Header("Transition Settings")]
        [SerializeField] private Interfaces.CameraTransition _transitionType = Interfaces.CameraTransition.Smooth;
        [SerializeField] private float _defaultDuration = 1f;
        [SerializeField] private AnimationCurve _customCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion

        #region Properties

        public bool IsTransitioning { get; private set; }
        public float Progress { get; private set; }
        public CameraTransition TransitionType { get; set; }

        #endregion

        #region Private Fields

        private float _duration;
        private float _timer;

        // Start state
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private float _startFov;

        // Target state
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _targetFov;

        // Current interpolated state
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;
        private float _currentFov;

        #endregion

        #region Transition Control

        public void StartTransition(
            Vector3 fromPosition, Quaternion fromRotation, float fromFov,
            Vector3 toPosition, Quaternion toRotation, float toFov,
            Interfaces.CameraTransition transitionType, float duration = -1f)
        {
            // Store start state
            _startPosition = fromPosition;
            _startRotation = fromRotation;
            _startFov = fromFov;

            // Store target state
            _targetPosition = toPosition;
            _targetRotation = toRotation;
            _targetFov = toFov;

            // Setup transition
            _transitionType = transitionType;
            _duration = duration > 0f ? duration : GetTransitionDuration(transitionType);
            _timer = 0f;
            Progress = 0f;
            IsTransitioning = true;

            // Initialize current state
            _currentPosition = _startPosition;
            _currentRotation = _startRotation;
            _currentFov = _startFov;
        }

        public void UpdateTransition(float deltaTime)
        {
            if (!IsTransitioning) return;

            _timer += deltaTime;
            Progress = Mathf.Clamp01(_timer / _duration);

            if (Progress >= 1f)
            {
                // Transition complete
                _currentPosition = _targetPosition;
                _currentRotation = _targetRotation;
                _currentFov = _targetFov;
                IsTransitioning = false;
                Progress = 1f;
            }
            else
            {
                // Interpolate based on transition type
                var easedProgress = ApplyEasing(Progress);

                _currentPosition = Vector3.Lerp(_startPosition, _targetPosition, easedProgress);
                _currentRotation = Quaternion.Slerp(_startRotation, _targetRotation, easedProgress);
                _currentFov = Mathf.Lerp(_startFov, _targetFov, easedProgress);
            }
        }

        public void StopTransition()
        {
            IsTransitioning = false;
            Progress = 1f;
            _timer = 0f;
        }

        public void CompleteTransition()
        {
            if (!IsTransitioning) return;

            _currentPosition = _targetPosition;
            _currentRotation = _targetRotation;
            _currentFov = _targetFov;
            IsTransitioning = false;
            Progress = 1f;
        }

        #endregion

        #region State Getters

        public Vector3 GetCurrentPosition() => _currentPosition;
        public Quaternion GetCurrentRotation() => _currentRotation;
        public float GetCurrentFov() => _currentFov;

        public (Vector3 position, Quaternion rotation, float fov) GetCurrentState()
        {
            return (_currentPosition, _currentRotation, _currentFov);
        }

        #endregion

        #region Easing Functions

        private float ApplyEasing(float t)
        {
            switch (_transitionType)
            {
                case Interfaces.CameraTransition.Instant:
                    return 1f;

                case Interfaces.CameraTransition.Smooth:
                    return t;

                case Interfaces.CameraTransition.EaseIn:
                    return EaseIn(t);

                case Interfaces.CameraTransition.EaseOut:
                    return EaseOut(t);

                case Interfaces.CameraTransition.EaseInOut:
                    return EaseInOut(t);

                case Interfaces.CameraTransition.Bounce:
                    return Bounce(t);

                default:
                    return t;
            }
        }

        private float EaseIn(float t) => t * t;
        private float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
        private float Bounce(float t) => 1f - Mathf.Abs(Mathf.Cos(t * Mathf.PI * 2f)) * (1f - t);

        #endregion

        #region Duration Management

        private float GetTransitionDuration(Interfaces.CameraTransition transitionType)
        {
            switch (transitionType)
            {
                case Interfaces.CameraTransition.Instant:
                    return 0f;
                case Interfaces.CameraTransition.Smooth:
                    return _defaultDuration;
                case Interfaces.CameraTransition.EaseIn:
                case Interfaces.CameraTransition.EaseOut:
                case Interfaces.CameraTransition.EaseInOut:
                    return _defaultDuration * 0.8f;
                case Interfaces.CameraTransition.Bounce:
                    return _defaultDuration * 1.2f;
                default:
                    return _defaultDuration;
            }
        }

        public void SetTransitionDuration(float duration)
        {
            _defaultDuration = Mathf.Max(0.1f, duration);
        }

        public float GetTransitionDuration() => _duration;
        public float GetTimeRemaining() => IsTransitioning ? Mathf.Max(0f, _duration - _timer) : 0f;

        #endregion

        #region Utility Methods

        public void SetCustomCurve(AnimationCurve curve)
        {
            _customCurve = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        public bool IsInFirstHalf() => IsTransitioning && Progress < 0.5f;
        public bool IsInSecondHalf() => IsTransitioning && Progress >= 0.5f;

        #endregion

        #region Debug

        public void DrawDebugInfo()
        {
            if (!IsTransitioning) return;

            // Draw start position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_startPosition, 0.3f);

            // Draw target position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_targetPosition, 0.3f);

            // Draw current position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_currentPosition, 0.2f);

            // Draw transition line
            Gizmos.color = Color.white;
            Gizmos.DrawLine(_startPosition, _targetPosition);
        }

        #endregion

        #region Reset

        public void Reset()
        {
            IsTransitioning = false;
            Progress = 0f;
            _timer = 0f;
            _duration = _defaultDuration;
            _transitionType = Interfaces.CameraTransition.Smooth;
        }

        #endregion
    }
}