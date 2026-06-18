using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DY01.Entities;

public class Explosion
{
    public float X, Y, Radius, Damage;
    public int Life = 22;
    public int MaxLife = 22;

    public Explosion(float x, float y, float radius, float damage)
    {
        X = x; Y = y; Radius = radius; Damage = damage;
    }

    public bool Update()
    {
        Life--;
        return Life > 0;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float camX)
    {
        float a = (float)Life / MaxLife;
        float r = Radius * (1 - a * 0.5f) * (a < 0.3f ? a / 0.3f : 1);
        int cx = (int)(X - camX), cy = (int)Y;

        // Outer ring
        var c1 = new Color(255, 102, 0) * (a * 0.6f);
        DrawFilledCircle(sb, pixel, new Vector2(cx, cy), r, c1);

        // Mid ring
        var c2 = new Color(255, 204, 0) * (a * 0.8f);
        DrawFilledCircle(sb, pixel, new Vector2(cx, cy), r * 0.55f, c2);

        // Inner
        var c3 = Color.White * a;
        DrawFilledCircle(sb, pixel, new Vector2(cx, cy), r * 0.25f, c3);
    }

    private void DrawFilledCircle(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius, Color color)
    {
        int r = (int)radius;
        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy <= r * r)
                {
                    sb.Draw(pixel, new Rectangle((int)center.X + dx, (int)center.Y + dy, 1, 1), color);
                }
            }
        }
    }
}