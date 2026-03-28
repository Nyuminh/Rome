using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Xử lý sát thương cho Player trong multiplayer.
    /// 
    /// VẤN ĐỀ GỐC: 
    ///   HitProcessor gửi RPC tới Authority (Host). Nhưng CoreStatsHandler.ModifyStat() 
    ///   yêu cầu IsOwner (vì NetworkList có WritePermission = Owner).
    ///   → Host KHÔNG phải Owner của Player khách → ModifyStat bị chặn → không mất máu.
    ///
    /// GIẢI PHÁP:
    ///   Authority (Host) nhận hit → gửi ClientRpc tới Owner → Owner tự trừ máu của mình.
    ///   Flow: Attacker → [RPC to Authority] → HandleHit → [RPC to Owner] → ModifyStat ✓
    /// </summary>
    public class PlayerHitReceiver : HitProcessor
    {
        private CoreStatsHandler statsHandler;
        private int healthStatHash;

        void Awake()
        {
            statsHandler = GetComponent<CoreStatsHandler>();
            healthStatHash = Animator.StringToHash("Health");
        }

        /// <summary>
        /// Chạy trên Authority (Host/Server). 
        /// Thay vì gọi ModifyStat trực tiếp (sẽ fail vì !IsOwner),
        /// ta forward sang Owner qua RPC.
        /// </summary>
        protected override void HandleHit(HitInfo info)
        {
            if (statsHandler == null) return;

            // Nếu Authority cũng là Owner (Host bị đánh) → xử lý trực tiếp
            if (IsOwner)
            {
                ApplyDamage(info);
            }
            else
            {
                // Authority KHÔNG phải Owner → gửi RPC tới Owner để tự trừ máu
                ApplyDamageClientRpc(info);
            }
        }

        /// <summary>
        /// RPC gửi từ Authority tới tất cả Client.
        /// Chỉ Owner mới thực sự xử lý (trừ máu).
        /// </summary>
        [Rpc(SendTo.Owner, RequireOwnership = false)]
        private void ApplyDamageClientRpc(HitInfo info)
        {
            ApplyDamage(info);
        }

        /// <summary>
        /// Thực sự trừ máu. Chỉ chạy trên Owner.
        /// </summary>
        private void ApplyDamage(HitInfo info)
        {
            if (statsHandler == null || !IsOwner) return;

            statsHandler.ModifyStat(
                healthStatHash,
                -info.amount,
                info.attackerId,
                ModificationSource.Direct
            );

            Debug.Log($"[Hit] Player {OwnerClientId} mất {info.amount} máu. Attacker: {info.attackerId}");
        }
    }
}