using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
    [SerializeField]
    private Transform _stateRoot = null;

    [SerializeField]
    private StateItem _stateItemPrefab = null;

    [SerializeField]
    private Text _title = null;

    [SerializeField]
    private Text _performanceValue = null;

    [SerializeField]
    private Image _performanceMeter = null;

    private List<StateItem> _stateItems = new List<StateItem>();

    public void SetShovelData(ShovelData shovelData)
    {
        // Clear previously created state items
        foreach(var item in _stateItems)
        {
            Destroy(item.gameObject);
        }

        _stateItems.Clear();

        _title.text = shovelData.Name;

        if(shovelData.Report != null)
        {
            // Fill Performance Data
            float performancePerUnit = (float)(shovelData.Report.Performance / shovelData.Report.PlannedPerformance);
            _performanceMeter.fillAmount = performancePerUnit;
            _performanceValue.text = Mathf.FloorToInt(performancePerUnit * 100)+"%";

            // Fill State Items
            foreach(var state in shovelData.Report.LastStates)
            {
                AddStateItem(state);
            }
        }
    }

    private void AddStateItem(ShovelState state)
    {
        var stateItem = Instantiate<StateItem>(_stateItemPrefab);
        stateItem.transform.SetParent(_stateRoot, false);
        stateItem.SetState(state);
        _stateItems.Add(stateItem);
    }

}
