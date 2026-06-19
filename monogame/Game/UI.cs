using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DY01.Data;
using DY01.Engine;
using DY01.Entities;

namespace DY01.Game;

public class MenuScreen
{
    public static readonly (string mode, string name, string desc)[] MENU_ITEMS = new[]
    {
        ("campaign", "战役模式", "单人闯关，消灭所有敌人进入下一层"),
        ("endless", "无尽模式", "层层推进，敌人越来越强"),
        ("coop", "双人合作", "和朋友一起战斗"),
    };

    public int Selected = 0;
    public float Time = 0;
    private List<(float x, float y, float speed, int size, int brightness)> _stars = new();
    private Random _rng = new();

    public MenuScreen()
    {
        for (int i = 0; i < 80; i++)
            _stars.Add((_rng.Next(0, Config.W), _rng.Next(0, Config.H),
                (float)(_rng.NextDouble() * 0.8f + 0.2f),
                _rng.Next(1, 4), _rng.Next(40, 180)));
    }

    public void Update()
    {
        Time += 0.016f;
        for (int i = 0; i < _stars.Count; i++)
        {
            var s = _stars[i];
            s.y += s.speed;
            if (s.y > Config.H) { s.y = -5; s.x = _rng.Next(0, Config.W); }
            _stars[i] = s;
        }
    }

    public void MoveUp()
    {
        Selected = (Selected - 1 + MENU_ITEMS.Length) % MENU_ITEMS.Length;
        AudioManager.MenuMove();
    }

    public void MoveDown()
    {
        Selected = (Selected + 1) % MENU_ITEMS.Length;
        AudioManager.MenuMove();
    }

    public string Confirm()
    {
        AudioManager.MenuSelect();
        return MENU_ITEMS[Selected].mode;
    }

