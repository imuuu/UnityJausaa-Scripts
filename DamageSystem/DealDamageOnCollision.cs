using UnityEngine;

[RequireComponent(typeof(IDamageReceiver))]
public class DealDamageOnCollision : MonoBehaviour 
{
    [SerializeField] private DamageDealer _damageDealer;
    private void OnCollisionEnter(Collision other) 
    {
        IDamageReceiver receiver = other.gameObject.GetComponent<IDamageReceiver>();

        if (receiver != null)
        {
            IDamageDealer dealer = GetComponent<IDamageDealer>();
            DamageCalculator.CalculateDamage(dealer, receiver);
            
        }
    }
}