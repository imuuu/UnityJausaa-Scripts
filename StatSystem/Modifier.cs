using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using static Game.StatSystem.Modifier;

namespace Game.StatSystem
{
    [System.Serializable]
    public class Modifier
    {
        public enum VALUE_RANGE { SINGLE, RANGE }
        public enum DECIMAL_ACCURACY { ZERO, ONE, TWO, NONE }

        [Title("Target Settings")]
        [SerializeField] private STAT_TYPE _target;
        [SerializeField][EnumToggleButtons] private MODIFIER_TYPE _type;

        [Title("Value Settings")]
        [SerializeField][EnumToggleButtons] private VALUE_RANGE _valueRange;

        [ShowIf("_valueRange", VALUE_RANGE.SINGLE)]
        [SerializeField] private float _value;

        [ShowIf("_valueRange", VALUE_RANGE.RANGE)]
        [SerializeField]
        [LabelWidth(50)]
        [HorizontalGroup("Range")]
        private float _min;

        [ShowIf("_valueRange", VALUE_RANGE.RANGE)]
        [SerializeField]
        [LabelWidth(50)]
        [HorizontalGroup("Range")]
        private float _max;

        [Title("Rounding Settings")]
        [SerializeField] private DECIMAL_ACCURACY _accuracy = DECIMAL_ACCURACY.ZERO;

        private bool _isCombined = false;
        private bool _isLocked = false;
        private float _resultValue = 0f;

        public Modifier()
        {
            _type = MODIFIER_TYPE.FLAT;
            _value = 0f;
        }

        public Modifier(MODIFIER_TYPE type, float value)
        {
            _type = type;
            _value = value;
        }

        public float GenerateValue()
        {
            if (_valueRange == VALUE_RANGE.SINGLE)
            {
                _resultValue = _value;
            }
            else if (_valueRange == VALUE_RANGE.RANGE)
            {
                if (_min > _max)
                {
                    Debug.LogWarning("Modifier range is invalid. _min is greater than _max.");
                    _resultValue = _min;
                }
                else
                {
                    _resultValue = UnityEngine.Random.Range(_min, _max);
                }
            }
            else
            {
                _resultValue = _value;
            }

            switch (_accuracy)
            {
                case DECIMAL_ACCURACY.ZERO:
                    _resultValue = (float)Math.Round(_resultValue, 0);
                    break;
                case DECIMAL_ACCURACY.ONE:
                    _resultValue = (float)Math.Round(_resultValue, 1);
                    break;
                case DECIMAL_ACCURACY.TWO:
                    _resultValue = (float)Math.Round(_resultValue, 2);
                    break;
            }

            return _resultValue;
        }

        public float GetValue()
        {
            if(_resultValue <= 0f)
            {
                GenerateValue();
            }

            return _resultValue;
        }

        public MODIFIER_TYPE GetTYPE()
        {
            return _type;
        }

        public STAT_TYPE GetTarget()
        {
            return _target;
        }

        public void SetLock(bool isLocked)
        {
            _isLocked = isLocked;
        }

        public bool IsLocked()
        {
            return _isLocked;
        }

        /// <summary>
        /// Combines a list of modifiers. Modifiers with the same target and type are merged into one.
        /// The combined modifier's value is the sum of each modifier's GetValue() result, and its accuracy is chosen as the highest of the group.
        /// The new modifier is marked as combined so that subsequent calls to GetValue() return the pre-calculated value.
        /// </summary>
        public static List<Modifier> CombineModifiers(List<Modifier> modifiers)
        {
            // Group modifiers by (target, type)
            IEnumerable<IGrouping<(STAT_TYPE _target, MODIFIER_TYPE _type), Modifier>> groups = modifiers.GroupBy(mod => (mod._target, mod._type));

            List<Modifier> combinedModifiers = new List<Modifier>();

            foreach (IGrouping<(STAT_TYPE _target, MODIFIER_TYPE _type), Modifier> group in groups)
            {
                float combinedValue = 0f;
                DECIMAL_ACCURACY highestAccuracy = DECIMAL_ACCURACY.ZERO;

                foreach (Modifier mod in group)
                {
                    combinedValue += mod.GetValue();
                    if ((int)mod._accuracy > (int)highestAccuracy)
                    {
                        highestAccuracy = mod._accuracy;
                    }
                }

                Modifier combined = new Modifier
                {
                    _target = group.Key._target,
                    _type = group.Key._type,
                    _accuracy = highestAccuracy,
                    _isCombined = true,
                    _isLocked = true,
                    _valueRange = VALUE_RANGE.SINGLE,
                    _resultValue = combinedValue,
                    _value = combinedValue
                };

                combinedModifiers.Add(combined);
            }
            return combinedModifiers;
        }

        /// <summary>
        /// Creates a shallow copy of a modifier.
        /// </summary>
        private static Modifier CloneModifier(Modifier mod)
        {
            Modifier clone = new Modifier
            {
                _target = mod._target,
                _type = mod._type,
                _accuracy = mod._accuracy,
                _valueRange = mod._valueRange,
                _isCombined = mod._isCombined,
                _isLocked = mod._isLocked,
                _resultValue = mod._resultValue,
                _value = mod._value,
            };

            if (mod._valueRange == VALUE_RANGE.SINGLE)
            {
                clone._value = mod._value;
            }
            else
            {
                clone._min = mod._min;
                clone._max = mod._max;
            }
            return clone;
        }

        public Modifier Clone()
        {
            return CloneModifier(this);
        }

        public override string ToString()
        {
            string valueString = _valueRange == VALUE_RANGE.SINGLE ? _value.ToString() : $"[{_min}, {_max}]";
            return $"Modifier: Target: {_target}, Type: {_type}, Value: {valueString}, Accuracy: {_accuracy}";
        }
    }
}
