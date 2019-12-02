using System;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI.Values;
using SharpDX;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using Color = System.Drawing.Color;
using static EnsoulSharp.SDK.Items;


namespace _14Senna
{

    class RecallData
    {
        public RecallData(UInt32 networkID, RecallStatus status)
        {
            NetworkID = networkID;
            Status = status;
        }

        public UInt32 NetworkID { get; set; }
        public RecallStatus Status { get; set; }

    }

    class RecallStatus
    {
        public RecallStatus(AIHeroClient hero)
       {
            IsRecalling = false;
            Hero = hero;
            Duration = 0;
            Starttime = 0;
        }

        public AIHeroClient Hero { get; set; }
        public int Duration { get; set; }
        public float Starttime { get; set; }
        public bool IsRecalling { get; set; }
    }

    class Senna
    {
        private static Spell Q1,Q2, W, R;
        private static Menu tyMenu, ComboMenu, HarassMenu, KSMenu, BaseULTMenu , DrawMenu ;
        static float lastQ, lastW, lastR = 0;
        private static List<RecallData> recalling = new List<RecallData>();
        private static Obj_SpawnPoint EnemyBase;
        private static Items.Item ward, pinkWard;

        public static void OnLoad()
        {
           foreach (var obj in ObjectManager.Get<Obj_SpawnPoint>())
            {
                if (obj.Team != ObjectManager.Player.Team)
                {
                    EnemyBase = obj;
                }
            }

            Q1 = new Spell(SpellSlot.Q, GetQRange());
            Q2 = new Spell(SpellSlot.Q, 1300f);

            W = new Spell(SpellSlot.W, 1100f);
            R = new Spell(SpellSlot.R, float.MaxValue);

            W.SetSkillshot(.25f, 60, 1000f, true, SkillshotType.Line);
            Q2.SetSkillshot(.4f, 50, float.MaxValue, false, SkillshotType.Line);
            R.SetSkillshot(1f, 80, 20000f, false, SkillshotType.Line);


            ward = new Items.Item((int)ItemId.Warding_Totem, 0);

            pinkWard = new Items.Item((int)ItemId.Control_Ward, 0);

            tyMenu = new Menu("14Senna", "14Senna", true);
            //Combo Menu

            ComboMenu = new Menu("Combo", "Combo Setting");
            ComboMenu.Add(new MenuBool("QAA", "Only Q After AA"));
            ComboMenu.Add(new MenuBool("UseQCombo", "Use Q"));
            ComboMenu.Add(new MenuBool("UseWCombo", "Use W"));
            tyMenu.Add(ComboMenu);

            HarassMenu = new Menu("Harass", "Harass Setting");
            HarassMenu.Add(new MenuBool("UseQHarass", "Use Q external"));
            HarassMenu.Add(new MenuBool("UseWHarass", "Use W"));
            tyMenu.Add(HarassMenu);


            KSMenu = new Menu("KS", "KS Setting");
            KSMenu.Add(new MenuBool("QKS", "Use Q KS"));
            KSMenu.Add(new MenuBool("WardQKS", "Try Ward Q KS"));
            KSMenu.Add(new MenuBool("RKS", "Use R"));
            KSMenu.Add(new MenuSlider("MinR", "Min Range to Ult", 1300, 0, 5000));
            KSMenu.Add(new MenuSlider("MaxR", "Max in Range to Ult", 5000, 0, 50000));

            tyMenu.Add(KSMenu);


            BaseULTMenu = new Menu("BaseULT", "BastULT Setting");
            BaseULTMenu.Add(new MenuBool("Enable", "Enable BaseULT"));
            BaseULTMenu.Add(new MenuBool("DisableCombo", "Disable BaseULT In Combo"));
            tyMenu.Add(BaseULTMenu);


            DrawMenu = new Menu("Draw", "Draw Setting");
            DrawMenu.Add(new MenuBool("DrawQ2", "Draw Q2"));
            DrawMenu.Add(new MenuBool("DrawW", "Draw W"));
            tyMenu.Add(DrawMenu);



            tyMenu.Attach();

            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                if (hero.Team != ObjectManager.Player.Team)
                {
                    recalling.Add(new RecallData(hero.NetworkId, new RecallStatus(hero)));
                }
            }

            Orbwalker.OnAction += OnOrbwalkerAction;
            Game.OnUpdate += Tick;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Teleport.OnTeleport +=OnTP;
        }

