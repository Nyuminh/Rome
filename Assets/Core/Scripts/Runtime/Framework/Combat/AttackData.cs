using UnityEngine;

namespace Blocks.Gameplay.Core.Combat
{
    /// <summary>
    /// ScriptableObject lưu trữ Frame Data của một đòn đánh.
    /// Chia làm 3 giai đoạn: Startup → Active → Recovery.
    /// Hitbox chỉ được bật trong giai đoạn Active.
    /// Ví dụ: Đấm nhẹ: Startup=4, Active=3, Recovery=10 (tổng 17 frames ở 60fps).
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttackData", menuName = "Combat/Attack Data")]
    public class AttackData : ScriptableObject
    {
        [Header("=== Frame Data (tính bằng frame ở 60fps) ===")]
        [Tooltip("Số frame lấy đà trước khi đòn đánh có hiệu lực.")]
        [Min(0)] public int startupFrames = 4;

        [Tooltip("Số frame đòn đánh gây sát thương (Hitbox bật).")]
        [Min(1)] public int activeFrames = 3;

        [Tooltip("Số frame thu hồi sau đòn đánh.")]
        [Min(0)] public int recoveryFrames = 10;

        [Header("=== Damage & Force ===")]
        [Tooltip("Sát thương cơ bản của đòn đánh.")]
        public float damage = 20f;

        [Tooltip("Lực đẩy khi đòn đánh trúng (knockback).")]
        public float knockbackForce = 5f;

        [Header("=== Hitbox Config ===")]
        [Tooltip("Bán kính quét của OverlapSphere cho đòn đánh này.")]
        public float hitboxRadius = 0.4f;

        [Tooltip("Offset vị trí quét so với transform gốc (local space).")]
        public Vector3 hitboxOffset = Vector3.zero;

        [Header("=== Hit Stop (Gamefeel) ===")]
        [Tooltip("Thời gian đóng băng khi đánh trúng (giây). 0 = không dừng.")]
        [Range(0f, 0.5f)] public float hitStopDuration = 0.08f;

        [Tooltip("TimeScale áp dụng trong lúc Hit Stop (0 = đóng băng hoàn toàn).")]
        [Range(0f, 1f)] public float hitStopTimeScale = 0f;

        [Header("=== Camera Shake ===")]
        [Tooltip("Cường độ rung camera khi trúng.")]
        [Range(0f, 2f)] public float cameraShakeIntensity = 0.3f;

        [Tooltip("Thời gian rung camera (giây).")]
        [Range(0f, 1f)] public float cameraShakeDuration = 0.15f;

        [Header("=== Animation ===")]
        [Tooltip("Tên Trigger trong Animator để chạy animation đòn đánh.")]
        public string animationTrigger = "IsAttack";

        [Tooltip("Tên Integer parameter cho combo count.")]
        public string comboParameter = "ComboCount";

        [Tooltip("Chỉ số combo (1, 2, 3...) cho đòn đánh này.")]
        public int comboIndex = 1;

        /// <summary>
        /// Tổng số frame của đòn đánh.
        /// </summary>
        public int TotalFrames => startupFrames + activeFrames + recoveryFrames;

        /// <summary>
        /// Frame bắt đầu giai đoạn Active (0-indexed).
        /// </summary>
        public int ActiveStartFrame => startupFrames;

        /// <summary>
        /// Frame kết thúc giai đoạn Active (exclusive, 0-indexed).
        /// </summary>
        public int ActiveEndFrame => startupFrames + activeFrames;
    }
}
