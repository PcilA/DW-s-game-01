using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3인칭 플레이어 컨트롤러
/// • Shift : 입력 방향으로 일정 거리/시간 동안 **직선 돌진**
///   └ 돌진 중 ⇒ ① 중력 off ② drag 0 ③ 방향 전환 & 가속 무시
///   └ 돌진 종료 ⇒ 버프 속도 유지 (dashBoostActive) + 쿨타임 시작
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMoveController : MonoBehaviour
{
    /* ─── Inspector ───────────────────────────────────── */
    [Header("References")]
    [SerializeField] private Transform cam;
    [SerializeField] private TrailRenderer dashTrail;
    [SerializeField] private Text dashText;

    [Header("Gravity / Physics")]
    public float extraGravityMultiplier = 2f;
    public float airDrag = 0.05f;

    [Header("Ground Drag")]
    public float groundMovingDrag = 0.25f;
    public float groundStopDrag = 5f;

    [Header("Movement")]
    public float moveAcceleration = 10f;
    public float burstAcceleration = 6f;
    public float maxMoveSpeed = 8f;
    public float turnSmoothTime = 0.1f;
    [Range(0, 1)] public float turnResponsiveness = 0.8f;

    [Header("Jump")]
    public float jumpImpulse = 3f;

    [Header("Dash (Shift)")]
    public float dashInitialSpeed = 25f;   // 돌진 속도
    public float dashDuration = 0.15f; // 유지 시간
    public float dashSpeedMultiplier = 1.5f;  // 이후 버프
    public float dashCooldown = 1.0f;  // 🔄 재사용 간격

    /* ─── Runtime ─────────────────────────────────────── */
    Rigidbody rb;
    bool isGrounded;
    bool isDashing;
    bool dashBoostActive;
    float dashTimer;
    float nextDashTime;              // Time.time 기준
    Vector3 dashDir;                   // 잠긴 방향
    float turnSmoothVelocity;

    /* ========== 초기화 ================================= */
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        if (cam == null && Camera.main) cam = Camera.main.transform;
        if (dashText) dashText.enabled = false;
        if (dashTrail) dashTrail.emitting = false;

        rb.drag = groundStopDrag;
    }

    /* ========== UPDATE ================================= */
    void Update()
    {
        HandleJump();
        HandleDashInput();        // 새 기능
    }
    void FixedUpdate()
    {
        HandleMovement();
        ApplyExtraGravity();
        ClampSpeed();
        UpdateDashState();
    }

    /* ─── Jump ────────────────────────────────────────── */
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isDashing)
        {
            isGrounded = false;
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        }
    }

    /* ─── Dash Trigger & Cooldown ─────────────────────── */
    void HandleDashInput()
    {
        if (isDashing) return;                      // 이미 돌진 중
        if (Time.time < nextDashTime) return;       // 쿨타임
        if (!Input.GetKeyDown(KeyCode.LeftShift)) return;
        if (!HasMovementInput()) return;

        dashDir = GetMoveDirection();
        if (dashDir.sqrMagnitude < 0.01f) return;

        // ─ 돌진 시작 ─
        isDashing = true;
        dashBoostActive = true;
        dashTimer = dashDuration;
        nextDashTime = Time.time + dashCooldown;

        rb.useGravity = false;                   // 중력 잠금
        rb.drag = 0f;
        rb.velocity = dashDir * dashInitialSpeed;

        if (dashTrail) dashTrail.emitting = true;
        if (dashText) dashText.enabled = true;

        // (무적 플래그를 여기에)  isInvincible = true;
    }

    /* ─── Dash 진행 & 종료 체크 ───────────────────────── */
    void UpdateDashState()
    {
        if (!isDashing) return;

        dashTimer -= Time.fixedDeltaTime;

        // 돌진 진행: 방향·속도 잠금
        rb.velocity = dashDir * dashInitialSpeed;

        if (dashTimer <= 0f)
        {
            // ─ 돌진 종료 ─
            isDashing = false;
            rb.useGravity = true;

            if (dashTrail) dashTrail.emitting = false;
            if (dashText) dashText.enabled = false;

            // (무적 해제 자리) isInvincible = false;
        }
    }

    /* ─── Movement (지상/공중 일반) ──────────────────── */
    void HandleMovement()
    {
        if (isDashing) return;       // 돌진 중엔 무시

        Vector3 dir = GetMoveDirection();

        // 드래그 결정
        rb.drag = isGrounded
                  ? (dir.sqrMagnitude > 0.01f ? groundMovingDrag : groundStopDrag)
                  : airDrag;

        if (dir.sqrMagnitude > 0.01f)
        {
            // 회전
            float tgt = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float ang = Mathf.SmoothDampAngle(transform.eulerAngles.y, tgt,
                                              ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0, ang, 0);

            // 스냅 가속(정지 → 출발)
            if (IsNearlyZero(rb.velocity))
                rb.AddForce(dir * burstAcceleration, ForceMode.VelocityChange);

            // 지속 가속
            rb.AddForce(dir * moveAcceleration, ForceMode.Acceleration);

            // 방향 보정
            Vector3 hv = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 want = dir * hv.magnitude;
            rb.AddForce((want - hv) * turnResponsiveness, ForceMode.VelocityChange);
        }
        else
        {
            dashBoostActive = false;         // 입력이 없으면 버프 소멸
        }
    }

    /* ─── Gravity & SpeedClamp ───────────────────────── */
    void ApplyExtraGravity()
    {
        if (!isDashing && !isGrounded && extraGravityMultiplier > 1f)
            rb.AddForce(Physics.gravity * (extraGravityMultiplier - 1f),
                        ForceMode.Acceleration);
    }

    void ClampSpeed()
    {
        float limit = maxMoveSpeed *
                      (dashBoostActive ? dashSpeedMultiplier : 1f);

        Vector3 hv = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (hv.magnitude > limit && !isDashing)     // 돌진 속도는 제한 X
            rb.velocity = hv.normalized * limit + Vector3.up * rb.velocity.y;
    }

    /* ─── Ground Check ───────────────────────────────── */
    void OnCollisionEnter(Collision col)
    {
        foreach (var c in col.contacts)
            if (Vector3.Angle(c.normal, Vector3.up) < 50f) { isGrounded = true; break; }
    }

    /* ─── Helpers ───────────────────────────────────── */
    bool HasMovementInput() =>
        Mathf.Abs(Input.GetAxisRaw("Horizontal")) > .01f ||
        Mathf.Abs(Input.GetAxisRaw("Vertical")) > .01f;

    Vector3 GetMoveDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 f = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 r = Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized;
        return (r * h + f * v).normalized;
    }
    bool IsNearlyZero(Vector3 v) => v.sqrMagnitude < 0.01f;
}
