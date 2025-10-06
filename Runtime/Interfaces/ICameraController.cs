using UnityEngine;

namespace Aetheriaum.CameraSystem.Interfaces
{
    /// <summary>
    /// Interface for camera controllers managing specific camera behaviors.
    /// Each controller handles a specific camera mode (Third Person, Combat, etc.).
    /// Follows Single Responsibility and Open/Closed principles.
    /// </summary>
    public interface ICameraController
    {
        #region Properties

        /// <summary>
        /// Camera mode this controller handles
        /// </summary>
        CameraMode CameraMode { get; }

        /// <summary>
        /// Transform of the camera this controller manages
        /// </summary>
        Transform CameraTransform { get; }

        /// <summary>
        /// Target that the camera should follow/look at
        /// </summary>
        Transform Target { get; set; }

        /// <summary>
        /// Camera settings used by this controller
        /// </summary>
        ICameraSettings Settings { get; set; }

        /// <summary>
        /// Whether this controller is currently active
        /// </summary>
        bool IsActive { get; }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initialize the camera controller
        /// </summary>
        void Initialize();

        /// <summary>
        /// Activate this camera controller
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivate this camera controller
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Update the camera controller (called every frame when active)
        /// </summary>
        void UpdateController(float deltaTime);

        #endregion

        #region Input Handling

        /// <summary>
        /// Handle look input (mouse/touch movement)
        /// </summary>
        void HandleLookInput(Vector2 lookInput);

        /// <summary>
        /// Handle zoom input (scroll wheel/pinch)
        /// </summary>
        void HandleZoomInput(float zoomInput);

        /// <summary>
        /// Handle additional input specific to this controller
        /// </summary>
        void HandleCustomInput();

        #endregion

        #region Camera Calculation

        /// <summary>
        /// Calculate desired camera position
        /// </summary>
        Vector3 CalculateDesiredPosition();

        /// <summary>
        /// Calculate desired camera rotation
        /// </summary>
        Quaternion CalculateDesiredRotation();

        /// <summary>
        /// Calculate desired field of view
        /// </summary>
        float CalculateDesiredFieldOfView();

        #endregion

        #region State Management

        /// <summary>
        /// Set camera position immediately (for teleportation, etc.)
        /// </summary>
        void SetPosition(Vector3 position);

        /// <summary>
        /// Set camera rotation immediately
        /// </summary>
        void SetRotation(Quaternion rotation);

        /// <summary>
        /// Reset controller to default state
        /// </summary>
        void Reset();

        #endregion

        #region Transition Support

        /// <summary>
        /// Get current camera state for smooth transitions
        /// </summary>
        //(Vector3 position, Quaternion rotation, float fov) GetCurrentState();

        /// <summary>
        /// Set camera state for smooth transitions
        /// </summary>
        void SetCurrentState(Vector3 position, Quaternion rotation, float fov);

        /// <summary>
        /// Check if this controller can transition to another mode
        /// </summary>
        bool CanTransitionTo(CameraMode targetMode);
        void TransitionFrom(ICameraController controller, CameraTransition transition);

        #endregion
    }
}