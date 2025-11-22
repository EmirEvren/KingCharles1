using UnityEngine;

public class PlantVisibilityUnit : MonoBehaviour
{
    [Header("Plant Settings")]
    [SerializeField] private float visibleDistance = 20f;
    [SerializeField] private bool startHidden = true;

    private MeshRenderer[] renderers;
    private float sqVisibleDistance;
    private bool currentVisible;
    private bool registered;

    private void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>(true);
        sqVisibleDistance = visibleDistance * visibleDistance;

        if (startHidden)
            SetRenderers(false);
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void Start()
    {
        // ✅ Instance geç geldiyse burada yakalıyoruz
        TryRegister();
    }

    private void OnDisable()
    {
        if (registered && PlantVisibilityManager.Instance != null)
            PlantVisibilityManager.Instance.Unregister(this);

        registered = false;
    }

    private void TryRegister()
    {
        if (registered) return;

        var mgr = PlantVisibilityManager.Instance;
        if (mgr != null)
        {
            mgr.Register(this);
            registered = true;
        }
    }

    public void UpdateVisibility(Transform[] animals)
    {
        Vector3 plantPos = transform.position;
        bool anyNear = false;

        for (int i = 0; i < animals.Length; i++)
        {
            var a = animals[i];
            if (a == null) continue;

            float sqDist = (a.position - plantPos).sqrMagnitude;
            if (sqDist <= sqVisibleDistance)
            {
                anyNear = true;
                break;
            }
        }

        if (anyNear != currentVisible)
            SetRenderers(anyNear);
    }

    private void SetRenderers(bool visible)
    {
        currentVisible = visible;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].enabled = visible;
    }

    private void OnValidate()
    {
        sqVisibleDistance = visibleDistance * visibleDistance;
    }
}
