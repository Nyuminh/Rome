using UnityEngine;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core.Combat
{
    /// <summary>
    /// Hitbox chuyên nghiệp - Hỗ trợ cả hình Cầu và hình Hộp.
    /// Tự động lấy kích thước từ BoxCollider (nếu có) để quét va chạm chính xác theo hình dáng vũ khí.
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [Header("=== Hitbox Config ===")]
        [Tooltip("Layer chứa Hurtbox đối phương.")]
        [SerializeField] private LayerMask hurtboxLayer;

        [Tooltip("Tham chiếu đến owner (nhân vật sở hữu).")]
        [SerializeField] private GameObject owner;

        [Header("=== Auto Detection ===")]
        [Tooltip("Tự động dùng BoxCollider trên chính GameObject này để quét va chạm.")]
        [SerializeField] private bool useAttachedBoxCollider = true;

        // Cache components
        private BoxCollider m_BoxCollider;
        private AttackData m_CurrentAttack;
        private readonly HashSet<GameObject> m_AlreadyHitOwners = new HashSet<GameObject>();

        public bool IsActive { get; private set; } = false;

        private void Awake()
        {
            if (owner == null) owner = transform.root.gameObject;
            if (useAttachedBoxCollider) m_BoxCollider = GetComponent<BoxCollider>();
        }

        public void Activate(AttackData attackData)
        {
            IsActive = true;
            m_CurrentAttack = attackData;
            m_AlreadyHitOwners.Clear();

            // Nếu dùng BoxCollider thật → tạm thời disable nó để tránh Physics Engine tự xử lý va chạm linh tinh
            if (m_BoxCollider != null) m_BoxCollider.enabled = false;
        }

        public void Deactivate()
        {
            IsActive = false;
            m_CurrentAttack = null;
            m_AlreadyHitOwners.Clear();
        }

        public List<HitResult> PerformScan()
        {
            var results = new List<HitResult>();
            if (!IsActive || m_CurrentAttack == null) return results;

            Collider[] hits;

            // === CODE-DRIVEN COLLISION: BOX vs SPHERE ===
            if (m_BoxCollider != null && useAttachedBoxCollider)
            {
                // Quét theo hình hộp của BoxCollider
                Vector3 center = transform.TransformPoint(m_BoxCollider.center);
                Vector3 halfExtents = Vector3.Scale(m_BoxCollider.size, transform.lossyScale) * 0.5f;
                Quaternion orientation = transform.rotation;

                hits = Physics.OverlapBox(center, halfExtents, orientation, hurtboxLayer);
            }
            else
            {
                // Quét theo hình cầu mặc định
                Vector3 scanCenter = transform.position + transform.TransformDirection(m_CurrentAttack.hitboxOffset);
                hits = Physics.OverlapSphere(scanCenter, m_CurrentAttack.hitboxRadius, hurtboxLayer);
            }

            foreach (var col in hits)
            {
                var hurtbox = col.GetComponent<Hurtbox>();
                if (hurtbox == null || !hurtbox.IsActive) continue;
                if (hurtbox.Owner == owner) continue;
                if (m_AlreadyHitOwners.Contains(hurtbox.Owner)) continue;

                m_AlreadyHitOwners.Add(hurtbox.Owner);
                results.Add(new HitResult
                {
                    hurtbox = hurtbox,
                    hitPoint = col.ClosestPoint(transform.position),
                    hitNormal = (hurtbox.transform.position - transform.position).normalized,
                    damage = m_CurrentAttack.damage * hurtbox.DamageMultiplier,
                    knockbackForce = m_CurrentAttack.knockbackForce,
                    attackData = m_CurrentAttack
                });
            }

            return results;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (m_BoxCollider == null && useAttachedBoxCollider) m_BoxCollider = GetComponent<BoxCollider>();

            if (m_BoxCollider != null && useAttachedBoxCollider)
            {
                // Vẽ Gizmos hình hộp khớp với Collider
                Gizmos.color = IsActive ? new Color(1, 0, 0, 0.5f) : new Color(1, 1, 0, 0.1f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(m_BoxCollider.center, m_BoxCollider.size);
            }
            else if (m_CurrentAttack != null)
            {
                // Vẽ hình cầu mặc định
                Gizmos.color = IsActive ? new Color(1, 0, 0, 0.5f) : new Color(1, 1, 0, 0.1f);
                Vector3 center = transform.position + transform.TransformDirection(m_CurrentAttack.hitboxOffset);
                Gizmos.DrawSphere(center, m_CurrentAttack.hitboxRadius);
            }
        }
#endif
    }

    public struct HitResult
    {
        public Hurtbox hurtbox;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public float damage;
        public float knockbackForce;
        public AttackData attackData;
    }
}

