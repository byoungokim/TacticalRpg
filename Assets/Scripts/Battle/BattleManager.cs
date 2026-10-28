using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tactics.Core;
using Tactics.Data;
using Tactics.Grid;
using Tactics.Story;
using Tactics.UI;
using Tactics.Units;
using Tactics.View;
using UnityEngine;

namespace Tactics.Battle
{
    public enum BattlePhase { Dialogue, PlayerAction, SelectTarget, Busy, EnemyTurn, Ended }
    public enum BattleOutcome { None, Victory, Defeat }

    public class BattleManager : MonoBehaviour
    {
        static readonly Color MoveTint = new Color(0.3f, 0.6f, 1f);
        static readonly Color AttackTint = new Color(1f, 0.3f, 0.25f);
        static readonly Color HealTint = new Color(0.3f, 1f, 0.5f);
        static readonly Color AreaTint = new Color(1f, 0.65f, 0.1f);

        public GridMap Map { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public List<Unit> Units { get; } = new List<Unit>();
        public Unit Active { get; private set; }
        public BattlePhase Phase { get; private set; } = BattlePhase.Busy;
        public BattleOutcome Outcome { get; private set; }
        public bool HasMoved { get; private set; }
        public bool HasActed { get; private set; }
        public Skill PendingSkill { get; private set; }
        public Tile HoverTile { get; private set; }
        public Unit HoverUnit => HoverTile?.Occupant;
        public DialogueLine CurrentLine { get; private set; }
        public List<string> Log { get; } = new List<string>();

        BattleSetup _setup;
        BattleHUD _hud;
        ReachResult _reach;
        readonly List<Tile> _areaTiles = new List<Tile>();
        Transform _marker;
        bool _advanceDialogue;
        bool _turnDone;

        public void Setup(BattleSetup setup)
        {
            _setup = setup;

            Map = new GameObject("Map").AddComponent<GridMap>();
            Map.transform.SetParent(transform, false);
            Map.Build(setup.MapRows);

            foreach (var s in setup.Spawns)
                Units.Add(Unit.Create(s.Def, s.Team, Map.Get(s.Coord), s.Facing, transform));

            CameraRig = new GameObject("Camera Rig").AddComponent<CameraRig>();
            CameraRig.transform.SetParent(transform, false);
            CameraRig.Init(Map.Center);

            _hud = gameObject.AddComponent<BattleHUD>();
            _hud.Init(this);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(marker.GetComponent<Collider>());
            marker.name = "Active Marker";
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = Vector3.one * 0.22f;
            marker.GetComponent<Renderer>().material.color = new Color(1f, 0.82f, 0.3f);
            _marker = marker.transform;

            AddLog(setup.Title);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return PlayDialogue(_setup.Intro);

            while (true)
            {
                Active = TurnOrder.Next(Units);
                HasMoved = HasActed = false;
                CameraRig.FocusOn(Active.transform.position);

                if (Active.Team == Team.Player) yield return PlayerTurn();
                else yield return EnemyTurn();

                Active.Ct -= TurnOrder.Threshold;
                CheckOutcome();
                if (Outcome != BattleOutcome.None) break;
            }

            Map.ClearTints();
            AddLog(Outcome == BattleOutcome.Victory ? "Victory!" : "Defeat...");
            yield return PlayDialogue(Outcome == BattleOutcome.Victory ? _setup.Victory : _setup.Defeat);
            Phase = BattlePhase.Ended;
        }

        IEnumerator PlayDialogue(List<DialogueLine> lines)
        {
            Phase = BattlePhase.Dialogue;
            foreach (var line in lines)
            {
                CurrentLine = line;
                _advanceDialogue = false;
                yield return new WaitUntil(() => _advanceDialogue);
            }
            CurrentLine = null;
            Phase = BattlePhase.Busy;
        }

        // ---------------------------------------------------------------- player turn

        IEnumerator PlayerTurn()
        {
            _turnDone = false;
            EnterActionMenu();
            yield return new WaitUntil(() => _turnDone);
        }

        void EnterActionMenu()
        {
            PendingSkill = null;
            ClearHighlights();
            if (HasMoved && HasActed) { EndTurn(); return; }

            Phase = BattlePhase.PlayerAction;
            if (!HasMoved)
            {
                _reach = Pathfinding.Reachable(Map, Active);
                foreach (var t in _reach.Destinations) { t.RangeTint = MoveTint; t.Repaint(); }
            }
        }

        // Called by the HUD.
        public void BeginTargeting(Skill skill)
        {
            if (Phase != BattlePhase.PlayerAction || HasActed || skill.MpCost > Active.Mp) return;
            ClearHighlights();
            PendingSkill = skill;
            Phase = BattlePhase.SelectTarget;
            var tint = skill.TargetsAllies ? HealTint : AttackTint;
            foreach (var t in Map.AllTiles)
            {
                if (!skill.InRange(Active.Tile, t)) continue;
                t.RangeTint = tint;
                t.Repaint();
            }
        }

        // Called by the HUD.
        public void CancelTargeting()
        {
            if (Phase == BattlePhase.SelectTarget) EnterActionMenu();
        }

        // Called by the HUD.
        public void EndTurn()
        {
            ClearHighlights();
            Phase = BattlePhase.Busy;
            _turnDone = true;
        }

        IEnumerator DoPlayerMove(Tile dest)
        {
            Phase = BattlePhase.Busy;
            ClearHighlights();
            yield return Active.MoveAlong(_reach.PathTo(dest));
            HasMoved = true;
            CameraRig.FocusOn(Active.transform.position);
            EnterActionMenu();
        }

        IEnumerator DoPlayerSkill(Tile center)
        {
            var skill = PendingSkill;
            Phase = BattlePhase.Busy;
            ClearHighlights();
            yield return ExecuteSkill(Active, skill, center);
            HasActed = true;
            CheckOutcome();
            if (Outcome != BattleOutcome.None) EndTurn();
            else EnterActionMenu();
        }

        bool IsValidTarget(Tile center) =>
            PendingSkill.InRange(Active.Tile, center) &&
            Units.Any(u => PendingSkill.AffectsUnit(Active, u) && GridMap.Distance(u.Tile, center) <= PendingSkill.Radius);

        // ---------------------------------------------------------------- enemy turn

        IEnumerator EnemyTurn()
        {
            Phase = BattlePhase.EnemyTurn;
            yield return new WaitForSeconds(0.4f);

            var plan = EnemyAI.Decide(Map, Active, Units);
            if (plan.MoveTo != null && plan.MoveTo != Active.Tile)
            {
                var reach = Pathfinding.Reachable(Map, Active);
                yield return Active.MoveAlong(reach.PathTo(plan.MoveTo));
                CameraRig.FocusOn(Active.transform.position);
            }
            if (plan.Skill != null)
            {
                yield return new WaitForSeconds(0.2f);
                yield return ExecuteSkill(Active, plan.Skill, plan.Target);
            }
            yield return new WaitForSeconds(0.3f);
        }

        // ---------------------------------------------------------------- shared

        IEnumerator ExecuteSkill(Unit user, Skill skill, Tile center)
        {
            user.SpendMp(skill.MpCost);
            if (center != user.Tile) user.FaceToward(center);
            AddLog($"{user.Name} uses {skill.Name}.");

            if (skill.CutIn)
            {
                _hud.ShowBanner(skill.Name, BattleHUD.TeamColor(user.Team));
                yield return CameraRig.CutIn(user.transform.position, center.TopCenter);
            }
            yield return user.Lunge();

            var targets = Units.Where(u => skill.AffectsUnit(user, u) && GridMap.Distance(u.Tile, center) <= skill.Radius).ToList();
            foreach (var t in targets)
            {
                var hit = DamageCalc.Compute(user, user.Tile, t, skill, true);
                var popupAt = t.transform.position + Vector3.up * 1.3f;
                if (hit.IsHeal)
                {
                    t.Heal(hit.Amount);
                    _hud.ShowPopup(popupAt, $"+{hit.Amount}", new Color(0.4f, 1f, 0.5f));
                    AddLog($"  {t.Name} recovers {hit.Amount} HP.");
                }
                else
                {
                    t.ApplyDamage(hit.Amount);
                    _hud.ShowPopup(popupAt, hit.Tag != null ? $"{hit.Tag}! {hit.Amount}" : hit.Amount.ToString(), Color.white);
                    AddLog($"  {t.Name} takes {hit.Amount}{(hit.Tag != null ? $" ({hit.Tag})" : "")}.");
                    if (!t.IsAlive) AddLog($"  {t.Name} is defeated!");
                }
            }
            yield return new WaitForSeconds(0.6f);
        }

        void CheckOutcome()
        {
            if (!Units.Any(u => u.IsAlive && u.Team == Team.Enemy)) Outcome = BattleOutcome.Victory;
            else if (!Units.Any(u => u.IsAlive && u.Team == Team.Player)) Outcome = BattleOutcome.Defeat;
        }

        void AddLog(string line)
        {
            Log.Add(line);
            if (Log.Count > 50) Log.RemoveAt(0);
        }

        void ClearHighlights()
        {
            Map.ClearTints();
            _areaTiles.Clear();
        }

        // ---------------------------------------------------------------- input

        void Update()
        {
            if (Phase == BattlePhase.Dialogue)
            {
                if (InputShim.LeftClickDown || InputShim.ConfirmDown) _advanceDialogue = true;
                return;
            }

            UpdateHover();

            bool click = InputShim.LeftClickDown && !_hud.IsPointerOverHud && HoverTile != null;
            if (Phase == BattlePhase.PlayerAction)
            {
                if (click && !HasMoved && HoverTile != Active.Tile && _reach.Destinations.Contains(HoverTile))
                    StartCoroutine(DoPlayerMove(HoverTile));
            }
            else if (Phase == BattlePhase.SelectTarget)
            {
                UpdateAreaPreview();
                if (InputShim.CancelDown || InputShim.RightClickDown) CancelTargeting();
                else if (click && IsValidTarget(HoverTile)) StartCoroutine(DoPlayerSkill(HoverTile));
            }
        }

        void UpdateHover()
        {
            Tile tile = null;
            if (!_hud.IsPointerOverHud)
            {
                var ray = CameraRig.Camera.ScreenPointToRay(InputShim.MousePosition);
                if (Physics.Raycast(ray, out var hit, 500f))
                {
                    var unit = hit.collider.GetComponentInParent<Unit>();
                    if (unit != null && unit.IsAlive) tile = unit.Tile;
                    else if (hit.collider.TryGetComponent<TileView>(out var view)) tile = view.Tile;
                }
            }
            if (tile == HoverTile) return;

            if (HoverTile != null) { HoverTile.Hovered = false; HoverTile.Repaint(); }
            HoverTile = tile;
            if (tile != null) { tile.Hovered = true; tile.Repaint(); }
        }

        void UpdateAreaPreview()
        {
            foreach (var t in _areaTiles) { t.AreaTint = null; t.Repaint(); }
            _areaTiles.Clear();
            if (HoverTile == null || !PendingSkill.InRange(Active.Tile, HoverTile)) return;

            _areaTiles.AddRange(Map.TilesWithin(HoverTile, PendingSkill.Radius));
            foreach (var t in _areaTiles) { t.AreaTint = AreaTint; t.Repaint(); }
        }

        void LateUpdate()
        {
            bool show = Active != null && Active.IsAlive && Phase != BattlePhase.Dialogue && Phase != BattlePhase.Ended;
            _marker.gameObject.SetActive(show);
            if (!show) return;
            _marker.position = Active.transform.position + Vector3.up * (1.65f + Mathf.Sin(Time.time * 3f) * 0.06f);
            _marker.rotation = Quaternion.Euler(45f, Time.time * 90f, 45f);
        }
    }
}
