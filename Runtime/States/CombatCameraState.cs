using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aetheriaum.CameraSystem.Interfaces;

namespace Aetheriaum.CameraSystem.States
{
    /// <summary>
    /// Combat camera state that focuses on combat scenarios.
    /// Provides dynamic framing of enemies and enhanced responsiveness during fights.
    /// Implements target prioritization and combat-specific behaviors.
    /// </summary>
    public class CombatCameraState : CameraStateBase
    {
        #region Properties

        public override CameraMode CameraMode => CameraMode.Combat;

        #endregion

        #region Private Fields

        // Combat-specific parameters
        private float _horizontalAngle = 0f;
        private float _verticalAngle = 15f;
        private float _currentDistance = 6f;
        private float _targetDistance = 6f;

        // Combat targets
        private Transform _primaryTarget;
        private List<Transform> _combatTargets = new List<Transform>();
        private float _targetSwitchCooldown = 0f;

        // Camera dynamics
        private Vector3 _combatCenter;
        private float _combatRadius = 10f;
        private Vector2 _inputVelocity;

        // Combat-specific constraints
        private readonly float _minVerticalAngle = -20f;
        private readonly float _maxVerticalAngle = 45f;
        //private readonly float _combatFollowSpeed = 8f;
        //private readonly float _combatRotationSpeed = 5f;

        // Auto-target tracking
        private float _targetTrackingWeight = 0f;
        private Vector3 _lastPlayerPosition;

        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // Combat camera uses closer distance
            _currentDistance = _targetDistance = 6f;
        }

        #endregion

        #region State Lifecycle

        protected override void OnEnterState(ICameraState previousState)
        {
            base.OnEnterState(previousState);

            // Inherit angle from previous state if possible
            if (previousState is ThirdPersonCameraState thirdPersonState)
            {
                var (horizontal, vertical, distance) = thirdPersonState.GetOrbitParameters();
                _horizontalAngle = horizontal;
                _verticalAngle = Mathf.Clamp(vertical, _minVerticalAngle, _maxVerticalAngle);
                _currentDistance = Mathf.Min(distance, 8f); // Closer for combat
            }

            // Initialize combat tracking
            UpdateCombatTargets();
            if (Controller?.Target != null)
            {
                _lastPlayerPosition = Controller.Target.position;
            }
        }

        protected override void OnExitState(ICameraState nextState)
        {
            base.OnExitState(nextState);

            // Clear combat-specific data
            _combatTargets.Clear();
            _primaryTarget = null;
            _targetTrackingWeight = 0f;
        }

        #endregion

        #region Update Logic

        protected override void OnUpdateState(float deltaTime)
        {
            // Update combat targets and priorities
            UpdateCombatTargets();

            // Update target tracking
            UpdateTargetTracking(deltaTime);

            // Update combat center calculation
            UpdateCombatCenter();

            // Update cooldowns
            if (_targetSwitchCooldown > 0f)
            {
                _targetSwitchCooldown -= deltaTime;
            }
        }

        private void UpdateCombatTargets()
        {
            _combatTargets.Clear();

            var player = GetTarget();
            if (player == null) return;

            // Find all enemies within combat range
            var colliders = Physics.OverlapSphere(player.position, 15f);

            foreach (var collider in colliders)
            {
                // Check if it's an enemy (you'll need to implement your own enemy detection)
                if (IsEnemyTarget(collider.transform))
                {
                    _combatTargets.Add(collider.transform);
                }
            }

            // Update primary target
            UpdatePrimaryTarget();
        }

        private bool IsEnemyTarget(Transform target)
        {
            // Implement your enemy detection logic here
            // This could check for enemy tags, components, etc.
            return target.CompareTag("Enemy") || target.GetComponent<Collider>() != null;
        }

