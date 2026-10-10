using System.Collections.Generic;
using Manager.GameManager;
using UnityEngine;

namespace Bosses.Vexara
{
    // The floor of the color intermission: the arena is cut into the squares of the floor grid, each square gets one of a few colors, the colors can be shown and hidden, and a color can be asked for: every square of another color explodes
    public class ColorSquareBoard : MonoBehaviour
    {
        [SerializeField] private Renderer _cellPrefab;
        [SerializeField] private float _gridCellSize = 4f;
        [SerializeField] private float _cellGap = 0.3f;
        [SerializeField] private float _height = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _colorAlpha = 0.5f;
        [SerializeField] private Color _explosionColor = new Color(0.9f, 0.28f, 0.12f, 0.8f);
        [SerializeField] private float _explosionFadeTime = 0.6f;
        [SerializeField] private float _shuffleStepTime = 0.07f;

        private readonly List<Cell> _cells = new List<Cell>();
        private Color[] _palette;
        private MaterialPropertyBlock _block;
        private float _shuffleEndTime;
        private float _nextShuffleStepTime;

        private static readonly int Tint_Color = Shader.PropertyToID("_TintColor");

        private void Update()
        {
            if (_shuffleEndTime > 0f)
                UpdateShuffle();

            foreach (Cell cell in _cells)
            {
                cell.current = new Color(
                    Mathf.MoveTowards(cell.current.r, cell.target.r, cell.speed * Time.deltaTime),
                    Mathf.MoveTowards(cell.current.g, cell.target.g, cell.speed * Time.deltaTime),
                    Mathf.MoveTowards(cell.current.b, cell.target.b, cell.speed * Time.deltaTime),
                    Mathf.MoveTowards(cell.current.a, cell.target.a, cell.speed * Time.deltaTime));
                Paint(cell);
            }
        }

        // arenaRect is (minX, minZ, maxX, maxZ); the squares follow the lines of the floor grid, and the border squares reach the edge of the arena
        public void Begin(Vector4 arenaRect, float groundY, Color[] palette)
        {
            _palette = palette;
            _block = new MaterialPropertyBlock();

            List<float> xs = GetEdges(arenaRect.x, arenaRect.z);
            List<float> zs = GetEdges(arenaRect.y, arenaRect.w);
            List<int> colors = GetShuffledColors((xs.Count - 1) * (zs.Count - 1));
            int index = 0;
            for (int ix = 0; ix < xs.Count - 1; ix++)
            {
                for (int iz = 0; iz < zs.Count - 1; iz++)
                {
                    Cell cell = new Cell { rect = new Vector4(xs[ix], zs[iz], xs[ix + 1], zs[iz + 1]), colorIndex = colors[index++] };
                    cell.renderer = Instantiate(_cellPrefab, transform);

                    // Each square is a flat quad lying on the floor, a little smaller than the square so the grid stays visible
                    Transform cellTransform = cell.renderer.transform;
                    cellTransform.SetPositionAndRotation(new Vector3((cell.rect.x + cell.rect.z) * 0.5f, groundY + _height, (cell.rect.y + cell.rect.w) * 0.5f), Quaternion.Euler(90f, 0f, 0f));
                    cellTransform.localScale = new Vector3(cell.rect.z - cell.rect.x - _cellGap, cell.rect.w - cell.rect.y - _cellGap, 1f);
                    cell.current = Transparent(palette[cell.colorIndex]);
                    cell.target = cell.current;
                    Paint(cell);
                    _cells.Add(cell);
                }
            }
        }

        // Throws the colors away: for a moment every square flickers through random colors, then the squares settle on a new random arrangement and stay lit
        public void Reshuffle(float duration)
        {
            List<int> colors = GetShuffledColors(_cells.Count);
            for (int i = 0; i < _cells.Count; i++)
                _cells[i].colorIndex = colors[i];

            _shuffleEndTime = Time.time + duration;
            _nextShuffleStepTime = 0f;
        }

        public void ShowColors(float fadeTime)
        {
            foreach (Cell cell in _cells)
                SetTarget(cell, WithAlpha(_palette[cell.colorIndex], _colorAlpha), fadeTime);
        }

