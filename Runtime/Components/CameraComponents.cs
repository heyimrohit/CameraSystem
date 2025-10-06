using Aetheriaum.CameraSystem.Interfaces;
using UnityEngine;

/// <summary>
/// Camera orbit component handling rotation and distance calculations
/// </summary>
public class CameraOrbit : MonoBehaviour, ICameraOrbit
{
    private ICameraSettings _settings;

    [Header("Orbit State")]
    [SerializeField] private float _horizontalAngle = 0f;
    [SerializeField] private float _verticalAngle = 20f;
    [SerializeField] private float _distance = 8f;

    [Header("Constraints")]
    [SerializeField] private float _minVerticalAngle = -30f;
    [SerializeField] private float _maxVerticalAngle = 60f;
    [SerializeField] private bool _constrainHorizontal = false;
    [SerializeField] private float _minHorizontalAngle = -180f;
    [SerializeField] private float _maxHorizontalAngle = 180f;

    // Interface Properties
    public float HorizontalAngle
    {
        get => _horizontalAngle;
        set => _horizontalAngle = _constrainHorizontal ?
            Mathf.Clamp(value, _minHorizontalAngle, _maxHorizontalAngle) : value;
    }

    public float VerticalAngle
    {
        get => _verticalAngle;
        set => _verticalAngle = Mathf.Clamp(value, _minVerticalAngle, _maxVerticalAngle);
    }

    public float Distance
    {
        get => _distance;
        set => _distance = _settings != null ?
            Mathf.Clamp(value, _settings.MinDistance, _settings.MaxDistance) : value;
    }

    public Vector2 Angles => new Vector2(_horizontalAngle, _verticalAngle);

    // Additional Properties (keep existing)
    public float MinVerticalAngle { get => _minVerticalAngle; set => _minVerticalAngle = value; }
    public float MaxVerticalAngle { get => _maxVerticalAngle; set => _maxVerticalAngle = value; }
    public bool ConstrainHorizontal { get => _constrainHorizontal; set => _constrainHorizontal = value; }
    public float MinHorizontalAngle { get => _minHorizontalAngle; set => _minHorizontalAngle = value; }
    public float MaxHorizontalAngle { get => _maxHorizontalAngle; set => _maxHorizontalAngle = value; }

    #region ICameraOrbit Implementation

    public void Initialize()
    {
        // Basic initialization
    }

    public void Initialize(float horizontalAngle, float verticalAngle, float distance)
    {
        _horizontalAngle = horizontalAngle;
        _verticalAngle = Mathf.Clamp(verticalAngle, _minVerticalAngle, _maxVerticalAngle);
        _distance = distance;
    }

    // Keep existing Initialize method
    public void Initialize(ICameraSettings settings)
    {
        _settings = settings;

        if (_settings != null)
        {
            _distance = Mathf.Clamp(_distance, _settings.MinDistance, _settings.MaxDistance);
        }
    }

    public void HandleInput(Vector2 inputDelta, float zoomDelta, ICameraSettings settings)
    {
        if (settings == null) return;

        // Apply input to angles
        _horizontalAngle += inputDelta.x * settings.LookSensitivity;

        // FIX: Correct the Y-axis logic here too
        _verticalAngle += inputDelta.y * settings.LookSensitivity * (settings.InvertY ? -1f : 1f);

        // Apply constraints
        _verticalAngle = Mathf.Clamp(_verticalAngle, _minVerticalAngle, _maxVerticalAngle);

        if (_constrainHorizontal)
        {
            _horizontalAngle = Mathf.Clamp(_horizontalAngle, _minHorizontalAngle, _maxHorizontalAngle);
        }
        else
        {
            // Normalize horizontal angle
            while (_horizontalAngle > 180f) _horizontalAngle -= 360f;
            while (_horizontalAngle < -180f) _horizontalAngle += 360f;
        }

        // Apply zoom
        if (Mathf.Abs(zoomDelta) > 0.01f)
        {
            _distance -= zoomDelta * settings.ZoomSpeed;
            if (settings != null)
            {
                _distance = Mathf.Clamp(_distance, settings.MinDistance, settings.MaxDistance);
            }
        }
    }

    public void UpdateOrbit(float deltaTime)
    {
        // For now, no additional update logic needed
        // Can be extended for smoothing in the future
    }