    public void Draw(Renderer renderer)
    {
        var sb = renderer.SpriteBatch;
        var font = renderer.Font;
        var pixel = renderer.Pixel;

        // Background gradient with animated color shift
        for (int y = 0; y < Config.H; y++)
        {
            float t = y / (float)Config.H;
            int r = (int)(8 + t * 12 + Math.Sin(Time * 0.5f) * 3);
            int g = (int)(8 + t * 12);
            int b = (int)(20 + t * 15 + Math.Cos(Time * 0.7f) * 5);
            renderer.DrawRect(new Color(r, g, b), 0, y, Config.W, 1);
        }

        // Stars with twinkling effect
        foreach (var s in _stars)
        {
            float twinkle = (float)Math.Sin(Time * 2 + s.x * 0.1f) * 0.3f + 0.7f;
            int brightness = (int)(s.brightness * twinkle);
            var sc = new Color(brightness, brightness, Math.Min(255, brightness + 40));
            renderer.DrawRect(sc, (int)s.x, (int)s.y, s.size, s.size);
        }

        // Animated scan lines
        int scanY1 = (int)(Time * 120) % Config.H;
        int scanY2 = (int)(Time * 80 + 200) % Config.H;
        renderer.DrawRect(new Color(255, 255, 255, 8), 0, scanY1, Config.W, 2);
        renderer.DrawRect(new Color(255, 51, 51, 5), 0, scanY2, Config.W, 1);

        // Title with enhanced glow effect
        string title = "DY01";
        var titleColor = new Color(255, 51, 51);
        var titleGlow = new Color(255, 0, 0);
        float glowX = Config.W / 2f + (float)Math.Sin(Time * 1.8f) * 3;
        int glowA = (int)(40 + Math.Sin(Time * 3f) * 20);
        
        // Multiple glow layers
        renderer.DrawStringCentered(title, new Vector2(glowX - 2, 100), titleGlow * ((float)glowA / 512f), 2.6f);
        renderer.DrawStringCentered(title, new Vector2(glowX + 2, 100), titleGlow * ((float)glowA / 512f), 2.6f);
        renderer.DrawStringCentered(title, new Vector2(glowX, 100), titleGlow * ((float)glowA / 255f), 2.5f);
        renderer.DrawStringCentered(title, new Vector2(Config.W / 2f, 100), titleColor, 2.5f);

        // Subtitle with pulsing effect
        string sub = "硬核像素射击";
        int subA = (int)(170 + Math.Sin(Time * 2.4f) * 50);
        renderer.DrawStringCentered(sub, new Vector2(Config.W / 2f, 180), Config.COLOR_GOLD * ((float)subA / 255f), 1.5f);

        // Animated divider with gradient
        for (int x = -200; x < 200; x++)
        {
            float dist = Math.Abs(x) / 200f;
            int alpha = (int)(60 * (1 - dist) * (0.5f + 0.5f * Math.Sin(Time * 2 + x * 0.02f)));
            renderer.DrawRect(new Color(255, 51, 51, alpha), Config.W / 2 + x, 215, 1, 1);
        }

        // Menu items with enhanced selection effect
        for (int i = 0; i < MENU_ITEMS.Length; i++)
        {
            bool isSel = i == Selected;
            float baseY = 270 + i * 80;

            if (isSel)
            {
                // Animated selection background
                float pulse = 0.5f + 0.5f * (float)Math.Sin(Time * 4f);
                int bgAlpha = (int)(25 + pulse * 15);
                renderer.DrawRect(new Color(255, 51, 51, bgAlpha), Config.W / 2 - 200, (int)baseY - 10, 400, 60);
                
                // Animated border
                int borderAlpha = (int)(100 + pulse * 80);
                renderer.DrawRect(new Color(255, 51, 51, borderAlpha), Config.W / 2 - 200, (int)baseY - 10, 400, 2);
                renderer.DrawRect(new Color(255, 51, 51, borderAlpha), Config.W / 2 - 200, (int)baseY + 48, 400, 2);
                
                // Animated arrows
                float indX = Config.W / 2f - 210 + (float)Math.Sin(Time * 6f) * 5;
                renderer.DrawString(">", new Vector2(indX, baseY + 5), new Color(255, 51, 51), 1.5f);
                renderer.DrawString("<", new Vector2(Config.W / 2f + 210 - 20, baseY + 5), new Color(255, 51, 51), 1.5f);
            }

            var color = isSel ? Config.COLOR_WHITE : Config.COLOR_GRAY;
            renderer.DrawStringCentered(MENU_ITEMS[i].name, new Vector2(Config.W / 2f, baseY), color, 1.5f);

            var descColor = isSel ? new Color(160, 160, 160) : Config.COLOR_DARK_GRAY;
            renderer.DrawStringCentered(MENU_ITEMS[i].desc, new Vector2(Config.W / 2f, baseY + 32), descColor, 0.8f);
        }

        // Bottom hints with better styling
        renderer.DrawRect(new Color(0, 0, 0, 100), 0, Config.H - 80, Config.W, 80);
        renderer.DrawStringCentered("W/S or ↑↓ Select  Enter/Space Confirm", new Vector2(Config.W / 2f, Config.H - 60), new Color(150, 150, 150), 0.8f);
        renderer.DrawStringCentered("P1: WASD Move | J Shoot | K Melee | Space Jump | Shift Dash | R Reload | 1-5 Weapons", new Vector2(Config.W / 2f, Config.H - 40), new Color(120, 120, 120), 0.7f);
        renderer.DrawStringCentered("P2: Arrows Move | Num1 Shoot | Num2 Melee | Num0 Jump | Num. Dash | 34567 Weapons", new Vector2(Config.W / 2f, Config.H - 24), new Color(120, 120, 120), 0.7f);
        
        // Version tag with style
        renderer.DrawString("MonoGame .NET", new Vector2(10, Config.H - 16), new Color(80, 80, 80), 0.7f);
    }
}

