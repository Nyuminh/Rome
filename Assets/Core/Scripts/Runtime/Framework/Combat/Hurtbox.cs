using UnityEngine;

namespace Blocks.Gameplay.Core.Combat
{
    /// <summary>
    /// Định nghĩa vùng trên cơ thể nhân vật (đầu, thân, tay, chân...)
    /// để tính hệ số sát thương khác nhau.
    /// </summary>
    public enum HurtboxZone
    {
        Head,       // x2 damage
        Torso,      // x1 damage
        Arms,       // x0.75 damage
        Legs        // x0.75 damage
    }

    /// <summary>
    /// Hurtbox - Vùng nhận sát thương.
    /// Gắn vào các bộ phận cơ thể (bone) của nhân vật.
    /// Khi Hitbox đối phương quét trúng collider này → nhân vật mất máu.
    /// 
    /// Layer: Cần gán vào layer riêng biệt (VD: "Hurtbox") để Hitbox chỉ quét đúng layer này.
    /// KHÔNG dùng Rigidbody, chỉ cần Collider (trigger).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour
    {
        [Header("=== Hurtbox Config ===")]
        [Tooltip("Tham chiếu đến GameObject gốc của nhân vật sở hữu Hurtbox này.")]
        [SerializeField] private GameObject owner;

        [Tooltip("Vùng cơ thể (ảnh hưởng đến hệ số sát thương).")]
        [SerializeField] private HurtboxZone zone = HurtboxZone.Torso;

        [Tooltip("Hệ số nhân sát thương cho vùng này (đầu = 2.0, thân = 1.0, tay/chân = 0.75).")]
        [SerializeField] private float damageMultiplier = 1.0f;

        /// <summary>
        /// GameObject gốc sở hữu Hurtbox (dùng để lấy component IHittable, CoreStatsHandler, v.v.).
        /// </summary>
        public GameObject Owner => owner;

        /// <summary>
        /// Vùng cơ thể.
        /// </summary>
        public HurtboxZone Zone => zone;

        /// <summary>
        /// Hệ số sát thương của vùng này.
        /// </summary>
        public float DamageMultiplier => damageMultiplier;

        /// <summary>
        /// Hurtbox đang bật hay tắt (có thể dùng i-frame, super armor, v.v.).
        /// </summary>
        public bool IsActive { get; set; } = true;

        private void Awake()
        {
            // Tự tìm owner nếu chưa gán
            if (owner == null)
            {
                owner = transform.root.gameObject;
            }

            // Đảm bảo collider là trigger
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!IsActive) return;

            // Hiển thị Hurtbox bằng màu xanh lá trong Editor
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

            var col = GetComponent<Collider>();
            if (col is SphereCollider sphere)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
            else if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawSphere(capsule.center, capsule.radius);
            }
        }
#endif
    }
}
