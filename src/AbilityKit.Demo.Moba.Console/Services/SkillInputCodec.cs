using System;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 技能输入 Codec
    /// 用于序列化和反序列化技能输入事件
    /// </summary>
    public static class SkillInputCodec
    {
        /// <summary>
        /// 序列化技能输入事件
        /// </summary>
        public static byte[] Serialize(in SkillInputEvent evt)
        {
            return System.Text.Encoding.UTF8.GetBytes(
                $"{{\"slot\":{evt.Slot},\"phase\":{(int)evt.Phase},\"target\":{evt.TargetActorId},\"x\":{evt.AimX:F4},\"z\":{evt.AimZ:F4}}}");
        }

        /// <summary>
        /// 反序列化技能输入事件
        /// </summary>
        public static SkillInputEvent Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length < 4)
                return default;

            var json = System.Text.Encoding.UTF8.GetString(payload);
            int slot = 0, phase = 0, target = 0;
            float x = 0f, z = 0f;

            foreach (var pair in json.Trim('{', '}').Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length != 2) continue;

                var key = kv[0].Trim('"');
                var valStr = kv[1].Trim();

                switch (key)
                {
                    case "slot": int.TryParse(valStr, out slot); break;
                    case "phase": int.TryParse(valStr, out phase); break;
                    case "target": int.TryParse(valStr, out target); break;
                    case "x": float.TryParse(valStr, out x); break;
                    case "z": float.TryParse(valStr, out z); break;
                }
            }

            return new SkillInputEvent(slot, (SkillInputPhase)phase, target, x, z);
        }
    }
}
