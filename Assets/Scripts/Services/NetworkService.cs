using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkService : MonoBehaviour, INetworkService
{
    [SerializeField] private string baseUrl = "https://localhost:7277/api";

    public void GetRequest(string endpoint, Action<bool, string> callback)
    {
        StartCoroutine(GetRequestRoutine(endpoint, callback));
    }

    public void PostRequest(string endpoint, string json, Action<bool, string> callback)
    {
        StartCoroutine(PostRequestRoutine(endpoint, json, callback));
    }

    private IEnumerator PostRequestRoutine(string endpoint, string json, Action<bool, string> callback)
    {
        using (UnityWebRequest req = new UnityWebRequest(BuildUrl(endpoint), "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            AttachCertificateHandler(req);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            callback(ok, ResponseText(req));
        }
    }

    private IEnumerator GetRequestRoutine(string endpoint, Action<bool, string> callback)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(BuildUrl(endpoint)))
        {
            AttachCertificateHandler(req);
            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            callback(ok, ResponseText(req));
        }
    }

    private string BuildUrl(string endpoint)
    {
        return baseUrl.TrimEnd('/') + "/" + endpoint.TrimStart('/');
    }

    private static string ResponseText(UnityWebRequest req)
    {
        string text = "";
        if (req.downloadHandler != null && !string.IsNullOrWhiteSpace(req.downloadHandler.text))
        {
            text = req.downloadHandler.text;
        }
        else if (!string.IsNullOrWhiteSpace(req.error))
        {
            text = req.error;
        }

        return text;
    }

    private static void AttachCertificateHandler(UnityWebRequest req)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        req.certificateHandler = new LocalhostCertificateHandler();
#endif
    }
}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class LocalhostCertificateHandler : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
#endif