    public Vector3 CalculatePosition(Vector3 targetPosition)
    {
        // Convert angles to radians
        float horizontalRad = _horizontalAngle * Mathf.Deg2Rad;
        float verticalRad = _verticalAngle * Mathf.Deg2Rad;

        // Calculate position using spherical coordinates
        float x = _distance * Mathf.Cos(verticalRad) * Mathf.Sin(horizontalRad);
        float y = _distance * Mathf.Sin(verticalRad);
        float z = _distance * Mathf.Cos(verticalRad) * Mathf.Cos(horizontalRad);

        return targetPosition + new Vector3(x, y, z);
    }

    public Quaternion CalculateRotation(Vector3 cameraPosition, Vector3 targetPosition)
    {
        var direction = (targetPosition - cameraPosition).normalized;

        if (direction == Vector3.zero)
            return Quaternion.identity;

        return Quaternion.LookRotation(direction);
    }

    // Keep existing overload for backwards compatibility
    public Quaternion CalculateRotation(Vector3 targetPosition)
    {
        var cameraPosition = CalculatePosition(targetPosition);
        return CalculateRotation(cameraPosition, targetPosition);
    }

    public void SetConstraints(float minVerticalAngle, float maxVerticalAngle, float minDistance, float maxDistance)
    {
        _minVerticalAngle = minVerticalAngle;
        _maxVerticalAngle = maxVerticalAngle;

        // Apply constraints immediately
        _verticalAngle = Mathf.Clamp(_verticalAngle, _minVerticalAngle, _maxVerticalAngle);

        if (_settings != null)
        {
            _settings.MinDistance = minDistance;
            _settings.MaxDistance = maxDistance;
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
        }
    }

    public void SetSmoothingTimes(float rotationSmoothTime, float distanceSmoothTime)
    {
        // For future implementation of smoothing
        // Currently using direct assignment
    }

    public void SetAngles(float horizontal, float vertical, bool instant = false)
    {
        _horizontalAngle = horizontal;
        _verticalAngle = Mathf.Clamp(vertical, _minVerticalAngle, _maxVerticalAngle);

        if (_constrainHorizontal)
        {
            _horizontalAngle = Mathf.Clamp(_horizontalAngle, _minHorizontalAngle, _maxHorizontalAngle);
        }
    }

    public void SetDistance(float distance, bool instant = false)
    {
        _distance = distance;
        if (_settings != null)
        {
            _distance = Mathf.Clamp(_distance, _settings.MinDistance, _settings.MaxDistance);
        }
    }

    public void SetOrbitInstant(float horizontal, float vertical, float distance)
    {
        SetAngles(horizontal, vertical, true);
        SetDistance(distance, true);
    }

    public void ApplyAutoRotation(Vector3 targetForward, float speed, float deltaTime)
    {
        // Calculate desired angle based on target's forward direction
        var targetAngle = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg + 180f;

        // Smooth rotation towards target's back
        var angleDifference = Mathf.DeltaAngle(_horizontalAngle, targetAngle);
        _horizontalAngle += angleDifference * speed * deltaTime * 0.01f;
    }

    public void DrawDebugInfo(Vector3 targetPosition)
    {
        var cameraPosition = CalculatePosition(targetPosition);

        // Draw orbit sphere
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPosition, _distance);

        // Draw camera position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cameraPosition, 0.2f);

        // Draw line from target to camera
        Gizmos.color = Color.white;
        Gizmos.DrawLine(targetPosition, cameraPosition);
    }

    #endregion

    #region Legacy Methods (keep for backwards compatibility)

    public void AddInput(float horizontal, float vertical)
    {
        HorizontalAngle += horizontal;
        VerticalAngle += vertical;

        // Normalize horizontal angle to -180 to 180 range
        while (_horizontalAngle > 180f) _horizontalAngle -= 360f;
        while (_horizontalAngle < -180f) _horizontalAngle += 360f;
    }

    public void AddZoom(float zoomDelta)
    {
        Distance -= zoomDelta; // Negative because scroll up should zoom in
    }

    public void Reset()
    {
        _horizontalAngle = 0f;
        _verticalAngle = 20f;

        if (_settings != null)
        {
            _distance = (_settings.MinDistance + _settings.MaxDistance) * 0.5f;
        }
        else
        {
            _distance = 8f;
        }
    }

    #endregion
}