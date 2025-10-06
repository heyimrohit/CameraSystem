using Aetheriaum.CoreSystem.Architecture.Interface;
using System;
using Unity.Mathematics;
using UnityEngine;
namespace Aetheriaum.CameraSystem.Interfaces
{
    /// <summary>
    /// Camera modes for different gameplay situations
    /// </summary>
    public enum CameraMode
    {
        ThirdPerson,
        FirstPerson,
        Fixed,
        Cinematic,
        Free,
        Combat,
        Dialogue
    }

    /// <summary>
    /// Camera transition types
    /// </summary>
    public enum CameraTransition
    {
        Instant,
        Smooth,
        EaseIn,
        EaseOut,
        EaseInOut,
        Bounce
    }

    /// <summary>
    /// Main camera system service interface following SOLID principles.
    /// Provides centralized camera management with state-based behavior.
    /// </summary>
    public interface ICameraSystem : IUpdatableService
    {
        #region Properties

        /// <summary>
        /// Main camera used by the system
        /// </summary>
        UnityEngine.Camera MainCamera { get; }

        /// <summary>
        /// Active camera controller
        /// </summary>
        ICameraController ActiveController { get; }

        /// <summary>
        /// Current camera mode
        /// </summary>
        CameraMode CurrentMode { get; }

        /// <summary>
        /// Camera target (usually the active character)
        /// </summary>
        Transform Target { get; set; }

        /// <summary>
        /// Whether camera input is enabled
        /// </summary>
        bool InputEnabled { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Fired when camera mode changes
        /// </summary>
        event Action<CameraMode, CameraMode> CameraModeChanged;

        /// <summary>
        /// Fired when camera target changes
        /// </summary>
        event Action<Transform, Transform> TargetChanged;

        #endregion

        #region Mode Management

        /// <summary>
        /// Set camera mode with optional transition
        /// </summary>
        void SetCameraMode(CameraMode mode, CameraTransition transition = CameraTransition.Smooth);

        /// <summary>
        /// Set camera target with optional transition
        /// </summary>
        void SetTarget(Transform target, CameraTransition transition = CameraTransition.Smooth);

        #endregion

        #region Controller Management

        /// <summary>
        /// Register a camera controller for a specific mode
        /// </summary>
        void RegisterController(CameraMode mode, ICameraController controller);

        /// <summary>
        /// Unregister a camera controller
        /// </summary>
        void UnregisterController(CameraMode mode);

        /// <summary>
        /// Get camera controller for a specific mode
        /// </summary>
        ICameraController GetController(CameraMode mode);

        #endregion

        #region State Management

        /// <summary>
        /// Register a camera state for a specific mode
        /// </summary>
        void RegisterState(CameraMode mode, ICameraState state);

        /// <summary>
        /// Unregister a camera state
        /// </summary>
        void UnregisterState(CameraMode mode);

        /// <summary>
        /// Get camera state for a specific mode
        /// </summary>
        ICameraState GetState(CameraMode mode);

        #endregion

        #region Modifier Management

        /// <summary>
        /// Add a camera modifier
        /// </summary>
        void AddModifier(ICameraModifier modifier);

        /// <summary>
        /// Remove a camera modifier
        /// </summary>
        void RemoveModifier(ICameraModifier modifier);

        /// <summary>
        /// Get a specific modifier by type
        /// </summary>
        T GetModifier<T>() where T : class, ICameraModifier;

        #endregion

        #region Camera Effects

        /// <summary>
        /// Start camera shake effect
        /// </summary>
        void ShakeCamera(float intensity, float duration, float frequency = 10f);

        /// <summary>
        /// Stop camera shake effect
        /// </summary>
        void StopCameraShake();

        /// <summary>
        /// Get camera shake component
        /// </summary>
        ICameraShake GetCameraShake();

        #endregion
    }
}