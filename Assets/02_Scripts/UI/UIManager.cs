using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Class that handle everithing about the UI in this application.
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

        // Link the exit button with the exit function.
        _exitButton.onClick.AddListener(OnExitClick);
    }

    public void OnExitClick()
    {
        Application.Quit();
    }

    // Show or hide a loading panel.
    public void ShowLoadingPanel(bool show)
    {
        _loadingPanel.gameObject.SetActive(show);
    }

    // Show the info panel for a selected shovel.
    public void ShowInfoPanel(bool show,ShovelData shovelData = null)
    {
        if(shovelData != null)
        {
            _infoPanel.SetShovelData(shovelData);
        }

        _infoPanel.gameObject.SetActive(show);
    }

    // Create a new shovel icon.
    public ShovelIcon CreateShovelIcon()
    {
        ShovelIcon shovelIcon = Instantiate<ShovelIcon>(_shovelIconPrefab);

        shovelIcon.transform.SetParent(_canvasRoot, false);

        return shovelIcon;
    }
}
