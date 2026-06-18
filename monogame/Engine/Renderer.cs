using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DY01.Game;
using static DY01.Data.Config;

namespace DY01.Engine;

public class Renderer
{
    public SpriteBatch SpriteBatch { get; private set; }
    public Camera Camera { get; private set; }
    public SpriteFont Font { get; private set; }
    public Texture2D Pixel { get; private set; }
    public GraphicsDevice GraphicsDevice { get; private set; }

    private RenderTarget2D _gameRenderTarget;
    private Dictionary<string, Texture2D> _tileTextures = new();
    private string? _lastTheme;
    private Random _rng = new();

    public Renderer(GraphicsDevice gd, SpriteBatch sb, SpriteFont font)
    {
        GraphicsDevice = gd;
        SpriteBatch = sb;
        Font = font;
        Camera = new Camera();

        Pixel = new Texture2D(gd, 1, 1);
        Pixel.SetData(new[] { Color.White });

        _gameRenderTarget = new RenderTarget2D(gd, W, H);
    }

    public void BeginFrame()
    {
        GraphicsDevice.SetRenderTarget(_gameRenderTarget);
        GraphicsDevice.Clear(Color.Transparent);
    }

    public void EndFrame()
    {
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        SpriteBatch.Draw(_gameRenderTarget,
            new Vector2(Camera.ShakeX, Camera.ShakeY),
            Color.White);
        SpriteBatch.End();
    }

    public void DrawRect(Color color, Rectangle rect)
    {
        SpriteBatch.Draw(Pixel, rect, color);
    }

    public void DrawRect(Color color, int x, int y, int w, int h)
    {
        SpriteBatch.Draw(Pixel, new Rectangle(x, y, w, h), color);
    }

    public void FillRect(Color color, int x, int y, int w, int h)
    {
        SpriteBatch.Draw(Pixel, new Rectangle(x, y, w, h), color);
    }

