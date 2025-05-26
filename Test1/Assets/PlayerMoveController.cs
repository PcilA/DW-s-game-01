using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3인칭 플레이어 컨트롤러
/// ─ Shift : 입력 방향으로 짧게 초고속 돌진(dash) → 키를 떼기 전까지 이동 속도 버프
/// ─ 돌진 중 Trail 파티클, 돌진 타임만큼 무적 상태 플래그 지원
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMoveController : MonoBehaviour
{
    /* ───────── Inspector ─────────────────────────────────── */
    [Header("References")]
    [SerializeField] private Transform cam;          // 비워 두면 Camera.main
    [SerializeField] private TrailRenderer dashTrail;   // ↖ TrailRenderer 참고
    [SerializeField] private Text dashText;     // “대쉬” UI (optional)

    [Header("Gravity / Physics")]
    public float extraGravityMultiplier = 2f;   // 낙하 가속
    public float airDrag = 0.05f; // 공중 마찰

    [Header("Ground Drag")]
    public float groundMovingDrag = 0.25f;
    public float groundStopDrag = 5f;

    [Header("Movement")]
    public float moveAcceleration = 10f;  // 지속 가속
    public float burstAcceleration = 6f;   // 첫 프레임 VelocityChange
    public float maxMoveSpeed = 8f;
    public float turnSmoothTime = 0.1f;
    [Range(0f, 1f)] public float turnResponsiveness = 0.8f;

    [Header("Jump")]
    public float jumpImpulse = 3f;

    [Header("Dash (Shift)")]
    public float dashInitialSpeed = 25f;   // 돌진 시작 속도
    public float dashDuration = 0.15f; // 돌진 유지 시간(초)
    public float dashSpeedMultiplier = 1.5f;  // 이후 이동 버프 배수

    /* ───────── Runtime ─────────────────────────────────── */
    Rigidbody rb;
    bool isGrounded;
    bool dashBoostActive;     // 버프 지속
    bool isDashing;           // 돌진 중
    float dashTimer;
    float turnSmoothVelocity;

    /* ================ 초기화 ============================== */
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (dashText) dashText.enabled = false;
        if (dashTrail) dashTrail.emitting = false;

        rb.drag = groundStopDrag;
    }

    /* ================ UPDATE ============================== */
    void Update()
    {
        HandleJump();
        HandleDashInput();   // Shift 입력
    }

    void FixedUpdate()
    {
        HandleMovement();
        ApplyExtraGravity();
        ClampSpeed();
        UpdateDashTimer();
    }

    /* ───────── Jump ────────────────────────────────────── */
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isDashing)
        {
            isGrounded = false;
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        }
    }

    /* ───────── Dash 입력 / 트리거 ───────────────────────── */
    void HandleDashInput()
    {
        if (isDashing) return;                    // 돌진 중엔 재입력 무시
        if (!Input.GetKeyDown(KeyCode.LeftShift)) return;
        if (!HasMovementInput()) return;          // 입력 방향 없으면 무시

        Vector3 dir = GetMoveDirection();
        if (dir.sqrMagnitude < 0.01f) return;

        // 돌진 시작 ------------------------------
        isDashing = true;
        dashTimer = dashDuration;
        dashBoostActive = true;

        if (dashTrail) dashTrail.emitting = true;
        if (dashText) dashText.enabled = true;

        // (무적 플래그 자리)  ---------------------
        // isInvincible = true;   // ← 나중에 여기에 변수 선언/해제
        //----------------------------------------

        rb.drag = 0f;                         // 마찰 제거
        rb.velocity = dir * dashInitialSpeed;     // 순간 속도
    }

    /* ───────── Dash 상태 업데이트 ───────────────────────── */
    void UpdateDashTimer()
    {
        if (!isDashing) return;

        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer <= 0f)
        {
            isDashing = false;
            if (dashTrail) dashTrail.emitting = false;
            if (dashText) dashText.enabled = false;

            // (무적 해제 자리) -------------------
            // isInvincible = false;
            //------------------------------------

            // 땅에 있으면 groundMovingDrag, 공중이면 airDrag 로 자연스럽게 이어짐
        }
    }

    /* ───────── Movement (일반 가속 + 버프) ─────────────── */
    void HandleMovement()
    {
        Vector3 dir = GetMoveDirection();

        // 드래그 결정
        if (isGrounded)
            rb.drag = dir.sqrMagnitude > 0.01f ? groundMovingDrag : groundStopDrag;
        else
            rb.drag = airDrag;

        // 입력 有
        if (dir.sqrMagnitude > 0.01f)
        {
            // 몸 회전
            float tgt = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float ang = Mathf.SmoothDampAngle(
                            transform.eulerAngles.y, tgt,
                            ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, ang, 0f);

            // 스냅 가속(정지 → 출발)
            if (IsNearlyZero(rb.velocity) && !isDashing)
                rb.AddForce(dir * burstAcceleration, ForceMode.VelocityChange);

            // 지속 가속
            if (!isDashing)   // 돌진 중엔 별도 속도 유지
                rb.AddForce(dir * moveAcceleration, ForceMode.Acceleration);

            // 방향 보정
            Vector3 hv = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 want = dir * hv.magnitude;
            rb.AddForce((want - hv) * turnResponsiveness, ForceMode.VelocityChange);
        }
        else if (!isDashing)   // 입력 無
        {
            dashBoostActive = false;              // 버프 해제
        }
    }

    /* ───────── Gravity / Speed Clamp ───────────────────── */
    void ApplyExtraGravity()
    {
        if (!isGrounded && extraGravityMultiplier > 1f)
            rb.AddForce(Physics.gravity * (extraGravityMultiplier - 1f), ForceMode.Acceleration);
    }

    void ClampSpeed()
    {
        float limit = maxMoveSpeed *
                      (dashBoostActive ? dashSpeedMultiplier : 1f) *
                      (isDashing ? 999f : 1f);   // 돌진 중엔 제한 없음

        Vector3 hv = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (hv.magnitude > limit)
            rb.velocity = hv.normalized * limit + Vector3.up * rb.velocity.y;
    }

    /* ───────── Ground Check ───────────────────────────── */
    void OnCollisionEnter(Collision col)
    {
        foreach (var c in col.contacts)
            if (Vector3.Angle(c.normal, Vector3.up) < 50f) { isGrounded = true; break; }
    }

    /* ───────── Helpers ───────────────────────────────── */
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
