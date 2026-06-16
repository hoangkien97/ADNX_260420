using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        // 1. Create a persistent GameObject for MonoBehaviour services (like NetworkService)
        GameObject go = new GameObject("[AppServices]");
        Object.DontDestroyOnLoad(go);

        // 2. Instantiate and Register MonoBehaviour Services
        NetworkService netService = go.AddComponent<NetworkService>();
        ServiceLocator.Register<INetworkService>(netService);

        // 3. Register Pure C# Services
        ServiceLocator.Register<IAuthService>(new AuthService());
        ServiceLocator.Register<ILeaderboardService>(new LeaderboardService());
        
        Debug.Log("[AppBootstrap] Services Initialized.");
    }
}
