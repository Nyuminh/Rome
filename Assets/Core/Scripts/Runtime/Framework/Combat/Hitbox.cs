using UnityEngine;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core.Combat
{
    /// <summary>
    /// Hitbox - Vùng gây sát thương (Code-Driven Collision).
    /// Gắn vào bone tay/chân/vũ khí của nhân vật.
    /// 
    /// KHÔNG dựa vào Physics Engine tự phát hiện va chạm.
    /// Thay vào đó, khi được kích hoạt bởi CombatManager, script này sẽ gọi
    /// Physics.OverlapSphere tại vị trí hiện tại để quét Hurtbox của đối phương.
    /// 
    /// Chỉ bật trong giai đoạn Active Frames của đòn đánh.
    /// Khi trúng 1 Hurtbox → ghi nhận hit, rồi tắt ngay để tránh multi-hit.
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [Header("=== Hitbox Config ===")]
        [Tooltip("Layer chứa Hurtbox đối phương.")]
        [SerializeField] private LayerMask hurtboxLayer;

        [Tooltip("Bán kính quét mặc định (có thể bị override bởi AttackData).")]
        [SerializeField] private float defaultRadius = 0.3f;

        [Tooltip("Tham chiếu đến owner (nhân vật sở hữu Hitbox này). Tự tìm nếu để trống.")]
        [SerializeField] private GameObject owner;

        /// <summary>Hitbox đang bật hay tắt. Chỉ quét khi IsActive = true.</summary>
        public bool IsActive { get; private set; } = false;

        // Danh sách các owner đã bị trúng trong đòn đánh hiện tại
        // → Tránh 1 cú đấm tính thành 2-3 hit trên cùng 1 đối thủ
        private readonly HashSet<GameObject> m_AlreadyHitOwners = new HashSet<GameObject>();

        // Dữ liệu đòn đánh hiện tại
        private AttackData m_CurrentAttack;
        private float m_CurrentRadius;

        private void Awake()
        {
            if (owner == null)
            {
                owner = transform.root.gameObject;
            }
        }

        /// <summary>
        /// Kích hoạt Hitbox cho đòn đánh hiện tại. Gọi bởi CombatManager khi frame đến giai đoạn Active.
        /// </summary>
        /// <param name="attackData">Frame data của đòn đánh.</param>
        public void Activate(AttackData attackData)
        {
            IsActive = true;
            m_CurrentAttack = attackData;
            m_CurrentRadius = attackData != null ? attackData.hitboxRadius : defaultRadius;
            m_AlreadyHitOwners.Clear();
        }

        /// <summary>
        /// Tắt Hitbox. Gọi khi hết Active Frames hoặc khi đòn đánh kết thúc.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            m_CurrentAttack = null;
            m_AlreadyHitOwners.Clear();
        }

        /// <summary>
        /// Quét va chạm tại vị trí hiện tại bằng Physics.OverlapSphere (Code-Driven).
        /// Trả về danh sách HitResult chứa thông tin hit.
        /// Gọi mỗi frame trong giai đoạn Active bởi CombatManager.
        /// </summary>
        /// <returns>Danh sách kết quả hit (có thể rỗng nếu không trúng ai).</returns>
        public List<HitResult> PerformScan()
        {
            var results = new List<HitResult>();

            if (!IsActive || m_CurrentAttack == null) return results;

            // Tính vị trí quét = vị trí bone + offset (local space → world space)
            Vector3 scanCenter = transform.position + transform.TransformDirection(m_CurrentAttack.hitboxOffset);

            // === Code-Driven Collision: OverlapSphere ===
            // Dùng ~0 (Tất cả các layer) thay vì hurtboxLayer để đảm bảo quét trúng 
            // các quái vật cũ (điển hình như Lion) có thể đang nằm ở layer khác.
            Collider[] hits = Physics.OverlapSphere(scanCenter, m_CurrentRadius, ~0);

            // Tính năng bù đắp (Forgiveness Area): Do bàn tay có bán kính quét cực nhỏ (0.3), 
            // nó thường không chạm tới được vùng trung tâm cục Collider khổng lồ của Sư Tử.
            // Chúng ta thiết lập quét rộng thêm (2.0f) ở phía trước Player để bắt legacy targets.
            Vector3 wideScanCenter = owner.transform.position + owner.transform.forward * 1.5f + Vector3.up;
            Collider[] wideHits = Physics.OverlapSphere(wideScanCenter, 2.0f, ~0);

            var allColliders = new HashSet<Collider>(hits);
            allColliders.UnionWith(wideHits);

            foreach (var col in allColliders)
            {
                var hurtbox = col.GetComponent<Hurtbox>();
                GameObject targetOwner;
                float damageMultiplier = 1.0f;

                if (hurtbox != null)
                {
                    if (!hurtbox.IsActive) continue;
                    targetOwner = hurtbox.Owner;
                    damageMultiplier = hurtbox.DamageMultiplier;
                }
                else
                {
                    // Fallback cho đối tượng cũ (VD: Sư Tử chỉ có LionHitReceiver/IHittable mà không có Hurtbox)
                    var hittable = col.GetComponentInParent<IHittable>();
                    if (hittable == null) continue;
                    
                    targetOwner = ((MonoBehaviour)hittable).gameObject;
                }

                // Không tự đánh mình
                if (targetOwner == owner) continue;

                // Đã trúng owner này trong đòn đánh hiện tại → bỏ qua (anti multi-hit)
                if (m_AlreadyHitOwners.Contains(targetOwner)) continue;

                // Ghi nhận hit
                m_AlreadyHitOwners.Add(targetOwner);

                results.Add(new HitResult
                {
                    hurtbox = hurtbox,
                    targetOwner = targetOwner,
                    hitPoint = col.ClosestPoint(scanCenter),
                    hitNormal = (targetOwner.transform.position - scanCenter).normalized,
                    damage = m_CurrentAttack.damage * damageMultiplier,
                    knockbackForce = m_CurrentAttack.knockbackForce,
                    attackData = m_CurrentAttack
                });
            }

            return results;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            float radius = m_CurrentAttack != null ? m_CurrentAttack.hitboxRadius : defaultRadius;
            Vector3 offset = m_CurrentAttack != null ? m_CurrentAttack.hitboxOffset : Vector3.zero;
            Vector3 center = transform.position + transform.TransformDirection(offset);

            if (IsActive)
            {
                // Đỏ rực khi Active
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
                Gizmos.DrawSphere(center, radius);
            }
            else
            {
                // Vàng nhạt khi Inactive
                Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
                Gizmos.DrawWireSphere(center, radius);
            }
        }
#endif
    }

    /// <summary>
    /// Kết quả trả về từ 1 lần quét Hitbox.
    /// </summary>
    public struct HitResult
    {
        public Hurtbox hurtbox;
        public GameObject targetOwner;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public float damage;
        public float knockbackForce;
        public AttackData attackData;
    }
}
