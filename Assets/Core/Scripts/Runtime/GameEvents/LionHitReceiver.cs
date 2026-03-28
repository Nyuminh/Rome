using UnityEngine;
using Blocks.Gameplay.Core;

public class LionHitReceiver : MonoBehaviour, IHittable
{
    public float health = 100f;
    private float maxHealth;
    public bool isDead = false;
    private Animator anim;

    // Sử dụng C# Action tự động két nối với HUD, không cần kéo thả
    public static event System.Action<float, float> OnHealthChanged;
    public static event System.Action<bool> OnGameResult;

    void Awake()
    {
        anim = GetComponent<Animator>();
        maxHealth = health;
    }

    // IHittable interface - gọi trực tiếp không qua Network
    public void OnHit(HitInfo info)
    {
        HandleHit(info);
    }

    public void SubmitHitRpc(HitInfo info, Unity.Netcode.RpcParams rpcParams = default)
    {
        // Đối với non-network object, chỉ cần gọi HandleHit trực tiếp
        HandleHit(info);
    }

    private void HandleHit(HitInfo info)
    {
        // Nếu đã chết thì không nhận thêm sát thương hay chạy Anim nữa
        if (isDead) return;

        health -= info.amount;
        Debug.Log($"Sư tử bị chém! Máu còn: {health}");

        // Spawn hiệu ứng máu tóe ra
        BloodSplatterEffect.Spawn(info.hitPoint, info.hitNormal);

        // Phát sự kiện cập nhật máu enemy tự động
        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Chỉ chạy Anim trúng đòn nếu chưa chết
            if (anim != null) anim.SetTrigger("IsAttacked");
        }
    }

    void Die()
    {
        isDead = true; // Đánh dấu đã chết

        if (anim != null)
        {
            anim.SetBool("Die", true); // Dùng Bool trong Animator để chạy Anim chết
        }

        // Tắt các thành phần vật lý và AI
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.isStopped = true;

        OnGameResult?.Invoke(true); // Gửi event Player Win tự động

        Debug.Log("<color=red>Sư tử đã gục ngã!</color>");
    }
}