        static void OnOrbwalkerAction(object obj, OrbwalkerActionArgs args)
        {
            if (args.Type == OrbwalkerType.AfterAttack && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                if (ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled && ComboMenu["QAA"].GetValue<MenuBool>().Enabled && lastQ + 0.5 < Game.Time && Q1.IsReady())
                {
                    if (args.Target != null && args.Target.Type == GameObjectType.AIHeroClient)
                    {
                        Q1.CastOnUnit(args.Target, true);
                        lastQ = Game.Time;
                        //Orbwalker.ResetAutoAttackTimer();
                        //Console.WriteLine(" ComboQ "+lastQ);
                        //Game.Print(" ComboQ " + lastQ);
                        return;
                    }
                }

            }
        }

        static void OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen)
            {
                return;
            }
            if (DrawMenu["DrawQ2"].GetValue<MenuBool>().Enabled && Q1.IsReady())
            {
                Render.Circle.DrawCircle(GameObjects.Player.Position, Q2.Range, Color.FromArgb(48, 120, 252), 5);

            }
            if (DrawMenu["DrawW"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                Render.Circle.DrawCircle(GameObjects.Player.Position, W.Range, Color.FromArgb(48, 120, 252), 3);

            }

            Render.Circle.DrawCircle(EnemyBase.Position, 30, Color.FromArgb(48, 120, 252), 1);


        }
        static float GetQRange()
        {
            return ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius + ObjectManager.Player.BoundingRadius;
        }

        static void Tick(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            Q1.Range = GetQRange();

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                Combo();
            }else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {
                Harass();
            }

            BaseUlt();
            KS();
        }

        static void Combo()
        {

            if (ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled && !ComboMenu["QAA"].GetValue<MenuBool>().Enabled && lastQ + 0.5 < Game.Time && Q1.IsReady())
            {
                var target = TargetSelector.GetTarget(Q1.Range, DamageType.Physical);

                if (target != null && target.IsValidTarget() && Orbwalker.CanMove() && !ObjectManager.Player.Spellbook.IsAutoAttack)
                {
                    Q1.CastOnUnit(target, true);
                    lastQ = Game.Time;
                    //Console.WriteLine(" ComboQ "+lastQ);
                    //Game.Print(" ComboQ " + lastQ);
                    return;
                }

            }

            if (ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled && lastW + 0.5 < Game.Time && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if (target != null && target.IsValidTarget() && Orbwalker.CanMove() && !ObjectManager.Player.Spellbook.IsAutoAttack)
                {
                    var wPred = W.GetPrediction(target, false, 0, CollisionObjects.Minions | CollisionObjects.YasuoWall);
                    if (wPred.Hitchance >= HitChance.High)
                    {
                        W.Cast(wPred.CastPosition);
                        lastW = Game.Time;
                        //Console.WriteLine(" ComboW " + lastW);
                        //Game.Print(" ComboW " + lastW);
                        return;
                    }
                }

            }


        }

        static void BaseUlt()
        {
            if (BaseULTMenu["DisableCombo"].GetValue<MenuBool>().Enabled && Orbwalker.ActiveMode == OrbwalkerMode.Combo) { return; }

            if (BaseULTMenu["Enable"].GetValue<MenuBool>().Enabled && R.IsReady() && lastR + 2 <Game.Time)
            {
                foreach (var recall in recalling)
                {
                    if (recall.Status.IsRecalling)
                    {
                        //Game.Print("IsRecalling");
                        var leftTime = recall.Status.Starttime - Game.Time + (recall.Status.Duration/1000);
                        var distance = EnemyBase.Position.Distance(ObjectManager.Player.Position);
                        var hittime = distance / 20000 + 1 + Game.Ping/1000;


                        //Game.Print("leftTime " + leftTime);
                        //Game.Print("hittime " + hittime);
                        //Game.Print("cal Time " + (hittime * 1000 - leftTime));

                        if (hittime - leftTime > 0)
                        {
                            //Game.Print("cal Health");
                            var PredictedHealth = recall.Status.Hero.Health + recall.Status.Hero.HPRegenRate * hittime;
                            if (PredictedHealth < GetRDmg(recall.Status.Hero))
                            {
                                R.Cast(EnemyBase.Position);
                                lastR = Game.Time;
                            }
                        }

                    }
                }

            }
        }

        static void Harass()
        {
            var target = TargetSelector.GetTarget(Q2.Range, DamageType.Physical);
            if (HarassMenu["UseQHarass"].GetValue<MenuBool>().Enabled && target != null && target.IsValidTarget() && lastQ + 0.5 < Game.Time && Orbwalker.CanMove() && !ObjectManager.Player.Spellbook.IsAutoAttack && Q2.IsReady())
            {
                var q2Pred = Q2.GetPrediction(target, false, 0, 0);
                if (q2Pred.Hitchance >= HitChance.High)
                {
                    var targetPos = Extensions.Extend(ObjectManager.Player.Position, q2Pred.CastPosition, Q2.Range);

                    var minions = GameObjects.GetMinions(Q1.Range,MinionTypes.All, MinionTeam.All);
                    //var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q2.Range) && x.Health > 5).ToArray();

                    if (minions.Any())
                    {
                        foreach (var minion in minions)
                        {
                            var minionPos = Extensions.Extend(ObjectManager.Player.Position, minion.Position, Q2.Range);
                            if (minionPos.Distance(targetPos)<= (Q2.Width + target.BoundingRadius))
                            {
                                Q2.CastOnUnit(minion, true);
                                lastQ = Game.Time;
                                //Console.WriteLine(" HarassQ " + lastQ);
                                //Game.Print(" HarassQ " + lastQ);
                                return;

                            }
                        }

                    }


                }

            }

            if (HarassMenu["UseWHarass"].GetValue<MenuBool>().Enabled)
            {
                target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if (target != null && target.IsValidTarget() && lastW + 0.5 < Game.Time && Orbwalker.CanMove() && !ObjectManager.Player.Spellbook.IsAutoAttack && W.IsReady())
                {
                    var wPred = W.GetPrediction(target, false, 0, CollisionObjects.Minions | CollisionObjects.YasuoWall);
                    if (wPred.Hitchance >= HitChance.High)
                    {
                        W.Cast(wPred.CastPosition);
                        lastW = Game.Time;
                        //Console.WriteLine(" HarassW " + lastW);
                        //Game.Print(" HarassW " + lastW);
                        return;
                    }
                }
            }
        }

        static void KS()
        {
            if (KSMenu["RKS"].GetValue<MenuBool>().Enabled && lastR + 2 < Game.Time && R.IsReady())
            {
                var heros = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(KSMenu["MaxR"].GetValue<MenuSlider>().Value) && x.Health <= GetRDmg(x)).ToArray();

                if (heros.Any())
                {
                    foreach (var target in heros)
                    {
                        if (target.Position.DistanceToPlayer() < KSMenu["MinR"].GetValue<MenuSlider>().Value)
                        {
                            var rPred = R.GetPrediction(target, false, 0, CollisionObjects.Heroes | CollisionObjects.YasuoWall);
                            if (rPred.Hitchance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                                lastR = Game.Time;
                                //Console.WriteLine(" HarassW " + lastW);
                                //Game.Print(" HarassW " + lastW);
                                return;
                            }
                        }
                    }
                }
            }


            if (KSMenu["QKS"].GetValue<MenuBool>().Enabled && lastQ + 0.5 < Game.Time && Q2.IsReady())
            {
                var heros = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q2.Range) && x.Health <= GetQDmg(x)).ToArray();
                if (heros.Any())
                {

                    foreach (var target in heros)
                    {
                        var q2Pred = Q2.GetPrediction(target, false, 0, 0);
                        if (q2Pred.Hitchance >= HitChance.High)
                        {
                            var targetPos = Extensions.Extend(ObjectManager.Player.Position, q2Pred.CastPosition, Q2.Range);

                            //var minions = GameObjects.GetMinions(Q1.Range, MinionTypes.All, MinionTeam.All);
                            var minions = GameObjects.AttackableUnits.Where(x => x.IsValidTarget(Q1.Range)).ToArray()
                                .Concat(GameObjects.AllyWards.Where(x => x.InAutoAttackRange(0,false)).ToArray())
                                .Concat(GameObjects.AllyMinions.Where(x => x.InAutoAttackRange(0, false)).ToArray())
                                .Concat(GameObjects.AllyTurrets.Where(x => x.InAutoAttackRange(0, false)).ToArray());


                            if (minions.Any())
                            {
                                foreach (var minion in minions)
                                {
                                    var minionPos = Extensions.Extend(ObjectManager.Player.Position, minion.Position, Q2.Range);
                                    if (minionPos.Distance(targetPos) <= (Q2.Width + target.BoundingRadius))
                                    {
                                        Q2.CastOnUnit(minion, true);
                                        lastQ = Game.Time;
                                        //Console.WriteLine(" HarassQ " + lastQ);
                                       // Game.Print(" KSQ " + lastQ);
                                        return;
                                    }
                                }

                            }
                            else if(KSMenu["WardQKS"].GetValue<MenuBool>().Enabled)
                            {
                                var pos = ObjectManager.Player.Position.Extend(target.Position, 500);

                                if (ObjectManager.Player.CanUseItem(ward.Id)) {
                                    ObjectManager.Player.UseItem(ward.Id, pos);
                                }else if (ObjectManager.Player.CanUseItem(pinkWard.Id))
                                {
                                    ObjectManager.Player.UseItem(pinkWard.Id, pos);
                                }
                            }

                        }
                    }

                }

            }
        }


        static void OnTP(AIBaseClient sender, Teleport.TeleportEventArgs args)
        {

            if (sender.Team == ObjectManager.Player.Team || args.Type != Teleport.TeleportType.Recall)
            {
                return;
            }

            var unit = recalling.Find(x => x.NetworkID == sender.NetworkId);

            if (unit == null)
            {
                Console.WriteLine("ONTP can't find unit");
                return;
            }

            if (args.Status == Teleport.TeleportStatus.Start)
            {
                try
                {
                    unit.Status.Duration = args.Duration;
                    unit.Status.Starttime = Game.Time;
                    unit.Status.IsRecalling = true;
                    //Game.Print("startTP");

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in ONTP." + ex);
                }
            }
            else
            {
                unit.Status.IsRecalling = false;
            }
        }

        static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (sender.IsMe && Args.SData.Name =="SennaQ")
            {
                //Game.Print("castTime "+ Args.CastTime);
                //Game.Print("Name " + Args.SData.Name);

                Q2.Delay = Args.CastTime;

            }
        }
        static double GetRDmg(AIHeroClient target)
        {
            //Game.Print("getRdmg");
            if (R.Level == 0) { return 0; };

            var baseDmg = new float[]{ 250, 375, 500 }[R.Level-1];
            var bonusDmg = ObjectManager.Player.FlatPhysicalDamageMod;
            var bonusAP = ObjectManager.Player.FlatMagicDamageMod * 0.7f;

            var value = baseDmg + bonusDmg + bonusAP;
            //Game.Print("Rdmg: " + Damage.CalculatePhysicalDamage(ObjectManager.Player, target, value));
            //Game.Print("Rdmg: " + CalPhysicalDamage(ObjectManager.Player, target, (float)value));

            return CalPhysicalDamage(ObjectManager.Player, target, (float)value);
        }

        static double GetQDmg(AIHeroClient target)
        {
            if (Q1.Level == 0) { return 0; };
            var baseDmg = new float[] { 50, 80, 110,140,170 }[Q1.Level-1];
            var bonusDmg = ObjectManager.Player.FlatPhysicalDamageMod * 0.5;
            var passiveDmg = ObjectManager.Player.TotalAttackDamage *0.2;

            var value = baseDmg + bonusDmg + passiveDmg;

            //Game.Print("Dmg " + baseDmg + " bonusDmg " + bonusDmg + " passiveDMG " + passiveDmg);
            //Game.Print("bonusDmg " + bonusDmg);
            //Game.Print("passiveDMG " + passiveDmg);
            //Game.Print("Qdmg: " + Damage.CalculatePhysicalDamage(ObjectManager.Player, target, value));
            //Game.Print("MyQdmg: " + CalPhysicalDamage(ObjectManager.Player, target, (float)value));


            return CalPhysicalDamage(ObjectManager.Player, target, (float)value);
        }

        static double GetWDmg(AIHeroClient target)
        {
            var baseDmg = new float[] { 70, 115, 160, 205, 250 }[Q1.Level - 1];
            var bonusDmg = ObjectManager.Player.FlatPhysicalDamageMod * 0.7;

            var value = baseDmg + bonusDmg;

            return Damage.CalculatePhysicalDamage(ObjectManager.Player, target, (float)value);


        }

        static double CalPhysicalDamage(AIHeroClient source, AIHeroClient target, float amount)
        {
            var armorPenetrationPercent = source.PercentArmorPenetrationMod; 
             var armorPenetrationFlat = (source.FlatArmorPenetrationMod+source.PhysicalLethality) * (0.6 + 0.4 * source.Level/18);
            var bonusArmorPenetrationMod = source.PercentBonusArmorPenetrationMod;

            var armor = target.Armor;
            var bonusArmor = target.BonusArmor;
            float value;

            if (armor < 0)
            {
                value = 2 - 100 / (100 - armor);
            }else if (armor * armorPenetrationPercent - bonusArmor *
                (1 - bonusArmorPenetrationMod) - armorPenetrationFlat < 0)
            {
                value = 1;
            }
            else
            {
                    value = 100 / (100 + armor * armorPenetrationPercent - bonusArmor *
                (1 - bonusArmorPenetrationMod) - (float)armorPenetrationFlat);
            }
            return Math.Max((value * amount), 0);
        }
    }
}
