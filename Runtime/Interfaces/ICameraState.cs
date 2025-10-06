
using UnityEngine;
using System.Threading.Tasks;

namespace Aetheriaum.CameraSystem.Interfaces
{
    /// <summary>
    /// Interface for camera states following State Pattern.
    /// Each camera state represents a different camera behavior (Third Person, Combat, etc.)
    /// Follows SOLID principles with clear separation of concerns.
    /// </summary>
    public interface ICameraState
    {
        /// <summary>
        /// Camera mode this state represents
        /// </summary>
        CameraMode CameraMode { get; }

        /// <summary>
        /// Whether this state is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Camera controller associated with this state
        /// </summary>
        ICameraController Controller { get; }

        /// <summary>
        /// Camera settings for this state
        /// </summary>
        ICameraSettings Settings { get; set; }

        /// <summary>
        /// Initialize the camera state
        /// </summary>
        Task Initialize(ICameraController controller);

        /// <summary>
        /// Called when entering this state
        /// </summary>
        void OnEnter(ICameraState previousState);

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        void OnExit(ICameraState nextState);

        /// <summary>
        /// Update the camera state
        /// </summary>
        void UpdateState(float deltaTime);

        /// <summary>
        /// Handle input for this camera state
        /// </summary>
        void HandleInput(Vector2 lookInput, float zoomInput);

        /// <summary>
        /// Get desired camera position for this state
        /// </summary>
        Vector3 GetDesiredPosition();

        /// <summary>
        /// Get desired camera rotation for this state
        /// </summary>
        Quaternion GetDesiredRotation();

        /// <summary>
        /// Get desired field of view for this state
        /// </summary>
        float GetDesiredFieldOfView();

        /// <summary>
        /// Check if transition to another state is allowed
        /// </summary>
        bool CanTransitionTo(CameraMode targetMode);

        /// <summary>
        /// Apply camera modifiers specific to this state
        /// </summary>
        void ApplyModifiers(ref Vector3 position, ref Quaternion rotation);

        /// <summary>
        /// Reset state to default values
        /// </summary>
        void Reset();
    }
}