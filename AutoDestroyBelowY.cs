using UnityEngine;

public class AutoDestroyBelowY : MonoBehaviour
{
    [SerializeField] private float _minY = -100f; 

    void Update()
    {
        if (transform.position.y < _minY)
        {            
            Destroy(gameObject);
        }
    }
}
