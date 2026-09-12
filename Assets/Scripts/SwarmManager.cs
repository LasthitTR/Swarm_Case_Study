using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SwarmManager : MonoBehaviour
{
    [Header("Sürü Araçları")]
    public List<UGVController> groundVehicles;
    private bool isMissionActive = false;

    [Header("Kameralar (1:Serbest, 2:Drone, 3:Ambulans, 4:Polis)")]
    public GameObject freeCamera;
    public GameObject droneCamera;
    public GameObject ambulanceCamera;
    public GameObject policeCamera;

    [Header("Arayüz ve Metrikler")]
    public TextMeshProUGUI missionLogText;
    private float missionTimer = 0f;
    private bool isSearching = true;

    [Header("Oynanabilirlik (İnteraktivite)")]
    public Transform lostVehicle;

    void Update()
    {
        if (isSearching)
        {
            missionTimer += Time.deltaTime;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchCamera(1);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchCamera(2);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SwitchCamera(3);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SwitchCamera(4);
        }

        if (freeCamera != null && freeCamera.activeInHierarchy)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Camera activeCam = freeCamera.GetComponent<Camera>();
                if (activeCam != null)
                {
                    Vector2 mousePosition = Mouse.current.position.ReadValue();
                    Ray ray = activeCam.ScreenPointToRay(mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                    {
                        if (lostVehicle != null)
                        {
                            lostVehicle.position = hit.point + new Vector3(0f, 0.5f, 0f);

                            TriggerDynamicReplanning();

                            if (missionLogText != null)
                            {
                                missionLogText.text += "\n[SİMÜLASYON] Hedef kaçtı! Araçlar beklemeye alındı. İHA yeniden tarıyor...";
                            }
                        }
                    }
                }
            }
        }
    }

    private void SwitchCamera(int camIndex)
    {
        if (freeCamera != null) freeCamera.SetActive(camIndex == 1);
        if (droneCamera != null) droneCamera.SetActive(camIndex == 2);
        if (ambulanceCamera != null) ambulanceCamera.SetActive(camIndex == 3);
        if (policeCamera != null) policeCamera.SetActive(camIndex == 4);
    }

    public void OnTargetFound(Transform foundTarget)
    {
        if (isMissionActive) return;

        isMissionActive = true;
        isSearching = false;

        Debug.Log("Swarm Merkezi: Kayıp araç tespit edildi! Koordinatlar işleniyor...");

        if (missionLogText != null)
        {
            missionLogText.text += $"\n[HEDEF TESPİT] Kayıp obje bulundu!\n";
            missionLogText.text += $"> Tespit Süresi: {missionTimer:F2} saniye\n";
        }

        HighlightTarget(foundTarget);
        StartCoroutine(EngageRescueSequence(foundTarget));
    }

    private IEnumerator EngageRescueSequence(Transform target)
    {
        if (missionLogText != null) missionLogText.text += "\n[MÜZAYEDE] Contract Net Protocol devrede. Araçlar teklif veriyor...\n";

        // 1. AŞAMA: Sarı ihale çizgileri çizilir
        if (groundVehicles != null)
        {
            foreach (var ugv in groundVehicles)
            {
                if (ugv != null) ugv.DrawBidLine(target.position);
            }
        }

        // 2. AŞAMA: Jürinin müzayedeyi izlemesi için 3 saniye bekle
        yield return new WaitForSeconds(3f);

        UGVController winningUGV = null;
        float lowestCost = Mathf.Infinity;
        string auctionLog = "[Auction Details]\n";

        // 3. AŞAMA: Kazanan belirlenir ve araçlar yola çıkar
        if (groundVehicles != null)
        {
            foreach (var ugv in groundVehicles)
            {
                if (ugv != null)
                {
                    float cost = ugv.CalculatePathCost(target.position);
                    auctionLog += $"- {ugv.gameObject.name} teklifi: {cost:F2}\n";

                    if (cost < lowestCost)
                    {
                        lowestCost = cost;
                        winningUGV = ugv;
                    }
                }
            }
        }

        if (winningUGV != null && lowestCost != Mathf.Infinity)
        {
            auctionLog += $">>> BİRİNCİL MÜDAHALE: {winningUGV.gameObject.name}\n";
            winningUGV.targetNode = target;

            foreach (var ugv in groundVehicles)
            {
                if (ugv != null)
                {
                    ugv.SetWinnerLine(ugv == winningUGV);

                    if (ugv != winningUGV)
                    {
                        auctionLog += $">>> ÇEVRE GÜVENLİĞİ: {ugv.gameObject.name}\n";

                        Vector3 rawSupportPosition = target.position + new Vector3(12f, 0f, 12f);
                        Vector3 safePosition = rawSupportPosition;

                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(rawSupportPosition, out hit, 20f, NavMesh.AllAreas))
                        {
                            safePosition = hit.position;
                        }

                        GameObject perimeterTarget = new GameObject(ugv.gameObject.name + "_PerimeterTarget");
                        perimeterTarget.transform.position = safePosition;
                        ugv.targetNode = perimeterTarget.transform;
                    }
                }
            }

            Debug.Log(auctionLog);
            if (missionLogText != null)
            {
                missionLogText.text += $"\n[GÖREV ATAMASI BAŞARILI]\n";
                missionLogText.text += $"> Birincil Müdahale: {winningUGV.gameObject.name}\n";
                missionLogText.text += $"> Çevre Güvenliği: Destek Ekibi";
            }
        }
        else
        {
            if (missionLogText != null) missionLogText.text += "\n[UYARI] Hedefe ulaşılamıyor. Operasyon iptal!";
        }
    }

    private void HighlightTarget(Transform target)
    {
        GameObject highlight = new GameObject("TargetHighlightLight");
        highlight.transform.position = target.position + Vector3.up * 5f;
        highlight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Light spotLight = highlight.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = Color.red;
        spotLight.intensity = 15f;
        spotLight.range = 20f;
        spotLight.spotAngle = 45f;
    }

    private void TriggerDynamicReplanning()
    {
        StopAllCoroutines();
        isMissionActive = false;
        isSearching = true;

        GameObject oldLight = GameObject.Find("TargetHighlightLight");
        if (oldLight != null) Destroy(oldLight);

        if (groundVehicles != null)
        {
            foreach (var ugv in groundVehicles)
            {
                if (ugv != null)
                {
                    ugv.targetNode = null;
                    ugv.SetWinnerLine(false);
                    if (ugv.agent != null && ugv.agent.isOnNavMesh)
                    {
                        ugv.agent.ResetPath();
                    }
                }
            }
        }

        // SARI UYARI ÇÖZÜMÜ 1: Unity 6'ya uygun yeni obje arama kodu
        GameObject[] perimeters = UnityEngine.Object.FindObjectsByType<GameObject>(UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None);
        foreach (GameObject obj in perimeters)
        {
            if (obj.name.Contains("_PerimeterTarget")) Destroy(obj);
        }

        // SARI UYARI ÇÖZÜMÜ 2: Unity 6'ya uygun "FindAnyObjectByType" kullanımı
        UAVController drone = UnityEngine.Object.FindAnyObjectByType<UAVController>();
        if (drone != null)
        {
            drone.ResumeSearch();
        }
    }
}