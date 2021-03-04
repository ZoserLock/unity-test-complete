using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Main Panels")]
    [SerializeField]
    private GameObject _loadingPanel = null;

    [SerializeField]
    private InfoPanel _infoPanel = null;

    [SerializeField]
    private Transform _canvasRoot = null;

    [SerializeField]
    private Button _exitButton = null;

    [Header("UI Items")]
    [SerializeField]
    private ShovelIcon _shovelIconPrefab = null;

    private void Awake()
    {
        ShowLoadingPanel(false);
        ShowInfoPanel(false);

        _exitButton.onClick.AddListener(OnExitClick);
    }

    public void OnExitClick()
    {
        Application.Quit();
    }

    public void ShowLoadingPanel(bool show)
    {
        _loadingPanel.gameObject.SetActive(show);
    }

    public void ShowInfoPanel(bool show,ShovelData shovelData = null)
    {
        if(shovelData != null)
        {
            _infoPanel.SetShovelData(shovelData);
        }

        _infoPanel.gameObject.SetActive(show);
    }

    public ShovelIcon CreateShovelIcon()
    {
        ShovelIcon shovelIcon = Instantiate<ShovelIcon>(_shovelIconPrefab);

        shovelIcon.transform.SetParent(_canvasRoot, false);

        return shovelIcon;
    }
}