        public void HideColors(float fadeTime)
        {
            foreach (Cell cell in _cells)
                SetTarget(cell, Transparent(cell.current), fadeTime);
        }

        // Every square of another color explodes (and hurts a player standing on one); the squares of the asked color glow in their color for a moment so the right answer can be learned
        public void Explode(int safeColor, int damage, float revealTime)
        {
            int playerColor = GetColorAt(GameManager.Instance.player.transform.position);
            if (playerColor != safeColor)
                GameManager.Instance.player.DamagePlayer(damage);

            foreach (Cell cell in _cells)
            {
                if (cell.colorIndex == safeColor)
                {
                    cell.current = Transparent(_palette[cell.colorIndex]);
                    SetTarget(cell, WithAlpha(_palette[cell.colorIndex], _colorAlpha), 0.15f);
                    cell.revealUntil = Time.time + revealTime;
                }
                else
                {
                    cell.current = _explosionColor;
                    SetTarget(cell, Transparent(_explosionColor), _explosionFadeTime);
                }
            }

            Invoke(nameof(HideRevealed), revealTime);
        }

        public void Finish(float fadeTime)
        {
            HideColors(fadeTime);
            Destroy(gameObject, fadeTime + 0.2f);
        }

        // -1 when the position is outside every square
        public int GetColorAt(Vector3 position)
        {
            foreach (Cell cell in _cells)
                if (position.x >= cell.rect.x && position.x <= cell.rect.z && position.z >= cell.rect.y && position.z <= cell.rect.w)
                    return cell.colorIndex;

            return -1;
        }

        // The same number of squares of every color, in random places
        private List<int> GetShuffledColors(int count)
        {
            List<int> colors = new List<int>();
            for (int i = 0; i < count; i++)
                colors.Add(i % _palette.Length);

            for (int i = colors.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (colors[i], colors[j]) = (colors[j], colors[i]);
            }

            return colors;
        }

        private void UpdateShuffle()
        {
            if (Time.time >= _shuffleEndTime)
            {
                _shuffleEndTime = 0f;
                foreach (Cell cell in _cells)
                {
                    cell.current = WithAlpha(Color.white, _colorAlpha * 1.6f);
                    SetTarget(cell, WithAlpha(_palette[cell.colorIndex], _colorAlpha), 0.35f);
                }

                return;
            }

            if (Time.time < _nextShuffleStepTime)
                return;

            _nextShuffleStepTime = Time.time + _shuffleStepTime;
            foreach (Cell cell in _cells)
            {
                cell.current = WithAlpha(_palette[Random.Range(0, _palette.Length)], _colorAlpha * 1.2f);
                SetTarget(cell, cell.current, 1f);
            }
        }

        private void HideRevealed()
        {
            foreach (Cell cell in _cells)
                if (cell.revealUntil > 0f)
                {
                    SetTarget(cell, Transparent(cell.current), 0.4f);
                    cell.revealUntil = 0f;
                }
        }

        // Lines of the floor grid inside the range, without the ones so close to the edge that they would leave a sliver
        private List<float> GetEdges(float min, float max)
        {
            List<float> edges = new List<float> { min };
            for (float line = Mathf.Ceil(min / _gridCellSize) * _gridCellSize; line < max; line += _gridCellSize)
                if (line - min > _gridCellSize * 0.5f && max - line > _gridCellSize * 0.5f)
                    edges.Add(line);

            edges.Add(max);
            return edges;
        }

        private static void SetTarget(Cell cell, Color target, float fadeTime)
        {
            cell.target = target;
            cell.speed = 1f / Mathf.Max(fadeTime, 0.01f);
        }

        private static Color Transparent(Color color) => new Color(color.r, color.g, color.b, 0f);

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private void Paint(Cell cell)
        {
            cell.renderer.enabled = cell.current.a > 0.002f;
            cell.renderer.GetPropertyBlock(_block);
            _block.SetColor(Tint_Color, cell.current);
            cell.renderer.SetPropertyBlock(_block);
        }

        private class Cell
        {
            public Renderer renderer;
            public Vector4 rect;
            public int colorIndex;
            public Color current;
            public Color target;
            public float speed;
            public float revealUntil;
        }
    }
}
