using GTA;
using GTA.Math;
using GTA.UI;
using System;
using System.Drawing;
using System.Windows.Forms;
using static Utils.Abilities;
using static Utils.Entities;
using static Utils.UI;



/**
 *  LAST STAND
 *  
 *  A simple tower defense minigame   
 *  
 *  -- Technical reqs
 *  
 *  - Zone one
 *      - Straight line
 *      - Sniper spot
 *      
 *  - Zone two
 *      - Curves
 *      - Sniper spot
 *      - Two ally towers
 *  
 *  - Defense zone
 *      - Sniper spot
 *      - At least 150m in a straight line, clear of obstacles and other NPC's
 *      - Towers to spawn allies in
 *  
 *  1. Freemode
 *      + Tell player where to go
 *      - Draw defense blips
 *      - Draw physical markers
 *  2. Defense started
 *      + Fade screen
 *      + Teleport player to defense spot
 *      - Clear weapons
 *  
 *
 */

namespace LastStand
{
    public sealed class DefenseZone
    {
        public readonly Vector3 StartLocation;
        public readonly Vector3 SpawnLocation;
        public readonly Vector3 TargetLocation;
        public readonly Vector3 SniperNestLocation;
        public readonly PropLocation[] PropLocations;

        public DefenseZone(Vector3 startLocation, Vector3 spawnLocation, Vector3 targetLocation, Vector3 sniperNestLocation, PropLocation[] propLocations)
        {
            StartLocation = startLocation;
            SpawnLocation = spawnLocation;
            TargetLocation = targetLocation;
            SniperNestLocation = sniperNestLocation;
            PropLocations = propLocations;
        }
    }


    public sealed class LastStand : Script
    {
        //public static readonly Vector3 StartLocation = new Vector3(-920, -2748, 13);
        //public static readonly Vector3 SpawnLocation = new Vector3(-1225, -3294, 13);
        //public static readonly Vector3 TargetLocation = new Vector3(-1265, -3360, 13);
        //public static readonly Vector3 SniperNestLocation = new Vector3(-1265, -3360, 35);

        public static DefenseState State { get; set; } = DefenseState.FREEROAM;

        //static DefenseZone VespucciDefense = new DefenseZone(
        //    new Vector3(-1447, -778, 22.5f),
        //    new Vector3(-1711, -1083, 12),
        //    new Vector3(-1808, -1195, 12),
        //    new Vector3(-1818, -1202, 18),
        //    new[]
        //    {
        //        new Nullable<PropLocation>() a
        //    }
        //    );

        DefenseZone[] DefenseZones = {
        new DefenseZone(
            new Vector3(-920, -2748, 13),
            new Vector3(-1295, -3297, 13),
            new Vector3(-1220, -3340, 13),
            //new Vector3(-1260, -3355, 26),
            new Vector3(-1260, -3355, 25),
            new []
            {
                new PropLocation("prop_tri_finish_banner", new Vector3(-1220, -3340, 13), new Vector3(0, 0, 65)),
                new PropLocation("prop_bmu_02_ld", new Vector3(-1260, -3355, 25), new Vector3(0, 0, -30))
            }),
        //VespucciDefense
        };

        //Prop p = World.CreateProp("prop_bmu_02_ld", pos, new Vector3(0, 0, -30), true, false);


        // Object Name: 	prop_bmu_02
        //Object Hash: 	-1754285242
        //Object Hash(uInt32): 	2540682054


        Vector3 Ones = new Vector3(1, 1, 1);

        DefenseZone CurrentZone;

        public static int Wave = 0;
        public static PedGroup Attackers;
        //public static Blip StartBlip;
        //public static Blip TargetBlip;

        private readonly int FadeDuration = 1500;

        public static Vector3? StrikePosition;
        public static int strikeTargetTime;

        public enum DefenseState
        {
            FREEROAM,
            DEFENDING,
            DEFENSE_ENDED
        }

        void Setup()
        {
            Notification.Show("Mod loaded");
            GTA.UI.Screen.ShowHelpText("Go to the marker to begin the minigame.", 10000);
            CreateStartBlips(DefenseZones);

            // DEBUG
            Game.Player.Character.Position = DefenseZones[0].StartLocation;

            //foreach (var DefenseZone in DefenseZones)
            //{
            //    foreach (var (PropName, Position, Rotation) in DefenseZone.PropLocations)
            //    {
            //        World.CreateProp(PropName, Position, Rotation, false, false);
            //    }
            //}

            DeleteAllProps();
        }

