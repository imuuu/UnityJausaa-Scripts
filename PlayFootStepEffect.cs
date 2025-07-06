using UnityEngine;
using System.Collections.Generic;

public class FootstepEffects : MonoBehaviour
{
    [System.Serializable]
    private class Footstep
    {
        [SerializeField] private string _footName; // Used for event matching
        [SerializeField] private ParticleSystem _effect; // Footstep effect

        public string FootName => _footName;
        public ParticleSystem Effect => _effect;
    }

    [SerializeField] private List<Footstep> _footsteps = new List<Footstep>(); // List of footstep effects

    public void PlayFootstepEffect(string footName)
    {
        foreach (Footstep footstep in _footsteps)
        {
            if (footstep.FootName == footName && footstep.Effect != null)
            {
                footstep.Effect.Play();
                return;
            }
        }

        Debug.LogWarning($"Footstep effect for '{footName}' not found!", this);
    }
}