public class HUD
{
    public void Draw(Renderer renderer, Player? player, Player? player2, int score, int kills, string waveLabel, string modeName, int combo = 0, float comboMultiplier = 1f, Level? level = null)
    {
        var sb = renderer.SpriteBatch;
        var font = renderer.Font;
        var pixel = renderer.Pixel;

        // P1 HP with enhanced styling
        if (player != null)
        {
            // Background panel with gradient effect
            renderer.DrawRect(new Color(0, 0, 0, 180), 8, 8, 200, 56);
            renderer.DrawRect(new Color(255, 51, 51, 100), 8, 8, 200, 2);
            renderer.DrawRect(new Color(255, 51, 51, 50), 8, 62, 200, 2);
            
            float hpR = player.Hp / player.MaxHpActual;
            // HP bar background
            renderer.DrawRect(Config.COLOR_HP_BG, 16, 14, 184, 12);
            // HP bar with gradient effect
            var hpColor = hpR > 0.3f ? Config.COLOR_HP : new Color(255, 100, 0);
            renderer.DrawRect(hpColor, 16, 14, (int)(184 * hpR), 12);
            // HP bar highlight
            renderer.DrawRect(new Color(255, 255, 255, 80), 16, 14, (int)(184 * hpR), 3);

            renderer.DrawString($"HP {(int)player.Hp}/{(int)player.MaxHpActual}", new Vector2(20, 28), Color.White, 0.8f);
            renderer.DrawString($"KILLS {kills}", new Vector2(20, 42), new Color(255, 200, 100), 0.8f);
        }

        // Score/Wave with enhanced styling
        renderer.DrawRect(new Color(0, 0, 0, 180), Config.W - 208, 8, 200, 40);
        renderer.DrawRect(new Color(255, 170, 0, 100), Config.W - 208, 8, 200, 2);
        renderer.DrawRect(new Color(255, 170, 0, 50), Config.W - 208, 46, 200, 2);
        renderer.DrawString($"SCORE {score}", new Vector2(Config.W - 200, 10), new Color(255, 220, 100), 0.8f);
        renderer.DrawString($"{waveLabel}  {modeName}", new Vector2(Config.W - 200, 28), Color.White, 0.8f);

        // Combo display
        if (combo > 0)
        {
            float time = (float)Environment.TickCount / 1000f;
            float pulse = 1f + (float)Math.Sin(time * 8) * 0.1f;
            int comboAlpha = Math.Min(255, 150 + combo * 10);
            
            // Combo background
            renderer.DrawRect(new Color(255, 51, 51, 60), Config.W - 208, 52, 200, 36);
            renderer.DrawRect(new Color(255, 51, 51, 150), Config.W - 208, 52, 200, 2);
            
            // Combo text
            string comboText = $"COMBO {combo}";
            string multText = $"x{comboMultiplier:F1}";
            
            var comboColor = new Color(255, 100 + combo * 5, 100 + combo * 5, comboAlpha);
            renderer.DrawString(comboText, new Vector2(Config.W - 200, 56), comboColor, 0.9f * pulse);
            renderer.DrawString(multText, new Vector2(Config.W - 100, 56), new Color(255, 220, 100, comboAlpha), 0.9f * pulse);
        }

        // Weapon bar with enhanced styling
        if (player != null)
        {
            int ww = 300, wh = 40;
            int wx0 = Config.W / 2 - ww / 2;
            int wy0 = Config.H - 54;

            // Background with gradient
            renderer.DrawRect(new Color(0, 0, 0, 230), wx0, wy0, ww, wh);
            renderer.DrawRect(Config.COLOR_GOLD, wx0, wy0, ww, 2);
            renderer.DrawRect(new Color(255, 170, 0, 100), wx0, wy0 + wh - 2, ww, 2);

            for (int i = 0; i < 5; i++)
            {
                int boxX = wx0 + 8 + i * 58;
                var wp = player.Weapons[i];
                bool sel = player.WeaponIdx == i;

                if (sel)
                {
                    // Selected weapon glow effect
                    renderer.DrawRect(new Color(255, 170, 0, 60), boxX, wy0 + 5, 52, 30);
                    renderer.DrawRect(Config.COLOR_GOLD, boxX, wy0 + 2, 52, 2);
                    renderer.DrawRect(new Color(255, 170, 0, 150), boxX, wy0 + 33, 52, 2);
                }

                var borderC = sel ? Config.COLOR_GOLD : new Color(68, 68, 68);
                renderer.DrawRect(borderC, boxX, wy0 + 5, 52, 30);

                renderer.DrawString($"{i + 1}", new Vector2(boxX + 4, wy0 + 10), Color.White, 0.8f);
                renderer.DrawString(wp.Icon, new Vector2(boxX + 20, wy0 + 8), Color.White, 0.8f);
                renderer.DrawString($"{wp.Ammo}", new Vector2(boxX + 36, wy0 + 12), Color.White, 0.7f);

                if (sel)
                    renderer.DrawStringCentered(player.Weapon.Name, new Vector2(Config.W / 2f, wy0 + 36), new Color(255, 220, 100), 0.8f);
            }
        }

        // P2 HUD with enhanced styling
        if (player2 != null)
        {
            float hp2 = player2.Hp / player2.MaxHpActual;
            renderer.DrawRect(new Color(0, 0, 0, 180), Config.W - 208, Config.H - 90, 200, 22);
            renderer.DrawRect(new Color(51, 136, 238, 100), Config.W - 208, Config.H - 90, 200, 2);
            renderer.DrawRect(Config.COLOR_HP_BG, Config.W - 200, Config.H - 86, 184, 8);
            renderer.DrawRect(Config.COLOR_P2, Config.W - 200, Config.H - 86, (int)(184 * hp2), 8);
            // HP bar highlight
            renderer.DrawRect(new Color(255, 255, 255, 60), Config.W - 200, Config.H - 86, (int)(184 * hp2), 2);
            renderer.DrawString($"P2 HP {(int)player2.Hp}", new Vector2(Config.W - 196, Config.H - 88), Color.White, 0.8f);
        }

        // Minimap
        if (level != null && player != null)
        {
            DrawMinimap(renderer, player, player2, level);
        }
    }

