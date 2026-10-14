using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace WoguaShooter.Addressables
{
    public class AddressableAssetLoader : MonoBehaviour
    {
        public void LoadPrefab(string address, Transform parent, Action<GameObject> onLoaded)
        {
            StartCoroutine(LoadRoutine(address, parent, onLoaded));
        }

        private IEnumerator LoadRoutine(string address, Transform parent, Action<GameObject> onLoaded)
        {
            AsyncOperationHandle<GameObject> handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(address, parent);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                onLoaded?.Invoke(handle.Result);
            else
                Debug.LogError($"Addressable 加载失败：{address}");
        }
    }
}
