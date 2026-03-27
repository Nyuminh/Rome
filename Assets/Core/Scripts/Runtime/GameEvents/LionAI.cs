using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Blocks.Gameplay.Core;
using System.Collections.Generic;

public class LionBehavior : NetworkBehaviour
{
    private Transform targetPlayer;
    public float alertRange = 10f;
    public float attackRange = 2f;

    [Header("Combat Settings")]
    public float damageAmount = 15f;
    public float attackRate = 1.5f;
    private float nextAttackTime = 0f;

    private NavMeshAgent agent;
    private Animator anim;
    private LionHitReceiver hitReceiver;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        hitReceiver = GetComponent<LionHitReceiver>();
    }

    void Update()
    {
        // CHỈ SERVER mới điều khiển AI
        if (!IsServer) return;

        // Nếu sư tử đã chết, dừng mọi logic
        if (hitReceiver != null && hitReceiver.isDead)
        {
            if (agent.enabled) agent.isStopped = true;
            return;
        }

        // Tìm người chơi gần nhất mỗi 0.5 giây (tối ưu hiệu năng)
        if (Time.frameCount % 30 == 0 || targetPlayer == null)
        {
            FindNearestPlayer();
        }

        if (targetPlayer == null)
        {
            IdleState();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= attackRange)
        {
            AttackState();
        }
        else if (distanceToPlayer <= alertRange)
        {
            FollowState();
        }
        else
        {
            IdleState();
        }
    }

    void FindNearestPlayer()
    {
        float minDistance = float.MaxValue;
        Transform closest = null;

        // Quét tất cả object có tag Player (hoặc dùng NetworkManager.Singleton.ConnectedClients)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = p.transform;
            }
        }

        targetPlayer = closest;
    }

    void AttackState()
    {
        if (hitReceiver != null && hitReceiver.isDead) return;

        agent.isStopped = true;
        anim.SetBool("isWalking", false);
        anim.SetBool("isAttacking", true);
        LookAtPlayer();

        if (Time.time >= nextAttackTime)
        {
            DealDamage();
            nextAttackTime = Time.time + attackRate;
        }
    }

    void DealDamage()
    {
        if (targetPlayer == null) return;

        var hittable = targetPlayer.GetComponent<IHittable>();
        if (hittable != null)
        {
            // Debug.Log("<color=red>Sư tử đã vồ trúng Player!</color>");
            HitInfo info = new HitInfo
            {
                amount = damageAmount,
                hitPoint = targetPlayer.position,
                hitNormal = Vector3.up,
                attackerId = 999, // ID cho AI
                impactForce = transform.forward * 5f
            };
            hittable.OnHit(info);
        }
    }

    void FollowState()
    {
        if (targetPlayer == null) return;
        agent.isStopped = false;
        agent.SetDestination(targetPlayer.position);
        anim.SetBool("isWalking", true);
        anim.SetBool("isAttacking", false);
    }

    void IdleState()
    {
        agent.isStopped = true;
        anim.SetBool("isWalking", false);
        anim.SetBool("isAttacking", false);
    }

    void LookAtPlayer()
    {
        if (targetPlayer == null) return;
        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alertRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}