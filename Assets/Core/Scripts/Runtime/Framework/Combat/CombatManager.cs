using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core.Combat
{
    public enum AttackPhase
    {
        None,
        Startup,
        Active,
        Recovery
    }

    public class CombatManager : NetworkBehaviour
    {
        #region Fields & Properties
        [Header("=== Attack Data (Combo Chain) ===")]
        [SerializeField] private List<AttackData> comboChain = new List<AttackData>();

        [Header("=== Combo Settings ===")]
        [SerializeField] private float comboWindow = 0.8f;
        [SerializeField] private bool allowInputBuffer = true;

        [Header("=== Hitbox References ===")]
        [SerializeField] private Hitbox primaryHitbox;
        [SerializeField] private Hitbox secondaryHitbox;

        [Header("=== Component References ===")]
        [SerializeField] private Animator animator;
        [SerializeField] private CoreStatsHandler targetStatsHandler;

        [Header("=== Hit Effect Settings ===")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private SoundDef hitSoundDef;

        public AttackPhase CurrentPhase { get; private set; } = AttackPhase.None;
        public bool IsAttacking => CurrentPhase != AttackPhase.None;
        public int CurrentFrame { get; private set; } = 0;
        public AttackData CurrentAttack { get; private set; }

        private const float FRAME_DURATION = 1f / 60f;
        private float m_FrameAccumulator = 0f;
        private int m_CurrentComboIndex = 0;
        private float m_LastAttackTime = -999f;
        private bool m_InputBuffered = false;
        #endregion

        private void Awake() => CacheReferences();

        private void Update()
        {
            if (IsSpawned && !IsOwner) return;

            // Direct Mapping Input Logic
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && !IsAttacking)
            {
                bool leftPressed = mouse.leftButton.wasPressedThisFrame;
                bool rightPressed = mouse.rightButton.wasPressedThisFrame;

                if ((leftPressed && mouse.rightButton.isPressed) || (rightPressed && mouse.leftButton.isPressed))
                    RequestAttack(2); 
                else if (leftPressed)
                    RequestAttack(0); 
                else if (rightPressed)
                    RequestAttack(1); 
            }

            if (!IsAttacking) return;

            // KHÔNG đếm frame nếu đang bị khựng (HitStop) để tránh lệch với Animator
            if (HitStopManager.Instance != null && HitStopManager.Instance.IsInHitStop) return;

            m_FrameAccumulator += Time.deltaTime;
            while (m_FrameAccumulator >= FRAME_DURATION)
            {
                m_FrameAccumulator -= FRAME_DURATION;
                TickFrame();
            }
        }

        public void RequestAttack(int attackIndex = 0)
        {
            if (IsAttacking) return;

            if (comboChain == null || comboChain.Count <= attackIndex)
            {
                Debug.LogWarning($"[CombatManager] Khong tim thay AttackData tai index {attackIndex}!");
                return;
            }

            AttackData attack = comboChain[attackIndex];
            if (attack != null)
            {
                m_CurrentComboIndex = attackIndex;
                StartAttack(attack);
            }
        }

        private void StartAttack(AttackData attack)
        {
            CurrentAttack = attack;
            CurrentFrame = 0;
            CurrentPhase = AttackPhase.Startup;
            m_FrameAccumulator = 0f;
            m_InputBuffered = false;
            m_LastAttackTime = Time.time;

            if (animator != null)
            {
                // TỰ ĐỘNG GỬI SỐ (1=Trái, 2=Phải, 3=Cả hai)
                animator.SetInteger("ComboCount", m_CurrentComboIndex + 1);
                animator.SetTrigger("IsAttack"); 
            }
            Debug.Log($"[Combat] Don {attack.name} bat dau | ComboCount: {m_CurrentComboIndex + 1}");
        }

        private void TickFrame()
        {
            if (CurrentAttack == null) return;
            CurrentFrame++;

            if (CurrentFrame <= CurrentAttack.startupFrames)
            {
                if (CurrentPhase != AttackPhase.Startup) CurrentPhase = AttackPhase.Startup;
            }
            else if (CurrentFrame <= CurrentAttack.startupFrames + CurrentAttack.activeFrames)
            {
                if (CurrentPhase != AttackPhase.Active)
                {
                    CurrentPhase = AttackPhase.Active;
                    ActivateHitboxes();
                }
                ScanHitboxes();
            }
            else if (CurrentFrame <= CurrentAttack.TotalFrames)
            {
                if (CurrentPhase != AttackPhase.Recovery)
                {
                    CurrentPhase = AttackPhase.Recovery;
                    DeactivateHitboxes();
                }
            }
            else
            {
                EndAttack();
            }
        }

        private void ActivateHitboxes()
        {
            if (primaryHitbox != null) primaryHitbox.Activate(CurrentAttack);
            if (secondaryHitbox != null) secondaryHitbox.Activate(CurrentAttack);
        }

        private void DeactivateHitboxes()
        {
            if (primaryHitbox != null) primaryHitbox.Deactivate();
            if (secondaryHitbox != null) secondaryHitbox.Deactivate();
        }

        private void ScanHitboxes()
        {
            if (primaryHitbox != null && primaryHitbox.IsActive) ProcessHits(primaryHitbox.PerformScan());
            if (secondaryHitbox != null && secondaryHitbox.IsActive) ProcessHits(secondaryHitbox.PerformScan());
        }

        private void ProcessHits(List<HitResult> hits)
        {
            foreach (var hit in hits) ProcessHit(hit);
        }

        private void ProcessHit(HitResult hit)
        {
            var hittable = hit.hurtbox.Owner.GetComponent<IHittable>();
            if (hittable != null)
            {
                ulong attackerId = IsSpawned ? OwnerClientId : 0;
                hittable.OnHit(new HitInfo {
                    amount = hit.damage,
                    hitPoint = hit.hitPoint,
                    hitNormal = hit.hitNormal,
                    attackerId = attackerId,
                    impactForce = hit.hitNormal * hit.knockbackForce
                });
            }

            if (HitStopManager.Instance != null && hit.attackData != null)
            {
                if (IsSpawned)
                {
                    Animator victimAnim = hit.hurtbox.Owner.GetComponentInChildren<Animator>();
                    HitStopManager.Instance.TriggerAnimatorHitStop(animator, victimAnim, hit.attackData.hitStopDuration);
                }
                else HitStopManager.Instance.TriggerHitStop(hit.attackData);
            }

            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hit.hitPoint, Quaternion.LookRotation(hit.hitNormal));
            if (hitSoundDef != null) CoreDirector.RequestAudio(hitSoundDef).WithPosition(hit.hitPoint).Play();
        }

        private void EndAttack()
        {
            DeactivateHitboxes();
            CurrentPhase = AttackPhase.None;
            CurrentFrame = 0;
            CurrentAttack = null;
            if (animator != null) animator.SetInteger("ComboCount", 0);
        }

        private void CacheReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null) animator = GetComponentInChildren<Animator>();
            }
        }
    }
}
