using Tactics.Battle;
using Tactics.Data;
using UnityEngine;

namespace Tactics.Core
{
    // Builds the whole battle from code when Play is pressed, so no scene setup is needed.
    public static class GameBootstrap
    {
        static GameObject _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnLoad()
        {
            if (Object.FindAnyObjectByType<BattleManager>() != null) return;
            StartBattle();
        }

        public static void StartBattle()
        {
            if (_root != null) Object.Destroy(_root);
            EnsureSun();
            _root = new GameObject("Battle");
            _root.AddComponent<BattleManager>().Setup(Chapter1.Create());
        }

        static void EnsureSun()
        {
            if (Object.FindAnyObjectByType<Light>() != null) return;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
