using UnityEngine;

/// <summary>Orbit camera behind the player (mouse / right stick), pulled in when something is in the way.</summary>
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5.5f, height = 1.6f, mouseSensitivity = 0.12f, padSensitivity = 140f;
    public float minPitch = -20f, maxPitch = 60f;

    float yaw, pitch = 12f;

    void Start()
    {
        if (target) yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!target) return;
        if (!GameHUD.InputBlocked)
        {
            var look = InputUtil.Look(mouseSensitivity, padSensitivity);
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        }
        var rot = Quaternion.Euler(pitch, yaw, 0);
        var pivot = target.position + Vector3.up * height;
        var back = rot * Vector3.back;
        float dist = distance;
        // the player sits on the Ignore Raycast layer, so this only hits the world
        if (Physics.SphereCast(pivot, 0.25f, back, out var hit, distance, ~(1 << 2), QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(0.6f, hit.distance - 0.1f);
        transform.SetPositionAndRotation(pivot + back * dist, rot);
    }
}
