using System.Collections.Generic;
using UnityEngine;
using static MalbersAnimations.UI.StatMonitorUI;

public class StatueInteractable : MonoBehaviour
{
    [Header("Interact")]
    public string playerTag = "Animal";
    public KeyCode interactKey = KeyCode.E;

    private bool inRange = false;
    private bool used = false;

    private void Update()
    {
        if (used) return;
        if (!inRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            TryOpenStatue();
        }
    }

    private void TryOpenStatue()
    {
        if (used) return;

        if (ChestRewardManager.Instance == null || StatueUI.Instance == null)
        {
            Debug.LogWarning("[StatueInteractable] ChestRewardManager veya StatueUI yok!");
            return;
        }

        used = true;

        // 3 farklý item type olacak þekilde roll
        List<ChestReward> options = Roll3DistinctRewards();

        // UI aç: seçileni uygula + heykeli yok et
        StatueUI.Instance.ShowChoices(options, (chosenReward) =>
        {
            if (ChestRewardManager.Instance != null)
            {
                ChestRewardManager.Instance.ApplyReward(chosenReward);
            }

            Destroy(gameObject);
        });
    }

    private List<ChestReward> Roll3DistinctRewards()
    {
        List<ChestReward> list = new List<ChestReward>(3);
        HashSet<ChestItemType> usedTypes = new HashSet<ChestItemType>();

        int safety = 0;
        while (list.Count < 3 && safety < 200)
        {
            safety++;

            ChestReward r = ChestRewardManager.Instance.RollReward();
            if (usedTypes.Contains(r.type)) continue;

            usedTypes.Add(r.type);
            list.Add(r);
        }

        // Normalde havuz yeterli olduðu için buraya düþmez; yine de garanti
        while (list.Count < 3)
        {
            list.Add(ChestRewardManager.Instance.RollReward());
        }

        return list;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            inRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            inRange = false;
    }
}
