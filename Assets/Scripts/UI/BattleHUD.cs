using System.Collections.Generic;
using Tactics.Battle;
using Tactics.Core;
using Tactics.Units;
using UnityEngine;

namespace Tactics.UI
{
    // Immediate-mode (IMGUI) HUD. Laid out in a 720px-tall virtual space and scaled to the screen.
    public class BattleHUD : MonoBehaviour
    {
        const float RefHeight = 720f;
        const float PopupLifetime = 1.1f;

        public static readonly Color PlayerColor = new Color(0.45f, 0.72f, 1f);
        public static readonly Color EnemyColor = new Color(1f, 0.45f, 0.4f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.45f);

        struct Popup
        {
            public Vector3 World;
            public string Text;
            public Color Color;
            public float Born;
        }

        BattleManager _bm;
        readonly List<Rect> _blockers = new List<Rect>();
        readonly List<Popup> _popups = new List<Popup>();
        float _scale = 1f;
        string _banner;
        Color _bannerColor;
        float _bannerUntil;

        Texture2D _panelTex;
        GUIStyle _panel, _text, _dialogue, _title, _small, _smallRight, _smallCenter, _big, _popup, _button, _buttonCenter;

        public void Init(BattleManager bm) => _bm = bm;

        public static Color TeamColor(Team team) => team == Team.Player ? PlayerColor : EnemyColor;

        public void ShowPopup(Vector3 world, string text, Color color) =>
            _popups.Add(new Popup { World = world, Text = text, Color = color, Born = Time.time });

        public void ShowBanner(string text, Color color, float duration = 1.2f)
        {
            _banner = text;
            _bannerColor = color;
            _bannerUntil = Time.time + duration;
        }

        // Uses the panel rects from the last repaint, so world clicks under the HUD are ignored.
        public bool IsPointerOverHud
        {
            get
            {
                var m = InputShim.MousePosition;
                var p = new Vector2(m.x, Screen.height - m.y) / _scale;
                foreach (var r in _blockers)
                    if (r.Contains(p)) return true;
                return false;
            }
        }

        void OnDestroy()
        {
            if (_panelTex != null) Destroy(_panelTex);
        }

