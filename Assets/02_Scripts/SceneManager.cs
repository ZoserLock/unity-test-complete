using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Dummiesman;
using UnityEngine.EventSystems;

// Class to hold a state of multiple calls. 
public class FutureResult
{
    public bool Completed = false;
    public bool Success = false;
    public List<string> CompletedActions = new List<string>();
    public List<string> FailureReasons = new List<string>();
}

// Main class of the application.
public class SceneManager : MonoBehaviour
{
    [SerializeField]
    private CameraController _cameraController = null;

    [SerializeField]
    private BackendManager _backendManager = null;

    [SerializeField]
    private UIManager _uiManager = null;

    [SerializeField]
    private GameObject _shovelPrefab = null;

    [Header("Map")]

    [SerializeField]
    private GameObject _mapRoot = null;

    [SerializeField]
    private BorderGenerator _mapBorderGenerator = null;

    [SerializeField]
    private Material _mapBaseMaterial = null;

    // True when the application has all the data needed to work.
    private bool _applicationStarted = false;

    // True when something happened during startup
    private bool _applicationFailed = false;

    // Variable that hold the status of the data retrieving process. 
    private FutureResult _dataRetrievingFuture = null;

    // Caches Web Service Data
    private List<ShovelData> _shovels = new List<ShovelData>();
    private List<Report>     _shovelReports = new List<Report>();

    // Derived Data
    private Dictionary<int, Report> _idToShovelReport = new Dictionary<int, Report>();

    // Shovel Visual Representation objects.
    private List<ShovelVisual> _shovelVisuals = new List<ShovelVisual>();

    // Current selected shovel. if none is selected this variable is null.
    private ShovelVisual _selectedShovel = null;

    // The local path of the download meshes and textures.
    private string _mapModelMeshPath    = "";
    private string _mapModelMtlPath     = "";
    private string _mapModelTexturePath = "";

    void Start()
    {
        // Start the recolection of data from the web services
        _dataRetrievingFuture = BeginDataRetrieving();

        _uiManager.ShowLoadingPanel(true);
        EnableCameraControls(false);
    }

