using System;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Platform.Console_
{
    /// <summary>
    /// Console 平台渲染器实现
    /// </summary>
    public sealed class ConsoleRenderer : IRenderer
    {
        private readonly int _width;
        private readonly int _height;
        private readonly float _worldMinX = -50f, _worldMaxX = 50f;
        private readonly float _worldMinZ = -50f, _worldMaxZ = 50f;
        private readonly char[,] _buffer;
        private bool _dirty = true;
        private string _cachedOutput;

        public const char Empty = '.';
        public const char Border = '=';
        public const char Player = 'P';
        public const char Enemy = 'E';
        public const char Projectile = '*';

        public ConsoleRenderer(int width = 80, int height = 40)
        {
            _width = width;
            _height = height;
            _buffer = new char[_height, _width];
            ClearMap();
        }

        public int Width => _width;
        public int Height => _height;

        public void Clear()
        {
            System.Console.Clear();
            _dirty = true;
        }

        public void DrawText(int x, int y, string text)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;

            for (int i = 0; i < text.Length && x + i < _width; i++)
            {
                _buffer[y, x + i] = text[i];
            }
            _dirty = true;
        }

        public void DrawRect(int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || x + width > _width || y + height > _height) return;

            for (int i = 0; i < width; i++)
            {
                _buffer[y, x + i] = Border;
                _buffer[y + height - 1, x + i] = Border;
            }
            for (int i = 0; i < height; i++)
            {
                _buffer[y + i, x] = '|';
                _buffer[y + i, x + width - 1] = '|';
            }

            _dirty = true;
        }

        public void DrawLine(int x1, int y1, int x2, int y2)
        {
            var dx = System.Math.Abs(x2 - x1);
            var dy = System.Math.Abs(y2 - y1);
            var sx = x1 < x2 ? 1 : -1;
            var sy = y1 < y2 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                if (x1 >= 0 && x1 < _width && y1 >= 0 && y1 < _height)
                {
                    _buffer[y1, x1] = '*';
                }

                if (x1 == x2 && y1 == y2) break;

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
            _dirty = true;
        }

        public void Present()
        {
            if (!_dirty && _cachedOutput != null)
            {
                System.Console.Write(_cachedOutput);
                return;
            }

            var sb = new System.Text.StringBuilder();
            ClearMap();

            sb.AppendLine(new string(Border, _width));
            for (int y = 0; y < _height; y++)
            {
                sb.Append('|');
                for (int x = 0; x < _width; x++)
                    sb.Append(_buffer[y, x]);
                sb.AppendLine("|");
            }
            sb.AppendLine(new string(Border, _width));

            _cachedOutput = sb.ToString();
            _dirty = false;
            System.Console.Write(_cachedOutput);
        }

        public (int px, int py) WorldToScreen(float worldX, float worldZ)
        {
            var usableWidth = _width;
            var usableHeight = _height;
            var px = (int)((worldX - _worldMinX) / (_worldMaxX - _worldMinX) * usableWidth);
            var py = (int)((_worldMaxZ - worldZ) / (_worldMaxZ - _worldMinZ) * usableHeight);
            px = System.Math.Clamp(px, 0, _width - 1);
            py = System.Math.Clamp(py, 0, _height - 1);
            return (px, py);
        }

        private void ClearMap()
        {
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    _buffer[y, x] = Empty;
        }

        public void MarkDirty() => _dirty = true;
    }
}
