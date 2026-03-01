using System.Collections.Generic;
using UnityEngine;

public class ShieldOrbitVFX : MonoBehaviour
{
    [Header("Parent")]
    [Tooltip("Boþ býrakýrsan bu objenin transformu kullanýlýr. Player hareket eden root'u buraya ver.")]
    public Transform orbitParent;

    [Header("Visual Prefab")]
    public GameObject shieldVisualPrefab;

    [Header("Orbit Settings")]
    [Range(1, 16)] public int shieldCount = 4;
    public float radius = 1.6f;
    public float heightOffset = 1.0f;
    public float orbitSpeedDeg = 180f;
    public float selfSpinDeg = 240f;
    public bool faceOutward = true;

    private readonly List<Transform> shields = new List<Transform>(16);
    private bool lastActive = false;
    private float angleDeg = 0f;

    private Transform Parent => orbitParent != null ? orbitParent : transform;

    private void Awake()
    {
        EnsureSpawned();
        SetShieldsActive(false);
    }

    private void Update()
    {
        bool active = (GlobalShield.Instance != null && GlobalShield.Instance.IsActive);

        if (active != lastActive)
        {
            EnsureSpawned();
            SetShieldsActive(active);
            lastActive = active;
        }

        if (!active) return;

        angleDeg += orbitSpeedDeg * Time.deltaTime;
        float step = 360f / Mathf.Max(1, shieldCount);

        for (int i = 0; i < shields.Count; i++)
        {
            Transform t = shields[i];
            if (t == null) continue;

            float a = (angleDeg + i * step) * Mathf.Deg2Rad;

            Vector3 localPos = new Vector3(Mathf.Cos(a) * radius, heightOffset, Mathf.Sin(a) * radius);
            t.localPosition = localPos;

            if (faceOutward)
            {
                Vector3 outward = new Vector3(localPos.x, 0f, localPos.z);
                if (outward.sqrMagnitude > 0.0001f)
                    t.localRotation = Quaternion.LookRotation(outward.normalized, Vector3.up);
            }

            if (selfSpinDeg != 0f)
                t.Rotate(0f, selfSpinDeg * Time.deltaTime, 0f, Space.Self);
        }
    }

    private void EnsureSpawned()
    {
        if (shieldVisualPrefab == null) return;

        // Parent deðiþtiyse mevcutlarý yeni parent'a taþý
        for (int i = 0; i < shields.Count; i++)
        {
            if (shields[i] != null && shields[i].parent != Parent)
                shields[i].SetParent(Parent, false);
        }

        while (shields.Count < shieldCount)
        {
            GameObject go = Instantiate(shieldVisualPrefab, Parent);
            go.name = $"{shieldVisualPrefab.name}_Orbit_{shields.Count}";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < cols.Length; c++)
                if (cols[c] != null) cols[c].enabled = false;

            shields.Add(go.transform);
        }

        for (int i = 0; i < shields.Count; i++)
        {
            if (shields[i] != null)
                shields[i].gameObject.SetActive(i < shieldCount);
        }
    }

    private void SetShieldsActive(bool active)
    {
        for (int i = 0; i < shields.Count; i++)
        {
            if (shields[i] != null)
                shields[i].gameObject.SetActive(active && i < shieldCount);
        }
    }
}