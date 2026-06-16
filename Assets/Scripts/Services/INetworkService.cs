using System;

public interface INetworkService
{
    void GetRequest(string endpoint, Action<bool, string> callback);
    void PostRequest(string endpoint, string json, Action<bool, string> callback);
}
