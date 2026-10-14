using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace WoguaShooter.Addressables
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string gameRootAddress = "game/root";

        private IEnumerator Start()
        {
            AsyncOperationHandle initialization = UnityEngine.AddressableAssets.Addressables.InitializeAsync();
            yield return initialization;

            AsyncOperationHandle<GameObject> load = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(gameRootAddress);
            yield return load;

            if (load.Status != AsyncOperationStatus.Succeeded)
                Debug.LogError($"无法加载游戏根预制件：{gameRootAddress}");
        }
    }
}
