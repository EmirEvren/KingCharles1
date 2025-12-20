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
        ResetHold();

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

        // UI göster; butona basýnca sandýðý yok et
        ChestUI.Instance.ShowReward(reward, () =>
        {
            Destroy(gameObject);
        });
    }

    private void ResetHold()
    {
        holdTimer = 0f;
        if (holdSlider != null) holdSlider.value = 0f;
        if (holdRoot != null) holdRoot.SetActive(false);
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
