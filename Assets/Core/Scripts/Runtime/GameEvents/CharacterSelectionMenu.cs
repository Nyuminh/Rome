using UnityEngine;
using TMPro; // S? d?ng n?u b?n dùng TextMeshPro (Khuyên dùng)
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// T?o m?t class ð? lýu tr? thông tin c?a t?ng nhân v?t
[System.Serializable]
public class CharacterStats
{
    public string characterName; // Ví d?: "SECUTOR"
    public string strength;      // Ví d?: "Strength IV"
    public string speed;         // Ví d?: "Speed III"
    public string agility;       // Ví d?: "Agility III"

    // N?u b?n có h?nh ?nh nhân v?t thay ð?i ? gi?a màn h?nh, b? comment d?ng dý?i:
    // public Sprite characterSprite; 
}

public class CharacterSelectionMenu : MonoBehaviour
{
    [Header("Danh sách nhân v?t")]
    public CharacterStats[] characters;
    private int currentIndex = 0;

    [Header("UI References")]
    public TextMeshProUGUI nameText;    // Text hi?n th? tên (SECUTOR)
    public TextMeshProUGUI statsText;   // Text hi?n th? ch? s? (Strength, Speed...)

    // public Image characterDisplay; // Kéo Image c?a nhân v?t vào ðây n?u có

    void Start()
    {
        // Hi?n th? nhân v?t ð?u tiên khi v?a m? menu
        if (characters.Length > 0)
        {
            UpdateCharacterUI();
        }
    }

    // Hàm g?n vào nút "TI?P" (Next)
    public void NextCharacter()
    {
        currentIndex++;
        // N?u vý?t quá s? lý?ng nhân v?t, quay l?i nhân v?t ð?u tiên
        if (currentIndex >= characters.Length)
        {
            currentIndex = 0;
        }
        UpdateCharacterUI();
    }

    // Hàm g?n vào nút "TRÝ?C" (Previous)
    public void PreviousCharacter()
    {
        currentIndex--;
        // N?u lùi quá nhân v?t ð?u tiên, ði t?i nhân v?t cu?i cùng
        if (currentIndex < 0)
        {
            currentIndex = characters.Length - 1;
        }
        UpdateCharacterUI();
    }

    // C?p nh?t text trên màn h?nh
    private void UpdateCharacterUI()
    {
        CharacterStats currentChar = characters[currentIndex];

        nameText.text = currentChar.characterName;

        // Dùng \n ð? xu?ng d?ng cho các ch? s?
        statsText.text = $"{currentChar.strength}\n{currentChar.speed}\n{currentChar.agility}";

        // characterDisplay.sprite = currentChar.characterSprite; // C?p nh?t h?nh n?u có
    }

    // Hàm g?n vào nút "PLAY CHÕI"
    public void PlayGame()
    {
        // Lýu l?i index c?a nhân v?t ð? ch?n ð? load vào Scene game
        PlayerPrefs.SetInt("SelectedCharacterIndex", currentIndex);
        PlayerPrefs.Save();

        Debug.Log("Ðang load game v?i nhân v?t: " + characters[currentIndex].characterName);

        // Thay "GameScene" b?ng tên Scene chính xác c?a b?n
        // SceneManager.LoadScene("GameScene"); 
    }
}