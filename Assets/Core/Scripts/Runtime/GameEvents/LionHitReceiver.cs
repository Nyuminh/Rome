using UnityEngine;
using Blocks.Gameplay.Core;

public class LionHitReceiver : HitProcessor
{
    public float health = 100f;
    public bool isDead = false; // Thêm biến Boolean để kiểm tra
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    protected override void HandleHit(HitInfo info)
    {
        // Nếu đã chết thì không nhận thêm sát thương hay chạy Anim nữa
        if (isDead) return;

        health -= info.amount;
        Debug.Log($"Sư tử bị chém! Máu còn: {health}");

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

        Debug.Log("<color=red>Sư tử đã gục ngã!</color>");
    }
}