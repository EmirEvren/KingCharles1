using UnityEngine;
using System.Collections.Generic; // Dictionary için eklendi

public class Block : MonoBehaviour
{
    // const float BLOCK_UNIT_HEIGHT = 1f; // Bu artýk blockScale.y'den gelecek

    /// <summary>
    /// Bloðu belirlenen yüksekliðe kaldýrýr ve altýndaki boþluklarý doldurmak için 
    /// normal bloklar spawn eder.
    /// </summary>
    /// <param name="currentElevation">Bloðun ulaþacaðý ýzgara yüksekliði.</param>
    /// <param name="normalBlockPrefab">Altý doldurmak için kullanýlacak Normal Blok Prefab'i.</param>
    /// <param name="parentTransform">Spawn edilen bloklarýn ebeveyn nesnesi.</param>
    /// <param name="allSpawnedBlocks">Tüm spawn edilen bloklarý takip etmek için sözlük.</param>
    /// <param name="blockScale">Bloklarýn ölçeði (MapGenerator'dan gelir).</param>
    public void Elevate(int currentElevation, GameObject normalBlockPrefab, Transform parentTransform, Dictionary<Vector3Int, GameObject> allSpawnedBlocks, Vector3 blockScale)
    {
        // Yüksekliði ölçeðe göre ayarla
        float blockUnitHeight = blockScale.y;

        // 1. Bloðun dikey pozisyonunu ayarla
        Vector3 newPosition = new Vector3(transform.position.x, currentElevation * blockUnitHeight, transform.position.z);
        transform.position = newPosition;

        // 2. Altýndaki boþluklarý doldurmak için bloklarý spawn et
        for (int i = 0; i < currentElevation; i++)
        {
            // Pozisyonu ölçeðe göre ayarla
            Vector3 fillPosition = new Vector3(transform.position.x, i * blockUnitHeight, transform.position.z);

            // Sözlük anahtarý (grid koordinatý) için pozisyonu geri hesapla
            Vector3Int fillPosInt = new Vector3Int(
                Mathf.RoundToInt(fillPosition.x / blockScale.x),
                i,
                Mathf.RoundToInt(fillPosition.z / blockScale.z)
            );

            // Eðer o pozisyonda (muhtemelen baþka bir bloðun dolgusu) zaten bir blok varsa, tekrar oluþturma
            if (allSpawnedBlocks.ContainsKey(fillPosInt))
            {
                continue;
            }

            // Doldurma bloðunu spawn et
            GameObject fillBlock = Instantiate(normalBlockPrefab, fillPosition, Quaternion.identity, parentTransform);

            // Ölçeði uygula
            fillBlock.transform.localScale = blockScale;

            fillBlock.name = $"{normalBlockPrefab.name} (Fill) ({fillPosInt.x}, {fillPosInt.y}, {fillPosInt.z})";

            // Doldurma bloklarýný da sözlüðe ekle
            allSpawnedBlocks[fillPosInt] = fillBlock;
        }
    }
}