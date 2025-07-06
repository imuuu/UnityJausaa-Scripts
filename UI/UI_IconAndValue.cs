using Nova;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class UI_IconAndValue : MonoBehaviour
{
    [SerializeField] private CURRENCY _currencyType;
    [SerializeField] private bool _showOnlyIfValueIsPositive = true;
    [SerializeField] private ItemView _itemView;

    [SerializeField] private bool _isPlayerBalance = false;

    private void Awake()
    {
        if (_isPlayerBalance) Events.OnCurrencyBalanceChange.AddListener(OnCurrencyBalanceChange);
    }

    private void OnEnable()
    {

        if (_isPlayerBalance) ActionScheduler.RunNextFrame(() => GetPlayerBalance());
    }

    private void OnDestroy()
    {
        Events.OnCurrencyBalanceChange.RemoveListener(OnCurrencyBalanceChange);
    }

    public void SetCurrencyType(CURRENCY currencyType)
    {
        _currencyType = currencyType;
    }

    private bool OnCurrencyBalanceChange(CURRENCY currency, int totalValue)
    {
        if (currency != _currencyType)
        {
            return true;
        }

        GetPlayerBalance();
        return true;
    }

    private bool GetPlayerBalance()
    {
        if (this == null) return false;

        IconAndValueVisual visual = _itemView.Visuals as IconAndValueVisual;

        int value = ManagerCurrency.Instance.GetBalance(_currencyType);

        if (value <= 0 && _showOnlyIfValueIsPositive)
        {
            this.gameObject.SetActive(false);
            return false;
        }

        visual.Bind(ManagerCurrency.Instance.GetCurrencyDefinition(_currencyType));

        this.gameObject.SetActive(true);

        return true;
    }
    
    public void SetCurrency(CurrencyAmount currencyAmount)
    {
        if (this == null) return;

        IconAndValueVisual visual = _itemView.Visuals as IconAndValueVisual;

        visual.Bind(currencyAmount);

        this.gameObject.SetActive(true);
    }
}