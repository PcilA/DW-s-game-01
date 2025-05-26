using UnityEngine;

/// <summary>
/// 플레이어를 따라다니며 마우스로만 회전하는 간단한 카메라 리그
/// </summary>
public class CameraRigFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;   // Player Transform

    [Header("Look Settings")]
    [SerializeField] private float mouseSpeed = 8f;
    [SerializeField] private float minPitch = -50f;
    [SerializeField] private float maxPitch = 30f;
    private float yaw;   // 좌우
    private float pitch; // 위아래

    private void LateUpdate()
    {
        if (target == null) return;

        // ① 타깃 위치 따라가기 (Y는 필요에 따라 오프셋)
        transform.position = target.position;

        // ② 마우스 입력으로 회전(리그만 회전하므로 Player는 그대로)
        yaw += Input.GetAxis("Mouse X") * mouseSpeed;
        pitch -= Input.GetAxis("Mouse Y") * mouseSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
    }
}
