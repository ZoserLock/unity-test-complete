using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Class that hold a result of a backend requests.
public class BackendRequestResult
{
    public bool Success = false;
    public string ResponseString = "";
    public byte[] ResponseBytes = null;
    public string ErrorString    = "";
}

// Class that handle all related to web services or file download.
public class BackendManager : MonoBehaviour
{
    private const string _modelURL      = "https://unity-exercise.dt.timlabtesting.com/data/mesh-obj";
    private const string _shovelsURL    = "https://unity-exercise.dt.timlabtesting.com/data/shovels";
    private const string _shovelInfoURL = "https://unity-exercise.dt.timlabtesting.com/data/report";

    // Get the information about the mesh.
    public void GetModelInfo(System.Action<BackendRequestResult, GetModelResponse> resultFunction)
    {
        StartCoroutine(GetRequest(_modelURL,(BackendRequestResult result)=>
        {
            GetModelResponse responseObject = new GetModelResponse();

            if (result.Success)
            {
                responseObject = JsonUtility.FromJson<GetModelResponse>(result.ResponseString);
            }
    
            if (resultFunction != null)
            {
                resultFunction(result, responseObject);
            }
        }));
    }

    // Get the information about the shovel.
    public void GetShovels(System.Action<BackendRequestResult, GetShovelsResponse> resultFunction)
    {
        StartCoroutine(GetRequest(_shovelsURL, (BackendRequestResult result) =>
        {
            GetShovelsResponse responseObject = new GetShovelsResponse();

            if (result.Success)
            {
                responseObject = JsonUtility.FromJson<GetShovelsResponse>(result.ResponseString);
            }

            if (resultFunction != null)
            {
                resultFunction(result, responseObject);
            }
        }));
    }

    // Get information about the state of the shovels.
    public void GetShovelInfo(System.Action<BackendRequestResult, GetShovelInfoResponse> resultFunction)
    {
        StartCoroutine(GetRequest(_shovelInfoURL, (BackendRequestResult result) =>
        {
            GetShovelInfoResponse responseObject = new GetShovelInfoResponse();

            if (result.Success)
            {
                responseObject = JsonUtility.FromJson<GetShovelInfoResponse>(result.ResponseString);
            }

            if (resultFunction != null)
            {
                resultFunction(result, responseObject);
            }
        }));
    }

    // Download a file directly to the folder streaming assets.
    public void DownloadFile(string uri, System.Action<BackendRequestResult,string> resultCallback = null)
    {
        var lastSlash = uri.LastIndexOf('/');
    
        if(lastSlash==-1)
        {
            BackendRequestResult result = new BackendRequestResult();
            result.ErrorString = "Invalid input uri string";
            result.Success = false;

            if (resultCallback != null)
            {
                resultCallback(result, "");
            }
        }

        string filePath = Application.streamingAssetsPath + uri.Substring(lastSlash);

        StartCoroutine(GetRequest(uri, (BackendRequestResult result) =>
        {
            if (result.Success)
            {
                try
                {
                    // Ensure Download Folder Path exists
                    Directory.CreateDirectory(Application.streamingAssetsPath);

                    // Write file to disk
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(result.ResponseBytes, 0, result.ResponseBytes.Length);
                    }
                }
                catch (Exception ex)
                { 
                    result.ErrorString = "Backend request succeeded of but the system were unable to write the file to disk. Reason: "+ ex.Message;
                    result.Success = false;
                }
            }

            if (resultCallback != null)
            {
                resultCallback(result, filePath);
            }
        }));
    }

    // Base function to made a request.
    private IEnumerator GetRequest(string uri, System.Action<BackendRequestResult> responseCallback)
    {
        BackendRequestResult result = new BackendRequestResult();

        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (!webRequest.isNetworkError && webRequest.responseCode == 200)
            {
                string responseStr = System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data);

                result.Success = true;
                result.ResponseString = responseStr;
                result.ResponseBytes  = webRequest.downloadHandler.data;

                if (responseCallback != null)
                {
                    responseCallback(result);
                }
            }
            else
            {
                result.Success = false;
                result.ResponseString = "";
                result.ErrorString = "GetRequest -> Something went wrong while doing a Get Request";

                if (responseCallback != null)
                {
                    responseCallback(result);
                }
            }
        }
    }
}
