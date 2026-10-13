using UnityEngine;

namespace Shared
{
    // A tornado made of a few nested funnels that spin at different speeds. Built in code with a generated texture, so nothing is wired in a scene; the owner moves it and sets how grown it is
    public class Whirlwind : MonoBehaviour
    {
        private const int Segments = 28;
        private const int Rings = 10;
        private const float BaseShare = 0.35f;
        private const int TextureWidth = 256;
        private const int TextureHeight = 128;

        private static Mesh _funnelMesh;
        private static Texture2D _swirlTexture;

        private Layer[] _layers;
        private float _radius;
        private float _height;

        private void Update()
        {
            foreach (Layer layer in _layers)
                layer.transform.Rotate(0f, layer.spinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        // radius is the width at the top of the funnel; the funnel is narrow at the feet
        public static Whirlwind Create(Vector3 position, float radius, float height)
        {
            GameObject root = new GameObject("Whirlwind");
            root.transform.position = position;
            Whirlwind whirlwind = root.AddComponent<Whirlwind>();
            whirlwind._radius = radius;
            whirlwind._height = height;
            whirlwind.Build();
            whirlwind.SetGrowth(0f);
            return whirlwind;
        }

        // 0 is nothing, 1 is the full funnel; it grows up from the ground
        public void SetGrowth(float growth)
        {
            float clamped = Mathf.Clamp01(growth);
            transform.localScale = new Vector3(Mathf.Max(clamped, 0.01f), Mathf.Max(clamped, 0.01f), Mathf.Max(clamped, 0.01f));
            foreach (Layer layer in _layers)
            {
                Color color = layer.baseColor;
                color.a *= clamped;
                layer.material.color = color;
            }
        }

        private void OnDestroy()
        {
            if (_layers == null)
                return;

            foreach (Layer layer in _layers)
                if (layer.material)
                    Destroy(layer.material);
        }

        private void Build()
        {
            _layers = new Layer[]
            {
                CreateLayer(1f, 1f, 260f, new Color(0.8f, 0.7f, 0.55f, 0.95f)),
                CreateLayer(0.78f, 0.92f, -380f, new Color(0.97f, 0.94f, 0.88f, 0.85f)),
                CreateLayer(0.55f, 0.8f, 520f, new Color(0.62f, 0.54f, 0.42f, 0.85f)),
            };
        }

        private Layer CreateLayer(float radiusShare, float heightShare, float spinSpeed, Color color)
        {
            GameObject layerObject = new GameObject("Funnel");
            layerObject.transform.SetParent(transform, false);
            layerObject.transform.localScale = new Vector3(_radius * radiusShare, _height * heightShare, _radius * radiusShare);
            layerObject.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            layerObject.AddComponent<MeshFilter>().sharedMesh = GetFunnelMesh();

            MeshRenderer meshRenderer = layerObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = GetSwirlTexture();
            meshRenderer.sharedMaterial = material;

            Layer layer = new Layer();
            layer.transform = layerObject.transform;
            layer.material = material;
            layer.baseColor = color;
            layer.spinSpeed = spinSpeed;
            return layer;
        }

        // A funnel one unit tall that is BaseShare wide at the bottom and one unit wide at the top
        private static Mesh GetFunnelMesh()
        {
            if (_funnelMesh)
                return _funnelMesh;

            Vector3[] vertices = new Vector3[(Segments + 1) * (Rings + 1)];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[Segments * Rings * 6];
            for (int ring = 0; ring <= Rings; ring++)
            {
                float height = ring / (float)Rings;
                float radius = Mathf.Lerp(BaseShare, 1f, Mathf.Pow(height, 0.8f));
                for (int segment = 0; segment <= Segments; segment++)
                {
                    float angle = segment / (float)Segments * Mathf.PI * 2f;
                    int index = ring * (Segments + 1) + segment;
                    vertices[index] = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                    uvs[index] = new Vector2(segment / (float)Segments, height);
                }
            }

            int triangle = 0;
            for (int ring = 0; ring < Rings; ring++)
            {
                for (int segment = 0; segment < Segments; segment++)
                {
                    int a = ring * (Segments + 1) + segment;
                    int b = a + Segments + 1;
                    triangles[triangle++] = a;
                    triangles[triangle++] = b;
                    triangles[triangle++] = a + 1;
                    triangles[triangle++] = a + 1;
                    triangles[triangle++] = b;
                    triangles[triangle++] = b + 1;
                }
            }

            _funnelMesh = new Mesh { name = "WhirlwindFunnel", vertices = vertices, uv = uvs, triangles = triangles };
            _funnelMesh.RecalculateBounds();
            return _funnelMesh;
        }

        // Soft diagonal streaks that fade out at the top and the bottom; they only show up because the funnel spins
        private static Texture2D GetSwirlTexture()
        {
            if (_swirlTexture)
                return _swirlTexture;

            _swirlTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            _swirlTexture.wrapModeU = TextureWrapMode.Repeat;
            _swirlTexture.wrapModeV = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureHeight; y++)
            {
                float v = y / (float)(TextureHeight - 1);
                float edge = Mathf.SmoothStep(0f, 1f, v / 0.2f) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.25f);
                for (int x = 0; x < TextureWidth; x++)
                {
                    float u = x / (float)TextureWidth;
                    float streaks = Mathf.Sin(Mathf.PI * 2f * (4f * u + 1.6f * v)) * 0.5f + 0.5f;
                    float detail = Mathf.Sin(Mathf.PI * 2f * (9f * u - 2.3f * v + 0.3f)) * 0.5f + 0.5f;
                    float alpha = Mathf.SmoothStep(0.25f, 0.85f, streaks * 0.7f + detail * 0.3f) * edge;
                    _swirlTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            _swirlTexture.Apply();
            return _swirlTexture;
        }

        private class Layer
        {
            public Transform transform;
            public Material material;
            public Color baseColor;
            public float spinSpeed;
        }
    }
}
