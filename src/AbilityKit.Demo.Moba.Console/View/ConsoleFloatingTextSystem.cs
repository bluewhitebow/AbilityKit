using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    public sealed class FloatingTextEntry
    {
        public int TargetActorId;
        public string Text;
        public bool IsHeal;
        public long SpawnFrame;

        public FloatingTextEntry() { }

        public FloatingTextEntry(int targetActorId, string text, bool isHeal, long spawnFrame)
        {
            TargetActorId = targetActorId;
            Text = text;
            IsHeal = isHeal;
            SpawnFrame = spawnFrame;
        }
    }

    public sealed class ConsoleFloatingTextSystem
    {
        private readonly List<FloatingTextEntry> _floatingTexts = new();
        private long _currentFrame;

        public void Spawn(int targetActorId, string text, bool isHeal)
        {
            _floatingTexts.Add(new FloatingTextEntry(targetActorId, text, isHeal, _currentFrame));
        }

        public void Tick()
        {
            _currentFrame++;
            while (_floatingTexts.Count > 0 && _currentFrame - _floatingTexts[0].SpawnFrame > 30)
            {
                _floatingTexts.RemoveAt(0);
            }
        }

        public IReadOnlyList<FloatingTextEntry> GetActive() => _floatingTexts;
        public void Clear() { _floatingTexts.Clear(); _currentFrame = 0; }
    }
}
