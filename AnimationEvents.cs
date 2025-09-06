using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class AnimationEvents : MonoBehaviour
{
    [Header("Effects (child objects)")]
    [SerializeField] private List<EffectEntry> _effects = new();

    [Header("Optional: Generic Unity Event")]
    [SerializeField] private UnityEvent _onEvent;

    private Dictionary<string, EffectEntry> _effectById;

    [Serializable]
    private struct EffectEntry
    {
        public string id;                   // kutsu tätä ID:tä animation eventissä
        public GameObject target;           // lapsiobjekti, jonka haluat aktivoida
        public bool playParticleOnActivate; // yrittää Play() jos löytyy ParticleSystem
        public bool restartIfPlaying;       // pysäyttää + tyhjentää ennen Play()
        public float autoDeactivateAfter;   // 0 = ei auto-deaktivoida
    }

    private void Awake()
    {
        _effectById = new Dictionary<string, EffectEntry>(StringComparer.Ordinal);
        foreach (var e in _effects)
        {
            if (!string.IsNullOrEmpty(e.id) && e.target != null)
                _effectById[e.id] = e;
        }
    }

    // ========= Peruskutsut Animation Eventeille =========

    /// <summary>
    /// Aktivoi efektin ID:llä (asettaa active = true ja halutessa Play()).
    /// Jos autoDeactivateAfter > 0, disabloi sen viiveellä.
    /// </summary>
    public void ActivateById(string id)
    {
        if (TryGet(id, out var entry))
            Activate(entry);
        else
            Debug.LogWarning($"[AnimationEvents] ID:tä '{id}' ei löytynyt.", this);
    }

    /// <summary>
    /// Deaktivoi efektin ID:llä (active = false).
    /// </summary>
    public void DeactivateById(string id)
    {
        if (TryGet(id, out var entry))
            SetActive(entry.target, false);
        else
            Debug.LogWarning($"[AnimationEvents] ID:tä '{id}' ei löytynyt.", this);
    }

    /// <summary>
    /// Toggle efektin ID:llä (vaihtaa active-tilaa).
    /// </summary>
    public void ToggleById(string id)
    {
        if (TryGet(id, out var entry))
            SetActive(entry.target, !entry.target.activeSelf);
        else
            Debug.LogWarning($"[AnimationEvents] ID:tä '{id}' ei löytynyt.", this);
    }

    /// <summary>
    /// Aktivoi suoraan Object reference -kenttään annetun lapsiobjektin.
    /// </summary>
    public void Activate(GameObject target)
    {
        if (target == null) return;
        // käytä oletuslogiikkaa ilman id-kohtaisia asetuksia
        SetActive(target, true);
        TryPlayParticles(target, restart: false);

        // ei auto-deaktivoida ilman ID:tä (koska ei asetuksia)
    }

    /// <summary>
    /// Deaktivoi suoraan Object reference -kenttään annetun lapsiobjektin.
    /// </summary>
    public void Deactivate(GameObject target)
    {
        if (target == null) return;
        SetActive(target, false);
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Kutsu valmista UnityEventiä (esim. kameratärinä).
    /// </summary>
    public void InvokeEvent()
    {
        _onEvent?.Invoke();
    }

    // ================= Sisäinen logiikka =================

    private bool TryGet(string id, out EffectEntry entry)
    {
        if (_effectById != null && _effectById.TryGetValue(id, out entry))
        {
            return true;
        }

        entry = default;
        return false;
    }


    private void Activate(EffectEntry entry)
    {
        if (entry.target == null) return;

        SetActive(entry.target, true);

        if (entry.playParticleOnActivate)
            TryPlayParticles(entry.target, entry.restartIfPlaying);

        if (entry.autoDeactivateAfter > 0f)
            StartCoroutine(DeactivateAfterDelay(entry.target, entry.autoDeactivateAfter));
    }

    private IEnumerator DeactivateAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
            SetActive(target, false);
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go.activeSelf == active) return;
        go.SetActive(active);
    }

    private void TryPlayParticles(GameObject go, bool restart)
    {
        // Etsi sekä pääobjektista että lapsista (yleinen tapa koostaa efektejä)
        var particleSystems = go.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var ps = particleSystems[i];
            if (restart)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            ps.Play(true);
        }
    }
}
