
using Game.HitDetectorSystem;
using Game.StatSystem;
using UnityEngine;

namespace Game.SkillSystem
{
    /// <summary>
    /// Indicates that when a hand is touching this object
    /// This is always created by the Ability_PhysicHand and destroys itself when the hand is no longer touching.
    /// This manipulates scripts so that physic hand works with them. Also adds some scripts to the object
    /// to make it work with the physic hand.
    /// </summary>
    public class PhysicHandTouched : MonoBehaviour
    {
        public IOwner OwnerOfPhysicHand { get; set; }

        private IOwner _owner;
        private OWNER_TYPE _lastOwnerType;

        private OWNER_TYPE OWNER_OF_PHYSIC_HAND_TYPE = OWNER_TYPE.PHYSIC_HAND;

        private bool _isOwnerCreated = false;
        private Owner _createdOwner;
        private HitDetector _foundHitDetector;
        private HitDetector_PhysicHand _hitDetectorPhysicHand;

        private bool _isDamageDealerCreated = false;
        private float _oldDamage;
        private IDamageDealer _damageDealer;
        private DAMAGE_SOURCE _oldDamageSource;

        private void Awake()
        {
            _owner = GetComponent<IOwner>();
        }

        private void Start()
        {
            if (_owner != null)
            {
                _lastOwnerType = _owner.GetOwnerType();
                _owner.SetOwner(OWNER_OF_PHYSIC_HAND_TYPE);
                _owner.SetManipulatedOwner(OwnerOfPhysicHand);
            }
        }

        private void OnDisable()
        {
            if (_owner != null)
            {
                _owner.SetManipulatedOwner(null);
                _owner.SetOwner(_lastOwnerType);
            }

            if (_foundHitDetector != null)
            {
                _foundHitDetector.SetEnable(true);
            }

            if (_isOwnerCreated)
            {
                Destroy(_createdOwner);
                _isOwnerCreated = false;
            }

            if (_hitDetectorPhysicHand != null)
            {
                _hitDetectorPhysicHand.DestroyThisHitDetectorScript();
            }

            if (_isDamageDealerCreated)
            {
                Destroy(_damageDealer as Component);
            }
            else
            {
                if (_damageDealer != null)
                {
                    _damageDealer.SetDamage(_oldDamage);
                    _damageDealer.SetDamageSource(_oldDamageSource);
                }
            }

            _isDamageDealerCreated = false;
            _isOwnerCreated = false;
            _oldDamage = -1f;
            _damageDealer = null;
            _createdOwner = null;
            _foundHitDetector = null;
            _hitDetectorPhysicHand = null;

            Destroy(this);
        }

        public void Initialize(StatList baseStats, Rigidbody rigidbody, float weightReduceMultiplier)
        {
            IOwner owner = this.gameObject.GetComponent<IOwner>();
            if (owner == null)
            {
                owner = this.gameObject.AddComponent<Owner>();
                _createdOwner = owner as Owner;
                _isOwnerCreated = true;
            }

            _foundHitDetector = this.gameObject.GetComponent<HitDetector>();
            if (_foundHitDetector != null)
            {
                _foundHitDetector.SetEnable(false);
            }

            //this is due to give little bit time to register the HitDetector_PhysicHand
            ActionScheduler.RunNextFrame( () =>
            {
                _hitDetectorPhysicHand = this.gameObject.gameObject.GetOrAdd<HitDetector_PhysicHand>();
            });

            _damageDealer = this.gameObject.GetComponent<IDamageDealer>();
            if (_damageDealer == null)
            {
                _damageDealer = this.gameObject.gameObject.AddComponent<DamageDealer>();
                _isDamageDealerCreated = true;
            }
            else
            {
                _oldDamageSource = _damageDealer.GetDamageSource();
                _oldDamage = _damageDealer.GetDamage();
            }

            float weighMultiplier = baseStats.GetValueOfStat(STAT_TYPE.WEIGHT);

            float mass = rigidbody.mass;

            float newDamage = baseStats.GetValueOfStat(STAT_TYPE.DAMAGE);

            //newDamage += mass * weightReduceMultiplier * weighMultiplier;
            newDamage *= mass * weightReduceMultiplier * weighMultiplier;

            _damageDealer.SetDamage(newDamage);
            _damageDealer.SetDamageSource(DAMAGE_SOURCE.PHYSIC_HAND);
        }
    }
}