    private void DrawMinimap(Renderer renderer, Player player, Player? player2, Level level)
    {
        int mapW = 160;
        int mapH = 100;
        int mapX = Config.W - mapW - 10;
        int mapY = Config.H - mapH - 10;

        // Background
        renderer.DrawRect(new Color(0, 0, 0, 180), mapX, mapY, mapW, mapH);
        renderer.DrawRect(new Color(255, 255, 255, 100), mapX, mapY, mapW, 2);
        renderer.DrawRect(new Color(255, 255, 255, 100), mapX, mapY + mapH - 2, mapW, 2);
        renderer.DrawRect(new Color(255, 255, 255, 100), mapX, mapY, 2, mapH);
        renderer.DrawRect(new Color(255, 255, 255, 100), mapX + mapW - 2, mapY, 2, mapH);

        // Scale factors
        float scaleX = (float)mapW / (level.W * Config.TILE);
        float scaleY = (float)mapH / (level.H * Config.TILE);

        // Draw terrain (simplified)
        for (int y = 0; y < level.H; y++)
        {
            for (int x = 0; x < level.W; x++)
            {
                if (level.Tiles[x][y].Solid)
                {
                    int px = mapX + (int)(x * Config.TILE * scaleX);
                    int py = mapY + (int)(y * Config.TILE * scaleY);
                    int tw = Math.Max(1, (int)(Config.TILE * scaleX));
                    int th = Math.Max(1, (int)(Config.TILE * scaleY));
                    renderer.DrawRect(new Color(100, 100, 100, 150), px, py, tw, th);
                }
            }
        }

        // Draw exit
        int exitX = mapX + (int)(level.ExitX * scaleX);
        int exitY = mapY + (int)(level.ExitY * scaleY);
        renderer.DrawRect(new Color(0, 255, 0, 200), exitX - 3, exitY - 3, 6, 6);

        // Draw player
        int p1X = mapX + (int)(player.X * scaleX);
        int p1Y = mapY + (int)(player.Y * scaleY);
        renderer.DrawRect(new Color(255, 50, 50, 255), p1X - 2, p1Y - 2, 4, 4);

        // Draw player 2
        if (player2 != null && !player2.Dead)
        {
            int p2X = mapX + (int)(player2.X * scaleX);
            int p2Y = mapY + (int)(player2.Y * scaleY);
            renderer.DrawRect(new Color(50, 150, 255, 255), p2X - 2, p2Y - 2, 4, 4);
        }
    }
}

