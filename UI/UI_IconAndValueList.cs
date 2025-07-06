using System.Collections.Generic;
using Nova;
using UnityEngine;

public class UI_IconAndValueList : MonoBehaviour
{
    [SerializeField] private ListView _listView;

    [SerializeField] private List<CurrencyAmount> _currencies;

    [SerializeField] private bool _getBalances = false;

    private float _offset = 0;

    private void Awake()
    {
        _offset = _listView.UIBlock.AutoLayout.Offset;
    }

    private void Start()
    {
        ClearListChildren();
        Initialize();
    }

    private void OnEnable()
    {
        if (_getBalances)
        {
            ActionScheduler.RunAfterDelay(0.1f, () =>
            {
                SetCurrencies(ManagerCurrency.Instance.GetAllBalances());
            });
           
        }
    }

    private void Initialize()
    {
        _listView.AddDataBinder<CurrencyAmount, IconAndValueVisual>(BindItem);
        _listView.SetDataSource(_currencies);
    }

    private void BindItem(Data.OnBind<CurrencyAmount> evt, IconAndValueVisual target, int index)
    {
        CurrencyAmount currencyAmounts = evt.UserData;
        target.Bind(currencyAmounts);

        if (_getBalances)
        {
            target.View.gameObject.GetComponent<UI_IconAndValue>().SetCurrencyType(currencyAmounts.CurrencyType);
        }
    }

    public void SetCurrencies(List<CurrencyAmount> currencyAmounts)
    {
        _currencies.Clear();
        _currencies.AddRange(currencyAmounts);

        _listView.Refresh();
        _listView.UIBlock.AutoLayout.Offset = _offset;
    }

    private void ClearListChildren()
    {
        if (_listView == null) return;

        Transform listview = _listView.transform;

        for (int i = listview.childCount - 1; i >= 0; i--)
        {
            Transform child = listview.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    } 


}