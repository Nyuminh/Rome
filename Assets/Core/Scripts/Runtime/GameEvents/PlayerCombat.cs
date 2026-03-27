using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Blocks.Gameplay.Core;

/// <summary>
/// [LEGACY] Script combat cũ. Đã sửa thêm IsOwner check cho multiplayer.
/// Khuyến khích chuyển sang CombatManager mới cho hệ thống Frame Data chuyên nghiệp.
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    public float attackRange = 2.5f;
    public float damageAmount = 20f;
    public LayerMask enemyLayer;

    [Header("Combo Settings")]
    public int comboCount = 0;
    public float lastClickTime = 0f;
    public float comboDelay = 1f; // Thời gian tối đa giữa các cú nhấn để tính combo

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // ★ FIX: Chỉ chạy trên Owner — ngăn instance đối phương xử lý input
        if (IsSpawned && !IsOwner) return;

        // Kiểm tra Reset combo nếu để quá lâu không đánh
        if (Time.time - lastClickTime > comboDelay)
        {
            comboCount = 0;
            if (anim != null) anim.SetInteger("ComboCount", 0);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Attack();
        }
    }

    void Attack()
    {
        lastClickTime = Time.time;
        comboCount++;
        Debug.Log($"Combo {comboCount}");
        // Reset về đòn 1 nếu đã đánh hết 3 đòn
        if (comboCount > 3) comboCount = 1;

        // Gửi dữ liệu sang Animator
        if (anim != null)
        {
            anim.SetInteger("ComboCount", comboCount);
            anim.SetTrigger("IsAttack");
        }

        // Tăng sát thương cho đòn thứ 3 (đòn kết liễu)
        float currentDamage = (comboCount == 3) ? damageAmount * 2f : damageAmount;

        // Quét gây sát thương
        Vector3 scanPosition = transform.position + transform.forward + Vector3.up;
        Collider[] hitEnemies = Physics.OverlapSphere(scanPosition, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            // ★ FIX: Bỏ qua chính mình — OverlapSphere đang quét trúng collider của bản thân
            if (enemy.transform.root == transform.root) continue;
            if (enemy.transform.IsChildOf(transform)) continue;

            var networkObj = enemy.GetComponent<Unity.Netcode.NetworkObject>();
            if (networkObj == null) networkObj = enemy.GetComponentInParent<Unity.Netcode.NetworkObject>();

            // Chỉ đánh nếu đã spawned và không phải chính mình
            if (networkObj != null && networkObj.IsSpawned)
            {
                // ★ FIX: Double-check không phải chính mình qua NetworkObject
                if (networkObj == NetworkObject) continue;

                var hittable = networkObj.GetComponent<IHittable>();
                var lion = networkObj.GetComponent<LionHitReceiver>();

                if (hittable != null && (lion == null || !lion.isDead))
                {
                    ulong myId = IsSpawned ? OwnerClientId : 0;
                    HitInfo info = new HitInfo
                    {
                        amount = currentDamage,
                        attackerId = myId,
                        hitPoint = enemy.ClosestPoint(scanPosition),
                        hitNormal = (enemy.transform.position - transform.position).normalized,
                        impactForce = transform.forward * 5f
                    };
                    hittable.OnHit(info);
                    Debug.Log($"Combo {comboCount} trúng {networkObj.name}! Sát thương: {currentDamage}");
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward + Vector3.up, attackRange);
    }
}