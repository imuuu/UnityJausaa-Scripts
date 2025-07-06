using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeObstructors : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private float _checkRadius = 0.3f;
    [SerializeField] private float _fadeSpeed = 3f;
    [SerializeField] private float _targetAlpha = 0.1f;
    [SerializeField] private LayerMask _obstructionMask;
    [SerializeField] private Shader _fadeShader; // Drag "Shader Graphs/Fading Walls" here

    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new();
    private readonly Dictionary<Renderer, Material[]> _fadedMaterials = new();
    private readonly Dictionary<Renderer, Coroutine> _fadeCoroutines = new();

    private void Start()
    {
        if (_player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("FadeObstructors: Player object with tag 'Player' not found.");
            }
        }

        if (_fadeShader == null)
        {
            _fadeShader = Shader.Find("Shader Graphs/Fading Walls");
            if (_fadeShader == null)
            {
                Debug.LogError("FadeObstructors: Fading shader not found!");
            }
        }
    }

    private void Update()
    {
        if (_player == null || _fadeShader == null) return;

        Vector3 dir = _player.position - Camera.main.transform.position;
        float dist = Vector3.Distance(Camera.main.transform.position, _player.position);
        Ray ray = new(Camera.main.transform.position, dir);

        RaycastHit[] hits = Physics.SphereCastAll(ray, _checkRadius, dist, _obstructionMask);

        HashSet<Renderer> currentHits = new();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                currentHits.Add(rend);
                StartFading(rend, _targetAlpha);
            }
        }

        List<Renderer> keys = new(_originalMaterials.Keys);
        foreach (Renderer rend in keys)
        {
            if (!currentHits.Contains(rend))
            {
                StartFading(rend, 1f, true);
            }
        }
    }

    private void StartFading(Renderer rend, float target, bool restore = false)
    {
        if (!_originalMaterials.ContainsKey(rend))
        {
            _originalMaterials[rend] = rend.sharedMaterials;

            Material[] faded = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < faded.Length; i++)
            {
                faded[i] = new Material(_fadeShader);
                faded[i].SetFloat("_Alpha", 1f);
            }
            _fadedMaterials[rend] = faded;
            rend.materials = faded;
        }

        if (_fadeCoroutines.TryGetValue(rend, out Coroutine existing) && existing != null)
        {
            StopCoroutine(existing);
        }

        _fadeCoroutines[rend] = StartCoroutine(FadeRoutine(rend, target, restore));
    }

    private IEnumerator FadeRoutine(Renderer rend, float targetAlpha, bool restore)
    {
        Material[] originalMats = _originalMaterials[rend];
        Material[] fadeMats = _fadedMaterials[rend];
        Material[] currentMats = rend.materials;

        bool switched = false;
        float currentAlpha = currentMats[0].GetFloat("_Alpha");

        while (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, _fadeSpeed * Time.deltaTime);

            foreach (Material mat in currentMats)
            {
                mat.SetFloat("_Alpha", currentAlpha);
            }

            if (!switched && targetAlpha < 1f && currentAlpha <= 0.95f)
            {
                rend.materials = fadeMats;
                currentMats = rend.materials;
                switched = true;

                foreach (Material mat in currentMats)
                {
                    mat.SetFloat("_Alpha", currentAlpha);
                }
            }

            yield return null;
        }

        if (restore)
        {
            rend.materials = originalMats;
            _originalMaterials.Remove(rend);
            _fadedMaterials.Remove(rend);
        }

        _fadeCoroutines.Remove(rend);
    }
}
