using UnityEngine;
using UnityEngine.UI;          // ―― “대쉬” 문구용 UI

/// <summary>
/// 3인칭 플레이어 컨트롤러 (강한 낙하, 스냅 가속, 대시)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMoveController : MonoBehaviour
{
    /* ── Inspector ─────────────────────────────────────────────── */
    [Header("References")]
    [SerializeField] private Transform cam;          // 비워 두면 Camera.main
    [SerializeField] private Text dashText;     // “대쉬” 표시용 <UI Text>

    [Header("Gravity")]
    [Tooltip("추가 중력 배수 (1 = 기본)")]
    public float extraGravityMultiplier = 2f;        // 엘든링스러운 급낙하

    [Header("Movement")]
    public float moveAcceleration = 10f;       // 지속 가속
    public float burstAcceleration = 6f;        // ★키를 눌렀을 때 1-프레임 VelocityChange
    public float maxMoveSpeed = 8f;
    public float movingDrag = 0.25f;
    public float stopDrag = 5f;
    public float turnSmoothTime = 0.1f;
    [Range(0f, 1f)]
    public float turnResponsiveness = 0.8f;      // 방향 전환 보정

    [Header("Jump")]
    public float jumpImpulse = 3f;

    [Header("Dash")]
    public float dashDistance = 4f;        // 순간 이동 거리
    public float dashSpeedMultiplier = 1.5f;      // ↖ 방향키 유지 중 속도 배수

    /* ── Private ──────────────────────────────────────────────── */
    Rigidbody rb;
    bool isGrounded;
    bool dashBoostActive;
    float turnSmoothVelocity;

    /* ── 초기화 ──────────────────────────────────────────────── */
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 옆으로 뒤집히지 않도록 XZ 고정 (Y 회전은 허용)
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (dashText != null) dashText.enabled = false;

        rb.drag = stopDrag;     // 정지-드래그로 시작
    }

    /* ── 일반 Update : 점프 & 대시 입력 ───────────────────────── */
    void Update()
    {
        HandleJump();
        HandleDashInput();
    }

    /* ── FixedUpdate : 이동/중력/속도제한 ─────────────────────── */
    void FixedUpdate()
    {
        HandleMovement();
        ApplyExtraGravity();
        ClampSpeed();
    }

    /* ── Jump ──────────────────────────────────────────────── */
    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            isGrounded = false;
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        }
    }

    /* ── Dash ──────────────────────────────────────────────── */
    void HandleDashInput()
    {
        // 이동 입력이 있는 상태에서 Shift 누르면 대시
        if (Input.GetKeyDown(KeyCode.LeftShift) && HasMovementInput())
        {
            Vector3 moveDir = GetMoveDirection();
            if (moveDir.sqrMagnitude > 0.01f)
            {
                // 순간이동
                rb.MovePosition(rb.position + moveDir * dashDistance);

                // 대쉬 표기 & 부스트 적용
                dashBoostActive = true;
                if (dashText != null) dashText.enabled = true;
            }
        }

        // 방향키를 전부 떼면 대시 부스트 종료
        if (!HasMovementInput() && dashBoostActive)
        {
            dashBoostActive = false;
            if (dashText != null) dashText.enabled = false;
        }
    }

    /* ── Movement ─────────────────────────────────────────── */
    void HandleMovement()
    {
        Vector3 moveDir = GetMoveDirection();

        // ── 입력 有 ─────────────────────────────────
        if (moveDir.sqrMagnitude > 0.01f)
        {
            rb.drag = movingDrag;

            /* (1) 몸 회전 */
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(
                                    transform.eulerAngles.y,
                                    targetAngle,
                                    ref turnSmoothVelocity,
                                    turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            /* (2) 스냅 가속 : 첫 프레임에 VelocityChange */
            if (IsNearlyZero(rb.velocity))
                rb.AddForce(moveDir * burstAcceleration, ForceMode.VelocityChange);

            /* (3) 지속 가속 */
            rb.AddForce(moveDir * moveAcceleration, ForceMode.Acceleration);

            /* (4) 빠른 방향 전환 보정 */
            Vector3 horizVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            Vector3 desiredVel = moveDir * horizVel.magnitude;
            Vector3 snapDelta = (desiredVel - horizVel) * turnResponsiveness;
            rb.AddForce(snapDelta, ForceMode.VelocityChange);
        }
        // ── 입력 無 ────────────────────────────────
        else
        {
            rb.drag = stopDrag;
            dashBoostActive = false;
            if (dashText != null) dashText.enabled = false;
        }
    }

    /* ── Extra Gravity ────────────────────────────────────── */
    void ApplyExtraGravity()
    {
        if (!isGrounded && extraGravityMultiplier > 1f)
        {
            Vector3 extraForce = Physics.gravity * (extraGravityMultiplier - 1f);
            rb.AddForce(extraForce, ForceMode.Acceleration);
        }
    }

    /* ── Clamp Speed ─────────────────────────────────────── */
    void ClampSpeed()
    {
        float speedLimit = maxMoveSpeed * (dashBoostActive ? dashSpeedMultiplier : 1f);

        Vector3 horizVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (horizVel.magnitude > speedLimit)
        {
            Vector3 limited = horizVel.normalized * speedLimit;
            rb.velocity = new Vector3(limited.x, rb.velocity.y, limited.z);
        }
    }

    /* ── Ground Check ────────────────────────────────────── */
    void OnCollisionEnter(Collision col)
    {
        foreach (ContactPoint c in col.contacts)
        {
            if (Vector3.Angle(c.normal, Vector3.up) < 50f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    /* ── Helper : 입력 & 방향 구하기 ─────────────────────── */
    bool HasMovementInput() =>
        Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f ||
        Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;

    Vector3 GetMoveDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized;

        return (camRight * h + camForward * v).normalized;
    }

    bool IsNearlyZero(Vector3 v) => v.sqrMagnitude < 0.01f;
}