        private void UpdatePrimaryTarget()
        {
            if (_combatTargets.Count == 0)
            {
                _primaryTarget = null;
                return;
            }

            var player = GetTarget();
            if (player == null) return;

            // Find closest enemy as primary target
            Transform closestEnemy = null;
            float closestDistance = float.MaxValue;

            foreach (var enemy in _combatTargets)
            {
                if (enemy == null) continue;

                var distance = Vector3.Distance(player.position, enemy.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }

            _primaryTarget = closestEnemy;
        }

        private void UpdateTargetTracking(float deltaTime)
        {
            var player = GetTarget();
            if (player == null) return;

            // Increase tracking weight when player moves or when there are targets
            var playerMovement = Vector3.Distance(player.position, _lastPlayerPosition);
            _lastPlayerPosition = player.position;

            if (_combatTargets.Count > 0 || playerMovement > 0.1f)
            {
                _targetTrackingWeight = Mathf.Min(1f, _targetTrackingWeight + deltaTime * 2f);
            }
            else
            {
                _targetTrackingWeight = Mathf.Max(0f, _targetTrackingWeight - deltaTime * 0.5f);
            }
        }

        private void UpdateCombatCenter()
        {
            var player = GetTarget();
            if (player == null) return;

            if (_combatTargets.Count == 0)
            {
                _combatCenter = player.position;
                _combatRadius = 5f;
                return;
            }

            // Calculate center point of combat area
            var center = player.position;
            var totalWeight = 1f; // Player weight

            foreach (var target in _combatTargets)
            {
                if (target != null)
                {
                    center += target.position * 0.3f; // Enemies have less weight
                    totalWeight += 0.3f;
                }
            }

            _combatCenter = center / totalWeight;

            // Calculate combat radius
            _combatRadius = 5f;
            foreach (var target in _combatTargets)
            {
                if (target != null)
                {
                    var distance = Vector3.Distance(_combatCenter, target.position);
                    _combatRadius = Mathf.Max(_combatRadius, distance + 3f);
                }
            }

            _combatRadius = Mathf.Min(_combatRadius, 12f); // Cap the radius
        }

        #endregion

        #region Input Handling

        protected override void OnHandleInput(Vector2 lookInput, float zoomInput)
        {
            // Handle look input with combat-specific sensitivity
            HandleCombatLookInput(lookInput);

            // Handle zoom input (more restricted in combat)
            HandleCombatZoomInput(zoomInput);

            // Handle target switching
            HandleTargetSwitching();
        }

        private void HandleCombatLookInput(Vector2 lookInput)
        {
            if (Settings == null) return;

            // Higher sensitivity in combat for quick reactions
            var combatSensitivity = Settings.LookSensitivity * 1.2f;
            var horizontalInput = lookInput.x * combatSensitivity;
            var verticalInput = lookInput.y * combatSensitivity * (Settings.InvertY ? 1f : -1f);

            // Less smoothing in combat for responsiveness
            var smoothedInput = Vector2.SmoothDamp(
                Vector2.zero,
                new Vector2(horizontalInput, verticalInput),
                ref _inputVelocity,
                0.05f // Faster response
            );

            _horizontalAngle += smoothedInput.x;
            _verticalAngle += smoothedInput.y;

            // Combat-specific angle constraints
            _verticalAngle = Mathf.Clamp(_verticalAngle, _minVerticalAngle, _maxVerticalAngle);
            _horizontalAngle = _horizontalAngle % 360f;
        }

        private void HandleCombatZoomInput(float zoomInput)
        {
            if (Settings == null || Mathf.Abs(zoomInput) < 0.01f) return;

            // More restricted zoom range in combat
            var minCombatDistance = Mathf.Max(Settings.MinDistance, 4f);
            var maxCombatDistance = Mathf.Min(Settings.MaxDistance, 10f);

            _targetDistance -= zoomInput * Settings.ZoomSpeed * 0.8f; // Slower zoom in combat
            _targetDistance = Mathf.Clamp(_targetDistance, minCombatDistance, maxCombatDistance);
        }

        private void HandleTargetSwitching()
        {
            // Implement target switching logic here
            // This could be triggered by input or automatic based on threat level
            if (_targetSwitchCooldown <= 0f && _combatTargets.Count > 1)
            {
                // Example: Switch to closest threatening target
                // You would implement your target switching logic here
            }
        }

        #endregion

        #region Camera Calculations

        protected override Vector3 CalculateTargetPosition()
        {
            var player = GetTarget();
            if (player == null) return _currentPosition;

            // Base position calculation
            var baseTargetPosition = player.position + Vector3.up * (Settings?.HeightOffset ?? 1.5f);

            // Apply combat center influence
            var combatInfluence = Vector3.Lerp(baseTargetPosition, _combatCenter, _targetTrackingWeight * 0.3f);

            // Calculate camera offset using spherical coordinates
            var horizontalRadians = _horizontalAngle * Mathf.Deg2Rad;
            var verticalRadians = _verticalAngle * Mathf.Deg2Rad;

            var horizontalDistance = _currentDistance * Mathf.Cos(verticalRadians);
            var verticalDistance = _currentDistance * Mathf.Sin(verticalRadians);

            var offset = new Vector3(
                horizontalDistance * Mathf.Sin(horizontalRadians),
                verticalDistance,
                horizontalDistance * Mathf.Cos(horizontalRadians)
            );

            return combatInfluence - offset;
        }

        protected override Quaternion CalculateTargetRotation()
        {
            var player = GetTarget();
            if (player == null) return _currentRotation;

            var cameraPosition = CalculateTargetPosition();
            Vector3 lookTarget;

            // Blend between player and primary combat target
            if (_primaryTarget != null && _targetTrackingWeight > 0.1f)
            {
                var playerPosition = player.position + Vector3.up * (Settings?.HeightOffset ?? 1.5f);
                var enemyPosition = _primaryTarget.position + Vector3.up * 1f;

                lookTarget = Vector3.Lerp(playerPosition, enemyPosition, _targetTrackingWeight * 0.4f);
            }
            else
            {
                lookTarget = player.position + Vector3.up * (Settings?.HeightOffset ?? 1.5f);
            }

            var lookDirection = (lookTarget - cameraPosition).normalized;
            return Quaternion.LookRotation(lookDirection);
        }

        protected override float CalculateTargetFieldOfView()
        {
            var baseFov = Settings?.DefaultFieldOfView ?? 60f;

            // Slightly wider FOV in combat for better situational awareness
            var combatFovBonus = _targetTrackingWeight * 5f;

            // Adjust based on combat intensity (number of enemies)
            var intensityMultiplier = Mathf.Clamp01(_combatTargets.Count / 3f) * 3f;

            return baseFov + combatFovBonus + intensityMultiplier;
        }

        #endregion

        #region Transition Rules

        protected override bool OnCanTransitionTo(CameraMode targetMode)
        {
            switch (targetMode)
            {
                case CameraMode.Combat:
                    return false; // Already in combat mode

                case CameraMode.ThirdPerson:
                    return _combatTargets.Count == 0; // Can exit combat when no enemies

                case CameraMode.Cinematic:
                case CameraMode.Dialogue:
                    return true; // Allow for cutscenes/dialogue even in combat

                case CameraMode.FirstPerson:
                    return _combatTargets.Count == 0; // Restrict first person in combat

                case CameraMode.Fixed:
                case CameraMode.Free:
                    return false; // Don't allow these during combat

                default:
                    return false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Force switch to a specific combat target
        /// </summary>
        public void SetPrimaryTarget(Transform target)
        {
            if (_combatTargets.Contains(target))
            {
                _primaryTarget = target;
                _targetSwitchCooldown = 1f; // Prevent rapid switching
            }
        }

        /// <summary>
        /// Add a combat target manually
        /// </summary>
        public void AddCombatTarget(Transform target)
        {
            if (target != null && !_combatTargets.Contains(target))
            {
                _combatTargets.Add(target);

                if (_primaryTarget == null)
                {
                    _primaryTarget = target;
                }
            }
        }

        /// <summary>
        /// Remove a combat target
        /// </summary>
        public void RemoveCombatTarget(Transform target)
        {
            _combatTargets.Remove(target);

            if (_primaryTarget == target)
            {
                UpdatePrimaryTarget();
            }
        }

        /// <summary>
        /// Get current combat status
        /// </summary>
        public bool IsInCombat()
        {
            return _combatTargets.Count > 0;
        }

        /// <summary>
        /// Get combat parameters for debugging
        /// </summary>
        public (int targetCount, Vector3 combatCenter, float combatRadius) GetCombatInfo()
        {
            return (_combatTargets.Count, _combatCenter, _combatRadius);
        }

        #endregion

        #region Reset

        protected override void OnReset()
        {
            base.OnReset();

            _horizontalAngle = 0f;
            _verticalAngle = 15f;
            _currentDistance = _targetDistance = 6f;
            _inputVelocity = Vector2.zero;
            _targetTrackingWeight = 0f;
            _targetSwitchCooldown = 0f;

            _combatTargets.Clear();
            _primaryTarget = null;
        }

        #endregion
    }
}