    public void DrawString(string text, Vector2 pos, Color color, float scale = 1f)
    {
        if (Font != null)
            SpriteBatch.DrawString(Font, text, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    public void DrawStringCentered(string text, Vector2 pos, Color color, float scale = 1f)
    {
        if (Font != null)
        {
            var size = Font.MeasureString(text) * scale;
            SpriteBatch.DrawString(Font, text, new Vector2(pos.X - size.X / 2, pos.Y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }

    public Vector2 MeasureString(string text, float scale = 1f)
    {
        return Font?.MeasureString(text) * scale ?? Vector2.Zero;
    }

    public void DrawLevel(Level level)
    {
        var th = level.Theme;
        int camX = (int)Camera.OffsetX;
        int sc = camX / TILE;
        int ec = sc + (W / TILE) + 2;

        EnsureTileTextures(th);

        for (int y = 0; y < level.H; y++)
        {
            for (int x = Math.Max(0, sc); x < Math.Min(ec, level.W); x++)
            {
                var t = level.Tiles[y][x];
                if (!t.Solid) continue;
                int px = x * TILE - camX;
                int py = y * TILE;

                Texture2D tex;
                if (t.Type == "ground")
                {
                    bool aboveAir = (y == 0) || (!level.Tiles[y - 1][x].Solid);
                    tex = aboveAir ? _tileTextures["grass"] : _tileTextures["dirt"];
                }
                else if (t.Type == "plat")
                    tex = _tileTextures["plank"];
                else
                    tex = _tileTextures["stone"];

                SpriteBatch.Draw(tex, new Vector2(px, py), Color.White);
            }
        }

        // Draw barrels
        foreach (var b in level.Barrels)
        {
            if (!b.Ok) continue;
            int bx = (int)(b.X - camX - 8);
            int by = (int)(b.Y - 8);
            DrawRect(COLOR_BARREL, bx, by, 16, 16);
            DrawRect(new Color(255, 102, 0), bx + 4, by + 4, 8, 8);
            DrawRect(Color.Yellow, bx + 6, by + 2, 4, 4);
        }

        // Draw exit
        int ex = (int)(level.ExitX - camX);
        int ey = (int)level.ExitY;
        float t_ms = Environment.TickCount;
        int pulse = (int)(50 + 20 * Math.Sin(t_ms * 0.004f));

        DrawRect(new Color(0, 255, 100, pulse), ex, ey - TILE * 3, TILE, TILE * 4);
        DrawRect(new Color(0, 255, 100, Math.Max(0, pulse - 30)), ex, ey - TILE * 5, TILE, TILE * 2);
        DrawRect(new Color(0, 200, 0), ex, ey - 32, TILE, TILE * 2); // door frame
    }

    private void EnsureTileTextures(MapTheme theme)
    {
        string key = theme.Name;
        if (key == _lastTheme) return;
        _lastTheme = key;
        _tileTextures.Clear();

        // Grass tile - top layer with grass and dirt below
        _tileTextures["grass"] = CreateTileTexture(TILE, (row, col) =>
        {
            int seed = row * TILE + col;
            Color grassTop = theme.Colors.TryGetValue("ground_top", out var gt) ? gt : new Color(74, 124, 89);
            Color grassDark = new Color(
                Math.Max(0, grassTop.R - 25), Math.Max(0, grassTop.G - 25), Math.Max(0, grassTop.B - 20));
            Color dirt = theme.Colors.TryGetValue("ground", out var gd) ? gd : new Color(101, 67, 33);
            Color dirtDark = new Color(
                Math.Max(0, dirt.R - 20), Math.Max(0, dirt.G - 20), Math.Max(0, dirt.B - 15));

            // Top rows: grass (proportional to tile size)
            int grassRows = Math.Max(3, TILE / 5);
            if (row < grassRows)
            {
                var c = Noise(grassTop, 15, 18, 12, seed);
                // Grass blades
                if (row == 0 && (col % 3 == 0 || col % 5 == 0))
                    c = Noise(new Color(grassTop.R + 20, grassTop.G + 30, grassTop.B + 10), 10, 10, 10, seed);
                return c;
            }
            // Transition row
            if (row == grassRows)
            {
                return col % 2 == 0 ? Noise(grassDark, 10, 12, 8, seed) : Noise(dirt, 12, 10, 10, seed);
            }
            // Dirt with pebbles
            var dc = Noise(dirt, 18, 15, 15, seed);
            if (seed % 11 == 0) dc = Noise(new Color(140, 110, 80), 12, 12, 12, seed);
            if (seed % 17 == 0) dc = Noise(dirtDark, 10, 10, 10, seed);
            if (seed % 23 == 0) dc = Noise(new Color(120, 90, 60), 8, 8, 8, seed);
            return dc;
        });

        // Dirt tile - deeper underground
        _tileTextures["dirt"] = CreateTileTexture(TILE, (row, col) =>
        {
            int seed = row * TILE + col + 1000;
            Color dirt = theme.Colors.TryGetValue("ground", out var gd2) ? gd2 : new Color(101, 67, 33);
            Color dirtDark = new Color(
                Math.Max(0, dirt.R - 25), Math.Max(0, dirt.G - 25), Math.Max(0, dirt.B - 20));
            Color stone = new Color(90, 85, 80);

            var c = Noise(dirt, 20, 18, 18, seed);
            // Pebbles and rocks
            if (seed % 9 == 0) c = Noise(new Color(130, 100, 70), 14, 14, 14, seed + 50);
            if (seed % 13 == 0) c = Noise(dirtDark, 12, 12, 12, seed + 80);
            if (seed % 19 == 0) c = Noise(stone, 10, 10, 10, seed + 120);
            // Root-like patterns
            if (row < 4 && col % 7 == row % 3) c = Noise(new Color(80, 60, 40), 8, 8, 8, seed);
            return c;
        });

        // Stone tile - for walls
        _tileTextures["stone"] = CreateTileTexture(TILE, (row, col) =>
        {
            int seed = row * TILE + col + 2000;
            Color stone = theme.Colors.TryGetValue("wall", out var wl) ? wl : new Color(100, 100, 100);
            Color stoneLight = new Color(
                Math.Min(255, stone.R + 25), Math.Min(255, stone.G + 25), Math.Min(255, stone.B + 25));
            Color stoneDark = new Color(
                Math.Max(0, stone.R - 30), Math.Max(0, stone.G - 30), Math.Max(0, stone.B - 30));

            var c = Noise(stone, 22, 22, 22, seed);
            // Brick-like pattern (proportional to tile size)
            int brickH = Math.Max(4, TILE / 5);
            int brickW = Math.Max(8, TILE / 2);
            int brickRow = row / brickH;
            int brickCol = (col + (brickRow % 2) * (brickW / 2)) / brickW;
            if (row % brickH == 0 || (col + (brickRow % 2) * (brickW / 2)) % brickW == 0)
                c = Noise(stoneDark, 8, 8, 8, seed);
            // Highlights
            if (seed % 8 == 0) c = Noise(stoneLight, 10, 10, 10, seed);
            // Cracks
            if (seed % 31 == 0) c = Noise(stoneDark, 6, 6, 6, seed);
            return c;
        });

        // Plank tile - for platforms
        _tileTextures["plank"] = CreateTileTexture(TILE, (row, col) =>
        {
            int seed = row * TILE + col + 3000;
            Color wood = theme.Colors.TryGetValue("plat", out var pt) ? pt : new Color(139, 90, 43);
            Color woodLight = new Color(
                Math.Min(255, wood.R + 20), Math.Min(255, wood.G + 15), Math.Min(255, wood.B + 10));
            Color woodDark = new Color(
                Math.Max(0, wood.R - 25), Math.Max(0, wood.G - 20), Math.Max(0, wood.B - 15));

            // Wood grain - horizontal lines
            int grain = (row / 3) % 2;
            var c = grain == 0 ? Noise(wood, 14, 10, 8, seed) : Noise(woodLight, 12, 10, 8, seed);
            // Plank separators
            if (row == 0 || row == TILE / 2) c = Noise(woodDark, 6, 4, 3, seed);
            // Nail heads
            if ((row == 2 || row == TILE / 2 + 2) && (col == 2 || col == TILE - 7))
                c = Noise(new Color(60, 60, 60), 5, 5, 5, seed);
            // Knots
            if (seed % 29 == 0) c = Noise(woodDark, 8, 8, 8, seed);
            return c;
        });
    }

    private Texture2D CreateTileTexture(int size, Func<int, int, Color> pixelFunc)
    {
        var tex = new Texture2D(GraphicsDevice, size, size);
        var data = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                data[y * size + x] = pixelFunc(y, x);
        tex.SetData(data);
        return tex;
    }

    private Color Noise(Color base_, int rVar, int gVar, int bVar, int seed)
    {
        int r = Math.Clamp(base_.R + (Hash(seed) % (rVar * 2 + 1)) - rVar, 0, 255);
        int g = Math.Clamp(base_.G + (Hash(seed + 1) % (gVar * 2 + 1)) - gVar, 0, 255);
        int b = Math.Clamp(base_.B + (Hash(seed + 2) % (bVar * 2 + 1)) - bVar, 0, 255);
        return new Color(r, g, b);
    }

    private static int Hash(int n)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + n;
            return h;
        }
    }

    public void DrawBackground(float camX, Dictionary<string, Color>? theme = null)
    {
        if (theme == null)
            theme = new Dictionary<string, Color> { ["sky"] = new Color(26, 26, 46), ["bg_hill"] = new Color(15, 15, 26), ["bg_build"] = new Color(21, 21, 37) };

        // Sky
        GraphicsDevice.Clear(theme["sky"]);

        // Stars
        for (int i = 0; i < 40; i++)
        {
            int sx = Math.Abs(Hash(42 + i)) % W;
            int sy = Math.Abs(Hash(142 + i)) % 300;
            int brightness = 100 + (Math.Abs(Hash(i)) % 156);
            var c = new Color(brightness, brightness, brightness);
            // Need to draw stars on the render target, not via GraphicsDevice
            // We'll draw them in BeginFrame
            DrawRect(c, sx, sy, 2, 2);
        }

        // Far hills (slowest parallax)
        for (int i = 0; i < 6; i++)
        {
            int mx = (int)((i * 280 - camX * 0.03f) % 1300) - 150;
            DrawTriangle(theme["bg_hill"],
                new Vector2(mx, 520), new Vector2(mx + 140, 360), new Vector2(mx + 280, 520));
        }

        // Mid buildings
        for (int i = 0; i < 10; i++)
        {
            int bx = (int)((i * 170 - camX * 0.06f) % 1200) - 50;
            int h = 60 + (Math.Abs(Hash(i)) % 80);
            DrawRect(theme["bg_build"], bx, 450 - h, 28, h);
            DrawRect(theme["bg_build"], bx + 38, 460 - h + 30, 22, h - 30);
        }

        // Near clouds
        for (int i = 0; i < 5; i++)
        {
            int cx = (int)((i * 350 - camX * 0.12f) % 1400) - 100;
            int cy = 80 + (i * 37) % 120;
            // Simple cloud rectangle
            var cloudColor = new Color(255, 255, 255, 30);
            DrawRect(cloudColor, cx, cy, 60 + (i * 23) % 40, 20 + (i * 17) % 15);
        }
    }

    private void DrawTriangle(Color color, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        // Simple triangle using line drawing
        DrawLine(color, v1, v2, 1);
        DrawLine(color, v2, v3, 1);
        DrawLine(color, v3, v1, 1);

        // Fill: draw horizontal lines
        float minY = Math.Min(v1.Y, Math.Min(v2.Y, v3.Y));
        float maxY = Math.Max(v1.Y, Math.Max(v2.Y, v3.Y));
        for (int y = (int)minY; y <= maxY; y++)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            TryIntersect(v1, v2, y, ref minX, ref maxX);
            TryIntersect(v2, v3, y, ref minX, ref maxX);
            TryIntersect(v3, v1, y, ref minX, ref maxX);
            if (minX < maxX)
                DrawRect(color, (int)minX, y, (int)(maxX - minX), 1);
        }
    }

    private void TryIntersect(Vector2 a, Vector2 b, float y, ref float minX, ref float maxX)
    {
        if ((a.Y <= y && b.Y > y) || (b.Y <= y && a.Y > y))
        {
            float t = (y - a.Y) / (b.Y - a.Y);
            float x = a.X + t * (b.X - a.X);
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }
    }

    public void DrawLine(Color color, Vector2 start, Vector2 end, float width = 1)
    {
        float length = Vector2.Distance(start, end);
        float angle = MathF.Atan2(end.Y - start.Y, end.X - start.X);
        SpriteBatch.Draw(Pixel, start, null, color, angle, Vector2.Zero,
            new Vector2(length, width), SpriteEffects.None, 0);
    }

    public void DrawCircle(Color color, Vector2 center, float radius, int thickness = 1)
    {
        int segments = 32;
        float angleStep = MathF.PI * 2 / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;
            Vector2 p1 = center + new Vector2(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius);
            Vector2 p2 = center + new Vector2(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius);
            DrawLine(color, p1, p2, thickness);
        }
    }

    public void DrawCrosshair(float x, float y, Color color)
    {
        DrawLine(color, new Vector2(x - 8, y), new Vector2(x + 8, y), 2);
        DrawLine(color, new Vector2(x, y - 8), new Vector2(x, y + 8), 2);
        DrawCircle(color, new Vector2(x, y), 4, 2);
    }
}