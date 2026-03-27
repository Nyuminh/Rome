using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    [Header("Cài ð?t tay c?m")]
    public Transform handSocket; // Kéo WeaponSocket ? Bý?c 1 vào ðây

    private GameObject currentEquippedWeapon; // Lýu v? khí ðang c?m trên tay

    // Hàm này t? ð?ng g?i khi nhân v?t ch?m vào m?t Trigger Collider
    private void OnTriggerEnter(Collider other)
    {
        // Ki?m tra xem v?t ch?m vào có ph?i là v? khí không
        if (other.CompareTag("Weapon"))
        {
            EquipWeapon(other.gameObject);
        }
    }

    private void EquipWeapon(GameObject groundWeapon)
    {
        // 1. N?u ðang c?m v? khí c?, h?y nó ði (ho?c b?n có th? vi?t code v?t nó xu?ng ð?t)
        if (currentEquippedWeapon != null)
        {
            Destroy(currentEquippedWeapon);
        }

        // 2. T?o m?t b?n sao c?a v? khí g?n tr?c ti?p vào v? trí c?a Hand Socket
        currentEquippedWeapon = Instantiate(groundWeapon, handSocket.position, handSocket.rotation);

        // 3. Ð?t Hand Socket làm cha (Parent) ð? v? khí di chuy?n theo tay
        currentEquippedWeapon.transform.SetParent(handSocket);

        // 4. Vô hi?u hóa v?t l? trên v? khí ðang c?m ð? nó không r?t xu?ng hay c?n ðý?ng
        Rigidbody rb = currentEquippedWeapon.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb); // Ho?c rb.isKinematic = true;
        }

        Collider col = currentEquippedWeapon.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 5. Quan tr?ng: Tiêu h?y v? khí dý?i ð?t
        // L?nh này s? làm bi?n currentWeapon trong WeaponSpawner bi?n thành null, kích ho?t spawn v? khí m?i!
        Destroy(groundWeapon);
    }
}