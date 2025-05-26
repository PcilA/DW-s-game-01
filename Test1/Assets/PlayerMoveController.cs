using UnityEngine;
using UnityEngine.UI;          // “대쉬” 문구용 UI

/// <summary>
/// 3인칭 플레이어 컨트롤러
/// ─ 강한 낙하, 스냅 가속, 대시
/// ─ **공중에서는 Drag≈0 으로 유지** → 점프 높이/낙하 버그 제거
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMoveController : MonoBehaviour
{
    /* ── Inspector ───────────────────────────────────────── */
    [Header("References")]
    [SerializeField] private Transform cam;          // 비워 두면 Camera.main
    [SerializeField] private Text dashText;     // “대쉬” 표시용

    [Header("Gravity")]
    [Tooltip("추가 중력 배수 (1 = 기본)")]
    public float extraGravityMultiplier = 2f;

    [Header("Movement — 가속 관련")]
    public float moveAcceleration = 10f;   // 지속 가속
    public float burstAcceleration = 6f;    // 첫 프레임 스냅
    public float maxMoveSpeed = 8f;
    [Space(4)]
    public float groundMovingDrag = 0.25f; // 땅, 입력 有
    public float groundStopDrag = 5f;    // 땅, 입력 無
    public float airDrag = 0.05f; // ✦ 공중 ✦
    [Space(4)]
    public float turnSmoothTime = 0.1f;
    [Range(0f, 1f)]
    public float turnResponsiveness = 0.8f;

    [Header("Jump")]
    public float jumpImpulse = 3f;

    [Header("Dash")]
    public float dashDistance = 4f;
    public float dashSpeedMultiplier = 1.5f;

    /* ── Private ─────────────────────────────────────────── */
    Rigidbody rb;
    bool isGrounded;
    bool dashBoostActive;
    float turnSmoothVelocity;

    /* ── 초기화 ──────────────────────────────────────────── */
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (dashText != null) dashText.enabled = false;

        rb.drag = groundStopDrag;  // 시작은 정지
    }

    /* ── 일반 Update ─────────────────────────────────────── */
    void Update()
    {
        HandleJump();
        HandleDashInput();
    }

    /* ── FixedUpdate ─────────────────────────────────────── */
    void FixedUpdate()
    {
        HandleMovement();
        ApplyExtraGravity();
        ClampSpeed();
    }

    /* ── Jump ────────────────────────────────────────────── */
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            isGrounded = false;
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        }
    }

    /* ── Dash ────────────────────────────────────────────── */
    void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && HasMovementInput())
        {
            Vector3 dir = GetMoveDirection();
            if (dir.sqrMagnitude > 0.01f)
            {
                rb.MovePosition(rb.position + dir * dashDistance);
                dashBoostActive = true;
                if (dashText) dashText.enabled = true;
            }
        }

        if (!HasMovementInput() && dashBoostActive)
        {
            dashBoostActive = false;
            if (dashText) dashText.enabled = false;
        }
    }

    /* ── Movement ───────────────────────────────────────── */
    void HandleMovement()
    {
        Vector3 dir = GetMoveDirection();

        // **드래그 결정** — 위치에 따라
        if (isGrounded)
            rb.drag = dir.sqrMagnitude > 0.01f ? groundMovingDrag : groundStopDrag;
        else
            rb.drag = airDrag;                         // ← 공중은 항상 low-drag

        /* ─ 입력 有 ──────────────────────────────────── */
        if (dir.sqrMagnitude > 0.01f)
        {
            /* 1) 몸 회전 */
            float tgt = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float ang = Mathf.SmoothDampAngle(
                            transform.eulerAngles.y, tgt,
                            ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, ang, 0f);

            /* 2) 스냅 가속 (첫 프레임) */
            if (IsNearlyZero(rb.velocity))
                rb.AddForce(dir * burstAcceleration, ForceMode.VelocityChange);

            /* 3) 지속 가속 */
            rb.AddForce(dir * moveAcceleration, ForceMode.Acceleration);

            /* 4) 방향 스냅 */
            Vector3 hVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 want = dir * hVel.magnitude;
            rb.AddForce((want - hVel) * turnResponsiveness, ForceMode.VelocityChange);
        }
        /* ─ 입력 無 ─────────────────────────────────── */
        else { /* 드래그는 위에서 이미 결정 */ }
    }

    /* ── Extra Gravity ─────────────────────────────────── */
    void ApplyExtraGravity()
    {
        if (!isGrounded && extraGravityMultiplier > 1f)
        {
            Vector3 extra = Physics.gravity * (extraGravityMultiplier - 1f);
            rb.AddForce(extra, ForceMode.Acceleration);
        }
    }

    /* ── Clamp Speed ───────────────────────────────────── */
    void ClampSpeed()
    {
        float cap = maxMoveSpeed * (dashBoostActive ? dashSpeedMultiplier : 1f);
        Vector3 hv = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        if (hv.magnitude > cap)
            rb.velocity = hv.normalized * cap + Vector3.up * rb.velocity.y;
    }

    /* ── Ground Check ─────────────────────────────────── */
    void OnCollisionEnter(Collision col)
    {
        foreach (var c in col.contacts)
            if (Vector3.Angle(c.normal, Vector3.up) < 50f) { isGrounded = true; break; }
    }

    /* ── Helpers ──────────────────────────────────────── */
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