        public LastStand()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += OnAbort;

            Setup();
            //World.GetAllProps().
        }

        public void OnTick(object sender, EventArgs e)
        {
            OnKeyDown();

            switch (State)
            {
                case DefenseState.FREEROAM:

                    foreach (var DefenseZone in DefenseZones)
                    {
                        if (World.GetDistance(Game.Player.Character.Position, DefenseZone.StartLocation) < 50)
                        {
                            World.DrawMarker(MarkerType.VerticalCylinder, DefenseZone.StartLocation, Vector3.Zero, Vector3.Zero, Ones * 3, Color.Yellow);
                        }

                        if (World.GetDistance(Game.Player.Character.Position, DefenseZone.StartLocation) < 5)
                        {
                            GTA.UI.Screen.ShowHelpText("Press ~INPUT_CONTEXT~ to start the defense.");

                            if (Game.IsControlJustPressed(GTA.Control.Context))
                            {
                                CurrentZone = DefenseZone;
                                SetupPlayer();
                            }
                        }
                    }




                    break;
                case DefenseState.DEFENDING:
                    // TODO: Hide start blip
                    DefenseLoop();

                    AllowStrikeAbilityThisFrame();

                    if (Game.Player.IsAiming)
                    {
                        GTA.UI.Screen.ShowHelpText("Press ~INPUT_DETONATE~ to call in an orbital strike.");

                        if (Game.IsControlJustPressed(GTA.Control.Detonate))
                        {
                            RaycastResult rr = World.GetCrosshairCoordinates();

                            // By setting these vars, we schedule a strike
                            if (rr.DidHit)
                            {
                                StrikePosition = rr.HitPosition + Vector3.WorldUp * 3;
                                strikeTargetTime = Game.GameTime + 3 * 1000;
                            }
                            else
                            {
                                Notification.Show("Not a valid position.");
                            }
                        }
                    }

                    break;
                case DefenseState.DEFENSE_ENDED:

                    ResetPlayer();
                    RemoveTargetBlip();
                    ClearAllPools();
                    DeleteAttackers();
                    CreateStartBlips(DefenseZones);
                    DeletePeds();
                    DeleteProps();
                    break;
            }
        }

        void NextWave()
        {
            if (Wave == 5)
            {
                // TODO: Show big text
                //GTA.World.
                State = DefenseState.DEFENSE_ENDED;
                return;
            }

            Wave++;

            CreateTargetBlip(CurrentZone.TargetLocation);
            GTA.UI.Screen.ShowSubtitle($"Get ready for wave ~b~{Wave}");

            Wait(2500);

            SpawnAttackers(CurrentZone);
            State = DefenseState.DEFENDING;
        }


        void SetupPlayer()
        {
            Game.Player.IsInvincible = true;
            Game.Player.IgnoredByEveryone = true;
            Game.MaxWantedLevel = 0;

            foreach (var propLocation in CurrentZone.PropLocations)
            {
                SpawnProp(propLocation);
            }

            GTA.UI.Screen.ShowSubtitle("Defense starting!", FadeDuration);
            GTA.UI.Screen.FadeOut(FadeDuration);
            Wait(FadeDuration);
            Game.Player.Character.Position = CurrentZone.SniperNestLocation + Vector3.WorldUp;
            Wait(FadeDuration);
            GTA.UI.Screen.FadeIn(FadeDuration);

            State = DefenseState.DEFENDING;
            Wave = 0;

            NextWave();
        }

        void ResetPlayer()
        {
            GTA.UI.Screen.ShowSubtitle("Defense ended!", FadeDuration);
            GTA.UI.Screen.FadeOut(FadeDuration);
            Wait(FadeDuration);
            Game.Player.Character.Position = CurrentZone.StartLocation;
            Wait(FadeDuration);
            GTA.UI.Screen.FadeIn(FadeDuration);

            State = DefenseState.FREEROAM;
            Wave = 0;
            CurrentZone = null;

            Game.Player.IsInvincible = false;
            Game.Player.IgnoredByEveryone = false;
            Game.MaxWantedLevel = 5;
        }

