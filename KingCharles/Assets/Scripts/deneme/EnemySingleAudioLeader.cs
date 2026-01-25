using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySingleAudioLeader : MonoBehaviour
{
    [Header("Group")]
    [Tooltip("Ayný düþman tipindeki tüm prefablar ayný key'i kullanmalý. Örn: Zombie, Skeleton, Bat")]
    [SerializeField] private string groupKey = "EnemyTypeA";

    [Header("Target Audio")]
    [Tooltip("Boþ býrakýrsan bu GameObject veya child'lardan ilk AudioSource bulunur.")]
    [SerializeField] private AudioSource targetAudio;

    [Header("Behavior")]
    [SerializeField] private bool autoPlayWhenLeader = true;
    [SerializeField] private bool disableAudioSourceWhenNotLeader = true;
    [SerializeField] private bool forceStopWhenNotLeader = true;

    // Grup baþýna 1 lider
    private static readonly Dictionary<string, EnemySingleAudioLeader> leadersByGroup
        = new Dictionary<string, EnemySingleAudioLeader>(16);

    private void Awake()
    {
        if (targetAudio == null)
            targetAudio = GetComponentInChildren<AudioSource>(true);

        if (string.IsNullOrWhiteSpace(groupKey))
            groupKey = "EnemyTypeA";
    }

    private void OnEnable()
    {
        TryBecomeLeaderOrFollow();
    }

    private void Start()
    {
        // Bazý sistemler Start'ta Play() yapabiliyor; garanti olsun:
        ApplyState();
    }

    private void Update()
    {
        // Lider yoksa bu obje lider olabilir
        if (!HasValidLeader())
        {
            TryBecomeLeaderOrFollow();
            return;
        }

        // Lider biz deðilsek sessiz kalalým (yanlýþlýkla açýldýysa da düzeltir)
        if (leadersByGroup[groupKey] != this)
            ApplyNonLeaderState();
    }

    private void OnDisable()
    {
        // Lider bizsek liderliði býrak
        if (IsLeader())
            leadersByGroup.Remove(groupKey);

        ApplyNonLeaderState();
    }

    private void OnDestroy()
    {
        if (IsLeader())
            leadersByGroup.Remove(groupKey);
    }

    private bool HasValidLeader()
    {
        if (!leadersByGroup.TryGetValue(groupKey, out var l)) return false;
        if (l == null) return false;
        if (!l.isActiveAndEnabled) return false;
        return true;
    }

    private bool IsLeader()
    {
        return leadersByGroup.TryGetValue(groupKey, out var l) && l == this;
    }

    private void TryBecomeLeaderOrFollow()
    {
        if (!HasValidLeader())
        {
            leadersByGroup[groupKey] = this;
            ApplyLeaderState();
        }
        else
        {
            ApplyNonLeaderState();
        }
    }

    private void ApplyState()
    {
        if (IsLeader()) ApplyLeaderState();
        else ApplyNonLeaderState();
    }

    private void ApplyLeaderState()
    {
        if (targetAudio == null) return;

        if (disableAudioSourceWhenNotLeader)
            targetAudio.enabled = true;
        else
            targetAudio.mute = false;

        if (autoPlayWhenLeader && !targetAudio.isPlaying)
            targetAudio.Play();
    }

    private void ApplyNonLeaderState()
    {
        if (targetAudio == null) return;

        if (forceStopWhenNotLeader && targetAudio.isPlaying)
            targetAudio.Stop();

        if (disableAudioSourceWhenNotLeader)
            targetAudio.enabled = false;
        else
            targetAudio.mute = true;
    }
}
