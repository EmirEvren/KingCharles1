using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MalbersAnimations.Controller;   // MAnimal için

public class WeaponChoiceManager : MonoBehaviour
{
    [Header("Silah Opsiyonları (4 tane doldur)")]
    public WeaponOption[] weapons;
    // 0: BoneAutoShooter
    // 1: SteakAutoShooter
    // 2: FireballAutoShooter
    // 3: TennisBallAutoShooter

    [Header("UI Referansları")]
    public GameObject choicePanel;   // Kartların olduğu ana panel
    public Button cardButton1;
    public Button cardButton2;
    public TMP_Text card1Title;
    public TMP_Text card2Title;

    private int optionIndex1 = -1;
    private int optionIndex2 = -1;
    private bool selectionDone = false;

    // Eklenenler:
    private EnemySpawner[] enemySpawners;  // Sahnedeki tüm spawnerlar
    private MAnimal playerAnimal;          // Oyuncunun Animal Controller'ı

    private void Start()
    {
        // Güvenlik
        if (weapons == null || weapons.Length < 2)
        {
            Debug.LogError("[WeaponChoiceManager] En az 2 silah tanımlaman gerekiyor!");
            return;
        }

        // --- PLAYER ANIMAL'INI BUL VE KAPAT ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Animal");
        if (playerObj != null)
        {
            playerAnimal = playerObj.GetComponent<MAnimal>();
            if (playerAnimal != null)
            {
                // Kart seçilene kadar MAnimal çalışmasın
                playerAnimal.enabled = false;
            }
        }

        // --- TÜM ENEMYSPAWNER'LARI BUL ---
#if UNITY_2023_1_OR_NEWER
        enemySpawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
#else
        enemySpawners = FindObjectsOfType<EnemySpawner>();
#endif
        // Hepsini kilitle (kart seçene kadar spawn yok)
        if (enemySpawners != null)
        {
            foreach (var sp in enemySpawners)
            {
                if (sp != null) sp.canSpawn = false;
            }
        }

        // Bütün silahları başta kapat
        DisableAllWeapons();

        // Oyunu durdur
        Time.timeScale = 0f;

        // Paneli aç
        if (choicePanel != null)
            choicePanel.SetActive(true);

        // 2 farklı random silah seç
        PickTwoRandomWeapons();
        SetupCards();
    }

    private void PickTwoRandomWeapons()
    {
        int count = weapons.Length;

        optionIndex1 = UnityEngine.Random.Range(0, count);

        // ikinci index, birinciden farklı olsun
        do
        {
            optionIndex2 = UnityEngine.Random.Range(0, count);
        }
        while (optionIndex2 == optionIndex1);
    }

    private void SetupCards()
    {
        if (cardButton1 != null)
        {
            cardButton1.onClick.RemoveAllListeners();
            cardButton1.onClick.AddListener(() => OnWeaponSelected(optionIndex1));
        }

        if (cardButton2 != null)
        {
            cardButton2.onClick.RemoveAllListeners();
            cardButton2.onClick.AddListener(() => OnWeaponSelected(optionIndex2));
        }

        // Butonların kendi Image component'lerini alıyoruz
        Image card1Image = cardButton1 != null ? cardButton1.GetComponent<Image>() : null;
        Image card2Image = cardButton2 != null ? cardButton2.GetComponent<Image>() : null;

        // Kart 1 UI
        if (optionIndex1 >= 0 && optionIndex1 < weapons.Length)
        {
            var w1 = weapons[optionIndex1];
            if (card1Title != null) card1Title.text = w1.weaponName;

            // Icon'u direkt butonun sprite'ı yap
            if (card1Image != null && w1.icon != null)
            {
                card1Image.sprite = w1.icon;
                card1Image.preserveAspect = true;
            }
        }

        // Kart 2 UI
        if (optionIndex2 >= 0 && optionIndex2 < weapons.Length)
        {
            var w2 = weapons[optionIndex2];
            if (card2Title != null) card2Title.text = w2.weaponName;

            if (card2Image != null && w2.icon != null)
            {
                card2Image.sprite = w2.icon;
                card2Image.preserveAspect = true;
            }
        }
    }

    private void OnWeaponSelected(int selectedIndex)
    {
        if (selectionDone) return;
        selectionDone = true;

        // Hepsini kapat, sadece seçileni aç
        DisableAllWeapons();
        EnableWeapon(selectedIndex);

        // --- SPAWNER'LARI AÇ ---
        if (enemySpawners != null)
        {
            foreach (var sp in enemySpawners)
            {
                if (sp != null) sp.canSpawn = true;
            }
        }

        // --- MAnimal'ı AÇ (kart seçilince karakter çalışsın) ---
        if (playerAnimal != null)
        {
            playerAnimal.enabled = true;
        }

        // Paneli kapat
        if (choicePanel != null)
            choicePanel.SetActive(false);

        // Oyunu devam ettir
        Time.timeScale = 1f;
    }

    private void DisableAllWeapons()
    {
        if (weapons == null) return;

        foreach (var w in weapons)
        {
            if (w != null && w.shooterScript != null)
            {
                w.shooterScript.enabled = false;
            }
        }
    }

    private void EnableWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length) return;

        var w = weapons[index];
        if (w != null && w.shooterScript != null)
        {
            w.shooterScript.enabled = true;
        }
    }
}

[Serializable]
public class WeaponOption
{
    public string weaponName;           // Kartta gözükecek isim (örn: "Bone Cannon")
    public MonoBehaviour shooterScript; // Player üstündeki AutoShooter script'i
    public Sprite icon;                 // Kart görseli → direkt buton sprite'ı olacak
}
