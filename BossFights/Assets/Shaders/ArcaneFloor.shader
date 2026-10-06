Shader "Arena/ArcaneFloor"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.03, 0.02, 0.07, 1)
        _NebulaColorA ("Nebula Color A", Color) = (0.38, 0.1, 0.75, 1)
        _NebulaColorB ("Nebula Color B", Color) = (0.1, 0.22, 0.85, 1)
        _NebulaStrength ("Nebula Strength", Range(0, 2)) = 0.5
        _StarCell ("Star Cell Size", Float) = 0.5
        _StarDensity ("Star Density", Range(0, 1)) = 0.14
        _StarBrightness ("Star Brightness", Range(0, 6)) = 2.5
        _GridColor ("Grid Color", Color) = (0.55, 0.3, 1, 1)
        _GridSize ("Grid Cell Size", Float) = 4
        _GridStrength ("Grid Strength", Range(0, 3)) = 0.3
        _RuneColor ("Rune Color", Color) = (0.75, 0.45, 1, 1)
        _RuneStrength ("Rune Strength", Range(0, 6)) = 1.6
        _RuneCenter ("Rune Center (x, z)", Vector) = (0, 0, 0, 0)
        _RuneRadius ("Rune Radius", Float) = 8
        _ArenaRect ("Arena Rect (minX, minZ, maxX, maxZ)", Vector) = (-18, -12, 18, 10)
        _EdgeColor ("Edge Color", Color) = (0.75, 0.35, 1, 1)
        _EdgeStrength ("Edge Glow", Range(0, 6)) = 1.6
        _EdgeLine ("Edge Line", Range(0, 3)) = 0.8
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
        };

        fixed4 _BaseColor;
        fixed4 _NebulaColorA;
        fixed4 _NebulaColorB;
        float _NebulaStrength;
        float _StarCell;
        float _StarDensity;
        float _StarBrightness;
        fixed4 _GridColor;
        float _GridSize;
        float _GridStrength;
        fixed4 _RuneColor;
        float _RuneStrength;
        float4 _RuneCenter;
        float _RuneRadius;
        float4 _ArenaRect;
        fixed4 _EdgeColor;
        float _EdgeStrength;
        float _EdgeLine;
        half _Smoothness;
        half _Metallic;

        float hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float valueNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = hash21(i);
            float b = hash21(i + float2(1, 0));
            float c = hash21(i + float2(0, 1));
            float d = hash21(i + float2(1, 1));
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        float fbm(float2 p)
        {
            float v = 0.0;
            float a = 0.5;
            for (int i = 0; i < 4; i++)
            {
                v += a * valueNoise(p);
                p = p * 2.03 + 11.7;
                a *= 0.5;
            }
            return v;
        }

        float lineGlow(float distance, float width)
        {
            float core = smoothstep(width, 0.0, distance);
            float halo = smoothstep(width * 6.0, 0.0, distance) * 0.25;
            return core + halo;
        }

        // Distance from p to the outline of a regular polygon with n sides (circumradius R), rotated by rot
        float polygonOutline(float2 p, float n, float R, float rot)
        {
            float a = atan2(p.y, p.x) + rot;
            float sector = 6.2831853 / n;
            float d = cos(floor(0.5 + a / sector) * sector - a) * length(p);
            float apothem = R * cos(sector * 0.5);
            return abs(d - apothem);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 p = IN.worldPos.xz;
            float t = _Time.y;

            // Nebula: slow drifting clouds in two tints
            float cloud = fbm(p * 0.07 + float2(t * 0.012, -t * 0.008));
            float tint = fbm(p * 0.05 + 40.0 - t * 0.006);
            float cloudMask = smoothstep(0.38, 0.85, cloud);
            float3 nebula = lerp(_NebulaColorA.rgb, _NebulaColorB.rgb, tint) * cloudMask * _NebulaStrength;

            // Stars: one possible twinkling dot per cell
            float2 cellId = floor(p / _StarCell);
            float2 cellUv = frac(p / _StarCell);
            float starRand = hash21(cellId);
            float2 starPos = float2(hash21(cellId + 7.1), hash21(cellId + 19.3)) * 0.6 + 0.2;
            float star = step(1.0 - _StarDensity, starRand) * smoothstep(0.11, 0.0, length(cellUv - starPos));
            float twinkle = 0.55 + 0.45 * sin(t * (1.5 + 3.0 * hash21(cellId + 3.3)) + starRand * 60.0);
            float3 starColor = lerp(float3(0.8, 0.85, 1.0), float3(0.85, 0.55, 1.0), hash21(cellId + 5.5));
            float3 stars = starColor * star * twinkle * _StarBrightness;

            // Faint tile grid
            float2 g = abs(frac(p / _GridSize) - 0.5) * _GridSize;
            float gridDistance = _GridSize * 0.5 - max(g.x, g.y);
            float3 grid = _GridColor.rgb * smoothstep(0.05, 0.0, gridDistance) * _GridStrength;

            // Rune circle: rings, a spinning dashed ring and a hexagram
            float2 rp = p - _RuneCenter.xz;
            float r = length(rp);
            float angle = atan2(rp.y, rp.x);
            float R = _RuneRadius;
            float rune = lineGlow(abs(r - R), 0.07);
            rune += lineGlow(abs(r - R * 0.93), 0.03) * step(frac((angle + t * 0.12) / 6.2831853 * 40.0), 0.55);
            rune += lineGlow(abs(r - R * 0.5), 0.05);
            rune += lineGlow(abs(r - R * 0.12), 0.05);
            float spin = t * 0.08;
            rune += lineGlow(polygonOutline(rp, 3.0, R * 0.9, spin), 0.045);
            rune += lineGlow(polygonOutline(rp, 3.0, R * 0.9, spin + 3.1415927), 0.045);
            float runeFade = smoothstep(R * 1.08, R * 0.97, r) + smoothstep(R * 1.12, R * 1.0, r) * 0.0;
            float3 runes = _RuneColor.rgb * rune * runeFade * _RuneStrength;

            // Glowing border where the floor meets the walls
            float edgeDistance = min(min(p.x - _ArenaRect.x, _ArenaRect.z - p.x), min(p.y - _ArenaRect.y, _ArenaRect.w - p.y));
            float3 edge = _EdgeColor.rgb * (exp(-edgeDistance * 1.4) * _EdgeStrength + smoothstep(0.12, 0.0, abs(edgeDistance - 0.25)) * _EdgeLine);

            float variation = 0.75 + 0.5 * cloud;
            o.Albedo = _BaseColor.rgb * variation;
            o.Emission = nebula + stars + grid + runes + edge;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
