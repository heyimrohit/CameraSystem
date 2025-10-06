
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aetheriaum.CameraSystem.Interfaces;

namespace Aetheriaum.CameraSystem.States
{
    /// <summary>
    /// Abstract base class for camera states implementing Template Method pattern.
    /// Provides common functionality while allowing derived states to customize behavior.
    /// Follows SOLID principles with clear extension points.
    /// </summary>
    public abstract class CameraStateBase : ICameraState
    {
        #region Properties

        public abstract CameraMode CameraMode { get; }
        public bool IsActive { get; private set; }
        public ICameraController Controller { get; private set; }
        public ICameraSettings Settings { get; set; }

        #endregion

        #region Protected Fields

        protected Vector3 _currentPosition;
        protected Quaternion _currentRotation;
        protected float _currentFieldOfView = 60f;
        protected Vector3 _velocity;
        protected List<ICameraModifier> _modifiers = new List<ICameraModifier>();

        #endregion

        #region Initialization

        public virtual void Initialize(ICameraController controller)
        {
            Controller = controller;
            Settings = controller.Settings;
            OnInitialize();
        }

        /// <summary>
        /// Override for state-specific initialization
        /// </summary>
        protected virtual void OnInitialize() { }

        #endregion

        #region State Lifecycle

        public virtual void OnEnter(ICameraState previousState)
        {
            IsActive = true;

            // Inherit position and rotation from previous state if available
            if (previousState != null)
            {
                _currentPosition = previousState.GetDesiredPosition();
                _currentRotation = previousState.GetDesiredRotation();
                _currentFieldOfView = previousState.GetDesiredFieldOfView();
            }
            else
            {
                // Initialize to current camera transform
                if (Controller?.CameraTransform != null)
                {
                    _currentPosition = Controller.CameraTransform.position;
                    _currentRotation = Controller.CameraTransform.rotation;
                }
            }

            OnEnterState(previousState);
        }

        public virtual void OnExit(ICameraState nextState)
        {
            IsActive = false;
            OnExitState(nextState);
        }

        /// <summary>
        /// Override for state-specific enter logic
        /// </summary>
        protected virtual void OnEnterState(ICameraState previousState) { }

        /// <summary>
        /// Override for state-specific exit logic
        /// </summary>
        protected virtual void OnExitState(ICameraState nextState) { }

        #endregion

        #region Update Logic

        public virtual void UpdateState(float deltaTime)
        {
            if (!IsActive) return;

            // Update modifiers first
            UpdateModifiers(deltaTime);

            // Update state-specific logic
            OnUpdateState(deltaTime);

            // Calculate desired camera properties
            UpdateCameraProperties(deltaTime);
        }

        /// <summary>
        /// Override for state-specific update logic
        /// </summary>
        protected abstract void OnUpdateState(float deltaTime);

        /// <summary>
        /// Update camera position, rotation, and FOV
        /// </summary>
        protected virtual void UpdateCameraProperties(float deltaTime)
        {
            var targetPosition = CalculateTargetPosition();
            var targetRotation = CalculateTargetRotation();
            var targetFov = CalculateTargetFieldOfView();

            // Smooth interpolation to target values
            _currentPosition = Vector3.SmoothDamp(_currentPosition, targetPosition, ref _velocity, GetPositionSmoothTime());
            _currentRotation = Quaternion.Slerp(_currentRotation, targetRotation, GetRotationSmoothSpeed() * deltaTime);
            _currentFieldOfView = Mathf.Lerp(_currentFieldOfView, targetFov, GetFovSmoothSpeed() * deltaTime);
        }

        #endregion

        #region Input Handling

        public virtual void HandleInput(Vector2 lookInput, float zoomInput)
        {
            if (!IsActive) return;
            OnHandleInput(lookInput, zoomInput);
        }

        /// <summary>
        /// Override for state-specific input handling
        /// </summary>
        protected virtual void OnHandleInput(Vector2 lookInput, float zoomInput) { }

        #endregion

        #region Camera Properties

        public virtual Vector3 GetDesiredPosition()
        {
            return _currentPosition;
        }

        public virtual Quaternion GetDesiredRotation()
        {
            return _currentRotation;
        }

        public virtual float GetDesiredFieldOfView()
        {
            return _currentFieldOfView;
        }

        #endregion

        #region Abstract Methods - Must be implemented by derived states

        /// <summary>
        /// Calculate the target position for the camera
        /// </summary>
        protected abstract Vector3 CalculateTargetPosition();

        /// <summary>
        /// Calculate the target rotation for the camera
        /// </summary>
        protected abstract Quaternion CalculateTargetRotation();

        /// <summary>
        /// Calculate the target field of view for the camera
        /// </summary>
        protected virtual float CalculateTargetFieldOfView()
        {
            return Settings?.DefaultFieldOfView ?? 60f;
        }

        #endregion

        #region Transition Logic

        public virtual bool CanTransitionTo(CameraMode targetMode)
        {
            return OnCanTransitionTo(targetMode);
        }

        /// <summary>
        /// Override to implement state-specific transition rules
        /// </summary>
        protected virtual bool OnCanTransitionTo(CameraMode targetMode)
        {
            return true; // Allow all transitions by default
        }

        #endregion

        #region Modifiers

        public virtual void ApplyModifiers(ref Vector3 position, ref Quaternion rotation)
        {
            foreach (var modifier in _modifiers)
            {
                if (modifier.IsEnabled)
                {
                    position = modifier.ModifyPosition(position, Controller);
                    rotation = modifier.ModifyRotation(rotation, Controller);
                }
            }
        }

        public virtual void AddModifier(ICameraModifier modifier)
        {
            if (!_modifiers.Contains(modifier))
            {
                _modifiers.Add(modifier);
                _modifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        public virtual void RemoveModifier(ICameraModifier modifier)
        {
            _modifiers.Remove(modifier);
        }

        protected virtual void UpdateModifiers(float deltaTime)
        {
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].IsEnabled)
                {
                    _modifiers[i].UpdateModifier(deltaTime);
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get smoothing time for position interpolation
        /// </summary>
        protected virtual float GetPositionSmoothTime()
        {
            return 1f / (Settings?.FollowSpeed ?? 5f);
        }

        /// <summary>
        /// Get smoothing speed for rotation interpolation
        /// </summary>
        protected virtual float GetRotationSmoothSpeed()
        {
            return Settings?.RotationSpeed ?? 3f;
        }

        /// <summary>
        /// Get smoothing speed for FOV interpolation
        /// </summary>
        protected virtual float GetFovSmoothSpeed()
        {
            return 2f;
        }

        /// <summary>
        /// Get the target transform (usually the character)
        /// </summary>
        protected virtual Transform GetTarget()
        {
            return Controller?.Target;
        }

        #endregion

        #region Reset

        public virtual void Reset()
        {
            _velocity = Vector3.zero;

            if (Controller?.CameraTransform != null)
            {
                _currentPosition = Controller.CameraTransform.position;
                _currentRotation = Controller.CameraTransform.rotation;
            }

            OnReset();
        }

        /// <summary>
        /// Override for state-specific reset logic
        /// </summary>
        protected virtual void OnReset() { }

        Task ICameraState.Initialize(ICameraController controller)
        {
            throw new System.NotImplementedException();
        }

        void ICameraState.OnEnter(ICameraState previousState)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}