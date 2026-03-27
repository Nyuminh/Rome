using UnityEngine;
using System.Collections;

namespace Blocks.Gameplay.Core.Combat
{
    /// <summary>
    /// Quản lý hiệu ứng Hit Stop (đóng băng / khựng) khi đánh trúng.
    /// Đây là "bí quyết gamefeel" của các game đối kháng chuyên nghiệp.
    /// 
    /// Khi Hitbox chạm Hurtbox → dừng Animator + làm chậm Time.timeScale trong khoảng 0.05-0.2 giây
    /// → Tạo cảm giác "đánh có lực" thay vì đấm xuyên qua người.
    /// 
    /// Singleton pattern - chỉ cần 1 instance trong scene.
    /// </summary>
    public class HitStopManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static HitStopManager Instance { get; private set; }

        [Header("=== Global Settings ===")]
        [Tooltip("Thời gian Hit Stop mặc định nếu AttackData không chỉ định.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float defaultHitStopDuration = 0.1f;

        [Tooltip("TimeScale mặc định trong lúc Hit Stop.")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultHitStopTimeScale = 0f;

        private bool m_IsInHitStop = false;
        private Coroutine m_HitStopCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Kích hoạt Hit Stop với tham số từ AttackData.
        /// </summary>
        /// <param name="duration">Thời gian đóng băng (giây).</param>
        /// <param name="timeScale">TimeScale áp dụng (0 = đóng băng hoàn toàn).</param>
        public void TriggerHitStop(float duration = -1f, float timeScale = -1f)
        {
            float finalDuration = duration >= 0f ? duration : defaultHitStopDuration;
            float finalTimeScale = timeScale >= 0f ? timeScale : defaultHitStopTimeScale;

            if (finalDuration <= 0f) return;

            // Nếu đang trong Hit Stop → hủy cái cũ, bắt đầu cái mới
            if (m_HitStopCoroutine != null)
            {
                StopCoroutine(m_HitStopCoroutine);
                RestoreTimeScale();
            }

            m_HitStopCoroutine = StartCoroutine(HitStopRoutine(finalDuration, finalTimeScale));
        }

        /// <summary>
        /// Kích hoạt Hit Stop từ AttackData.
        /// </summary>
        public void TriggerHitStop(AttackData attackData)
        {
            if (attackData == null) return;
            TriggerHitStop(attackData.hitStopDuration, attackData.hitStopTimeScale);
        }

        /// <summary>
        /// Kích hoạt Animator-based Hit Stop (không dùng Time.timeScale).
        /// Phương pháp này tốt hơn cho multiplayer vì Time.timeScale ảnh hưởng toàn cục.
        /// Dừng Animator của cả attacker và victim trong khoảng thời gian nhất định.
        /// </summary>
        /// <param name="attackerAnimator">Animator của người đánh.</param>
        /// <param name="victimAnimator">Animator của người bị đánh.</param>
        /// <param name="duration">Thời gian dừng (giây).</param>
        public void TriggerAnimatorHitStop(Animator attackerAnimator, Animator victimAnimator, float duration)
        {
            if (duration <= 0f) return;
            StartCoroutine(AnimatorHitStopRoutine(attackerAnimator, victimAnimator, duration));
        }

        /// <summary>
        /// Coroutine xử lý Hit Stop bằng Time.timeScale.
        /// Dành cho singleplayer hoặc khi chỉ có host chơi.
        /// </summary>
        private IEnumerator HitStopRoutine(float duration, float timeScale)
        {
            m_IsInHitStop = true;

            // Đóng băng
            Time.timeScale = timeScale;

            // Chờ theo real-time (không bị ảnh hưởng bởi timeScale)
            yield return new WaitForSecondsRealtime(duration);

            // Khôi phục
            RestoreTimeScale();
            m_IsInHitStop = false;
            m_HitStopCoroutine = null;
        }

        /// <summary>
        /// Coroutine xử lý Hit Stop bằng cách dừng Animator (multiplayer-safe).
        /// Không ảnh hưởng đến Time.timeScale toàn cục.
        /// </summary>
        private IEnumerator AnimatorHitStopRoutine(Animator attacker, Animator victim, float duration)
        {
            m_IsInHitStop = true;
            
            // Dừng Animator
            float originalAttackerSpeed = 1f;
            float originalVictimSpeed = 1f;

            if (attacker != null)
            {
                originalAttackerSpeed = attacker.speed;
                attacker.speed = 0f;
            }
            if (victim != null)
            {
                originalVictimSpeed = victim.speed;
                victim.speed = 0f;
            }

            yield return new WaitForSecondsRealtime(duration);

            // Khôi phục Animator
            if (attacker != null) attacker.speed = originalAttackerSpeed;
            if (victim != null) victim.speed = originalVictimSpeed;
            
            m_IsInHitStop = false;
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }

        /// <summary>Trả về true nếu đang trong trạng thái Hit Stop.</summary>
        public bool IsInHitStop => m_IsInHitStop;
    }
}
