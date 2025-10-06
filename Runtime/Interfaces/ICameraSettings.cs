namespace Aetheriaum.CameraSystem.Interfaces
{
    /// <summary>
    /// Interface for camera settings configuration.
    /// Allows different settings implementations while maintaining consistency.
    /// Follows Interface Segregation Principle.
    /// </summary>
    public interface ICameraSettings
    {
        #region Movement Settings

        /// <summary>
        /// Speed at which camera follows the target
        /// </summary>
        float FollowSpeed { get; set; }

        /// <summary>
        /// Speed of camera rotation
        /// </summary>
        float RotationSpeed { get; set; }

        /// <summary>
        /// Speed of zoom in/out
        /// </summary>
        float ZoomSpeed { get; set; }

        /// <summary>
        /// Damping factor for smooth movements
        /// </summary>
        float Damping { get; set; }

        #endregion

        #region Distance Settings

        /// <summary>
        /// Minimum distance from target
        /// </summary>
        float MinDistance { get; set; }

        /// <summary>
        /// Maximum distance from target
        /// </summary>
        float MaxDistance { get; set; }

        /// <summary>
        /// Height offset from target position
        /// </summary>
        float HeightOffset { get; set; }

        #endregion

        #region Input Settings

        /// <summary>
        /// Mouse/touch sensitivity for look input
        /// </summary>
        float LookSensitivity { get; set; }

        /// <summary>
        /// Whether Y-axis input should be inverted
        /// </summary>
        bool InvertY { get; set; }

        #endregion

        #region Field of View Settings

        /// <summary>
        /// Default field of view
        /// </summary>
        float DefaultFieldOfView { get; set; }

        /// <summary>
        /// Minimum field of view
        /// </summary>
        float MinFieldOfView { get; set; }

        /// <summary>
        /// Maximum field of view
        /// </summary>
        float MaxFieldOfView { get; set; }

        #endregion

        #region Collision Settings

        /// <summary>
        /// Whether collision detection is enabled
        /// </summary>
        bool CollisionDetection { get; set; }

        /// <summary>
        /// Layers to check for collision
        /// </summary>
        UnityEngine.LayerMask CollisionLayers { get; set; }

        /// <summary>
        /// Buffer distance from collision surfaces
        /// </summary>
        float CollisionBuffer { get; set; }

        /// <summary>
        /// Radius for collision detection
        /// </summary>
        float CollisionRadius { get; set; }

        #endregion
    }
}