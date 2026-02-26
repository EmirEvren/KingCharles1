using UnityEngine;
using UnityEngine.UI;

public class ChestInteractable : MonoBehaviour
{
    [Header("Interact")]
    public string playerTag = "Animal";
    public KeyCode interactKey = KeyCode.E;

    [Header("Hold Settings")]
    public float holdTime = 2f;

    [Header("UI (Hold Slider)")]
    public GameObject holdRoot;
    public Slider holdSlider;

    [Header("Yok Edilecek Text (Opsiyonel)")]
    public GameObject textToDestroy; // Inspector'dan yok edilmesini istediğin text'i buraya sürükle

    private bool inRange = false;
    private float holdTimer = 0f;
    private bool opened = false;

    private void Start()
    {
        if (holdRoot != null) holdRoot.SetActive(false);
        if (holdSlider != null) holdSlider.value = 0f;
    }

    private void Update()
    {
        if (opened) return;
        if (!inRange) { ResetHold(); return; }

        if (Input.GetKey(interactKey))
        {
            // E tuşuna basılı tutulduğu an text'i gizle
            if (textToDestroy != null && textToDestroy.activeSelf)
            {
                textToDestroy.SetActive(false);
            }

            holdTimer += Time.deltaTime;

            if (holdRoot != null && !holdRoot.activeSelf)
                holdRoot.SetActive(true);

            if (holdSlider != null)
                holdSlider.value = Mathf.Clamp01(holdTimer / holdTime);

            if (holdTimer >= holdTime)
            {
                TryOpenChest();
            }
        }
        else
        {
            ResetHold();
        }
    }

    private void TryOpenChest()
    {
        // ResetHold çağırırsak text bir anlığına geri gelebilir (yanıp sönme yapar), 
        // bu yüzden UI'ı manuel sıfırlıyoruz.
        holdTimer = 0f;
        if (holdSlider != null) holdSlider.value = 0f;
        if (holdRoot != null) holdRoot.SetActive(false);

        if (opened) return;

        if (ChestPricingManager.Instance == null)
        {
            Debug.LogWarning("[ChestInteractable] ChestPricingManager yok!");
            return;
        }

        int price = ChestPricingManager.Instance.GetCurrentPrice();

        // Gold yetiyor mu?
        if (GoldCounterUI.Instance == null)
        {
            Debug.LogWarning("[ChestInteractable] GoldCounterUI.Instance yok!");
            return;
        }

        bool paid = GoldCounterUI.Instance.TrySpend(price);
        if (!paid)
        {
            Debug.Log("[ChestInteractable] Yeterli gold yok.");
            // Parası yetmediği için işlemi iptal ettik, text'i geri gösterelim
            if (textToDestroy != null) textToDestroy.SetActive(true);
            return;
        }

        ChestPricingManager.Instance.RegisterOpened(price);

        // Roll + UI
        if (ChestRewardManager.Instance == null || ChestUI.Instance == null)
        {
            Debug.LogWarning("[ChestInteractable] ChestRewardManager veya ChestUI yok!");
            return;
        }

        opened = true;

        ChestReward reward = ChestRewardManager.Instance.RollReward();

        // Ödülü uygula
        ChestRewardManager.Instance.ApplyReward(reward);

        // UI göster; butona basınca sandığı ve text'i yok et
        ChestUI.Instance.ShowReward(reward, () =>
        {
            // Eğer inspector'dan bir text atandıysa onu da yok et
            if (textToDestroy != null)
            {
                Destroy(textToDestroy);
            }
            
            Destroy(gameObject);
        });
    }

    private void ResetHold()
    {
        holdTimer = 0f;
        if (holdSlider != null) holdSlider.value = 0f;
        if (holdRoot != null) holdRoot.SetActive(false);

        // Eğer oyuncu E'yi bırakırsa veya alandan çıkarsa text'i tekrar görünür yap
        if (textToDestroy != null && !opened && !textToDestroy.activeSelf)
        {
            textToDestroy.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            inRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            inRange = false;
            ResetHold();
        }
    }
}