public class BuffSelectScreen
{
    public List<BuffDef> Choices = new();
    public float Anim = 0;
    public float[] CardAnims = new float[3];

    public void SetChoices(List<BuffDef> choices)
    {
        Choices = choices;
        for (int i = 0; i < 3; i++) CardAnims[i] = 0;
    }

    public void Update()
    {
        Anim += 0.016f;
        for (int i = 0; i < 3; i++)
            if (CardAnims[i] < 1f) CardAnims[i] = Math.Min(1f, CardAnims[i] + 0.06f);
    }

    public void Draw(Renderer renderer)
    {
        var sb = renderer.SpriteBatch;
        var font = renderer.Font;
        var pixel = renderer.Pixel;

        // Background
        renderer.DrawRect(new Color(0, 0, 0, 220), 0, 0, Config.W, Config.H);

        // Title
        renderer.DrawStringCentered("选择强化", new Vector2(Config.W / 2f, 40), new Color(255, 51, 51), 1.5f);
        renderer.DrawStringCentered("按 1 / 2 / 3 选择一张卡牌", new Vector2(Config.W / 2f, 75), new Color(150, 150, 150), 0.8f);

        for (int i = 0; i < Math.Min(3, Choices.Count); i++)
        {
            float a = CardAnims[i];
            var c = Choices[i];
            var tc = Config.TIER_COLORS[c.Tier];
            string tn = Config.TIER_NAMES[c.Tier];

            int cardW = 200, cardH = 260;
            float targetX = Config.W / 2f - 310 + i * 210;
            float bx = targetX;
            float by = 120 + (1 - a) * 40;

            // Shadow
            renderer.DrawRect(new Color(0, 0, 0, (int)(80 * a)), (int)bx - 3, (int)by + 3, cardW + 6, cardH + 6);

            // Card bg
            renderer.DrawRect(new Color(12, 12, 28, (int)(255 * a)), (int)bx, (int)by, cardW, cardH);
            renderer.DrawRect(tc * a, (int)bx, (int)by, cardW, cardH);

            // Tier tag
            renderer.DrawRect(tc * 0.3f * a, (int)bx, (int)by, 60, 20);
            renderer.DrawStringCentered(tn, new Vector2(bx + 30, by + 2), tc * a, 0.7f);

            // Icon
            renderer.DrawRect(tc * 0.15f * a, (int)(bx + cardW / 2 - 30), (int)(by + 35), 60, 60);
            renderer.DrawStringCentered(c.Icon, new Vector2(bx + cardW / 2f, by + 45), Color.White * a, 1.2f);

            // Name
            renderer.DrawStringCentered(c.Name, new Vector2(bx + cardW / 2f, by + 110), Color.White * a, 1f);

            // Weapon tag
            if (c.Weapon != null)
                renderer.DrawStringCentered("[武器专属]", new Vector2(bx + cardW / 2f, by + 138), tc * a, 0.7f);

            // Description
            renderer.DrawStringCentered(c.Desc, new Vector2(bx + cardW / 2f, by + 170), new Color(180, 180, 180) * a, 0.7f);

            // Key hint
            renderer.DrawRect(tc * 0.5f * a, (int)(bx + cardW / 2 - 15), (int)(by + cardH - 40), 30, 24);
            renderer.DrawStringCentered($"{i + 1}", new Vector2(bx + cardW / 2f, by + cardH - 38), Color.White * a, 0.8f);

            // Pulse
            float pulse = Math.Abs((float)Math.Sin(Anim * 3.6f));
            renderer.DrawRect(tc * (0.25f * pulse), (int)bx, (int)by, cardW, cardH);
        }
    }
}

