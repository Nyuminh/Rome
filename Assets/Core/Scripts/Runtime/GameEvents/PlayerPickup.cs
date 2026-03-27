using UnityEngine;
using UnityEngine.InputSystem; // B?t bu?c ph?i thêm thý vi?n này

public class PlayerPickup : MonoBehaviour
{
    [Header("Cài ð?t tay c?m")]
    public Transform handSocket;

    private GameObject currentEquippedWeapon;
    private GameObject weaponInRange;

    void Update()
    {
        // Ki?m tra xem bàn phím có ðang ðý?c k?t n?i không
        if (Keyboard.current == null) return;

        // S? d?ng cú pháp c?a New Input System ð? check phím F
        if (Keyboard.current.fKey.wasPressedThisFrame && weaponInRange != null)
        {
            EquipWeapon(weaponInRange);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Weapon"))
        {
            weaponInRange = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Weapon") && other.gameObject == weaponInRange)
        {
            weaponInRange = null;
        }
    }

    private void EquipWeapon(GameObject groundWeapon)
    {
        if (currentEquippedWeapon != null)
        {
            Destroy(currentEquippedWeapon);
        }

        currentEquippedWeapon = Instantiate(groundWeapon, handSocket.position, handSocket.rotation);
        currentEquippedWeapon.transform.SetParent(handSocket);

        Rigidbody rb = currentEquippedWeapon.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        Collider col = currentEquippedWeapon.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(groundWeapon);
        weaponInRange = null;
    }
}