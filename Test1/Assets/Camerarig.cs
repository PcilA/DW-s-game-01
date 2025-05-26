using UnityEngine;

/// <summary>
/// �÷��̾ ����ٴϸ� ���콺�θ� ȸ���ϴ� ������ ī�޶� ����
/// </summary>
public class CameraRigFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;   // Player Transform

    [Header("Look Settings")]
    [SerializeField] private float mouseSpeed = 8f;
    [SerializeField] private float minPitch = -50f;
    [SerializeField] private float maxPitch = 30f;
    private float yaw;   // �¿�
    private float pitch; // ���Ʒ�

    private void LateUpdate()
    {
        if (target == null) return;

        // �� Ÿ�� ��ġ ���󰡱� (Y�� �ʿ信 ���� ������)
        transform.position = target.position;

        // �� ���콺 �Է����� ȸ��(���׸� ȸ���ϹǷ� Player�� �״��)
        yaw += Input.GetAxis("Mouse X") * mouseSpeed;
        pitch -= Input.GetAxis("Mouse Y") * mouseSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
    }
}
