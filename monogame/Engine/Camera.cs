using System;
using Microsoft.Xna.Framework;
using DY01.Data;

namespace DY01.Engine;

public class Camera
{
    public float X { get; set; }
    public float TargetX { get; set; }
    public float ShakeAmount { get; set; }
    public float ShakeX { get; private set; }
    public float ShakeY { get; private set; }
    private Random _rng = new();

    // Lookahead
    private float _lookaheadX = 0;
    private float _targetLookahead = 0;
    private float _lastPlayerX = 0;
    private float _playerVelocity = 0;

    public float OffsetX => X + ShakeX;
    public float OffsetY => ShakeY;

    public void Reset()
    {
        X = 0; TargetX = 0; ShakeAmount = 0; ShakeX = 0; ShakeY = 0;
        _lookaheadX = 0; _targetLookahead = 0; _lastPlayerX = 0; _playerVelocity = 0;
    }

    public void Follow(float targetX, float smooth = 0.07f)
    {
        // Calculate player velocity for lookahead
        _playerVelocity = targetX - _lastPlayerX;
        _lastPlayerX = targetX;

        // Lookahead based on player velocity
        _targetLookahead = MathHelper.Clamp(_playerVelocity * 8f, -120f, 120f);
        _lookaheadX += (_targetLookahead - _lookaheadX) * 0.05f;

        TargetX = targetX - Config.W / 2f + _lookaheadX;
        X += (TargetX - X) * smooth;
    }

    public void Clamp(float minX, float maxX)
    {
        if (X < minX) X = minX;
        if (X > maxX) X = maxX;
    }

    public void AddShake(float amount)
    {
        ShakeAmount = Math.Max(ShakeAmount, amount);
    }

    public void Update()
    {
        if (ShakeAmount > 0)
        {
            ShakeX = (float)(_rng.NextDouble() * 2 - 1) * ShakeAmount;
            ShakeY = (float)(_rng.NextDouble() * 2 - 1) * ShakeAmount;
            ShakeAmount *= 0.88f;
            if (ShakeAmount < 0.4f)
            {
                ShakeAmount = 0;
                ShakeX = 0;
                ShakeY = 0;
            }
        }
        else
        {
            ShakeX = 0;
            ShakeY = 0;
        }
    }
}