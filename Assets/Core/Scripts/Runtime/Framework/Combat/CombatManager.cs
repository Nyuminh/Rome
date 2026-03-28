using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core.Combat
{
    /// <summary>
    /// Trạng thái của đòn đánh, tương ứng 3 giai đoạn trong game đối kháng chuyên nghiệp.
    /// </summary>
    public enum AttackPhase
    {
        /// <summary>Không đang tấn công.</summary>
        None,
        /// <summary>Giai đoạn lấy đà - nhân vật không thể hủy đòn.</summary>
        Startup,
        /// <summary>Giai đoạn gây sát thương - Hitbox bật, quét mỗi frame.</summary>
        Active,
        /// <summary>Giai đoạn thu hồi - Hitbox tắt, nhân vật đang recovery.</summary>
        Recovery
    }

    /// <summary>
    /// CombatManager - Bộ điều khiển chiến đấu chính của nhân vật.
    /// 
    /// Triển khai theo chuẩn game đối kháng chuyên nghiệp (Tekken, Street Fighter):
    /// - Frame Data Logic: Đếm frame trong Update() ở tốc độ cố định 60fps
    /// - Code-Driven Collision: Dùng Physics.OverlapSphere thay vì Physics Engine
    /// - Animation Event-free: Hitbox bật/tắt hoàn toàn bằng frame counter, không phụ thuộc animation
    /// - Hit Stop: Làm khựng game khi trúng đòn để tăng gamefeel
    /// 
    /// Gắn lên Player prefab, cùng cấp với CorePlayerManager.
    /// </summary>
    public class CombatManager : NetworkBehaviour
    {
        #region Fields & Properties

        [Header("=== Attack Data (Combo Chain) ===")]
        [Tooltip("Danh sách đòn đánh theo thứ tự combo (đòn 1, đòn 2, đòn 3...).")]
        [SerializeField] private List<AttackData> comboChain = new List<AttackData>();

        [Header("=== Combo Settings ===")]
        [Tooltip("Thời gian tối đa giữa 2 lần bấm tấn công để tính combo (giây).")]
        [SerializeField] private float comboWindow = 0.8f;

        [Tooltip("Cho phép buffer input trong giai đoạn Recovery (nhấn trước khi đòn cũ kết thúc).")]
        [SerializeField] private bool allowInputBuffer = true;

        [Header("=== Hitbox References ===")]
        [Tooltip("Hitbox chính (thường gắn ở tay phải hoặc vũ khí).")]
        [SerializeField] private Hitbox primaryHitbox;

        [Tooltip("Hitbox phụ (tay trái, chân, v.v.). Có thể bỏ trống.")]
        [SerializeField] private Hitbox secondaryHitbox;

        [Header("=== Component References ===")]
        [Tooltip("Animator của nhân vật.")]
        [SerializeField] private Animator animator;

        [Tooltip("CoreStatsHandler để gây sát thương qua hệ thống stat.")]
        [SerializeField] private CoreStatsHandler targetStatsHandler;

        [Header("=== Hit Effect Settings ===")]
        [Tooltip("Prefab VFX khi trúng đòn (có thể bỏ trống).")]
        [SerializeField] private GameObject hitEffectPrefab;

        [Tooltip("SoundDef phát khi trúng đòn (có thể bỏ trống).")]
        [SerializeField] private SoundDef hitSoundDef;

        /// <summary>Giai đoạn hiện tại của đòn đánh.</summary>
        public AttackPhase CurrentPhase { get; private set; } = AttackPhase.None;

        /// <summary>Nhân vật đang tấn công hay không.</summary>
        public bool IsAttacking => CurrentPhase != AttackPhase.None;

        /// <summary>Frame hiện tại trong đòn đánh (0-indexed).</summary>
        public int CurrentFrame { get; private set; } = 0;

        /// <summary>Đòn đánh hiện tại đang thực hiện.</summary>
        public AttackData CurrentAttack { get; private set; }

        // === Frame Timing ===
        // Đếm frame ở 60fps cố định, KHÔNG phụ thuộc vào deltaTime
        private const float FRAME_DURATION = 1f / 60f;
        private float m_FrameAccumulator = 0f;

        // === Combo State ===
        private int m_CurrentComboIndex = 0;
        private float m_LastAttackTime = -999f;
        private bool m_InputBuffered = false;



        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            // CHỈ owner mới xử lý input và combat logic
            // Nếu chưa spawn (offline/singleplayer) → vẫn cho chạy
            if (IsSpawned && !IsOwner) return;

            if (!IsAttacking)
            {
                // Xử lý input bấm chuột trái → tấn công
                if (UnityEngine.InputSystem.Mouse.current != null &&
                    UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    RequestAttack();
                }
                return;
            }

            // === Frame Counter Logic (60fps cố định) ===
            m_FrameAccumulator += Time.deltaTime;
            while (m_FrameAccumulator >= FRAME_DURATION)
            {
                m_FrameAccumulator -= FRAME_DURATION;
                TickFrame();
            }

            // Input buffer: ghi nhận nếu nhấn tấn công trong lúc đang đánh
            if (allowInputBuffer && CurrentPhase == AttackPhase.Recovery)
            {
                if (UnityEngine.InputSystem.Mouse.current != null &&
                    UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    m_InputBuffered = true;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Yêu cầu thực hiện đòn tấn công.
        /// Nếu đang tấn công → buffer input.
        /// Nếu rảnh → bắt đầu đòn tiếp theo trong combo chain.
        /// </summary>
        public void RequestAttack()
        {
            if (IsAttacking)
            {
                // Buffer input cho combo tiếp theo
                if (allowInputBuffer) m_InputBuffered = true;
                return;
            }

            // Kiểm tra combo window
            if (Time.time - m_LastAttackTime > comboWindow)
            {
                m_CurrentComboIndex = 0; // Reset combo
            }

            if (comboChain == null || comboChain.Count == 0)
            {
                Debug.LogWarning("[CombatManager] Chưa gán AttackData vào comboChain!");
                return;
            }

            // Lấy đòn đánh tiếp theo trong combo
            AttackData attack = comboChain[m_CurrentComboIndex % comboChain.Count];
            if (attack == null)
            {
                Debug.LogWarning($"[CombatManager] AttackData tại index {m_CurrentComboIndex} là null!");
                return;
            }

            StartAttack(attack);
        }

        #endregion

        #region Private Methods - Attack Lifecycle

        /// <summary>
        /// Bắt đầu một đòn đánh mới.
        /// </summary>
        private void StartAttack(AttackData attack)
        {
            CurrentAttack = attack;
            CurrentFrame = 0;
            CurrentPhase = AttackPhase.Startup;
            m_FrameAccumulator = 0f;
            m_InputBuffered = false;
            m_LastAttackTime = Time.time;

            // Trigger animation
            if (animator != null)
            {
                animator.SetInteger(attack.comboParameter, attack.comboIndex);
                animator.SetTrigger(attack.animationTrigger);
            }

            Debug.Log($"[Combat] Đòn {attack.name} bắt đầu | Combo #{attack.comboIndex} " +
                      $"| Startup:{attack.startupFrames} Active:{attack.activeFrames} Recovery:{attack.recoveryFrames}");
        }

        /// <summary>
        /// Được gọi mỗi "frame logic" (1/60 giây).
        /// Quản lý chuyển giai đoạn và quét hitbox.
        /// </summary>
        private void TickFrame()
        {
            if (CurrentAttack == null) return;

            CurrentFrame++;

            // === Xác định Phase hiện tại dựa trên frame counter ===
            if (CurrentFrame <= CurrentAttack.startupFrames)
            {
                // Giai đoạn Startup
                if (CurrentPhase != AttackPhase.Startup)
                {
                    CurrentPhase = AttackPhase.Startup;
                }
            }
            else if (CurrentFrame <= CurrentAttack.startupFrames + CurrentAttack.activeFrames)
            {
                // Giai đoạn Active
                if (CurrentPhase != AttackPhase.Active)
                {
                    CurrentPhase = AttackPhase.Active;
                    ActivateHitboxes();
                }

                // === Quét va chạm mỗi frame trong Active ===
                ScanHitboxes();
            }
            else if (CurrentFrame <= CurrentAttack.TotalFrames)
            {
                // Giai đoạn Recovery
                if (CurrentPhase != AttackPhase.Recovery)
                {
                    CurrentPhase = AttackPhase.Recovery;
                    DeactivateHitboxes();
                }
            }
            else
            {
                // Đòn đánh kết thúc
                EndAttack();
            }
        }

        /// <summary>
        /// Bật Hitbox khi vào giai đoạn Active.
        /// </summary>
        private void ActivateHitboxes()
        {
            if (primaryHitbox != null) primaryHitbox.Activate(CurrentAttack);
            if (secondaryHitbox != null) secondaryHitbox.Activate(CurrentAttack);
        }

        /// <summary>
        /// Tắt Hitbox khi rời giai đoạn Active.
        /// </summary>
        private void DeactivateHitboxes()
        {
            if (primaryHitbox != null) primaryHitbox.Deactivate();
            if (secondaryHitbox != null) secondaryHitbox.Deactivate();
        }

        /// <summary>
        /// Quét Hitbox mỗi frame trong giai đoạn Active.
        /// Nếu trúng → xử lý hit (gây sát thương, hit stop, VFX, SFX).
        /// </summary>
        private void ScanHitboxes()
        {
            List<HitResult> allHits = new List<HitResult>();

            if (primaryHitbox != null && primaryHitbox.IsActive)
            {
                allHits.AddRange(primaryHitbox.PerformScan());
            }
            if (secondaryHitbox != null && secondaryHitbox.IsActive)
            {
                allHits.AddRange(secondaryHitbox.PerformScan());
            }

            foreach (var hit in allHits)
            {
                ProcessHit(hit);
            }
        }

        /// <summary>
        /// Xử lý 1 kết quả hit: gây sát thương, hit stop, VFX, SFX.
        /// Tích hợp với hệ thống IHittable/HitProcessor có sẵn trong project.
        /// </summary>
        private void ProcessHit(HitResult hit)
        {
            // 1. Gây sát thương qua hệ thống IHittable có sẵn
            var hittable = hit.targetOwner.GetComponentInChildren<IHittable>();
            if (hittable != null)
            {
                ulong attackerId = 0;
                if (IsSpawned)
                {
                    attackerId = OwnerClientId;
                }

                HitInfo hitInfo = new HitInfo
                {
                    amount = hit.damage,
                    hitPoint = hit.hitPoint,
                    hitNormal = hit.hitNormal,
                    attackerId = attackerId,
                    impactForce = hit.hitNormal * hit.knockbackForce
                };

                hittable.OnHit(hitInfo);
            }

            // 2. Hit Stop (Gamefeel)
            if (HitStopManager.Instance != null && hit.attackData != null)
            {
                // Cho multiplayer: dùng Animator-based hit stop thay vì Time.timeScale
                if (IsSpawned)
                {
                    Animator victimAnimator = hit.targetOwner.GetComponentInChildren<Animator>();
                    HitStopManager.Instance.TriggerAnimatorHitStop(
                        animator, victimAnimator, hit.attackData.hitStopDuration);
                }
                else
                {
                    HitStopManager.Instance.TriggerHitStop(hit.attackData);
                }
            }

            // 3. VFX
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.hitPoint, Quaternion.LookRotation(hit.hitNormal));
            }

            // 4. SFX
            if (hitSoundDef != null)
            {
                CoreDirector.RequestAudio(hitSoundDef)
                    .WithPosition(hit.hitPoint)
                    .Play();
            }

            string targetName = hit.targetOwner != null ? hit.targetOwner.name : "UnknownTarget";
            string zoneName = hit.hurtbox != null ? hit.hurtbox.Zone.ToString() : "DirectBody";

            Debug.Log($"[Combat] HIT! {targetName} tại vùng {zoneName} " +
                      $"| Damage: {hit.damage:F1} " +
                      $"| Knockback: {hit.knockbackForce:F1}");
        }

        /// <summary>
        /// Kết thúc đòn đánh. Kiểm tra input buffer để chain combo.
        /// </summary>
        private void EndAttack()
        {
            DeactivateHitboxes();
            CurrentPhase = AttackPhase.None;
            CurrentFrame = 0;

            // Tăng combo index
            m_CurrentComboIndex++;
            if (m_CurrentComboIndex >= comboChain.Count)
            {
                m_CurrentComboIndex = 0; // Quay vòng combo
            }

            // Xử lý input buffer → chain sang đòn tiếp theo
            if (m_InputBuffered && allowInputBuffer)
            {
                m_InputBuffered = false;
                RequestAttack();
            }
            else
            {
                CurrentAttack = null;
            }
        }

        private void CacheReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null) animator = GetComponentInChildren<Animator>();
            }
        }

        #endregion
    }
}
