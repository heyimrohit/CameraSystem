using Aetheriaum.CameraSystem.Interfaces;
using UnityEngine;

namespace Aetheriaum.CameraSystem.Modifier
{
    /// <summary>
    /// Abstract base class for camera modifiers implementing Template Method pattern.
    /// Provides common functionality while allowing derived modifiers to customize behavior.
    /// Follows SOLID principles with clear extension points.
    /// </summary>
    public abstract class CameraModifierBase : ICameraModifier
    {
        #region Properties

        public abstract string ModifierName { get; }
        public abstract int Priority { get; }
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region Protected Fields

        protected bool _isInitialized = false;
        protected float _intensity = 1f;
        protected float _elapsedTime = 0f;

        #endregion

        #region Initialization

        public virtual void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"Camera modifier {ModifierName} is already initialized.");
                return;
            }

            OnInitialize();
            _isInitialized = true;
        }

        /// <summary>
        /// Override for modifier-specific initialization
        /// </summary>
        protected virtual void OnInitialize() { }

        #endregion

        #region Update

        public virtual void UpdateModifier(float deltaTime)
        {
            if (!_isInitialized || !IsEnabled) return;

            _elapsedTime += deltaTime;
            OnUpdateModifier(deltaTime);
        }

        /// <summary>
        /// Override for modifier-specific update logic
        /// </summary>
        protected virtual void OnUpdateModifier(float deltaTime) { }

        #endregion

        #region Modification Methods

        public virtual Vector3 ModifyPosition(Vector3 originalPosition, ICameraController controller)
        {
            if (!_isInitialized || !IsEnabled) return originalPosition;

            return OnModifyPosition(originalPosition, controller);
        }

        public virtual Quaternion ModifyRotation(Quaternion originalRotation, ICameraController controller)
        {
            if (!_isInitialized || !IsEnabled) return originalRotation;

            return OnModifyRotation(originalRotation, controller);
        }

        public virtual float ModifyFieldOfView(float originalFov, ICameraController controller)
        {
            if (!_isInitialized || !IsEnabled) return originalFov;

            return OnModifyFieldOfView(originalFov, controller);
        }

        #endregion

        #region Abstract Methods - Must be implemented by derived modifiers

        /// <summary>
        /// Apply position modification. Override to implement custom position effects.
        /// </summary>
        protected virtual Vector3 OnModifyPosition(Vector3 originalPosition, ICameraController controller)
        {
            return originalPosition; // No modification by default
        }

        /// <summary>
        /// Apply rotation modification. Override to implement custom rotation effects.
        /// </summary>
        protected virtual Quaternion OnModifyRotation(Quaternion originalRotation, ICameraController controller)
        {
            return originalRotation; // No modification by default
        }

        /// <summary>
        /// Apply field of view modification. Override to implement custom FOV effects.
        /// </summary>
        protected virtual float OnModifyFieldOfView(float originalFov, ICameraController controller)
        {
            return originalFov; // No modification by default
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Set the intensity of the modifier effect
        /// </summary>
        public virtual void SetIntensity(float intensity)
        {
            _intensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Get the current intensity
        /// </summary>
        public virtual float GetIntensity()
        {
            return _intensity;
        }

        /// <summary>
        /// Enable the modifier
        /// </summary>
        public virtual void Enable()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// Disable the modifier
        /// </summary>
        public virtual void Disable()
        {
            IsEnabled = false;
        }

        /// <summary>
        /// Get elapsed time since modifier was initialized
        /// </summary>
        protected float GetElapsedTime()
        {
            return _elapsedTime;
        }

        #endregion

        #region Reset and Cleanup

        public virtual void Reset()
        {
            _elapsedTime = 0f;
            _intensity = 1f;
            OnReset();
        }

        public virtual void Cleanup()
        {
            IsEnabled = false;
            _isInitialized = false;
            OnCleanup();
        }

        /// <summary>
        /// Override for modifier-specific reset logic
        /// </summary>
        protected virtual void OnReset() { }

        /// <summary>
        /// Override for modifier-specific cleanup logic
        /// </summary>
        protected virtual void OnCleanup() { }

        #endregion
    }
}