// Assets/_Project/02_Camera/Interfaces/ICameraOrbit.cs
using UnityEngine;

namespace Aetheriaum.CameraSystem.Interfaces
{
    /// <summary>
    /// Interface for camera orbit functionality.
    /// Handles spherical coordinate camera movement around a target.
    /// Follows Interface Segregation Principle.
    /// </summary>
    public interface ICameraOrbit
    {
        #region Properties

        /// <summary>
        /// Current horizontal angle in degrees
        /// </summary>
        float HorizontalAngle { get; set; }

        /// <summary>
        /// Current vertical angle in degrees
        /// </summary>
        float VerticalAngle { get; set; }

        /// <summary>
        /// Current distance from target
        /// </summary>
        float Distance { get; set; }

        /// <summary>
        /// Current orbit angles as Vector2 (horizontal, vertical)
        /// </summary>
        Vector2 Angles { get; }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the orbit system
        /// </summary>
        void Initialize();

        /// <summary>
        /// Initialize with specific parameters
        /// </summary>
        void Initialize(float horizontalAngle, float verticalAngle, float distance);

        #endregion

        #region Input Handling

        /// <summary>
        /// Handle input for orbit control
        /// </summary>
        void HandleInput(Vector2 inputDelta, float zoomDelta, ICameraSettings settings);

        #endregion

        #region Update

        /// <summary>
        /// Update orbit calculations
        /// </summary>
        void UpdateOrbit(float deltaTime);

        #endregion

        #region Position Calculation

        /// <summary>
        /// Calculate camera position based on target position
        /// </summary>
        Vector3 CalculatePosition(Vector3 targetPosition);

        /// <summary>
        /// Calculate camera rotation based on positions
        /// </summary>
        Quaternion CalculateRotation(Vector3 cameraPosition, Vector3 targetPosition);

        #endregion

        #region Configuration

        /// <summary>
        /// Set orbit constraints
        /// </summary>
        void SetConstraints(float minVerticalAngle, float maxVerticalAngle, float minDistance, float maxDistance);

        /// <summary>
        /// Set smoothing parameters
        /// </summary>
        void SetSmoothingTimes(float rotationSmoothTime, float distanceSmoothTime);

        #endregion

        #region Direct Control

        /// <summary>
        /// Set orbit angles directly
        /// </summary>
        void SetAngles(float horizontal, float vertical, bool instant = false);

        /// <summary>
        /// Set orbit distance directly
        /// </summary>
        void SetDistance(float distance, bool instant = false);

        /// <summary>
        /// Set complete orbit state instantly
        /// </summary>
        void SetOrbitInstant(float horizontal, float vertical, float distance);

        #endregion

        #region Auto-Rotation

        /// <summary>
        /// Apply automatic rotation towards target's forward direction
        /// </summary>
        void ApplyAutoRotation(Vector3 targetForward, float speed, float deltaTime);

        #endregion

        #region Utility

        /// <summary>
        /// Reset orbit to default state
        /// </summary>
        void Reset();

        /// <summary>
        /// Draw debug information
        /// </summary>
        void DrawDebugInfo(Vector3 targetPosition);

        #endregion
    }
}