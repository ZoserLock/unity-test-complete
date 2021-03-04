using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShovelVisual : MonoBehaviour
{
    [SerializeField]
    private Transform _iconAttachPoint = null;
    [SerializeField]
    private GameObject _selection = null;

    private ShovelData _shovelData = null;
    private ShovelIcon _shovelIcon = null;

    public ShovelData ShovelData
    {
        get { return _shovelData; }
    }

    public void SetShovelData(ShovelData data)
    {
        _shovelData = data;

        if (_shovelData != null)
        {
            gameObject.transform.position = _shovelData.Position;
        }
    }

    public void SetShovelIcon(ShovelIcon icon)
    {
        _shovelIcon = icon;
        _shovelIcon.SetAttachPoint(_iconAttachPoint);
        _shovelIcon.SetText(_shovelData.Name);
    }

    public void SetSelected(bool selection)
    {
        _selection.SetActive(selection);
    }
}
