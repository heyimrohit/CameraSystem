using Aetheriaum.CameraSystem.Interfaces;
using UnityEngine;

namespace Aetheriaum.CameraSystem.Modifier
{
    /// <summary>
    /// Camera shake modifier for impact and explosion effects.
    /// Provides configurable shake patterns with smooth falloff.
    /// Implements ICameraShake interface for external control.
    /// </summary>
    public class CameraShakeModifier : CameraModifierBase, ICameraShake
    {
        #region Properties

        public override string ModifierName => "Camera Shake";
        public override int Priority => 100; // Apply shake last

        public bool IsShaking { get; private set; }
        public float Intensity => _currentIntensity;

        #endregion

        #region Private Fields

        // Shake parameters
        private float _duration = 0f;
        private float _frequency = 10f;
        private float _currentIntensity = 0f;
        private float _shakeTimer = 0f;

        // Shake pattern
        private Vector3 _shakeOffset = Vector3.zero;
        private Vector3 _rotationShake = Vector3.zero;

        // Noise offsets for different axes
        private float _noiseOffsetX;
        private float _noiseOffsetY;
        private float _noiseOffsetZ;
        private float _rotationNoiseOffset;

        // Falloff curve
        private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // Generate random noise offsets to prevent predictable patterns
            _noiseOffsetX = Random.Range(0f, 1000f);
            _noiseOffsetY = Random.Range(0f, 1000f);
            _noiseOffsetZ = Random.Range(0f, 1000f);
            _rotationNoiseOffset = Random.Range(0f, 1000f);

            // Create falloff curve if not set
            if (_intensityCurve == null || _intensityCurve.keys.Length == 0)
            {
                _intensityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            }
        }

        #endregion

        #region Update

        protected override void OnUpdateModifier(float deltaTime)
        {
            if (IsShaking)
            {
                _shakeTimer += deltaTime;

                if (_shakeTimer >= _duration)
                {
                    StopShake();
                    return;
                }

                UpdateShakeCalculation();
            }
            else
            {
                // Smoothly reduce shake offset when not shaking
                _shakeOffset = Vector3.Lerp(_shakeOffset, Vector3.zero, deltaTime * 10f);
                _rotationShake = Vector3.Lerp(_rotationShake, Vector3.zero, deltaTime * 10f);
            }
        }

        private void UpdateShakeCalculation()
        {
            // Calculate intensity falloff over time
            var normalizedTime = _shakeTimer / _duration;
            var intensityMultiplier = _intensityCurve.Evaluate(normalizedTime);
            _currentIntensity = _intensity * intensityMultiplier;

            // Generate shake using Perlin noise for smooth, organic movement
            var time = _shakeTimer * _frequency;

            _shakeOffset = new Vector3(
                (Mathf.PerlinNoise(time + _noiseOffsetX, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, time + _noiseOffsetY) - 0.5f) * 2f,
                (Mathf.PerlinNoise(time + _noiseOffsetZ, time + _noiseOffsetZ) - 0.5f) * 2f
            ) * _currentIntensity;

            // Add subtle rotation shake
            _rotationShake = new Vector3(
                (Mathf.PerlinNoise(time + _rotationNoiseOffset, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, time + _rotationNoiseOffset) - 0.5f) * 2f,
                (Mathf.PerlinNoise(time + _rotationNoiseOffset, time) - 0.5f) * 2f
            ) * _currentIntensity * 0.5f; // Rotation shake is subtler
        }

        #endregion

        #region ICameraShake Implementation

        public void StartShake(float intensity, float duration, float frequency = 10f)
        {
            _intensity = Mathf.Max(0f, intensity);
            _duration = Mathf.Max(0.1f, duration);
            _frequency = Mathf.Max(1f, frequency);

            _shakeTimer = 0f;
            _currentIntensity = _intensity;
            IsShaking = true;
            IsEnabled = true;

            // Generate new noise offsets for variation
            _noiseOffsetX = Random.Range(0f, 1000f);
            _noiseOffsetY = Random.Range(0f, 1000f);
            _noiseOffsetZ = Random.Range(0f, 1000f);
            _rotationNoiseOffset = Random.Range(0f, 1000f);
        }

        public void StopShake()
        {
            IsShaking = false;
            _currentIntensity = 0f;
            _shakeTimer = 0f;
            // Don't disable the modifier immediately to allow smooth falloff
        }

        public void UpdateShake(float deltaTime)
        {
            UpdateModifier(deltaTime);
        }

        public Vector3 GetShakeOffset()
        {
            return _shakeOffset;
        }

        #endregion

        #region Modification Implementation

        protected override Vector3 OnModifyPosition(Vector3 originalPosition, ICameraController controller)
        {
            return originalPosition + _shakeOffset;
        }

        protected override Quaternion OnModifyRotation(Quaternion originalRotation, ICameraController controller)
        {
            if (_rotationShake.magnitude < 0.001f)
                return originalRotation;

            // Apply subtle rotation shake
            var rotationOffset = Quaternion.Euler(_rotationShake);
            return originalRotation * rotationOffset;
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Set custom intensity falloff curve
        /// </summary>
        public void SetIntensityCurve(AnimationCurve curve)
        {
            _intensityCurve = curve ?? AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        }

        /// <summary>
        /// Start shake with custom curve
        /// </summary>
        public void StartShakeWithCurve(float intensity, float duration, AnimationCurve curve, float frequency = 10f)
        {
            SetIntensityCurve(curve);
            StartShake(intensity, duration, frequency);
        }

        /// <summary>
        /// Start impact shake (quick, sharp shake)
        /// </summary>
        public void StartImpactShake(float intensity = 1f)
        {
            var impactCurve = new AnimationCurve();
            impactCurve.AddKey(0f, 1f);
            impactCurve.AddKey(0.3f, 0.7f);
            impactCurve.AddKey(1f, 0f);

            StartShakeWithCurve(intensity, 0.5f, impactCurve, 15f);
        }

        /// <summary>
        /// Start explosion shake (longer, more intense)
        /// </summary>
        public void StartExplosionShake(float intensity = 1.5f)
        {
            var explosionCurve = new AnimationCurve();
            explosionCurve.AddKey(0f, 1f);
            explosionCurve.AddKey(0.2f, 1f);
            explosionCurve.AddKey(0.6f, 0.4f);
            explosionCurve.AddKey(1f, 0f);

            StartShakeWithCurve(intensity, 1.2f, explosionCurve, 12f);
        }

        /// <summary>
        /// Start continuous shake (for ongoing effects like earthquakes)
        /// </summary>
        public void StartContinuousShake(float intensity = 0.5f, float frequency = 8f)
        {
            var continuousCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            StartShakeWithCurve(intensity, float.MaxValue, continuousCurve, frequency);
        }

        #endregion

        #region Reset and Cleanup

        protected override void OnReset()
        {
            base.OnReset();

            StopShake();
            _shakeOffset = Vector3.zero;
            _rotationShake = Vector3.zero;
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();

            StopShake();
            _intensityCurve = null;
        }

        #endregion
    }
}