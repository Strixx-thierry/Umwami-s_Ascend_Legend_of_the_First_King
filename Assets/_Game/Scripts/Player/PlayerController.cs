using UnityEngine;

/// <summary>Minimal third-person movement: camera-relative walk/run with gravity. Stand-in until combat.</summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;
    public float walkSpeed = 3.2f, runSpeed = 6.5f, turnSpeed = 12f, gravity = -20f;

    CharacterController cc;
    float vy;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        Vector2 input = GameHUD.InputBlocked ? Vector2.zero : InputUtil.Move;
        Vector3 fwd = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 dir = fwd * input.y + right * input.x;
        if (dir.sqrMagnitude > 1) dir.Normalize();
        float speed = InputUtil.Sprint ? runSpeed : walkSpeed;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
        vy = cc.isGrounded ? -2f : vy + gravity * Time.deltaTime;
        cc.Move((dir * speed + Vector3.up * vy) * Time.deltaTime);
    }
}