public static class OverlayScreen
{
    public static void DrawPaused(Renderer renderer)
    {
        // Dark overlay with gradient
        for (int y = 0; y < Config.H; y++)
        {
            float t = y / (float)Config.H;
            int alpha = (int)(180 + t * 30);
            renderer.DrawRect(new Color(0, 0, 0, alpha), 0, y, Config.W, 1);
        }
        
        // Animated border
        float time = (float)Environment.TickCount / 1000f;
        int borderAlpha = (int)(100 + Math.Sin(time * 2) * 50);
        renderer.DrawRect(new Color(255, 51, 51, borderAlpha), 0, 0, Config.W, 2);
        renderer.DrawRect(new Color(255, 51, 51, borderAlpha), 0, Config.H - 2, Config.W, 2);
        
        renderer.DrawStringCentered("PAUSED", new Vector2(Config.W / 2f, Config.H / 2f - 60), Color.White, 2f);
        renderer.DrawStringCentered("ESC  Continue", new Vector2(Config.W / 2f, Config.H / 2f), new Color(200, 200, 200), 1.2f);
        renderer.DrawStringCentered("M  Return to Menu", new Vector2(Config.W / 2f, Config.H / 2f + 30), new Color(200, 200, 200), 1.2f);
    }

    public static void DrawGameOver(Renderer renderer, int kills, int score)
    {
        // Dark red overlay
        for (int y = 0; y < Config.H; y++)
        {
            float t = y / (float)Config.H;
            int r = (int)(20 + t * 10);
            int alpha = (int)(220 + t * 20);
            renderer.DrawRect(new Color(r, 0, 0, alpha), 0, y, Config.W, 1);
        }
        
        // Animated blood drip effect
        float time = (float)Environment.TickCount / 1000f;
        for (int i = 0; i < 5; i++)
        {
            float x = Config.W * (0.1f + i * 0.2f);
            float dripY = (float)Math.Sin(time + i) * 20 + 50;
            renderer.DrawRect(new Color(255, 0, 0, 100), (int)x, 0, 2, (int)dripY);
        }
        
        renderer.DrawStringCentered("Mission Failed", new Vector2(Config.W / 2f, Config.H / 2f - 80), new Color(255, 50, 50), 2f);
        renderer.DrawStringCentered($"Kills: {kills}  Score: {score}", new Vector2(Config.W / 2f, Config.H / 2f - 20), Config.COLOR_GOLD, 1.2f);
        renderer.DrawStringCentered("Enter  Retry", new Vector2(Config.W / 2f, Config.H / 2f + 20), new Color(200, 200, 200), 1.2f);
        renderer.DrawStringCentered("ESC  Return to Menu", new Vector2(Config.W / 2f, Config.H / 2f + 50), new Color(200, 200, 200), 1.2f);
    }

    public static void DrawVictory(Renderer renderer, string mapName, int kills, int score)
    {
        // Dark green overlay with gradient
        for (int y = 0; y < Config.H; y++)
        {
            float t = y / (float)Config.H;
            int g = (int)(20 + t * 15);
            int alpha = (int)(220 + t * 20);
            renderer.DrawRect(new Color(0, g, 0, alpha), 0, y, Config.W, 1);
        }
        
        // Animated sparkle effect
        float time = (float)Environment.TickCount / 1000f;
        var rng = new Random(42);
        for (int i = 0; i < 20; i++)
        {
            float x = (float)(rng.NextDouble() * Config.W);
            float y = (float)(rng.NextDouble() * Config.H);
            float sparkle = (float)Math.Sin(time * 3 + i) * 0.5f + 0.5f;
            if (sparkle > 0.8f)
            {
                int size = (int)(sparkle * 4);
                renderer.DrawRect(new Color(255, 255, 100, (int)(sparkle * 200)), (int)x, (int)y, size, size);
            }
        }
        
        renderer.DrawStringCentered($"{mapName} Cleared!", new Vector2(Config.W / 2f, Config.H / 2f - 100), new Color(0, 255, 100), 2f);
        renderer.DrawStringCentered($"Kills: {kills}  Score: {score}", new Vector2(Config.W / 2f, Config.H / 2f - 40), Color.Yellow, 1.2f);
        renderer.DrawStringCentered("Enter  Continue Next Map", new Vector2(Config.W / 2f, Config.H / 2f + 20), new Color(200, 200, 200), 1.2f);
        renderer.DrawStringCentered("ESC  Return to Menu", new Vector2(Config.W / 2f, Config.H / 2f + 55), new Color(180, 180, 180), 1f);
    }
}