using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartSimulation()
    {
        // 1 numaralı sahneyi (Asıl oyun sahnemizi) yükler
        SceneManager.LoadScene(1);
    }

    public void QuitSimulation()
    {
        Debug.Log("Simülasyon Kapatılıyor...");
        Application.Quit();
    }
}