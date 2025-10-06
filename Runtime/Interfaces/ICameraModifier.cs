using UnityEngine;

namespace Aetheriaum.CameraSystem.Interfaces
{
    /// <summary>
    /// Interface for camera modifiers that can affect camera behavior.
    /// Follows Open/Closed Principle - camera can be extended with new modifiers
    /// without modifying existing code.
    /// </summary>
    public interface ICameraModifier
    {
        /// <summary>
        /// Modifier name for identification
        /// </summary>
        string ModifierName { get; }

        /// <summary>
        /// Priority for modifier execution order (lower executes first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether this modifier is currently enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Initialize the modifier
        /// </summary>
        void Initialize();

        /// <summary>
        /// Update the modifier (called every frame)
        /// </summary>
        void UpdateModifier(float deltaTime);

        /// <summary>
        /// Apply position modification
        /// </summary>
        Vector3 ModifyPosition(Vector3 originalPosition, ICameraController controller);

        /// <summary>
        /// Apply rotation modification
        /// </summary>
        Quaternion ModifyRotation(Quaternion originalRotation, ICameraController controller);

        /// <summary>
        /// Apply field of view modification
        /// </summary>
        float ModifyFieldOfView(float originalFov, ICameraController controller);

        /// <summary>
        /// Reset modifier to default state
        /// </summary>
        void Reset();

        /// <summary>
        /// Cleanup modifier resources
        /// </summary>
        void Cleanup();
    }

    /// <summary>
    /// Interface for camera shake functionality
    /// Separated from main modifier interface (Interface Segregation Principle)
    /// </summary>
    public interface ICameraShake
    {
        /// <summary>
        /// Whether camera is currently shaking
        /// </summary>
        bool IsShaking { get; }

        /// <summary>
        /// Current shake intensity
        /// </summary>
        float Intensity { get; }

        /// <summary>
        /// Start camera shake with specified parameters
        /// </summary>
        void StartShake(float intensity, float duration, float frequency = 10f);

        /// <summary>
        /// Stop camera shake immediately
        /// </summary>
        void StopShake();

        /// <summary>
        /// Update shake calculation
        /// </summary>
        void UpdateShake(float deltaTime);

        /// <summary>
        /// Get current shake offset
        /// </summary>
        Vector3 GetShakeOffset();
    }
}