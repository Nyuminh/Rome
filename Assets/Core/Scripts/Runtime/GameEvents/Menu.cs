using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using Blocks.Gameplay.Core; // Để dùng PauseMenuController

public class MainMenu : MonoBehaviour
{
  
    public Button continueButton; // Kéo nút "Tiếp Tục" vào đây trong Inspector
    // --- CÁC HÀM CŨ CỦA BẠN ---

    void Start()
    {
       
    }
    public void BatDauGame()
    {
        SceneManager.LoadScene("[BB] Core");
        Debug.Log("Đang tải trò chơi...");
    }

    public void ThoatGame()
    {
        Application.Quit();
        Debug.Log("Đã thoát game!");
    }
   
}