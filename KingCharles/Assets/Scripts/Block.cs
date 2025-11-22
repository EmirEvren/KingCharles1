using UnityEngine;
using System.Collections.Generic;

public class Block : MonoBehaviour
{
    public bool IsSlope { get; private set; }

    // MapGenerator spawn ederken setler
    public void Init(bool isSlope)
    {
        IsSlope = isSlope;
    }

    public void Elevate(
        int currentElevation,
        GameObject normalBlockPrefab,
        Transform parentTransform,
        Dictionary<Vector3Int, GameObject> allSpawnedBlocks,
        Vector3 blockScale)
    {
        float blockUnitHeight = blockScale.y;

        // 1) Yükseðe kaldýr
        Vector3 newPosition = new Vector3(
            transform.position.x,
            currentElevation * blockUnitHeight,
            transform.position.z
        );
        transform.position = newPosition;

        // 2) Altýný doldur
        for (int i = 0; i < currentElevation; i++)
        {
            Vector3 fillPosition = new Vector3(
                transform.position.x,
                i * blockUnitHeight,
                transform.position.z
            );

            Vector3Int fillPosInt = new Vector3Int(
                Mathf.RoundToInt(fillPosition.x / blockScale.x),
                i,
                Mathf.RoundToInt(fillPosition.z / blockScale.z)
            );

            if (allSpawnedBlocks.ContainsKey(fillPosInt))
                continue;

            GameObject fillBlock = Instantiate(normalBlockPrefab, fillPosition, Quaternion.identity, parentTransform);
            fillBlock.transform.localScale = blockScale;

            fillBlock.name = $"{normalBlockPrefab.name} (Fill) ({fillPosInt.x}, {fillPosInt.y}, {fillPosInt.z})";

            // fill blok da slope deðildir
            if (fillBlock.TryGetComponent(out Block fb))
                fb.Init(false);

            allSpawnedBlocks[fillPosInt] = fillBlock;
        }
    }
}
