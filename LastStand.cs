using GTA;
using GTA.Math;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static LastStand.UI;
using static LastStand.Utils;

namespace LastStand
{

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

    public sealed class LastStand : Script
    {
        public static readonly Vector3 StartLocation = new Vector3(-920, -2748, 13);
        public static readonly Vector3 SpawnLocation = new Vector3(-1225, -3294, 13);
        public static readonly Vector3 TargetLocation = new Vector3(-1265, -3360, 13);
        public static readonly Vector3 SniperNestLocation = new Vector3(-1265, -3360, 35);

        private DefenseState State { get; set; } = DefenseState.FREEROAM;

        public static readonly List<Ped> PedPool = new List<Ped>();
        public static readonly List<Blip> BlipPool = new List<Blip>();
        public static readonly List<Vehicle> VehiclePool = new List<Vehicle>();

        private int Wave = 0;
        private PedGroup Attackers;
        public static Blip StartBlip;
        public static Blip TargetBlip;

        private readonly int FadeDuration = 1500;

        private enum DefenseState
        {
            FREEROAM,
            DEFENDING,
            PREPARING,
            DEFENSE_ENDED
        }

        void Setup()
        {
            Notification.Show("Mod loaded");
            GTA.UI.Screen.ShowHelpText("Go to the marker to begin the minigame.", 10000);
            CreateStartBlip();

            // DEBUG
            Game.Player.Character.Position = StartLocation;
        }

        public LastStand()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += OnAbort;

            Setup();
        }

        public void OnTick(object sender, EventArgs e)
        {
            OnKeyDown();

            switch (State)
            {
                case DefenseState.FREEROAM:
                    World.DrawMarker(MarkerType.VerticalCylinder, StartLocation, Vector3.Zero, Vector3.Zero, new Vector3(1, 1, 1), Color.Yellow);

                    if (World.GetDistance(Game.Player.Character.Position, StartLocation) < 5)
                    {
                        GTA.UI.Screen.ShowHelpText("Press ~INPUT_CONTEXT~ to start the defense.");

                        if (Game.IsControlJustPressed(GTA.Control.Context))
                        {
                            SetupPlayer();
                        }
                    }

                    break;
                case DefenseState.DEFENDING:
                    // TODO: Hide start blip
                    DefenseLoop();
                    break;
                case DefenseState.PREPARING:
                    //EnableStoreMenu();
                    RemoveStartBlip();
                    //NextWave();
                    //Notification.Show("Next wave called from switch");

                    break;
                case DefenseState.DEFENSE_ENDED:
                    DeleteAttackers();
                    ResetPlayer();
                    CreateStartBlip();
                    RemoveTargetBlip();
                    break;
            }
        }


        void NextWave()
        {

            if (Wave == 5)
            {
                // TODO: Show big text
                State = DefenseState.DEFENSE_ENDED;
                return;
            }

            Wave++;

            CreateTargetBlip();
            GTA.UI.Screen.ShowSubtitle($"Get ready for wave ~b~{Wave}");

            Wait(2500);

            SpawnAttackers();
            State = DefenseState.DEFENDING;

        }


        void SetupPlayer()
        {
            Game.Player.IsInvincible = true;
            Game.Player.IgnoredByEveryone = true;
            Game.MaxWantedLevel = 0;

            GTA.UI.Screen.ShowSubtitle("Defense starting!", FadeDuration);
            GTA.UI.Screen.FadeOut(FadeDuration);
            Wait(FadeDuration);
            Game.Player.Character.Position = SniperNestLocation;
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
            Game.Player.Character.Position = StartLocation;
            Wait(FadeDuration);
            GTA.UI.Screen.FadeIn(FadeDuration);

            State = DefenseState.FREEROAM;
            Wave = 0;
        }


        void SpawnAttackers()
        {
            Attackers = new PedGroup();

            for (int i = 0; i < Wave * 2; i++)
            {
                Ped p = World.CreatePed(PedHash.MexGang01GMY, SpawnLocation.Around(5), TargetLocation.ToHeading());
                p.BlockPermanentEvents = true;
                p.AlwaysKeepTask = true;
                p.Task.RunTo(TargetLocation);
                Attackers.Add(p, i == 0);
                p.NeverLeavesGroup = true;
                PedPool.Add(p);

                BlipPool.Add(p.AddBlip());
            }

            Attackers.Formation = Formation.Loose;
        }


        void DeleteAttackers()
        {
            for (int i = PedPool.Count - 1; i >= 0; i--)
            {
                Ped p = PedPool[i];
                DeletePedAndBlip(p);
            }
        }

        bool IsGroupDead(PedGroup pedGroup)
        {
            foreach (Ped p in pedGroup)
            {
                if (p.IsAlive) return false;
            }

            return true;
        }

        bool DidGroupGetToWaypoint(PedGroup pedGroup)
        {
            foreach (Ped p in pedGroup)
            {
                if (p.IsDead) continue;

                if (p.Position.DistanceTo2D(TargetLocation) < 5)
                {
                    return true;
                }
            }

            return false;
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

            GTA.UI.Screen.ShowSubtitle($"Wave {Wave} | Prevent the ~r~Attackers ~w~from reaching the ~y~target.", 1);

            // Show the target marker
            World.DrawMarker(MarkerType.VerticalCylinder, TargetLocation, Vector3.Zero, Vector3.Zero, new Vector3(10, 10, 10), Color.Red);

            // We check for a wave ending condition, and stop checking if there's one
            if (DidGroupGetToWaypoint(Attackers))
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
                        Notification.Show("Collided with: " + rr.HitPosition.ToString());
                    }



                    break;
                case Keys.Y:
                    State = DefenseState.PREPARING;
                    break;
            }
        }

        private void OnAbort(object sender, EventArgs e)
        {
            List<PoolObject> AllObjects = new List<PoolObject>();
            AllObjects.AddRange(BlipPool);
            AllObjects.AddRange(PedPool);
            AllObjects.AddRange(VehiclePool);

            foreach (var item in AllObjects)
            {
                item.Delete();
            }
        }
    }
}
