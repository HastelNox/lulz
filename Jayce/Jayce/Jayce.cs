﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace Jayce
{
    internal class Jayce : Helper
    {
        public static Spell Q, W, E, R, Qm, Wm, Em, Qe;

        public static void OnLoad(EventArgs args)
        {
               if (Player.ChampionName != "Jayce") return;
            MenuConfig.OnLoad();
            //ranged
            Q = new Spell(SpellSlot.Q, 1050);
            Qe = new Spell(SpellSlot.Q, 1470);
            W = new Spell(SpellSlot.W, int.MaxValue);
            E = new Spell(SpellSlot.E, 650);

            //   melee
            Qm = new Spell(SpellSlot.Q, 600);
            Wm = new Spell(SpellSlot.W, int.MaxValue);
            Em = new Spell(SpellSlot.E, 240);
            R = new Spell(SpellSlot.R, int.MaxValue);


            Q.SetSkillshot(0f, 70f, 1200, true, SkillshotType.SkillshotLine);
            Qe.SetSkillshot(0f, 70f, 2350, true, SkillshotType.SkillshotLine);
            Qm.SetTargetted(0.25f, float.MaxValue);
            Em.SetTargetted(0.25f, float.MaxValue);
            Game.OnUpdate += OnUpdate;
            Spellbook.OnCastSpell += OnCastSpell;
            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += GeneralOnUpdate;
            Obj_AI_Base.OnDoCast += OnDoCastRange;
            Obj_AI_Base.OnDoCast += OnDoCastMelee;
            Obj_AI_Base.OnDoCast += LaneClear;
            CustomEvents.Unit.OnDash += OnDash;
          // Obj_AI_Base.OnProcessSpellCast += OnProcessCast;
            AntiGapcloser.OnEnemyGapcloser += OnGapClose;
            Interrupter2.OnInterruptableTarget += OnInterrupt;
           // GameObject.OnCreate += OnCreate;

        }

        //private static void OnProcessCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        //{
        //    if (args.Slot == SpellSlot.Q)
        //    {
        //        Game.PrintChat(args.SData.DelayCastOffsetPercent.ToString());
        //    }
        //}

        //private static void OnCreate(GameObject sender, EventArgs args)
        //{
        //        var sen = (MissileClient) sender;
        //    if (sen == null) return;
        //        Game.PrintChat(sen.SData.LineMissileTimePulseBetweenCollisionSpellHits.ToString());
        //}

        private static void OnInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!GetBool("autoeint", typeof(bool))) return;
            if (sender.IsMe || sender.IsAlly) return;
            if (sender.Distance(Player) < Em.Range)
            {
                if (!Ismelee())
                {
                    R.Cast();
                }

                if (Ismelee())  
                {
                    Em.Cast(sender);
                }
            }
        }

        private static void OnGapClose(ActiveGapcloser gapcloser)
        {
            if (!GetBool("autoegap", typeof (bool))) return;
            if (gapcloser.Sender.IsMe || gapcloser.Sender.IsAlly) return;

            if (!Ismelee() && R.IsReady())
            {
                R.Cast();
            }
            if (gapcloser.End.Distance(Player.Position) < Em.Range && Ismelee())
            {
                Em.Cast(gapcloser.Sender);
            }
        }

        //this is bad??
        private static void OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (!GetBool("autoedash", typeof (bool))) return;
            if (sender.IsMe || sender.IsAlly) return;
            if (args.Unit == null) return;

            if (Ismelee() && R.IsReady())
            {
                Em.Cast(args.Unit);
            }

        }

        private static void OnDoCastMelee(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

        }


        private static void OnDoCastRange(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == "jayceshockblast" && !Ismelee())
            {
                if (GetBool("manualeq", typeof(KeyBind)))
                {
                    var pos = Player.Position.Extend(Game.CursorPos, Player.BoundingRadius + 150);
                    E.Cast(pos);
                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && GetBool("useecr", typeof(bool)))
                {
                    var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                    if (target == null) return;
                    var pred = Q.GetPrediction(target).CastPosition;
                    var castposition = Player.Position.Extend(pred, Player.BoundingRadius + 150);
                    E.Cast(castposition);
                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && GetBool("useehr", typeof (bool)))
                {
                    var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                    if (target == null) return;
                    var pred = Q.GetPrediction(target).CastPosition;
                    var castposition = Player.Position.Extend(pred, Player.BoundingRadius + 150);
                    E.Cast(castposition);
                }
            }
            if (!Orbwalking.IsAutoAttack(args.SData.Name)) return;
            if (!sender.IsMe) return;
            if (!args.SData.IsAutoAttack()) return;
            if (args.Target.Type != GameObjectType.obj_AI_Hero) return;
            if (Ismelee()) return;

            if (W.IsReady())
            {
                if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && GetBool("usewcr", typeof (bool)))
                    || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && GetBool("usewhr", typeof(bool))))
                {
                    W.Cast();
                    Orbwalker.ForceTarget((Obj_AI_Hero) args.Target);
                    Orbwalking.ResetAutoAttackTimer();
                }
            }
        }


        private static void LaneClear(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear) return;
            if (!Orbwalking.IsAutoAttack(args.SData.Name)) return;
            if (!sender.IsMe) return;
            if (!args.SData.IsAutoAttack()) return;
            if (Ismelee()) return;
            var obj = (Obj_AI_Base) args.Target;
            if (!GetBool("usewlr", typeof (bool))) return;
            if (GetValue("minmana") > Player.ManaPercent) return;

            if (W.IsReady() && obj.Health > Player.GetAutoAttackDamage(obj) + 30)
            {
                W.Cast();
                Orbwalker.ForceTarget((Obj_AI_Base) args.Target);
                Orbwalking.ResetAutoAttackTimer();
            }
            var minions =
                MinionManager.GetMinions(Player.Position, 300);
            foreach (var min in minions.Where(
                x => x.NetworkId != ((Obj_AI_Base) args.Target).NetworkId && x.Health < Player.GetAutoAttackDamage(x) + 15))
            {
                if (obj.Health < Player.GetAutoAttackDamage(obj))
                {
                    if (W.IsReady())
                    {
                            W.Cast();
                            Orbwalker.ForceTarget(min);                      
                        Orbwalking.ResetAutoAttackTimer();
                    }
                }
            }
        }


        private static void GeneralOnUpdate(EventArgs args)
        {
            if (GetBool("manualeq", typeof (KeyBind)))
            {
                ManualEq();
            }

            if (GetBool("flee", typeof(KeyBind)))
            {
             //   Flee();
            }

            if (GetBool("insec", typeof (KeyBind)))
            {
                Insec();
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    FormChangeManager();
                    if (!Ismelee())
                        Combo();
                    else
                        Combomelee();
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;

                    case Orbwalking.OrbwalkingMode.LaneClear:
                    Laneclearrange(); // also has Melee Q
                    Laneclearmelee();
                    break;
            }
        }

        private static void Insec()
        {
          
        }

        private static void Flee()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (!Ismelee())
            {
                R.Cast();
            }

            if (Ismelee())
            {
                var min =
    ObjectManager.Get<Obj_AI_Minion>()
        .Where(x => x.Distance(Player) < 300 && !x.IsDead && x.IsEnemy).ToList();

                foreach (var minions in min)
                {
                    if (E.IsReady() && Q.IsReady())
                    {
                        Em.Cast(minions);
                       
                    }
                    if (!E.IsReady())
                    {
                        Qm.Cast(minions);
                    }

                }
            }

        }

        private static void Laneclearmelee()
        {
            if (GetValue("minmana") > Player.ManaPercent) return;
            if (!Ismelee()) return;
            var min =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.Distance(Player) < 300 && !x.IsDead && x.IsEnemy).ToList();

            if (min.FirstOrDefault() == null)
            {
                minionscirclemelee = null;
                return;
            }

           
            
            foreach (var minions in min)
            {
                minionscirclemelee = new Geometry.Polygon.Circle(minions.Position, 300);
                if (E.IsReady() && GetBool("useelm", typeof(bool)))
                {
                    if (minions.Health < EMeleeDamage(minions))
                    {
                        Em.Cast(minions);
                    }
                }
            }

            var count = min.Where(x => minionscirclemelee.IsInside(x));
            var objAiMinions = count as IList<Obj_AI_Minion> ?? count.ToList();
            if (objAiMinions.Count() >= GetValue("minhitwq"))
            {
                if (W.IsReady() && GetBool("usewlm", typeof(bool)))
                W.Cast();

                if (Q.IsReady() && GetBool("useqlm", typeof(bool)))
                    Qm.Cast(objAiMinions.FirstOrDefault());
            }
        }

        private static void Laneclearrange()
        {
            if (GetValue("minmana") > Player.ManaPercent) return;
            var min =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.Distance(Player) < Q.Range -200 && !x.IsDead && x.IsEnemy && x.IsTargetable);


            var objAiMinions = min as IList<Obj_AI_Minion> ?? min.ToList();
            foreach (var minions in objAiMinions)
            {
                minionscircle = new Geometry.Polygon.Circle(minions.Position, 250);
            }

            var count =objAiMinions.Where(x => minionscircle.IsInside(x));

            if (count.Count() < GetValue("minhitwq")) return;
            if (!Ismelee() && Q.IsReady() && GetBool("useqlr", typeof(bool)))
                Q.Cast(minionscircle.Center);
        }


        private static void ManualEq()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (Ismelee())
            {
                if (R.IsReady())
                    R.Cast();
            }

            if (Ismelee()) return;
            if (E.IsReady() && Q.IsReady())
            {
                Q.Cast(Game.CursorPos);
            }
        }

        private static void FormChangeManager()
        {
            if (!GetBool("usercf", typeof (bool))) return;

            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
            if (target == null) return;
            if (!R.IsReady()) return;
            if (Ismelee())
            {
                var aarange = Orbwalking.GetRealAutoAttackRange(target);
                if (((!Q.IsReady() || (SpellTimer["Qm"] > 1.3f) && !E.IsReady()) ||
                     Player.Distance(target) > aarange + 50)) 
                {
                    R.Cast();
                }
            }
            else
            {
                var getpred = Q.GetPrediction(target);
                var spellbook = Player.Spellbook.GetSpell(SpellSlot.W);
                if ((getpred.Hitchance == HitChance.Collision || !Q.IsReady()))
                {
                    if (spellbook.State != SpellState.Surpressed && Environment.TickCount - W.LastCastAttemptT > 300)
                    {
                        if (SpellTimer["Q"] > 2f && SpellTimer["W"] > 2)
                        {
                            R.Cast();
                        }
                    }
                }
                if (target.Health < QMeleeDamage() && Ready("Qm") && target.Distance(Player) < Qm.Range)
                {
                    R.Cast();
                }

                if (target.Health < QMeleeDamage() + EMeleeDamage(target) && Ready("Qm") && Ready("Em") &&
                    target.Distance(Player) < Qm.Range)
                {
                    R.Cast();
                }
            }
        }

        public static double QMeleeDamage()
        {
            return new double[] {30, 70, 110, 150, 190, 230}[Q.Level - 1]
                   + 1*Player.FlatPhysicalDamageMod;
        }

        public static double EMeleeDamage(Obj_AI_Base target)
        {
           return (new double[] { 8, 10.4, 12.8, 15.2, 17.6, 20 }[Q.Level - 1] / 100) *target.MaxHealth
                + 1*Player.FlatPhysicalDamageMod;
        }



        private static void Combomelee()
        {
           // if (Player.IsWindingUp) return;
            var target = TargetSelector.GetTarget(Qm.Range, TargetSelector.DamageType.Physical);
            if (target == null) return;

            if (Player.Distance(target) < Orbwalking.GetRealAutoAttackRange(target))
            {
                if (Wm.IsReady())
                    Wm.Cast();
            }

            if (Player.Distance(target) < 300)
            {
                if (Qm.IsReady() && !Em.IsReady() && GetBool("useqcm", typeof(bool)))
                {
                    Qm.Cast(target);
                }

                if (Em.IsReady() && GetBool("useecm", typeof(bool)))
                {
                    var aarange = Orbwalking.GetRealAutoAttackRange(target);
                    if (((!Q.IsReady() || (SpellTimer["Qm"] > 1.3f) && !E.IsReady()) ||
                         Player.Distance(target) < aarange + 150))
                    Em.Cast(target);

                    if (target.Health < EMeleeDamage(target) + 200)
                    {
                        Em.Cast(target);
                    }
                }
            }
            else
            {
                if (Ready("Q") || Ready("W") && Em.IsReady() && GetBool("useecm", typeof(bool)))
                {
                    Em.Cast(target);
                }
                if (Qm.IsReady() && GetBool("useqcm", typeof(bool)))
                {
                    Qm.Cast(target);
                }

            }
        }
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1050, TargetSelector.DamageType.Physical);
            if (target == null) return;
            var pred = Q.GetPrediction(target);
            if (!Ismelee())
            {
                if (Q.IsReady() && GetBool("useqhr", typeof(bool)))
                {
                    Q.Cast(pred.CastPosition);
                }
            }
            else
            {
                if (Q.IsReady() && GetBool("useqhm", typeof(bool)))
                {
                    Q.Cast(target);
                }
                if (W.IsReady() && GetBool("usewhm", typeof (bool)) && target.Distance(Player) < W.Range)
                {
                    W.Cast();
                }
            }
        }
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target == null) return;
           var prede =  Q.GetPrediction(target);
            var pred = Qe.GetPrediction(target);
            if (pred.CollisionObjects.Count > 1) return;
            if (Q.IsReady() && E.IsReady() && GetBool("useqcr", typeof (bool)) &&
                Player.Mana >
                Player.Spellbook.GetSpell(SpellSlot.E).ManaCost + Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost) 
            {                         
                Q.Cast(pred.CastPosition);
            }

            if (Q.IsReady() && !E.IsReady())
            {
                if (Player.Distance(target) < 1050 && GetBool("useqcr", typeof (bool)))
                {
                    Q.Cast(prede.CastPosition);
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            var x = Drawing.WorldToScreen(Player.Position).X;
            var y = Drawing.WorldToScreen(Player.Position).Y;
            if (Ismelee())
            {
                if (GetBool("drawtimers", typeof(bool)))
                {
                    Drawing.DrawText(x + 50, y, Color.Red,
                        "[Q] " + ((int)SpellTimer["Q"]).ToString(CultureInfo.InvariantCulture));
                    Drawing.DrawText(x - 20, y, Color.Red,
                        "[E] " + ((int)SpellTimer["E"]).ToString(CultureInfo.InvariantCulture));
                    Drawing.DrawText(x - 80, y, Color.Red,
                        "[W] " + ((int)SpellTimer["W"]).ToString(CultureInfo.InvariantCulture));
                }

                if (Q.Level >= 1 && GetBool("drawq", typeof(bool)))
                {
                    Render.Circle.DrawCircle(Player.Position, Qm.Range, Color.Violet, 4);
                }

                if (E.Level >= 1 && GetBool("drawe", typeof(bool)))
                {
                    Render.Circle.DrawCircle(Player.Position, Em.Range, Color.Blue, 4);
                }
            }

            if (!Ismelee())
            {
                if (GetBool("drawtimers", typeof(bool)))
                {
                    Drawing.DrawText(x + 50, y, Color.Red,
                        "[Qm] " + ((int)SpellTimer["Q"]).ToString(CultureInfo.InvariantCulture));
                    Drawing.DrawText(x - 20, y, Color.Red,
                        "[Em] " + ((int)SpellTimer["E"]).ToString(CultureInfo.InvariantCulture));
                    Drawing.DrawText(x - 80, y, Color.Red,
                        "[Wm] " + ((int)SpellTimer["W"]).ToString(CultureInfo.InvariantCulture));
                }
                //if (minionscircle != null)
                //{
                //    minionscircle.Draw(Color.Blue);
                //}
                if (Q.Level >= 1 && GetBool("drawq", typeof(bool)))
                {
                    Render.Circle.DrawCircle(Player.Position, Q.Range, Color.Violet, 4);
                }

                if (E.Level >= 1 && GetBool("drawe", typeof(bool)))
                {
                    Render.Circle.DrawCircle(Player.Position, E.Range, Color.Blue, 4);
                }
            }


        }

        public static bool Ismelee()
        {
            return Player.HasBuff("JayceStanceHammer");
        }

        private static void OnUpdate(EventArgs args)
        {
            SpellTimer["Q"] = ((TimeStamp["Q"] - Game.Time) > 0)
                ? (TimeStamp["Q"] - Game.Time)
                : 0;

            SpellTimer["Qm"] = ((TimeStamp["Qm"] - Game.Time) > 0)
                ? (TimeStamp["Qm"] - Game.Time)
                : 0;

            SpellTimer["E"] = ((TimeStamp["E"] - Game.Time) > 0)
                ? (TimeStamp["E"] - Game.Time)
                : 0;

            SpellTimer["Em"] = ((TimeStamp["Em"] - Game.Time) > 0)
                ? (TimeStamp["Em"] - Game.Time)
                : 0;
            SpellTimer["W"] = ((TimeStamp["W"] - Game.Time) > 0)
                ? (TimeStamp["W"] - Game.Time)
                : 0;

            SpellTimer["Wm"] = ((TimeStamp["Wm"] - Game.Time) > 0)
                ? (TimeStamp["Wm"] - Game.Time)
                : 0;

        }

        internal static Dictionary<string, float> TimeStamp = new Dictionary<string, float>
        {
            {"Q", 0f},
            {"W", 0f},
            {"E", 0f},
            {"Qm", 0f},
            {"Em", 0f},
            {"Wm", 0f}
        };

        /// <summary>
        /// Stores the current tickcount of the spell.
        /// </summary>
        internal static Dictionary<string, float> SpellTimer = new Dictionary<string, float>
        {
            {"Q", 0f},
            {"W", 0f},
            {"E", 0f},
            {"Qm", 0f},
            {"Em", 0f},
            {"Wm", 0f}
        };

        private static Geometry.Polygon.Circle minionscircle;
        private static Geometry.Polygon.Circle minionscirclemelee;


        public static float Cooldown
        {
            get { return Player.PercentCooldownMod; }
        }

        public static bool Ready(string spell)
        {
            return SpellTimer[spell] < 0.4f;
        }


        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {

            if (!Ismelee())
            {
                if (args.Slot == SpellSlot.Q)
                {
                    var qperlevel = new[] {8, 8, 8, 8, 8, 8}[Q.Level - 1];
                    TimeStamp["Q"] = Game.Time + 1.5f + (qperlevel + (qperlevel*Cooldown));
                }

                if (args.Slot == SpellSlot.W)
                {
                    var wperlevel = new[] {13, 11.4f, 9.8f, 8.2f, 6.6f, 5}[W.Level - 1];
                    TimeStamp["W"] = Game.Time + 2.0f + (wperlevel + (wperlevel*Cooldown));
                }

                if (args.Slot == SpellSlot.E)
                {
                    var eperlevel = new[] {16, 16, 16, 16, 16, 16}[E.Level - 1];
                    TimeStamp["E"] = Game.Time + 1.5f + (eperlevel + (eperlevel*Cooldown));
                }
            }
            else
            {
                if (args.Slot == SpellSlot.Q)
                {
                    var qmperlevel = new[] {16, 14, 12, 10, 8, 6}[Qm.Level - 1];
                    TimeStamp["Qm"] = Game.Time + 1.5f + (qmperlevel + (qmperlevel*Cooldown));
                }

                if (args.Slot == SpellSlot.W)
                {
                    var wmperlevel = new[] {10, 10, 10, 10, 10, 10}[Wm.Level - 1];
                    TimeStamp["Wm"] = Game.Time + 1.5f + (wmperlevel + (wmperlevel*Cooldown));
                }

                if (args.Slot == SpellSlot.E)
                {
                    var emperlevel = new[] {15, 14, 13, 12, 11, 10}[Em.Level - 1];
                    TimeStamp["Em"] = Game.Time + 1.5f + (emperlevel + (emperlevel*Cooldown));
                }
            }
        
        }
    }
}

