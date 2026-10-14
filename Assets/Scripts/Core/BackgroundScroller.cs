using UnityEngine;

namespace WoguaShooter.Core
{
    public class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private Transform[] tiles;
        [SerializeField] private float tileWidth = 20f;
        [SerializeField] private float speedMultiplier = 1f;

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
    }
}
