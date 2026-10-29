using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace WoguaShooter.Core
{
    public class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private Transform[] tiles;
        [SerializeField] private float tileWidth = 20f;
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private string levelTwoBackgroundAddress = "map/level2/background/1.0.1";

        private SpriteRenderer[] tileRenderers;
        private Sprite[] defaultSprites;
        private AsyncOperationHandle<Sprite> levelThemeHandle;
        private bool hasLevelThemeHandle;

        private void Awake()
        {
            CacheTileRenderers();
        }

        public IEnumerator ApplyLevelTheme(int levelId)
        {
            CacheTileRenderers();
            if (levelId != 2)
            {
                ApplySprites(defaultSprites);
                ReleaseLevelTheme();
                yield break;
            }

            AsyncOperationHandle<Sprite> handle =
                UnityAddressables.LoadAssetAsync<Sprite>(levelTwoBackgroundAddress);
            yield return handle;
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                if (handle.IsValid())
                    UnityAddressables.Release(handle);
                ApplySprites(defaultSprites);
                Debug.LogWarning("第二关背景加载失败，本次使用包内默认背景。");
                yield break;
            }

            ReleaseLevelTheme();
            levelThemeHandle = handle;
            hasLevelThemeHandle = true;
            for (int i = 0; i < tileRenderers.Length; i++)
            {
                if (tileRenderers[i] != null)
                    tileRenderers[i].sprite = handle.Result;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            float speed = GameManager.Instance.GetBackgroundSpeed();

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                    continue;

                tiles[i].position += Vector3.left * speed * speedMultiplier * Time.deltaTime;
                if (tiles[i].position.x < -tileWidth)
                    tiles[i].position += Vector3.right * tileWidth * tiles.Length;
            }
        }

        private void CacheTileRenderers()
        {
            if (tileRenderers != null && tileRenderers.Length == tiles.Length)
                return;

            tileRenderers = new SpriteRenderer[tiles.Length];
            defaultSprites = new Sprite[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                    continue;
                tileRenderers[i] = tiles[i].GetComponent<SpriteRenderer>();
                if (tileRenderers[i] != null)
                    defaultSprites[i] = tileRenderers[i].sprite;
            }
        }

        private void ApplySprites(Sprite[] sprites)
        {
            if (sprites == null)
                return;
            for (int i = 0; i < tileRenderers.Length && i < sprites.Length; i++)
            {
                if (tileRenderers[i] != null)
                    tileRenderers[i].sprite = sprites[i];
            }
        }

        private void ReleaseLevelTheme()
        {
            if (!hasLevelThemeHandle)
                return;
            if (levelThemeHandle.IsValid())
                UnityAddressables.Release(levelThemeHandle);
            hasLevelThemeHandle = false;
        }

        private void OnDestroy()
        {
            ReleaseLevelTheme();
        }
    }
}
