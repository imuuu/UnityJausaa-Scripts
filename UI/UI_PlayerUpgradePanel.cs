using Nova;
using UnityEngine;

public class UI_PlayerUpgradePanel : MonoBehaviour
{
    public BlessingUpgradeGrouper BlessingUpgradeGrouper;
    [SerializeField] ListView _listView;

    private void Start()
    {
        ClearBodyListChildrens();
        Initialize();
    }

    private void Initialize()
    {
        BlessingGroup[] blesses = BlessingUpgradeGrouper.Groups;
        _listView.AddDataBinder<BlessingGroup, BlessingUpgradeVisual>(BindItem);
        _listView.SetDataSource(blesses);
    }

    private void OnEnable() 
    {
        ActionScheduler.RunAfterDelay(0.1f, () =>
        {
            if(_listView == null || !_listView.gameObject.activeInHierarchy) return;

            _listView.JumpToIndex(0);
        });
    }

    // NOTE: this happens every time the item comes in list bound, meaning if list is scrollable 
    // and item is scrolled out of view and back in, this will be called again
    private void BindItem(Data.OnBind<BlessingGroup> evt, BlessingUpgradeVisual target, int index)
    {
        BlessingGroup blessingGroup = evt.UserData;
        target.Bind(blessingGroup);

        target.View.gameObject.GetComponent<PlayerUpgradeCard>().Bind(blessingGroup, target);
    }

    private void ClearBodyListChildrens()
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