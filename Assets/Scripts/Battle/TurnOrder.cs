using System.Collections.Generic;
using Tactics.Units;

namespace Tactics.Battle
{
    // Charge-time turn order: every tick each unit gains CT equal to its speed,
    // and whoever reaches the threshold first acts.
    public static class TurnOrder
    {
        public const float Threshold = 100f;

        public static Unit Next(List<Unit> units)
        {
            Unit ready;
            while ((ready = Ready(units, u => u.Ct)) == null)
                foreach (var u in units)
                    if (u.IsAlive) u.Ct += u.Stats.Spd;
            return ready;
        }

        // Simulates upcoming turns without touching the real CT values.
        public static List<Unit> Preview(List<Unit> units, Unit current, int count)
        {
            var order = new List<Unit>();
            var ct = new Dictionary<Unit, float>();
            foreach (var u in units)
                if (u.IsAlive) ct[u] = u.Ct;
            if (ct.Count == 0) return order;

            if (current != null && ct.ContainsKey(current))
            {
                order.Add(current);
                ct[current] -= Threshold;
            }

            while (order.Count < count)
            {
                var best = Ready(units, u => ct.TryGetValue(u, out var v) ? v : float.MinValue);
                if (best != null)
                {
                    order.Add(best);
                    ct[best] -= Threshold;
                    continue;
                }
                foreach (var u in units)
                    if (ct.ContainsKey(u)) ct[u] += u.Stats.Spd;
            }
            return order;
        }

        // Highest CT at or above the threshold wins; ties go to the earlier unit in the list.
        static Unit Ready(List<Unit> units, System.Func<Unit, float> ctOf)
        {
            Unit best = null;
            float bestCt = 0f;
            foreach (var u in units)
            {
                if (!u.IsAlive) continue;
                float v = ctOf(u);
                if (v >= Threshold && (best == null || v > bestCt))
                {
                    best = u;
                    bestCt = v;
                }
            }
            return best;
        }
    }
}
