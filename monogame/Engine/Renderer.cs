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

        // Draw terrain tiles with depth-based textures
        for (int y = 0; y < level.H; y++)
        {
            for (int x = Math.Max(0, sc); x < Math.Min(ec, level.W); x++)
            {
                var t = level.Tiles[y][x];
                int px = x * TILE - camX;
                int py = y * TILE;

                if (t.Type == "water")
                {
                    // Draw water tile (animated)
                    DrawWaterTile(px, py, x, y);
                    continue;
                }
                if (t.Type == "lava")
                {
                    // Draw lava tile (animated)
                    DrawLavaTile(px, py, x, y);
                    continue;
                }
                if (t.Type == "ice")
                {
                    // Draw ice tile
                    DrawIceTile(px, py, th);
                    continue;
                }

                if (!t.Solid) continue;

                Texture2D tex;
                if (t.Type == "ground")
                {
                    // Depth-based texture selection
                    if (t.Depth == 1)
                        tex = _tileTextures["surface"];
                    else if (t.Depth == 2)
                        tex = _tileTextures["shallow"];
                    else if (t.Depth == 3)
                        tex = _tileTextures["deep"];
                    else
                        tex = _tileTextures["stone"];
                }
                else if (t.Type == "bedrock")
                    tex = _tileTextures["bedrock"];
                else if (t.Type == "plat")
                    tex = _tileTextures["plank"];
                else
                    tex = _tileTextures["stone"];

                SpriteBatch.Draw(tex, new Vector2(px, py), Color.White);

                // Draw damage overlay
                if (t.DamageLevel == 1)
                    DrawCrackOverlay(px, py, x, y, false);
                else if (t.DamageLevel == 2)
                    DrawCrackOverlay(px, py, x, y, true);
            }
        }

        // Draw decorations (behind barrels, in front of terrain)
        DrawDecorations(level, camX);

        // Draw barrels (improved with 3D effect)
        foreach (var b in level.Barrels)
        {
            if (!b.Ok) continue;
            int bx = (int)(b.X - camX - 8);
            int by = (int)(b.Y - 8);
            
            // Barrel shadow
            DrawRect(new Color(0, 0, 0, 40), bx + 2, by + 14, 14, 4);
            
            // Barrel body (cylindrical effect)
            DrawRect(COLOR_BARREL, bx, by, 16, 16);
            // Left highlight
            DrawRect(new Color(180, 140, 80), bx, by, 2, 16);
            // Right shadow
            DrawRect(new Color(100, 70, 40), bx + 14, by, 2, 16);
            // Top rim
            DrawRect(new Color(140, 100, 60), bx, by, 16, 2);
            // Bottom rim
            DrawRect(new Color(100, 70, 40), bx, by + 14, 16, 2);
            
            // Hazard symbol (orange circle with black border)
            DrawRect(new Color(255, 102, 0), bx + 4, by + 4, 8, 8);
            DrawRect(new Color(200, 80, 0), bx + 3, by + 4, 1, 8);
            DrawRect(new Color(200, 80, 0), bx + 12, by + 4, 1, 8);
            DrawRect(new Color(200, 80, 0), bx + 4, by + 3, 8, 1);
            DrawRect(new Color(200, 80, 0), bx + 4, by + 12, 8, 1);
            
            // Exclamation mark
            DrawRect(Color.Yellow, bx + 7, by + 5, 2, 4);
            DrawRect(Color.Yellow, bx + 7, by + 10, 2, 2);
        }

        // Draw exit (simplified - no alpha issues)
        int ex = (int)(level.ExitX - camX);
        int ey = (int)level.ExitY;
        
        // Door frame (stone arch)
        DrawRect(new Color(80, 80, 80), ex - 2, ey - TILE * 3 - 4, TILE + 4, 4); // top
        DrawRect(new Color(80, 80, 80), ex - 2, ey - TILE * 3, 2, TILE * 3); // left
        DrawRect(new Color(80, 80, 80), ex + TILE, ey - TILE * 3, 2, TILE * 3); // right
        
        // Portal glow (solid green, no alpha to avoid white block issue)
        DrawRect(new Color(0, 200, 50), ex, ey - TILE * 3, TILE, TILE * 3);
        DrawRect(new Color(50, 255, 100), ex + 4, ey - TILE * 3 + 4, TILE - 8, TILE * 3 - 8);
    }

    private void EnsureTileTextures(MapTheme theme)
    {
        string key = theme.Name;
        if (key == _lastTheme) return;
        _lastTheme = key;
        _tileTextures.Clear();

        // Surface tile - top layer with grass/snow/sand and dirt below
        _tileTextures["surface"] = CreateTileTexture(TILE, (row, col) =>
        {
            int seed = row * TILE + col;
            Color surfaceTop = theme.Colors.TryGetValue("ground_top", out var gt) ? gt : new Color(74, 124, 89);
            Color surfaceDark = new Color(
                Math.Max(0, surfaceTop.R - 25), Math.Max(0, surfaceTop.G - 25), Math.Max(0, surfaceTop.B - 20));
            Color dirt = theme.Colors.TryGetValue("ground", out var gd) ? gd : new Color(101, 67, 33);
            Color dirtDark = new Color(
                Math.Max(0, dirt.R - 20), Math.Max(0, dirt.G - 20), Math.Max(0, dirt.B - 15));

            // Top rows: surface material (proportional to tile size)
            int surfaceRows = Math.Max(4, TILE / 4);
            if (row < surfaceRows)
            {
                var c = Noise(surfaceTop, 15, 18, 12, seed);
                // Surface texture details (grass blades / snow sparkle / sand grains)
                if (row == 0 && (col % 3 == 0 || col % 5 == 0))
                    c = Noise(new Color(surfaceTop.R + 20, surfaceTop.G + 30, surfaceTop.B + 10), 10, 10, 10, seed);
                // Small surface details
                if (seed % 7 == 0)
                    c = Noise(new Color(surfaceTop.R - 10, surfaceTop.G - 10, surfaceTop.B - 5), 8, 8, 8, seed);
                return c;
            }
            // Transition row
            if (row == surfaceRows)
            {
                return col % 2 == 0 ? Noise(surfaceDark, 10, 12, 8, seed) : Noise(dirt, 12, 10, 10, seed);
            }
            // Dirt with pebbles
            var dc = Noise(dirt, 18, 15, 15, seed);
            if (seed % 11 == 0) dc = Noise(new Color(140, 110, 80), 12, 12, 12, seed);
            if (seed % 17 == 0) dc = Noise(dirtDark, 10, 10, 10, seed);
            if (seed % 23 == 0) dc = Noise(new Color(120, 90, 60), 8, 8, 8, seed);
            return dc;
        });

        // Shallow underground tile
        _tileTextures["shallow"] = CreateTileTexture(TILE, (row, col) =>
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

        // Deep underground tile
        _tileTextures["deep"] = CreateTileTexture(TILE, (row, col) =>
        {
            int seed = row * TILE + col + 2000;
            Color stone = new Color(80, 75, 70);
            Color stoneDark = new Color(60, 55, 50);
            Color stoneLight = new Color(100, 95, 90);

            var c = Noise(stone, 18, 18, 18, seed);
            // More stone, less dirt
            if (seed % 7 == 0) c = Noise(stoneDark, 12, 12, 12, seed + 50);
            if (seed % 11 == 0) c = Noise(stoneLight, 10, 10, 10, seed + 80);
            // Crystal veins
            if (seed % 29 == 0) c = Noise(new Color(120, 130, 140), 8, 8, 8, seed);
            // Dark cracks
            if (row % 5 == 0 && col % 4 == 0) c = Noise(stoneDark, 6, 6, 6, seed);
            return c;
        });

        // Bedrock tile
        _tileTextures["bedrock"] = CreateTileTexture(TILE, (row, col) =>
        {
            int seed = row * TILE + col + 3000;
            Color bedrock = new Color(50, 45, 40);
            Color bedrockDark = new Color(35, 30, 25);
            Color bedrockLight = new Color(65, 60, 55);

            var c = Noise(bedrock, 15, 15, 15, seed);
            // Heavy texture
            if (seed % 5 == 0) c = Noise(bedrockDark, 10, 10, 10, seed + 50);
            if (seed % 8 == 0) c = Noise(bedrockLight, 8, 8, 8, seed + 100);
            // Iron veins
            if (seed % 23 == 0) c = Noise(new Color(100, 80, 60), 6, 6, 6, seed);
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

    private void DrawCrackOverlay(int px, int py, int tileX, int tileY, bool heavyDamage)
    {
        // 使用tile坐标作为随机种子，确保裂纹位置一致
        int seed = tileX * 1000 + tileY;
        
        if (heavyDamage)
        {
            // 严重受损 - 深色裂纹和缺口
            Color crackDark = new Color(0, 0, 0, 120);
            Color crackLight = new Color(255, 255, 255, 40);
            
            // 主裂纹 - 从中心向外延伸
            int centerX = TILE / 2 + (Hash(seed) % 6) - 3;
            int centerY = TILE / 2 + (Hash(seed + 1) % 6) - 3;
            
            // 绘制多条裂纹
            for (int i = 0; i < 4; i++)
            {
                int angle = (Hash(seed + i * 10) % 360);
                float rad = angle * MathF.PI / 180f;
                int length = 6 + (Hash(seed + i * 20) % 5);
                
                for (int j = 0; j < length; j++)
                {
                    int dx = (int)(MathF.Cos(rad) * j);
                    int dy = (int)(MathF.Sin(rad) * j);
                    int crackX = centerX + dx;
                    int crackY = centerY + dy;
                    
                    if (crackX >= 0 && crackX < TILE && crackY >= 0 && crackY < TILE)
                    {
                        DrawRect(crackDark, px + crackX, py + crackY, 1, 1);
                        // 裂纹边缘高光
                        if (j % 2 == 0 && crackX + 1 < TILE)
                            DrawRect(crackLight, px + crackX + 1, py + crackY, 1, 1);
                    }
                }
            }
            
            // 缺口 - 深色方块
            for (int i = 0; i < 3; i++)
            {
                int chipX = Math.Abs(Hash(seed + i * 30)) % (TILE - 3);
                int chipY = Math.Abs(Hash(seed + i * 40)) % (TILE - 3);
                int chipW = 2 + (Math.Abs(Hash(seed + i * 50)) % 2);
                int chipH = 2 + (Math.Abs(Hash(seed + i * 60)) % 2);
                
                DrawRect(crackDark, px + chipX, py + chipY, chipW, chipH);
            }
            
            // 边缘破损效果
            if (Hash(seed + 100) % 2 == 0)
            {
                DrawRect(crackDark, px, py, TILE, 2); // 顶部破损
            }
            if (Hash(seed + 101) % 2 == 0)
            {
                DrawRect(crackDark, px, py + TILE - 2, TILE, 2); // 底部破损
            }
        }
        else
        {
            // 轻微受损 - 细小裂纹
            Color crack = new Color(0, 0, 0, 80);
            Color crackHighlight = new Color(255, 255, 255, 30);
            
            // 1-2条细裂纹
            int crackCount = 1 + (Hash(seed) % 2);
            
            for (int i = 0; i < crackCount; i++)
            {
                int startX = Math.Abs(Hash(seed + i * 100)) % TILE;
                int startY = Math.Abs(Hash(seed + i * 200)) % TILE;
                int length = 3 + (Hash(seed + i * 300) % 4);
                int angle = (Hash(seed + i * 400) % 360);
                float rad = angle * MathF.PI / 180f;
                
                for (int j = 0; j < length; j++)
                {
                    int dx = (int)(MathF.Cos(rad) * j);
                    int dy = (int)(MathF.Sin(rad) * j);
                    int crackX = startX + dx;
                    int crackY = startY + dy;
                    
                    if (crackX >= 0 && crackX < TILE && crackY >= 0 && crackY < TILE)
                    {
                        DrawRect(crack, px + crackX, py + crackY, 1, 1);
                        // 细微高光
                        if (j == length - 1 && crackX + 1 < TILE)
                            DrawRect(crackHighlight, px + crackX + 1, py + crackY, 1, 1);
                    }
                }
            }
        }
    }

    private void DrawDecorations(Level level, int camX)
    {
        int sc = camX / TILE;
        int ec = sc + (W / TILE) + 2;

        foreach (var dec in level.Decorations)
        {
            if (dec.TX < sc - 2 || dec.TX > ec + 2) continue;
            int px = dec.TX * TILE - camX;
            int py = dec.TY * TILE;

            switch (dec.Kind)
            {
                case "tree":
                    DrawTree(px, py, dec.Variant, level.Theme);
                    break;
                case "bush":
                    DrawBush(px, py, dec.Variant, level.Theme);
                    break;
                case "flower":
                    DrawFlower(px, py, dec.Variant);
                    break;
                case "rock":
                    DrawRock(px, py, dec.Variant, level.Theme);
                    break;
                case "cactus":
                    DrawCactus(px, py, dec.Variant);
                    break;
                case "ice":
                    DrawIce(px, py, dec.Variant);
                    break;
                case "lava_pool":
                    DrawLavaPool(px, py, dec.Variant);
                    break;
                case "mushroom":
                    DrawMushroom(px, py, dec.Variant);
                    break;
                case "lake":
                    DrawLakeSurface(px, py, dec.Width, dec.Variant, level.Theme);
                    break;
            }
        }
    }

    private void DrawTree(int px, int py, int variant, MapTheme theme)
    {
        // Ground shadow
        DrawRect(new Color(0, 0, 0, 40), px + TILE / 2 - 12, py - 2, 24, 4);

        // Tree trunk with more detail
        Color trunk = new Color(101, 67, 33);
        Color trunkDark = new Color(80, 50, 20);
        Color trunkLight = new Color(139, 90, 43);
        int trunkW = 6;
        int trunkH = 35 + variant * 6;
        int trunkX = px + TILE / 2 - trunkW / 2;
        int trunkY = py - trunkH;

        // Trunk base (wider)
        DrawRect(trunkDark, trunkX - 1, py - 4, trunkW + 2, 4);
        
        // Main trunk
        DrawRect(trunk, trunkX, trunkY, trunkW, trunkH);
        DrawRect(trunkDark, trunkX, trunkY, 1, trunkH);
        DrawRect(trunkLight, trunkX + trunkW - 1, trunkY, 1, trunkH);
        
        // Bark texture
        for (int i = 0; i < trunkH; i += 8)
        {
            int barkW = 2 + (i % 3);
            DrawRect(trunkDark, trunkX + 2, trunkY + i, barkW, 1);
        }

        // Tree canopy - rounded shape using multiple layers
        if (variant < 3)
        {
            // Regular tree - rounded canopy
            Color leaf = theme.Name == "雪山" ? new Color(100, 160, 100) : new Color(34, 139, 34);
            Color leafLight = new Color(50, 180, 50);
            Color leafDark = new Color(0, 100, 0);
            Color leafHighlight = new Color(100, 220, 100);

            int canopyR = 14 + variant * 3;
            int canopyCX = px + TILE / 2;
            int canopyCY = trunkY - canopyR + 4;

            // Draw rounded canopy using stacked rectangles
            for (int layer = 0; layer < canopyR * 2; layer += 2)
            {
                float t = (float)layer / (canopyR * 2);
                int layerW = (int)(canopyR * 2 * Math.Sin(t * Math.PI));
                if (layerW < 2) continue;
                
                int layerY = canopyCY - canopyR + layer;
                Color c = layer < canopyR ? leafDark : leaf;
                if (layer > canopyR / 2 && layer < canopyR * 3 / 2)
                    c = leafLight;
                
                DrawRect(c, canopyCX - layerW / 2, layerY, layerW, 2);
            }

            // Highlight spots on top
            DrawRect(leafHighlight, canopyCX - 4, canopyCY - canopyR + 4, 3, 2);
            DrawRect(leafHighlight, canopyCX + 2, canopyCY - canopyR + 6, 2, 2);

            // Snow on top for snow map
            if (theme.Name == "雪山")
            {
                Color snow = new Color(255, 255, 255);
                Color snowShadow = new Color(220, 230, 240);
                
                // Snow cap
                for (int i = 0; i < canopyR; i += 2)
                {
                    float t = (float)i / canopyR;
                    int snowW = (int)(canopyR * 1.5 * Math.Sin(t * Math.PI * 0.7));
                    if (snowW < 2) continue;
                    DrawRect(snowShadow, canopyCX - snowW / 2, canopyCY - canopyR + i - 2, snowW, 2);
                    DrawRect(snow, canopyCX - snowW / 2, canopyCY - canopyR + i - 3, snowW, 2);
                }
            }
        }
        else
        {
            // Pine tree - triangular with layered branches
            Color pine = new Color(0, 128, 0);
            Color pineLight = new Color(34, 139, 34);
            Color pineDark = new Color(0, 80, 0);
            Color pineHighlight = new Color(100, 200, 100);

            int layers = 5;
            int baseWidth = 28 + variant * 4;
            
            for (int i = 0; i < layers; i++)
            {
                int layerW = baseWidth - i * 4;
                int layerH = 8;
                int layerX = px + TILE / 2 - layerW / 2;
                int layerY = trunkY - (layers - i) * (layerH - 1);

                // Layer with rounded edges
                DrawRect(pineDark, layerX, layerY + 2, layerW, layerH - 2);
                DrawRect(pine, layerX + 1, layerY, layerW - 2, layerH - 2);
                DrawRect(pineLight, layerX + 2, layerY - 1, layerW - 4, layerH - 3);
                
                // Highlight
                DrawRect(pineHighlight, layerX + 3, layerY + 1, 4, 2);

                // Snow on pine
                if (theme.Name == "雪山")
                {
                    Color snow = new Color(255, 255, 255);
                    DrawRect(snow, layerX + 2, layerY - 2, layerW - 4, 3);
                }
            }
        }
    }

    private void DrawBush(int px, int py, int variant, MapTheme theme)
    {
        // Ground shadow
        DrawRect(new Color(0, 0, 0, 40), px + TILE / 2 - 10, py - 2, 20, 3);

        // Bush colors based on theme
        Color bush = theme.Name == "雪山" ? new Color(100, 160, 100) : new Color(34, 139, 34);
        Color bushLight = new Color(50, 180, 50);
        Color bushDark = new Color(0, 100, 0);
        Color bushHighlight = new Color(100, 220, 100);

        int bushW = 18 + variant * 4;
        int bushH = 14 + variant * 3;
        int bushX = px + TILE / 2 - bushW / 2;
        int bushY = py - bushH;

        // Draw rounded bush shape using stacked rectangles
        for (int layer = 0; layer < bushH; layer += 2)
        {
            float t = (float)layer / bushH;
            int layerW = (int)(bushW * Math.Sin(t * Math.PI));
            if (layerW < 2) continue;
            
            int layerY = bushY + layer;
            Color c = layer < bushH / 3 ? bushDark : (layer < bushH * 2 / 3 ? bush : bushLight);
            
            DrawRect(c, bushX + (bushW - layerW) / 2, layerY, layerW, 2);
        }

        // Highlight spots
        DrawRect(bushHighlight, bushX + 4, bushY + 4, 3, 2);
        DrawRect(bushHighlight, bushX + bushW - 7, bushY + 5, 2, 2);

        // Berries on bush (red, more visible)
        if (variant == 0)
        {
            Color berry = new Color(220, 20, 60);
            Color berryHighlight = new Color(255, 100, 100);
            DrawRect(berry, bushX + 3, bushY + 4, 3, 3);
            DrawRect(berryHighlight, bushX + 4, bushY + 4, 1, 1);
            DrawRect(berry, bushX + bushW - 6, bushY + 5, 3, 3);
            DrawRect(berryHighlight, bushX + bushW - 5, bushY + 5, 1, 1);
            DrawRect(berry, bushX + bushW / 2, bushY + 6, 3, 3);
            DrawRect(berryHighlight, bushX + bushW / 2 + 1, bushY + 6, 1, 1);
        }
    }

    private void DrawFlower(int px, int py, int variant)
    {
        // Ground shadow
        DrawRect(new Color(0, 0, 0, 30), px + TILE / 2 - 3, py - 1, 6, 2);

        Color[] flowerColors = {
            new Color(220, 80, 80),   // red
            new Color(220, 180, 60),  // yellow
            new Color(180, 80, 200),  // purple
            new Color(80, 150, 220),  // blue
        };
        Color petal = flowerColors[variant % flowerColors.Length];
        Color petalDark = new Color(petal.R - 40, petal.G - 40, petal.B - 40);
        Color center = new Color(240, 220, 80);
        Color centerDark = new Color(200, 180, 40);

        int stemH = 8;
        int stemX = px + TILE / 2;
        int stemY = py - stemH;

        // Stem with slight curve
        DrawRect(new Color(60, 120, 40), stemX, stemY, 1, stemH);
        DrawRect(new Color(80, 140, 60), stemX + 1, stemY + 2, 1, stemH - 2);

        // Leaf on stem
        DrawRect(new Color(60, 120, 40), stemX - 2, stemY + 4, 2, 1);
        DrawRect(new Color(80, 140, 60), stemX - 3, stemY + 3, 2, 1);

        // Flower head - draw petals in cross pattern
        int petalSize = 3;
        // Top petal
        DrawRect(petal, stemX - 1, stemY - petalSize - 1, 2, petalSize);
        DrawRect(petalDark, stemX - 1, stemY - petalSize - 1, 2, 1);
        // Bottom petal
        DrawRect(petal, stemX - 1, stemY + 1, 2, petalSize);
        DrawRect(petalDark, stemX - 1, stemY + petalSize, 2, 1);
        // Left petal
        DrawRect(petal, stemX - petalSize - 1, stemY - 1, petalSize, 2);
        DrawRect(petalDark, stemX - petalSize - 1, stemY - 1, 1, 2);
        // Right petal
        DrawRect(petal, stemX + 1, stemY - 1, petalSize, 2);
        DrawRect(petalDark, stemX + petalSize, stemY - 1, 1, 2);

        // Center
        DrawRect(center, stemX - 1, stemY - 1, 2, 2);
        DrawRect(centerDark, stemX, stemY, 1, 1);
    }

    private void DrawRock(int px, int py, int variant, MapTheme theme)
    {
        // Ground shadow
        DrawRect(new Color(0, 0, 0, 40), px + TILE / 2 - 6, py - 1, 12, 2);

        Color rock;
        if (theme.Name == "火山")
            rock = new Color(60, 40, 35);
        else if (theme.Name == "雪山")
            rock = new Color(140, 150, 160);
        else if (theme.Name == "沙地")
            rock = new Color(160, 130, 80);
        else
            rock = new Color(100, 100, 100);

        Color rockLight = new Color(Math.Min(255, rock.R + 30), Math.Min(255, rock.G + 30), Math.Min(255, rock.B + 30));
        Color rockDark = new Color(Math.Max(0, rock.R - 25), Math.Max(0, rock.G - 25), Math.Max(0, rock.B - 25));

        int rockW = 10 + variant * 3;
        int rockH = 7 + variant * 2;
        int rockX = px + TILE / 2 - rockW / 2;
        int rockY = py - rockH;

        // Draw rounded rock shape
        for (int layer = 0; layer < rockH; layer += 1)
        {
            float t = (float)layer / rockH;
            int layerW = (int)(rockW * Math.Sin(t * Math.PI * 0.9 + 0.1));
            if (layerW < 1) continue;
            
            int layerY = rockY + layer;
            Color c = layer < rockH / 3 ? rockDark : (layer < rockH * 2 / 3 ? rock : rockLight);
            
            DrawRect(c, rockX + (rockW - layerW) / 2, layerY, layerW, 1);
        }

        // Highlight on top
        DrawRect(rockLight, rockX + rockW / 3, rockY + 1, rockW / 3, 1);

        // Skull variant for desert
        if (variant == 3 && theme.Name == "沙地")
        {
            Color bone = new Color(220, 210, 190);
            Color boneDark = new Color(180, 170, 150);
            DrawRect(bone, rockX + 1, rockY - 3, 6, 4); // skull
            DrawRect(boneDark, rockX + 2, rockY - 2, 1, 1); // eye
            DrawRect(boneDark, rockX + 5, rockY - 2, 1, 1); // eye
            DrawRect(bone, rockX + 3, rockY + 1, 3, 2); // jaw
        }
    }

    private void DrawCactus(int px, int py, int variant)
    {
        // Ground shadow
        DrawRect(new Color(0, 0, 0, 40), px + TILE / 2 - 6, py - 2, 12, 3);

        Color cactus = new Color(60, 130, 60);
        Color cactusLight = new Color(80, 160, 80);
        Color cactusDark = new Color(40, 100, 40);
        Color cactusHighlight = new Color(120, 180, 120);

        int bodyW = 6;
        int bodyH = variant == 0 ? 16 : 12;
        int bodyX = px + TILE / 2 - bodyW / 2;
        int bodyY = py - bodyH;

        // Main body with rounded top
        DrawRect(cactus, bodyX, bodyY + 2, bodyW, bodyH - 2);
        DrawRect(cactusLight, bodyX + 1, bodyY + 2, 1, bodyH - 2);
        DrawRect(cactusDark, bodyX + bodyW - 1, bodyY + 2, 1, bodyH - 2);
        
        // Rounded top
        DrawRect(cactus, bodyX + 1, bodyY, bodyW - 2, 2);
        DrawRect(cactusLight, bodyX + 2, bodyY, 1, 1);

        // Arms with rounded ends
        if (variant == 0)
        {
            // Left arm
            DrawRect(cactus, bodyX - 5, bodyY + 4, 5, 3);
            DrawRect(cactus, bodyX - 5, bodyY + 2, 3, 3);
            DrawRect(cactusLight, bodyX - 4, bodyY + 4, 1, 3);
            DrawRect(cactusDark, bodyX - 1, bodyY + 4, 1, 3);
            
            // Right arm
            DrawRect(cactus, bodyX + bodyW, bodyY + 6, 5, 3);
            DrawRect(cactus, bodyX + bodyW + 2, bodyY + 4, 3, 3);
            DrawRect(cactusLight, bodyX + bodyW + 1, bodyY + 6, 1, 3);
            DrawRect(cactusDark, bodyX + bodyW + 4, bodyY + 6, 1, 3);
        }

        // Spines (more visible)
        Color spine = new Color(220, 220, 180);
        DrawRect(spine, bodyX - 1, bodyY + 3, 1, 1);
        DrawRect(spine, bodyX + bodyW, bodyY + 5, 1, 1);
        DrawRect(spine, bodyX - 1, bodyY + 8, 1, 1);
        DrawRect(spine, bodyX + bodyW, bodyY + 10, 1, 1);
        DrawRect(spine, bodyX + 2, bodyY - 1, 1, 1);
    }

    private void DrawIce(int px, int py, int variant)
    {
        Color ice = new Color(180, 210, 240, 180);
        Color iceLight = new Color(220, 240, 255, 200);
        Color iceDark = new Color(140, 180, 220, 160);

        int iceW = 10 + variant * 4;
        int iceH = 3;
        int iceX = px + TILE / 2 - iceW / 2;
        int iceY = py - iceH;

        DrawRect(ice, iceX, iceY, iceW, iceH);
        DrawRect(iceLight, iceX + 1, iceY, iceW / 3, 1);
        DrawRect(iceDark, iceX + iceW / 2, iceY + iceH - 1, iceW / 3, 1);

        // Sparkle
        DrawRect(new Color(255, 255, 255, 220), iceX + 2, iceY, 1, 1);
        DrawRect(new Color(255, 255, 255, 180), iceX + iceW - 3, iceY, 1, 1);
    }

    private void DrawLavaPool(int px, int py, int variant)
    {
        float t = Environment.TickCount * 0.003f;
        int pulse = (int)(20 * Math.Sin(t + variant));

        Color lava = new Color(220 + pulse, 100 + pulse / 2, 20);
        Color lavaHot = new Color(255, 180 + pulse, 60);
        Color lavaDark = new Color(180, 60, 10);

        int poolW = 12 + variant * 4;
        int poolH = 4;
        int poolX = px + TILE / 2 - poolW / 2;
        int poolY = py - poolH;

        DrawRect(lava, poolX, poolY, poolW, poolH);
        DrawRect(lavaHot, poolX + 2, poolY, poolW / 3, 2);
        DrawRect(lavaDark, poolX, poolY + poolH - 1, poolW, 1);

        // Bubbles
        int bubbleX = poolX + 3 + (int)(Math.Sin(t * 2) * 2);
        DrawRect(lavaHot, bubbleX, poolY + 1, 2, 2);
    }

    private void DrawMushroom(int px, int py, int variant)
    {
        // Ground shadow
        DrawRect(new Color(0, 0, 0, 35), px + TILE / 2 - 6, py - 1, 12, 2);

        Color[] capColors = {
            new Color(180, 60, 60),   // red
            new Color(160, 120, 60),  // brown
            new Color(140, 80, 160),  // purple
        };
        Color cap = capColors[variant % capColors.Length];
        Color capLight = new Color(Math.Min(255, cap.R + 40), Math.Min(255, cap.G + 40), Math.Min(255, cap.B + 30));
        Color capDark = new Color(Math.Max(0, cap.R - 30), Math.Max(0, cap.G - 30), Math.Max(0, cap.B - 30));
        Color stem = new Color(220, 210, 190);
        Color stemDark = new Color(190, 180, 160);
        Color stemLight = new Color(240, 235, 220);

        int stemW = 4;
        int stemH = 6;
        int stemX = px + TILE / 2 - stemW / 2;
        int stemY = py - stemH - 5;

        // Stem with rounded shape
        DrawRect(stem, stemX, stemY, stemW, stemH);
        DrawRect(stemDark, stemX, stemY, 1, stemH);
        DrawRect(stemLight, stemX + stemW - 1, stemY, 1, stemH);

        // Cap - rounded mushroom shape
        int capW = 10 + variant * 2;
        int capH = 5;
        int capX = px + TILE / 2 - capW / 2;
        int capY = stemY - capH + 1;

        // Draw rounded cap using stacked rectangles
        for (int layer = 0; layer < capH; layer++)
        {
            float t = (float)layer / capH;
            int layerW = (int)(capW * Math.Sin(t * Math.PI));
            if (layerW < 2) continue;
            
            int layerY = capY + layer;
            Color c = layer < capH / 2 ? capLight : cap;
            if (layer == capH - 1) c = capDark;
            
            DrawRect(c, capX + (capW - layerW) / 2, layerY, layerW, 1);
        }

        // Spots on cap (white dots)
        Color spot = new Color(240, 230, 210);
        DrawRect(spot, capX + 2, capY + 1, 2, 2);
        DrawRect(spot, capX + capW - 4, capY + 1, 2, 2);
        if (capW > 10)
        {
            DrawRect(spot, capX + capW / 2 - 1, capY + 2, 2, 1);
        }
    }

    private void DrawWaterTile(int px, int py, int tileX, int tileY)
    {
        float t = Environment.TickCount * 0.002f;
        int wave = (int)(Math.Sin(t + tileX * 0.5f) * 2);

        // Draw opaque dark backing first so water is visible against sky
        Color waterBacking = new Color(15, 40, 80);
        DrawRect(waterBacking, px, py - 2, TILE, TILE + 4);

        // Bright, opaque water colors
        Color water = new Color(30, 100, 200);
        Color waterLight = new Color(80, 170, 255);
        Color waterDark = new Color(15, 60, 140);
        Color waterHighlight = new Color(180, 220, 255);

        // Base water (fully opaque)
        DrawRect(water, px, py + wave, TILE, TILE);

        // Wave highlights
        int waveOffset = (int)(Math.Sin(t * 1.5f + tileX * 0.3f) * 3);
        DrawRect(waterLight, px + 2 + waveOffset, py + 3 + wave, 6, 2);
        DrawRect(waterLight, px + 10 - waveOffset, py + 8 + wave, 5, 2);

        // Bright surface highlight line
        DrawRect(waterHighlight, px + 1, py + 1 + wave, TILE - 2, 1);

        // Darker bottom
        DrawRect(waterDark, px, py + TILE - 4 + wave, TILE, 4);
    }

    private void DrawLavaTile(int px, int py, int tileX, int tileY)
    {
        float t = Environment.TickCount * 0.003f;
        int pulse = (int)(Math.Sin(t + tileX * 0.4f) * 15);

        // Dark backing for depth
        DrawRect(new Color(100, 30, 5), px, py - 2, TILE, TILE + 4);

        // Bright, opaque lava colors
        Color lava = new Color(220 + pulse, 80 + pulse / 2, 20);
        Color lavaHot = new Color(255, 160 + pulse, 40);
        Color lavaDark = new Color(150, 40, 10);
        Color lavaGlow = new Color(255, 200, 80);

        // Base lava (fully opaque)
        DrawRect(lava, px, py, TILE, TILE);

        // Hot spots
        int hotOffset = (int)(Math.Sin(t * 2 + tileX) * 2);
        DrawRect(lavaHot, px + 3 + hotOffset, py + 4, 5, 4);
        DrawRect(lavaHot, px + 12 - hotOffset, py + 10, 4, 3);

        // Bright glow spots
        DrawRect(lavaGlow, px + 5 + hotOffset, py + 5, 2, 2);

        // Darker edges
        DrawRect(lavaDark, px, py, TILE, 2);
        DrawRect(lavaDark, px, py + TILE - 2, TILE, 2);
    }

    private void DrawIceTile(int px, int py, MapTheme theme)
    {
        Color ice = new Color(180, 220, 240);
        Color iceLight = new Color(220, 240, 255);
        Color iceDark = new Color(140, 180, 210);

        // Base ice
        DrawRect(ice, px, py, TILE, TILE);

        // Light reflections
        DrawRect(iceLight, px + 2, py + 3, 4, 2);
        DrawRect(iceLight, px + 10, py + 8, 5, 2);
        DrawRect(iceLight, px + 5, py + 14, 3, 2);

        // Darker cracks
        DrawRect(iceDark, px + 7, py + 5, 1, 6);
        DrawRect(iceDark, px + 13, py + 10, 1, 5);
    }

    private void DrawLakeSurface(int px, int py, int width, int variant, MapTheme theme)
    {
        float t = Environment.TickCount * 0.002f;

        // Draw shore/bank edges first (sandy/dirt border)
        Color shore = new Color(139, 119, 101);
        Color shoreDark = new Color(100, 80, 60);
        Color shoreLight = new Color(180, 160, 140);

        // Left shore
        DrawRect(shoreDark, px - 3, py - 2, 4, TILE * 2 + 4);
        DrawRect(shore, px - 2, py - 1, 3, TILE * 2 + 2);
        DrawRect(shoreLight, px - 1, py, 1, TILE * 2);

        // Right shore
        int rightX = px + width * TILE;
        DrawRect(shoreDark, rightX, py - 2, 4, TILE * 2 + 4);
        DrawRect(shore, rightX + 1, py - 1, 3, TILE * 2 + 2);
        DrawRect(shoreLight, rightX + 2, py, 1, TILE * 2);

        // Water surface with waves - fully opaque and bright
        for (int i = 0; i < width; i++)
        {
            int wx = px + i * TILE;
            int wave = (int)(Math.Sin(t + i * 0.6f) * 2);

            // Bright, opaque water colors
            Color water = new Color(30, 100, 200);
            Color waterLight = new Color(80, 170, 255);
            Color waterDark = new Color(15, 60, 140);
            Color waterHighlight = new Color(180, 220, 255);

            // Dark backing for depth
            DrawRect(waterDark, wx, py + wave - 2, TILE, TILE * 2 + 4);

            // Water body (fully opaque)
            DrawRect(water, wx, py + wave, TILE, TILE * 2);

            // Surface highlight
            DrawRect(waterLight, wx + 3, py + 2 + wave, TILE - 6, 3);

            // Bright surface line
            DrawRect(waterHighlight, wx + 1, py + 1 + wave, TILE - 2, 1);

            // Sparkle
            if ((i + (int)(t * 2)) % 3 == 0)
                DrawRect(new Color(255, 255, 255), wx + 8, py + 4 + wave, 2, 2);
        }

        // Add reeds/cattails at shore edges
        Color reed = new Color(80, 120, 60);
        Color reedDark = new Color(60, 90, 40);
        Color reedTip = new Color(139, 90, 43);

        // Left reeds
        for (int r = 0; r < 3; r++)
        {
            int rx = px - 1 + r * 2;
            int ry = py + (int)(Math.Sin(t + r) * 2);
            int rh = 12 + r * 3;
            DrawRect(reedDark, rx, ry - rh, 1, rh);
            DrawRect(reed, rx + 1, ry - rh + 2, 1, rh - 2);
            // Cattail tip
            DrawRect(reedTip, rx, ry - rh - 3, 2, 4);
        }

        // Right reeds
        for (int r = 0; r < 3; r++)
        {
            int rx = rightX + r * 2;
            int ry = py + (int)(Math.Sin(t + r + 1) * 2);
            int rh = 10 + r * 2;
            DrawRect(reedDark, rx, ry - rh, 1, rh);
            DrawRect(reed, rx + 1, ry - rh + 2, 1, rh - 2);
            DrawRect(reedTip, rx, ry - rh - 3, 2, 4);
        }

        // Water lilies on surface
        Color lilyPad = new Color(34, 139, 34);
        Color lilyPadDark = new Color(0, 100, 0);
        Color lilyFlower = new Color(255, 182, 193);
        Color lilyCenter = new Color(255, 215, 0);

        // Place 1-2 lilies based on width
        int lilyCount = Math.Min(2, width / 3);
        for (int l = 0; l < lilyCount; l++)
        {
            int lx = px + (l + 1) * TILE * width / (lilyCount + 1);
            int ly = py + TILE / 2 + (int)(Math.Sin(t * 0.5f + l) * 2);

            // Lily pad (oval)
            DrawRect(lilyPadDark, lx - 4, ly, 8, 4);
            DrawRect(lilyPad, lx - 3, ly + 1, 6, 2);
            DrawRect(lilyPadDark, lx - 2, ly + 2, 4, 1);

            // Flower on top
            if (l == 0)
            {
                DrawRect(lilyFlower, lx - 2, ly - 3, 4, 3);
                DrawRect(lilyCenter, lx - 1, ly - 2, 2, 1);
            }
        }
    }

    public void DrawBackground(float camX, Dictionary<string, Color>? theme = null, string themeName = "")
    {
        if (theme == null)
            theme = new Dictionary<string, Color> { ["sky"] = new Color(26, 26, 46), ["bg_hill"] = new Color(15, 15, 26), ["bg_build"] = new Color(21, 21, 37) };

        // Sky
        GraphicsDevice.Clear(theme["sky"]);

        // Stars (more visible at night)
        for (int i = 0; i < 50; i++)
        {
            int sx = Math.Abs(Hash(42 + i)) % W;
            int sy = Math.Abs(Hash(142 + i)) % 350;
            int brightness = 120 + (Math.Abs(Hash(i)) % 136);
            var c = new Color(brightness, brightness, brightness);
            DrawRect(c, sx, sy, 2, 2);
        }

        // Theme-specific background elements
        if (themeName == "雪山")
        {
            // Snow mountains - far layer
            for (int i = 0; i < 5; i++)
            {
                int mx = (int)((i * 320 - camX * 0.02f) % 1400) - 200;
                DrawSnowMountain(theme["bg_hill"], mx, 380, 180, 200);
            }
            // Snow mountains - mid layer
            for (int i = 0; i < 4; i++)
            {
                int mx = (int)((i * 280 - camX * 0.05f) % 1200) - 100;
                DrawSnowMountain(theme["bg_build"], mx, 420, 150, 160);
            }
            // Snowflakes in background
            for (int i = 0; i < 30; i++)
            {
                int sx = (int)((i * 47 - camX * 0.08f) % W);
                int sy = (int)((i * 73 + Environment.TickCount * 0.02f) % (H - 100));
                DrawRect(new Color(255, 255, 255, 80), sx, sy, 2, 2);
            }
        }
        else if (themeName == "沙地")
        {
            // Desert dunes - far layer
            for (int i = 0; i < 6; i++)
            {
                int dx = (int)((i * 260 - camX * 0.03f) % 1300) - 150;
                DrawDesertDune(theme["bg_hill"], dx, 450, 160, 80);
            }
            // Desert dunes - mid layer
            for (int i = 0; i < 5; i++)
            {
                int dx = (int)((i * 220 - camX * 0.06f) % 1200) - 80;
                DrawDesertDune(theme["bg_build"], dx, 480, 140, 60);
            }
            // Distant pyramids
            for (int i = 0; i < 2; i++)
            {
                int px = (int)((i * 500 - camX * 0.04f) % 1100) - 50;
                DrawPyramid(new Color(120, 90, 40), px, 400, 80, 100);
            }
        }
        else if (themeName == "火山")
        {
            // Volcanic mountains - far layer
            for (int i = 0; i < 5; i++)
            {
                int mx = (int)((i * 300 - camX * 0.025f) % 1350) - 180;
                DrawVolcanicMountain(new Color(40, 20, 15), mx, 400, 170, 180);
            }
            // Lava glow on horizon
            for (int i = 0; i < 3; i++)
            {
                int gx = (int)((i * 400 - camX * 0.04f) % 1200) - 100;
                DrawLavaGlow(gx, 480, 120, 60);
            }
            // Embers floating
            for (int i = 0; i < 25; i++)
            {
                int ex = (int)((i * 53 - camX * 0.1f) % W);
                int ey = (int)((i * 89 - Environment.TickCount * 0.03f) % (H - 150));
                int alpha = 100 + (int)(Math.Sin(Environment.TickCount * 0.005f + i) * 50);
                DrawRect(new Color(255, 100, 0, alpha), ex, ey, 2, 2);
            }
        }
        else // 平原 or default
        {
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
        }

        // Near clouds (all themes)
        for (int i = 0; i < 5; i++)
        {
            int cx = (int)((i * 350 - camX * 0.12f) % 1400) - 100;
            int cy = 80 + (i * 37) % 120;
            var cloudColor = new Color(255, 255, 255, 30);
            DrawRect(cloudColor, cx, cy, 60 + (i * 23) % 40, 20 + (i * 17) % 15);
        }
    }

    private void DrawSnowMountain(Color color, int x, int y, int w, int h)
    {
        // Mountain body
        DrawTriangle(color, new Vector2(x, y + h), new Vector2(x + w / 2, y), new Vector2(x + w, y + h));
        // Snow cap
        Color snow = new Color(240, 245, 250);
        DrawTriangle(snow, new Vector2(x + w / 2 - 20, y + 30), new Vector2(x + w / 2, y), new Vector2(x + w / 2 + 20, y + 30));
    }

    private void DrawDesertDune(Color color, int x, int y, int w, int h)
    {
        // Rounded dune shape
        for (int i = 0; i < h; i += 2)
        {
            float t = (float)i / h;
            int layerW = (int)(w * Math.Sin(t * Math.PI));
            if (layerW < 2) continue;
            DrawRect(color, x + (w - layerW) / 2, y + i, layerW, 2);
        }
    }

    private void DrawPyramid(Color color, int x, int y, int w, int h)
    {
        DrawTriangle(color, new Vector2(x, y + h), new Vector2(x + w / 2, y), new Vector2(x + w, y + h));
        // Shadow side
        Color shadow = new Color(color.R - 30, color.G - 30, color.B - 30);
        DrawTriangle(shadow, new Vector2(x + w / 2, y), new Vector2(x + w, y + h), new Vector2(x + w / 2, y + h));
    }

    private void DrawVolcanicMountain(Color color, int x, int y, int w, int h)
    {
        DrawTriangle(color, new Vector2(x, y + h), new Vector2(x + w / 2, y), new Vector2(x + w, y + h));
        // Crater at top
        Color crater = new Color(60, 30, 20);
        DrawRect(crater, x + w / 2 - 15, y, 30, 10);
    }

    private void DrawLavaGlow(int x, int y, int w, int h)
    {
        float t = Environment.TickCount * 0.002f;
        int pulse = (int)(Math.Sin(t) * 20);
        Color glow = new Color(255, 80 + pulse, 0, 100);
        for (int i = 0; i < h; i += 2)
        {
            float ratio = (float)i / h;
            int layerW = (int)(w * (1 - ratio * 0.5f));
            DrawRect(glow, x + (w - layerW) / 2, y + i, layerW, 2);
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