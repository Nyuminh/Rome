using UnityEngine;

public class WeaponSpawner : MonoBehaviour
{
    [Header("Cài ð?t Spawn")]
    public GameObject[] weaponPrefabs; // Danh sách các prefab v? khí (giáo, ki?m, khiên...)
    public Transform[] spawnPoints;    // Các v? trí spawn trên b?n ð?
    public float spawnInterval = 5f;   // Th?i gian ð?m ngý?c ð? spawn (giây)

    private float timer;
    private GameObject currentWeapon;  // Bi?n lýu tr? v? khí ðang n?m trên sân

    void Start()
    {
        timer = 0f;
    }

    void Update()
    {
        // 1. Ki?m tra xem v? khí c? c?n trên sân không
        if (currentWeapon != null)
        {
            // N?u v?n c?n, gi? timer ? m?c 0 và không làm g? c?
            timer = 0f;
            return;
        }

        // 2. N?u không có v? khí trên sân (currentWeapon == null), b?t ð?u ð?m gi?
        timer += Time.deltaTime;

        // 3. Khi ð? th?i gian th? g?i hàm t?o v? khí m?i
        if (timer >= spawnInterval)
        {
            SpawnWeapon();
            timer = 0f; // Reset th?i gian sau khi spawn
        }
    }

    void SpawnWeapon()
    {
        // Ki?m tra an toàn
        if (weaponPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Chýa gán Weapon Prefabs ho?c Spawn Points!");
            return;
        }

        // Random ch?n 1 v? khí và 1 v? trí
        int randomWeaponIndex = Random.Range(0, weaponPrefabs.Length);
        int randomPointIndex = Random.Range(0, spawnPoints.Length);

        GameObject weaponToSpawn = weaponPrefabs[randomWeaponIndex];
        Transform spawnPoint = spawnPoints[randomPointIndex];

        // T?o v? khí và GÁN VÀO BI?N currentWeapon ð? theo d?i
        currentWeapon = Instantiate(weaponToSpawn, spawnPoint.position, spawnPoint.rotation);
    }
}