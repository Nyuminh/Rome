using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("Settings")]
    public string campaignSceneName = "CampaignScene";
    public string multiplayerSceneName = "MultiplayerScene";

    public void PlayGame()
    {
        Debug.Log("Bắt đầu Game...");
        // Chuyển đến màn hình chọn nhân vật hoặc vào game trực tiếp
    }

    public void OpenOptions()
    {
        Debug.Log("Mở cài đặt");
    }

    public void OpenCredits()
    {
        Debug.Log("Mở thông tin tác giả");
    }

    public void SelectCharacter()
    {
        Debug.Log("Mở menu chọn nhân vật");
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game");
        Application.Quit();
    }
}