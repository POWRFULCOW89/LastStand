using GTA;
using GTA.Math;
using GTA.UI;
using LastStand;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static LastStand.LastStand;
using static Utils.Entities;

namespace Utils
{
    static class Abilities
    {
        public static void AllowStrikeAbilityThisFrame()
        {
            if (StrikePosition != null)
            {
                // TODO: Countdown with number marker types

                World.DrawMarker(MarkerType.UpsideDownCone, (Vector3)StrikePosition, Vector3.Zero, Vector3.Zero, new Vector3(3, 3, 3), Color.Red);

                if (Game.GameTime > strikeTargetTime)
                {
                    // TODO: kinda lame


                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 finalPosition = ((Vector3)StrikePosition).Around(3.5f);

                        World.ShootBullet((Vector3)StrikePosition + Vector3.WorldUp * 10, finalPosition, Game.Player.Character, WeaponHash.RailgunXmas3, 100);
                        //Script.Wait(500 * i);
                    }
                    // TODO: No sound or VFX
                    //World.AddExplosion((Vector3)StrikePosition, ExplosionType.OrbitalCannon, 5, 5, Game.Player.Character);

                    StrikePosition = null;
                }
            }
        }
    }

    static class Entities
    {
        public static readonly List<Ped> PedPool = new List<Ped>();
        public static readonly List<Blip> BlipPool = new List<Blip>();
        public static readonly List<Vehicle> VehiclePool = new List<Vehicle>();
        public static readonly List<Prop> PropPool = new List<Prop>();

        public static void ClearAllPools()
        {
            DeleteBlips();
            DeletePeds();
            DeleteVehicles();
            DeleteProps();
        }

        public static void DeletePeds()
        {
            ClearPool(PedPool);
        }

        public static void DeleteBlips()
        {
            ClearPool(BlipPool);
        }

        public static void DeleteVehicles()
        {
            ClearPool(VehiclePool);
        }

        public static void DeleteProps()
        {
            ClearPool(PropPool);
        }

        public static void DeleteAllProps()
        {
            foreach (var item in World.GetAllProps())
            {
                item.Delete();
            }
        }

        public static void SpawnProp(PropLocation propLocation)
        {
            Prop p = World.CreateProp(propLocation.Name, propLocation.Position, propLocation.Rotation, false, false);
            if (p != null)
            {
                Notification.Show("Spawned " + propLocation.Name);
                PropPool.Add(p);

                p.IsPositionFrozen = true;

            }
        }

        public static List<PoolObject> ToPoolObject<T>(this List<T> list)
        {
            return list.Cast<PoolObject>().ToList();
        }

        public static void ClearPool<T>(List<T> pool)
        {
            List<PoolObject> casted = pool.ToPoolObject();
            for (int i = casted.Count - 1; i >= 0; i--)
            {
                PoolObject obj = casted[i];
                casted.Remove(obj);
                obj.Delete();
            }
        }

        public static void SpawnAttackers(DefenseZone zone)
        {
            Attackers = new PedGroup();

            for (int i = 0; i < (Wave * 2) + 1; i++)
            {
                Ped p = World.CreatePed(PedHash.MexGang01GMY, zone.SpawnLocation.Around(5), zone.TargetLocation.ToHeading());
                p.BlockPermanentEvents = true;
                p.AlwaysKeepTask = true;
                p.Task.RunTo(zone.TargetLocation);
                Attackers.Add(p, i == 0);
                p.NeverLeavesGroup = true;
                PedPool.Add(p);

                BlipPool.Add(p.AddBlip());
            }

            Attackers.Formation = Formation.Loose;
        }

        public static void DeleteAttackers()
        {
            for (int i = PedPool.Count - 1; i >= 0; i--)
            {
                Ped p = PedPool[i];
                DeletePedAndBlip(p);
            }
        }

        public static bool IsGroupDead(PedGroup pedGroup)
        {
            foreach (Ped p in pedGroup)
            {
                if (p.IsAlive) return false;
            }

            return true;
        }

        public static bool DidGroupGetToWaypoint(PedGroup pedGroup, DefenseZone zone)
        {
            foreach (Ped p in pedGroup)
            {
                if (p.IsDead) continue;

                if (p.Position.DistanceTo2D(zone.TargetLocation) < 3)
                {
                    return true;
                }
            }

            return false;
        }

        public static void DeletePedAndBlip(Ped p)
        {
            Blip b = p.AttachedBlip;
            UI.RemoveBlip(ref b);

            p.LeaveGroup();
            PedPool.Remove(p);
            p.Delete();
        }

    }

    static class UI
    {
        static Blip[] StartBlips;
        static Blip TargetBlip;

        static void CreateBlip(Vector3 location, BlipSprite sprite, string name, BlipColor color, ref Blip b)
        {
            if (b == null)
            {
                b = World.CreateBlip(location);
                b.Sprite = sprite;
                b.Name = name;
                b.Color = color;
                BlipPool.Add(b);
            }
        }

        public static void RemoveBlip(ref Blip b)
        {
            if (b != null)
            {
                BlipPool.Remove(b);
                b.Delete();
                b = null;
            }
        }

        public static void CreateStartBlips(DefenseZone[] zones)
        {
            StartBlips = new Blip[zones.Length];

            for (int i = 0; i < zones.Length; i++)
            {
                DefenseZone zone = zones[i];

                if (StartBlips[i] == null)
                {
                    Blip b = World.CreateBlip(zone.StartLocation);
                    b.Sprite = BlipSprite.Deathmatch;
                    b.Name = $"Defense {i + 1}";
                    b.Color = BlipColor.Blue;
                    BlipPool.Add(b);
                    StartBlips[i] = b;
                }
            }
        }
        public static void RemoveStartBlips()
        {
            for (int i = 0; i < StartBlips.Length; i++)
            {
                Blip b = StartBlips[i];
                if (b != null)
                {
                    BlipPool.Remove(b);
                    b.Delete();
                    StartBlips[i] = null;
                }
            }
        }
        public static void CreateTargetBlip(Vector3 targetLocation)
        {
            CreateBlip(targetLocation, BlipSprite.Standard, "Target", BlipColor.Yellow, ref TargetBlip);
        }
        public static void RemoveTargetBlip()
        {
            RemoveBlip(ref TargetBlip);
        }
        public static void RemoveDeadBlips()
        {
            foreach (Ped p in PedPool)
            {
                Blip b = p.AttachedBlip;

                if (b != null)
                {
                    if (p.IsDead)
                    {
                        BlipPool.Remove(b);
                        b.Delete();
                    }
                }
            }
        }
    }
}