    private void Update()
    {
        if(_applicationFailed)
        {
            return;
        }

        if (!_applicationStarted)
        {
            if (_dataRetrievingFuture != null && _dataRetrievingFuture.Completed)
            {
                if (_dataRetrievingFuture.Success)
                {
                    // If the application finished all the web request tasks start the application.
                    BeginApplication();
                }
                else
                {
                    _uiManager.ShowLoadingPanel(false);

                    // In case that something went wrong in the initialization show all the errors in the logs and stop the application.

                    Debug.LogError("Initialization Failed!");
                    Debug.LogError("Failure Actions:");
                    foreach (var reason in _dataRetrievingFuture.FailureReasons)
                    {
                        Debug.LogError("-> "+reason);
                    }

                    Debug.LogWarning("Success Actions:");
                    foreach (var action in _dataRetrievingFuture.CompletedActions)
                    {
                        Debug.LogWarning("-> " + action);
                    }
                    _applicationFailed = true;
                }
            }
        }
        else
        {
            // Try to select a shovel when mouse button is donw.
            if(Input.GetMouseButtonDown(0))
            {
                // Do not consider click in UI
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    // Select a shovel and unselect the last selected shovel.
                    if (_cameraController.TrySelectShovelAtMousePosition(out ShovelVisual shovel))
                    {
                        if (_selectedShovel != null)
                        {
                            _selectedShovel.SetSelected(false);
                            _selectedShovel = null;
                        }

                        _uiManager.ShowInfoPanel(true, shovel.ShovelData);

                        _selectedShovel = shovel;
                        _selectedShovel.SetSelected(true);
                    }
                    else
                    {
                        if(_selectedShovel!=null)
                        {
                            _selectedShovel.SetSelected(false);
                            _selectedShovel = null;
                        }
                        _uiManager.ShowInfoPanel(false);
                    }
                }
            }
        }
    }

    // Class that will start the process of getting all the data from the web services.
    FutureResult BeginDataRetrieving()
    {
        var futureResult = new FutureResult();

        // GetModel Data and then download the files.
        _backendManager.GetModelInfo((BackendRequestResult result, GetModelResponse response) =>
        {
            if (result.Success)
            {
                futureResult.CompletedActions.Add("GetModelInfo");
                BeginModelDownload(futureResult, response.ObjUrl, response.MtlUrl,response.TextureUrl);
            }
            else
            {
                if(!futureResult.Completed)
                {
                    futureResult.Completed = true;
                    futureResult.Success = false;
                    futureResult.FailureReasons.Add("Failed To Get Model Info");
                }
            }
        });

        // Get the shovel data
        _backendManager.GetShovels((BackendRequestResult result, GetShovelsResponse response) =>
        {
            if (result.Success)
            {
                futureResult.CompletedActions.Add("GetShovels");
                _shovels.Clear();
                _shovels.AddRange(response.Shovels);
            }
            else
            {
                if (!futureResult.Completed)
                {
                    futureResult.Completed = true;
                    futureResult.Success = false;
                    futureResult.FailureReasons.Add("Failed To Get Shovels");
                }
            }
        });

        // Get the shovel report data
        _backendManager.GetShovelInfo( (BackendRequestResult result, GetShovelInfoResponse response) =>
        {
            if (result.Success)
            {
                futureResult.CompletedActions.Add("GetShovelInfo");

                _shovelReports.Clear();
                _shovelReports.AddRange(response.Reports);
            }
            else
            {
                if (!futureResult.Completed)
                {
                    futureResult.Completed = true;
                    futureResult.Success = false;
                    futureResult.FailureReasons.Add("Failed To Get Shovel Reports");
                }
            }
        });

        StartCoroutine(CheckDataRetrieveStatus(futureResult));

        return futureResult;
    }

    void BeginModelDownload(FutureResult futureResult, string meshUri, string mtlUri, string textureUri)
    {
        _backendManager.DownloadFile(meshUri, (BackendRequestResult result, string filePath) =>
        { 
            if(result.Success)
            {
                _mapModelMeshPath = filePath;
                futureResult.CompletedActions.Add("GetMesh");
            }
            else
            {
                if (!futureResult.Completed)
                {
                    futureResult.Completed = true;
                    futureResult.Success = false;
                    futureResult.FailureReasons.Add("Failed to Download Mesh File: " + result.ErrorString);
                }
            }
        });

        _backendManager.DownloadFile(mtlUri, (BackendRequestResult result, string filePath) =>
        {
            if (result.Success)
            {
                _mapModelMtlPath = filePath;
                futureResult.CompletedActions.Add("GetMtl");
            }
            else
            {
                if (!futureResult.Completed)
                {
                    futureResult.Completed = true;
                    futureResult.Success = false;
                    futureResult.FailureReasons.Add("Failed to Download Mtl File: " + result.ErrorString);
                }
            }
        });

        _backendManager.DownloadFile(textureUri, (BackendRequestResult result, string filePath) =>
        {
            if (result.Success)
            {
                _mapModelTexturePath = filePath;
                futureResult.CompletedActions.Add("GetTexture");
            }
            else
            {
                if (!futureResult.Completed)
                {
                    futureResult.Completed = true;
                    futureResult.Success = false;
                    futureResult.FailureReasons.Add("Failed to Download Texture File: "+result.ErrorString);
                }
            }
        });
    }

    // Function that check if all the task are completed.
    // In this case there are always 6 tasks so the system waits that.
    // In case of an error the futureResult will be completed and this coroutine will stop.
    IEnumerator CheckDataRetrieveStatus(FutureResult futureResult)
    {
        while(!futureResult.Completed)
        {
            if(futureResult.CompletedActions.Count == 6)
            {
                futureResult.Completed = true;
                futureResult.Success = true;
                break;
            }
            yield return null;
        }
    }

    // Function called when the system already have all the information needed to show the task.
    void BeginApplication()
    {
        EnableCameraControls(true);

        _uiManager.ShowLoadingPanel(false);

        PrepareDerivatedData();

        LoadMapModel();

        LoadShovelVisuals();

        _applicationStarted = true;
    }

    // Function to prepare the data obtained from the webservice in a more useful way.
    void PrepareDerivatedData()
    {
        // Create a hashtable from id to shovel object.
        foreach (var report in _shovelReports)
        {
            _idToShovelReport.Add(report.ShovelID, report);
        }

        // Fill each shovel with its report.
        foreach (var shovel in _shovels)
        {
            if(_idToShovelReport.TryGetValue(shovel.ID,out Report report))
            {
                shovel.Report = report;
            }
        }
    }

    // Load and generate the map model
    void LoadMapModel()
    {
        // Parse de obj model object.
        var mapMeshGameObject = new OBJLoader().Load(_mapModelMeshPath, _mapModelMtlPath);

        var meshRenderer = mapMeshGameObject.GetComponentInChildren<MeshRenderer>();
        var meshFilter = mapMeshGameObject.GetComponentInChildren<MeshFilter>();
        var meshGameObject = meshRenderer.gameObject;

        // Extract the texture of the parsed mesh
        var texture = meshRenderer.material.GetTexture("_MainTex");

        // Change the material and set the texture.
        meshRenderer.material = _mapBaseMaterial;
        meshRenderer.sharedMaterial.SetTexture("_BaseMap", texture);

        // Attach the mesh to a know transform.
        mapMeshGameObject.transform.SetParent(_mapRoot.transform);

        // Generate a mesh collider using the same mesh.
        var meshCollider = meshGameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        // Generate the border of this mesh.
        _mapBorderGenerator.GenerateBorderMesh(meshFilter.sharedMesh);
    }

    // Load all the objects that represent a shovel in the model.
    // This also loads the UI icons.
    void LoadShovelVisuals()
    {
        foreach (var shovel in _shovels)
        {
            var gameObject = Instantiate(_shovelPrefab);
            var shovelVisual = gameObject.GetComponent<ShovelVisual>();

            if(shovelVisual != null)
            {
                var shovelIcon = _uiManager.CreateShovelIcon();
                shovelIcon.SetCamera(_cameraController.Camera);

                shovelVisual.SetShovelData(shovel);
                shovelVisual.SetShovelIcon(shovelIcon);

                shovelVisual.transform.localEulerAngles = new Vector3(0, Random.Range(0,360),0);

                _shovelVisuals.Add(shovelVisual);
            }
            else
            {
                Debug.LogError("Unable to instantiate a new Shovel visual. Did the shovel visual has the correct script attached?");
                Destroy(gameObject);
            }
        }
    }

    // Enable the camera controls to the user.
    void EnableCameraControls(bool enabled)
    {
        _cameraController.enabled = enabled;
    }
}
