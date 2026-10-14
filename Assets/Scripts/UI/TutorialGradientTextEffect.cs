using UnityEngine;
using UnityEngine.UI;

namespace WoguaShooter.UI
{
    [RequireComponent(typeof(Text))]
    public class TutorialGradientTextEffect : BaseMeshEffect
    {
        [SerializeField] private Color32 pink = new Color32(255, 112, 190, 255);
        [SerializeField] private Color32 blue = new Color32(74, 205, 255, 255);

        private float snakeAmplitude = 5f;
        private float snakePhase;
        private float bendAngle = 2f;

        public void SetSnakeMotion(float amplitude, float phase, float angle)
        {
            snakeAmplitude = Mathf.Max(0f, amplitude);
            snakePhase = phase;
            bendAngle = Mathf.Max(0f, angle);
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper.currentVertCount == 0)
                return;

            UIVertex vertex = default;
            int glyphCount = vertexHelper.currentVertCount / 4;
            for (int glyphIndex = 0; glyphIndex < glyphCount; glyphIndex++)
            {
                int firstVertex = glyphIndex * 4;
                float minX = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;
                Vector2 center = Vector2.zero;

                for (int corner = 0; corner < 4; corner++)
                {
                    vertexHelper.PopulateUIVertex(ref vertex, firstVertex + corner);
                    minX = Mathf.Min(minX, vertex.position.x);
                    maxX = Mathf.Max(maxX, vertex.position.x);
                    center += (Vector2)vertex.position;
                }
                center *= 0.25f;

                float wave = Mathf.Sin(snakePhase + glyphIndex * 0.82f);
                float angle = Mathf.Cos(snakePhase + glyphIndex * 0.82f) * bendAngle;
                float radians = angle * Mathf.Deg2Rad;
                float cosine = Mathf.Cos(radians);
                float sine = Mathf.Sin(radians);

                for (int corner = 0; corner < 4; corner++)
                {
                    vertexHelper.PopulateUIVertex(ref vertex, firstVertex + corner);
                    float horizontal = Mathf.InverseLerp(minX, maxX, vertex.position.x);
                    if ((glyphIndex & 1) != 0)
                        horizontal = 1f - horizontal;
                    horizontal = Mathf.SmoothStep(0f, 1f, horizontal);

                    Color32 gradientColor = Color32.Lerp(pink, blue, horizontal);
                    gradientColor.a = (byte)(gradientColor.a * vertex.color.a / 255);
                    vertex.color = gradientColor;

                    Vector2 local = (Vector2)vertex.position - center;
                    Vector2 bent = new Vector2(
                        local.x * cosine - local.y * sine,
                        local.x * sine + local.y * cosine);
                    Vector2 position = center + bent + Vector2.up * (wave * snakeAmplitude);
                    vertex.position = new Vector3(position.x, position.y, vertex.position.z);
                    vertexHelper.SetUIVertex(vertex, firstVertex + corner);
                }
            }
        }
    }
}
