using UnityEngine;
using Blocks.Gameplay.Core; // Để sử dụng HitInfo và IHittable

public class LionDamageDealer : MonoBehaviour
{
    public float damageAmount = 10f;
    public float pushForceMagnitude = 5f;

    public void DealDamage(GameObject target)
    {

        var hittable = target.GetComponent<IHittable>();

        if (hittable != null)
        {
            Debug.Log("Sư tử đang vồ!");
            // Cấu trúc chuẩn theo file bạn gửi
            HitInfo info = new HitInfo
            {
                amount = damageAmount, // Trong template của bạn dùng 'amount' thay vì 'damage'
                hitPoint = target.transform.position,
                hitNormal = (target.transform.position - transform.position).normalized,
                attackerId = 0, // ID của sư tử (0 là mặc định)
                impactForce = (target.transform.position - transform.position).normalized * pushForceMagnitude // Dùng Vector3
            };

            hittable.OnHit(info);
        }
    }
}