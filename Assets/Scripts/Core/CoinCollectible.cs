using System;
using System.Collections;
using UnityEngine;
using WoguaShooter.Lua;

namespace WoguaShooter.Core
{
    public class CoinCollectible : MonoBehaviour
    {
        public static event Action<CoinCollectible> Collected;

        private SpriteRenderer coinRenderer;
        private Collider2D coinCollider;
        private PlayerController targetPlayer;
        private float baseY;
        private float phase;
        private bool attracting;
        private bool collected;

        public bool IsTutorialTarget { get; private set; }

        public static CoinCollectible Create(Vector3 position)
        {
            GameObject coinObject = new GameObject("Coin");
            coinObject.transform.position = position;

            SpriteRenderer renderer = coinObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GameplaySpriteLibrary.Get("coin");
            renderer.sortingOrder = 12;
            ScaleToWidth(coinObject.transform, renderer.sprite, 0.3f);

            CircleCollider2D collider = coinObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.42f;

            return coinObject.AddComponent<CoinCollectible>();
        }

        public void MarkTutorialTarget()
        {
            IsTutorialTarget = true;
        }

        private void Awake()
        {
            GameManager.RegisterLevelObject(gameObject);
            coinRenderer = GetComponent<SpriteRenderer>();
            coinCollider = GetComponent<Collider2D>();
            targetPlayer = FindFirstObjectByType<PlayerController>();
            baseY = transform.position.y;
            phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (collected || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            if (targetPlayer == null)
                targetPlayer = FindFirstObjectByType<PlayerController>();

            if (!attracting && targetPlayer != null && targetPlayer.CanMagnetAttract(transform.position))
                attracting = true;

            if (attracting && targetPlayer != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPlayer.transform.position,
                    15f * Time.deltaTime);
                transform.Rotate(Vector3.forward, 720f * Time.deltaTime);
                if (Vector2.Distance(transform.position, targetPlayer.transform.position) <= 0.16f)
                    Collect();
            }
            else
            {
                transform.position += Vector3.left * GameManager.Instance.GetBackgroundSpeed() * Time.deltaTime;
                transform.position = new Vector3(transform.position.x,
                    baseY + Mathf.Sin(Time.time * 4f + phase) * 0.035f, transform.position.z);
                float spin = 0.35f + Mathf.Abs(Mathf.Sin(Time.time * 5f + phase)) * 0.65f;
                Vector3 scale = transform.localScale;
                transform.localScale = new Vector3(Mathf.Abs(scale.y) * spin, scale.y, scale.z);
            }

            GameManager.RequestPhysicsTransformSync();
            if (transform.position.x < -12f)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!collected && other.GetComponentInParent<PlayerController>() != null)
                Collect();
        }

        private void Collect()
        {
            if (collected)
                return;

            collected = true;
            if (coinCollider != null)
                coinCollider.enabled = false;
            int score = LuaHotUpdateRuntime.Instance != null
                ? LuaHotUpdateRuntime.Instance.GetInt("CombatRules", "coinScore", 1)
                : 1;
            GameManager.Instance?.AddScore(Mathf.Max(0, score));
            AudioManager.Instance?.PlayCoinCollect();
            Collected?.Invoke(this);
            StartCoroutine(CollectFeedback());
        }

        private IEnumerator CollectFeedback()
        {
            const float duration = 0.14f;
            float time = 0f;
            Vector3 startScale = transform.localScale;
            Color startColor = coinRenderer != null ? coinRenderer.color : Color.white;
            while (time < duration)
            {
                time += Time.deltaTime;
                float progress = Mathf.Clamp01(time / duration);
                transform.localScale = startScale * Mathf.Lerp(1f, 1.65f, progress);
                if (coinRenderer != null)
                    coinRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            GameManager.UnregisterLevelObject(gameObject);
        }

        private static void ScaleToWidth(Transform target, Sprite sprite, float width)
        {
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 1f;
            target.localScale = Vector3.one * (width / Mathf.Max(0.01f, spriteWidth));
        }
    }
}