        void EnsureStyles()
        {
            if (_panel != null) return;
            _panelTex = new Texture2D(1, 1);
            _panelTex.SetPixel(0, 0, new Color(0.05f, 0.06f, 0.09f, 0.82f));
            _panelTex.Apply();

            _panel = new GUIStyle(GUI.skin.box);
            _panel.normal.background = _panelTex;
            _text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            _text.normal.textColor = new Color(0.92f, 0.92f, 0.92f);
            _dialogue = new GUIStyle(_text) { fontSize = 17 };
            _title = new GUIStyle(_text) { fontSize = 17, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(_text) { fontSize = 12 };
            _smallRight = new GUIStyle(_small) { alignment = TextAnchor.MiddleRight };
            _smallCenter = new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter };
            _big = new GUIStyle(_text) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            _popup = new GUIStyle(_text) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 4, 4) };
            _buttonCenter = new GUIStyle(_button) { alignment = TextAnchor.MiddleCenter };
        }

        void OnGUI()
        {
            if (_bm == null || _bm.CameraRig == null) return;
            EnsureStyles();
            if (Event.current.type == EventType.Repaint) _blockers.Clear();

            _scale = Screen.height / RefHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));
            float w = Screen.width / _scale, h = RefHeight;

            DrawUnitBars();
            DrawPopups();

            if (_bm.CurrentLine != null)
            {
                DrawDialogue(w, h);
                return;
            }

            DrawTurnOrder();
            DrawLog(w);
            DrawHint(w);
            if (_bm.Active != null && _bm.Phase != BattlePhase.Ended)
                DrawUnitPanel(new Rect(12, h - 142, 300, 130), _bm.Active);
            DrawHoverInfo(w, h);
            DrawCommands(h);
            DrawBanner(w);
            if (_bm.Phase == BattlePhase.Ended) DrawEndPanel(w, h);
        }

        Rect Panel(Rect r)
        {
            GUI.Box(r, GUIContent.none, _panel);
            if (Event.current.type == EventType.Repaint) _blockers.Add(r);
            return r;
        }

        void Shadowed(Rect r, string text, GUIStyle style, Color color)
        {
            GUI.contentColor = new Color(0f, 0f, 0f, color.a * 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            GUI.contentColor = color;
            GUI.Label(r, text, style);
            GUI.contentColor = Color.white;
        }

        void Bar(Rect r, int value, int max, Color color, string label)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            if (max > 0) GUI.DrawTexture(new Rect(r.x, r.y, r.width * value / (float)max, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (label != null) GUI.Label(new Rect(r.x + 4, r.y - 3, r.width, r.height + 6), $"{label} {value}/{max}", _small);
        }

        Vector2? ToGui(Vector3 world)
        {
            var sp = _bm.CameraRig.Camera.WorldToScreenPoint(world);
            if (sp.z <= 0f) return null;
            return new Vector2(sp.x, Screen.height - sp.y) / _scale;
        }

        void DrawUnitBars()
        {
            foreach (var u in _bm.Units)
            {
                if (!u.IsAlive) continue;
                var p = ToGui(u.transform.position + Vector3.up * 1.25f);
                if (p == null) continue;
                var r = new Rect(p.Value.x - 22, p.Value.y - 3, 44, 6);
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), Texture2D.whiteTexture);
                GUI.color = TeamColor(u.Team);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width * u.Hp / (float)u.Stats.MaxHp, r.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        void DrawPopups()
        {
            _popups.RemoveAll(p => Time.time - p.Born > PopupLifetime);
            foreach (var p in _popups)
            {
                float age = Time.time - p.Born;
                var pos = ToGui(p.World + Vector3.up * (age * 0.8f));
                if (pos == null) continue;
                var c = p.Color;
                c.a = 1f - Mathf.Clamp01((age - 0.6f) / (PopupLifetime - 0.6f));
                Shadowed(new Rect(pos.Value.x - 80, pos.Value.y - 14, 160, 28), p.Text, _popup, c);
            }
        }

        void DrawDialogue(float w, float h)
        {
            var line = _bm.CurrentLine;
            var r = Panel(new Rect(w / 2 - 400, h - 180, 800, 160));
            GUI.contentColor = Gold;
            GUI.Label(new Rect(r.x + 18, r.y + 12, r.width - 36, 26), line.Speaker, _title);
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(r.x + 18, r.y + 44, r.width - 36, 90), line.Text, _dialogue);
            GUI.Label(new Rect(r.x + r.width - 260, r.y + r.height - 28, 244, 20), "Click / Space to continue", _smallRight);
        }

        void DrawTurnOrder()
        {
            var order = TurnOrder.Preview(_bm.Units, _bm.Active, 8);
            if (order.Count == 0) return;
            var r = Panel(new Rect(12, 12, 210, 42 + order.Count * 22));
            GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, 24), "Turn Order", _title);
            for (int i = 0; i < order.Count; i++)
            {
                GUI.contentColor = TeamColor(order[i].Team);
                GUI.Label(new Rect(r.x + 12, r.y + 34 + i * 22, r.width - 24, 22), (i == 0 ? "> " : "   ") + order[i].Name, _text);
            }
            GUI.contentColor = Color.white;
        }

        void DrawLog(float w)
        {
            int n = Mathf.Min(6, _bm.Log.Count);
            if (n == 0) return;
            var r = Panel(new Rect(w - 372, 12, 360, 16 + n * 20));
            for (int i = 0; i < n; i++)
                GUI.Label(new Rect(r.x + 10, r.y + 8 + i * 20, r.width - 20, 20), _bm.Log[_bm.Log.Count - n + i], _small);
        }

        void DrawHint(float w)
        {
            GUI.Label(new Rect(w / 2 - 300, 6, 600, 20),
                "LMB select   RMB/Esc cancel   Q/E rotate   WASD pan   Wheel zoom", _smallCenter);
        }

        void DrawUnitPanel(Rect rect, Unit u)
        {
            var r = Panel(rect);
            GUI.contentColor = TeamColor(u.Team);
            GUI.Label(new Rect(r.x + 10, r.y + 6, r.width - 20, 24), u.Name, _title);
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(r.x + 10, r.y + 28, r.width - 20, 20), u.Def.ClassName, _small);
            Bar(new Rect(r.x + 10, r.y + 52, r.width - 20, 14), u.Hp, u.Stats.MaxHp, new Color(0.3f, 0.75f, 0.35f), "HP");
            Bar(new Rect(r.x + 10, r.y + 72, r.width - 20, 14), u.Mp, u.Stats.MaxMp, new Color(0.35f, 0.5f, 0.95f), "MP");
            var s = u.Stats;
            GUI.Label(new Rect(r.x + 10, r.y + 90, r.width - 20, 36),
                $"ATK {s.Atk}   DEF {s.Def}   MAG {s.Mag}   RES {s.Res}\nSPD {s.Spd}   MOV {s.Move}   JMP {s.Jump}", _small);
        }

        void DrawHoverInfo(float w, float h)
        {
            var u = _bm.HoverUnit;
            if (u == null || u == _bm.Active || !u.IsAlive) return;
            var rect = new Rect(w - 312, h - 142, 300, 130);
            DrawUnitPanel(rect, u);

            var skill = _bm.PendingSkill;
            if (_bm.Phase != BattlePhase.SelectTarget || skill == null || !skill.AffectsUnit(_bm.Active, u)) return;
            if (!skill.InRange(_bm.Active.Tile, u.Tile) && skill.Radius == 0) return;

            var hit = DamageCalc.Compute(_bm.Active, _bm.Active.Tile, u, skill, false);
            string text = hit.IsHeal ? $"Forecast: +{hit.Amount} HP" : $"Forecast: ~{hit.Amount} damage{(hit.Tag != null ? $" ({hit.Tag})" : "")}";
            var f = Panel(new Rect(rect.x, rect.y - 40, rect.width, 34));
            GUI.Label(new Rect(f.x + 10, f.y + 7, f.width - 20, 20), text, _text);
        }

        void DrawCommands(float h)
        {
            var u = _bm.Active;
            if (_bm.Phase == BattlePhase.PlayerAction)
            {
                var skills = u.Def.Skills;
                float height = 46 + (skills.Count + 1) * 30 + (_bm.HasMoved ? 0 : 20);
                var r = Panel(new Rect(12, h - 150 - height, 300, height));
                float y = r.y + 8;
                GUI.Label(new Rect(r.x + 10, y, r.width - 20, 24), "Command", _title);
                y += 28;
                if (!_bm.HasMoved)
                {
                    GUI.Label(new Rect(r.x + 10, y, r.width - 20, 20), "Click a blue tile to move.", _small);
                    y += 20;
                }
                foreach (var skill in skills)
                {
                    GUI.enabled = !_bm.HasActed && skill.MpCost <= u.Mp;
                    string cost = skill.MpCost > 0 ? $"   {skill.MpCost} MP" : "";
                    if (GUI.Button(new Rect(r.x + 10, y, r.width - 20, 26), skill.Name + cost, _button)) _bm.BeginTargeting(skill);
                    y += 30;
                }
                GUI.enabled = true;
                if (GUI.Button(new Rect(r.x + 10, y, r.width - 20, 26), "End Turn", _button)) _bm.EndTurn();
            }
            else if (_bm.Phase == BattlePhase.SelectTarget && _bm.PendingSkill != null)
            {
                var s = _bm.PendingSkill;
                var r = Panel(new Rect(12, h - 150 - 124, 300, 116));
                GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, 24), s.Name, _title);
                GUI.Label(new Rect(r.x + 10, r.y + 34, r.width - 20, 44), s.Description, _small);
                if (GUI.Button(new Rect(r.x + 10, r.y + 80, 90, 26), "Back", _buttonCenter)) _bm.CancelTargeting();
                GUI.Label(new Rect(r.x + 110, r.y + 80, r.width - 120, 26), "Pick a target tile", _small);
            }
        }

        void DrawBanner(float w)
        {
            if (_banner == null || Time.time > _bannerUntil) return;
            Shadowed(new Rect(0, 80, w, 50), _banner, _big, _bannerColor);
        }

        void DrawEndPanel(float w, float h)
        {
            bool won = _bm.Outcome == BattleOutcome.Victory;
            var r = Panel(new Rect(w / 2 - 160, h / 2 - 75, 320, 150));
            Shadowed(new Rect(r.x, r.y + 18, r.width, 50), won ? "Victory" : "Defeat", _big, won ? Gold : EnemyColor);
            if (GUI.Button(new Rect(r.x + 80, r.y + 92, 160, 36), "Restart", _buttonCenter)) GameBootstrap.StartBattle();
        }
    }
}
