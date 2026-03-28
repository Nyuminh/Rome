using UnityEngine;

namespace Blocks.Gameplay.Core.Combat
{
    /// <summary>
    /// Pushbox - Vùng cản vật lý.
    /// Ngăn 2 nhân vật đi xuyên qua nhau bằng cách đẩy ra, KHÔNG dùng Rigidbody.
    /// 
    /// Mỗi nhân vật có 1 Pushbox (Capsule/Cylinder).
    /// Mỗi frame, kiểm tra overlap với Pushbox khác → tính vector đẩy → dịch chuyển CharacterController.
    /// 
    /// Đây là cách các game đối kháng chuyên nghiệp xử lý: không dùng physics engine, 
    /// mà chủ động code kiểm tra va chạm pushbox.
    /// </summary>
    public class Pushbox : MonoBehaviour
    {
        [Header("=== Pushbox Config ===")]
        [Tooltip("Bán kính capsule pushbox.")]
        [SerializeField] private float radius = 0.4f;

        [Tooltip("Chiều cao capsule pushbox.")]
        [SerializeField] private float height = 1.8f;

        [Tooltip("Lực đẩy mỗi frame khi overlap (đơn vị: unit/frame). Giá trị nhỏ = đẩy mềm.")]
        [SerializeField] private float pushStrength = 0.05f;

        [Tooltip("Layer chứa Pushbox đối phương.")]
        [SerializeField] private LayerMask pushboxLayer;

        [Tooltip("Tham chiếu đến CharacterController (tự tìm nếu bỏ trống).")]
        [SerializeField] private CharacterController characterController;

        /// <summary>Bán kính pushbox, dùng cho quét overlap.</summary>
        public float Radius => radius;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponentInParent<CharacterController>();
            }
        }

        private void LateUpdate()
        {
            ResolvePushCollisions();
        }

        /// <summary>
        /// Quét các Pushbox overlap và đẩy nhân vật ra.
        /// Dùng Physics.OverlapCapsule thay vì để Physics Engine tự xử lý.
        /// </summary>
        private void ResolvePushCollisions()
        {
            if (characterController == null || !characterController.enabled) return;

            Vector3 center = transform.position + Vector3.up * (height * 0.5f);
            Vector3 point1 = center + Vector3.up * (height * 0.5f - radius);
            Vector3 point2 = center - Vector3.up * (height * 0.5f - radius);

            Collider[] overlaps = Physics.OverlapCapsule(point1, point2, radius, pushboxLayer);

            foreach (var col in overlaps)
            {
                // Bỏ qua collider của chính mình
                if (col.transform.root == transform.root) continue;

                var otherPushbox = col.GetComponent<Pushbox>();
                if (otherPushbox == null) continue;

                // Tính hướng đẩy (từ đối phương → mình, theo mặt phẳng XZ)
                Vector3 pushDir = transform.position - col.transform.position;
                pushDir.y = 0f;

                // Nếu đứng chồng hoàn toàn → đẩy ra sau
                if (pushDir.sqrMagnitude < 0.001f)
                {
                    pushDir = -transform.forward;
                }

                pushDir.Normalize();

                // Tính khoảng cách overlap
                float combinedRadius = radius + otherPushbox.Radius;
                float distance = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(col.transform.position.x, 0, col.transform.position.z)
                );

                float overlap = combinedRadius - distance;
                if (overlap <= 0f) continue;

                // Dịch chuyển ra bằng CharacterController.Move
                // Chỉ đẩy 50% (mỗi bên tự đẩy 50% → tổng 100%)
                Vector3 pushVector = pushDir * (overlap * 0.5f + pushStrength);
                characterController.Move(pushVector);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Hiển thị pushbox bằng màu xanh dương trong Editor
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);

            Vector3 center = transform.position + Vector3.up * (height * 0.5f);
            Vector3 top = center + Vector3.up * (height * 0.5f - radius);
            Vector3 bottom = center - Vector3.up * (height * 0.5f - radius);

            Gizmos.DrawSphere(top, radius);
            Gizmos.DrawSphere(bottom, radius);
            Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
            Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
            Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
            Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
        }
#endif
    }
}
