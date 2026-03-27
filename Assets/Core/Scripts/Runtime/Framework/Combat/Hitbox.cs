using UnityEngine;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core.Combat
{
    /// <summary>
    /// Smart Hitbox - Tự động bám theo Box Collider của vũ khí.
    /// Giải quyết vấn đề "đánh hụt" bằng cách quét đúng hình dáng thực tế của Polearm.
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [Header("=== Hitbox Config ===")]
        [SerializeField] private LayerMask hurtboxLayer;
        [SerializeField] private GameObject owner;

        [Header("=== Box Settings ===")]
        [Tooltip("Kéo BoxCollider của lưỡi thương vào đây.")]
        [SerializeField] private BoxCollider weaponCollider;

        private AttackData m_CurrentAttack;
        private readonly HashSet<GameObject> m_AlreadyHitOwners = new HashSet<GameObject>();
        public bool IsActive { get; private set; } = false;

        private void Awake()
        {
            if (owner == null) owner = transform.root.gameObject;
            // Nếu chưa gán thì tự tìm BoxCollider trên cùng object
            if (weaponCollider == null) weaponCollider = GetComponent<BoxCollider>();
            
            // Đảm bảo Collider gốc không chặn vật lý làm nhân vật bị bay
            if (weaponCollider != null) weaponCollider.isTrigger = true;
        }

        public void Activate(AttackData attackData)
        {
            IsActive = true;
            m_CurrentAttack = attackData;
            m_AlreadyHitOwners.Clear();
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
            if (!IsActive || weaponCollider == null) return results;

            // === Lấy thông số hình hộp trong không gian thế giới (World Space) ===
            // 1. Tâm của Box
            Vector3 worldCenter = weaponCollider.transform.TransformPoint(weaponCollider.center);
            
            // 2. Kích thước (Half Extents) - tính cả Scale của nhân vật
            Vector3 worldHalfExtents = Vector3.Scale(weaponCollider.size, weaponCollider.transform.lossyScale) * 0.5f;
            
            // 3. Hướng xoay của vũ khí
            Quaternion worldRotation = weaponCollider.transform.rotation;

            // === Quét hình hộp (OverlapBox) ===
            Collider[] hits = Physics.OverlapBox(worldCenter, worldHalfExtents, worldRotation, hurtboxLayer);

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
                    hitPoint = col.ClosestPoint(worldCenter),
                    hitNormal = (hurtbox.transform.position - worldCenter).normalized,
                    damage = (m_CurrentAttack != null ? m_CurrentAttack.damage : 10) * hurtbox.DamageMultiplier,
                    knockbackForce = m_CurrentAttack != null ? m_CurrentAttack.knockbackForce : 5,
                    attackData = m_CurrentAttack
                });
            }

            return results;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (weaponCollider == null) weaponCollider = GetComponent<BoxCollider>();
            if (weaponCollider == null) return;

            // Vẽ khối hộp trùng khít với Box Collider để bạn dễ căn chỉnh
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = weaponCollider.transform.localToWorldMatrix;
            
            Gizmos.color = IsActive ? new Color(1, 0, 0, 0.5f) : new Color(1, 1, 0, 0.2f);
            Gizmos.DrawCube(weaponCollider.center, weaponCollider.size);
            
            Gizmos.color = IsActive ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(weaponCollider.center, weaponCollider.size);
            
            Gizmos.matrix = oldMatrix;
        }
#endif
    }

    /// <summary>
    /// Kết quả trả về từ 1 lần quét Hitbox.
    /// </summary>
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