        void EndWave(bool didWin)
        {
            DeleteAttackers();

            Wait(3000);

            if (didWin)
            {
                Notification.Show("Next wave called from endwave");
                NextWave();
                //State = DefenseState.PREPARING;
            }
            else
            {
                State = DefenseState.DEFENSE_ENDED;
            }
        }

        void DefenseLoop()
        {
            // We await for the attackers to win or for them to be killed
            // In the meantime, we remove dead blips and show the target marker
            RemoveDeadBlips();
            RemoveStartBlips();

            GTA.UI.Screen.ShowSubtitle($"Wave {Wave} | Prevent the ~r~Attackers ~w~from reaching the ~y~target.", 1);

            // Show the target marker
            World.DrawMarker(MarkerType.VerticalCylinder, CurrentZone.TargetLocation, Vector3.Zero, Vector3.Zero, Ones * 3, Color.Yellow);

            // We check for a wave ending condition, and stop checking if there's one
            if (DidGroupGetToWaypoint(Attackers, CurrentZone))
            {
                GTA.UI.Screen.ShowSubtitle("The attackers got to the target!");
                EndWave(false);
            }
            else if (IsGroupDead(Attackers))
            {
                //GTA.UI.Screen.ShowSubtitle("Prepare for next round");
                EndWave(true);
            }
        }

        public void OnKeyDown() { }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.T:
                    if (!Game.IsWaypointActive) return;
                    Game.Player.Character.Position = (World.WaypointPosition);
                    break;
                case Keys.L:
                    GTA.UI.Screen.ShowHelpText(Game.Player.Character.Position.ToString());
                    Notification.Show(Game.Player.Character.Position.ToString());

                    RaycastResult rr = World.GetCrosshairCoordinates();

                    if (rr.DidHit)
                    {
                        Notification.Show("1 Collided with: " + rr.HitPosition.ToString());
                        Game.Player.Character.Position = rr.HitPosition + Vector3.WorldUp * 2;
                    }

                    break;

                case Keys.Y:

                    RaycastResult rr2 = World.GetCrosshairCoordinates();

                    if (rr2.DidHit)
                    {
                        Notification.Show("2 Collided with: " + rr2.HitPosition.ToString());
                        Notification.Show(World.CreateProp("prop_tri_finish_banner", rr2.HitPosition, new Vector3(0, 0, 65), true, true) + "");
                        //SpawnProp(DefenseZones[0].PropLocations[0]);
                        // -1220 -3340 13
                    }

                    break;

                case Keys.U:

                    RaycastResult rr3 = World.GetCrosshairCoordinates();

                    if (rr3.DidHit)
                    {
                        Vector3 pos = new Vector3(-1260, -3355, 25);
                        Notification.Show("3 Collided with: " + rr3.HitPosition.ToString());
                        Prop p = World.CreateProp("prop_bmu_02_ld", pos, new Vector3(0, 0, -30), true, false);

                        if (p != null)
                        {
                            p.IsPositionFrozen = true;
                        }

                        Game.Player.Character.Position = pos + Vector3.WorldUp;


                        //SpawnProp(DefenseZones[0].PropLocations[0]);
                        // -1260 -3355 15
                    }

                    break;

                case Keys.X:

                    Vehicle v = World.CreateVehicle(VehicleHash.SeaSparrow, Game.Player.Character.Position + Vector3.WorldUp * 30);
                    Game.Player.Character.SetIntoVehicle(v, VehicleSeat.Driver);
                    v.IsEngineRunning = true;

                    break;

                case Keys.Z:
                    //DeleteAllProps();

                    //foreach (var item in PropPool)
                    //{
                    //    Notification.Show(item.Model.ToString() + ", " + item.Exists());
                    //}
                    //ClearAllPools();

                    //foreach (var item in World.GetAllBlips())
                    //{
                    //    if (item.Name.Equals("target"))
                    //    {
                    //        item.Delete();
                    //    }
                    //}
                    break;
            }
        }

        private void OnAbort(object sender, EventArgs e)
        {
            ClearAllPools();
        }
    }
}
