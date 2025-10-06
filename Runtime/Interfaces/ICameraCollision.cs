using UnityEngine;

namespace Aetheriaum.CameraSystem.Interfaces
{
    /// <summary>
    /// Interface for camera collision detection and handling.
    /// Ensures camera doesn't clip through geometry while maintaining smooth movement.
    /// Follows Single Responsibility Principle.
    /// </summary>
    public interface ICameraCollision
    {
        /// <summary>
        /// Whether collision detection is enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Collision detection layers
        /// </summary>
        LayerMask CollisionLayers { get; set; }

        /// <summary>
        /// Minimum distance from collision surfaces
        /// </summary>
        float CollisionBuffer { get; set; }

        /// <summary>
        /// Collision detection radius
        /// </summary>
        float CollisionRadius { get; set; }

        /// <summary>
        /// Speed of camera adjustment when collision detected
        /// </summary>
        float AdjustmentSpeed { get; set; }

        /// <summary>
        /// Check for collisions and adjust camera position
        /// </summary>
        /// <param name="desiredPosition">The desired camera position</param>
        /// <param name="targetPosition">The target the camera is looking at</param>
        /// <returns>Adjusted position that avoids collisions</returns>
        Vector3 CheckCollision(Vector3 desiredPosition, Vector3 targetPosition);

        /// <summary>
        /// Check if there's a clear line of sight between two points
        /// </summary>
        bool HasLineOfSight(Vector3 from, Vector3 to);

        /// <summary>
        /// Get the closest valid position to the desired position
        /// </summary>
        Vector3 GetClosestValidPosition(Vector3 desiredPosition, Vector3 targetPosition);

        /// <summary>
        /// Debug draw collision detection visualization
        /// </summary>
        void DrawDebugInfo();
    }
}