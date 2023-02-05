using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.UI;
using static LastStand.LastStand;
using static LastStand.Entities;

namespace LastStand
{
    internal static class Abilities
    {
        public static void AllowStrikeAbilityThisFrame()
        {
            if (StrikePosition == null) return;
            // TODO: Countdown with number marker types

            World.DrawMarker(MarkerType.UpsideDownCone, (Vector3)StrikePosition, Vector3.Zero, Vector3.Zero, new Vector3(3, 3, 3), Color.Red);

            if (Game.GameTime <= StrikeTargetTime) return;
            // TODO: kinda lame


            for (var i = 0; i < 5; i++)
            {
                var finalPosition = ((Vector3)StrikePosition).Around(3.5f);

                World.ShootBullet((Vector3)StrikePosition + Vector3.WorldUp * 10,
                    finalPosition,
                    Game.Player.Character,
                    WeaponHash.Railgun,
                    100);
                //Script.Wait(500 * i);
            }
            // TODO: No sound or VFX
            //World.AddExplosion((Vector3)StrikePosition, ExplosionType.OrbitalCannon, 5, 5, Game.Player.Character);

            StrikePosition = null;
        }
    }

    internal static class Entities
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

        private static void DeleteBlips()
        {
            ClearPool(BlipPool);
        }

        private static void DeleteVehicles()
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
            var p = World.CreateProp(propLocation.Name, propLocation.Position, propLocation.Rotation, false, false);
            if (p == null) return;
            
            Notification.Show("Spawned " + propLocation.Name);
            PropPool.Add(p);

            p.IsPositionFrozen = true;
        }

        public static List<PoolObject> ToPoolObject<T>(this List<T> list)
        {
            return list.Cast<PoolObject>().ToList();
        }

        public static void ClearPool<T>(List<T> pool)
        {
            var casted = pool.ToPoolObject();
            for (var i = casted.Count - 1; i >= 0; i--)
            {
                var obj = casted[i];
                casted.Remove(obj);
                obj.Delete();
            }
        }

        public static void SpawnAttackers(DefenseZone zone)
        {
            Attackers = new PedGroup();

            for (var i = 0; i < (Wave * 2) + 1; i++)
            {
                var p = World.CreatePed(PedHash.MexGang01GMY, zone.SpawnLocation.Around(5), zone.TargetLocation.ToHeading());
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
            return pedGroup.All(p => !p.IsAlive);
        }

        public static bool DidGroupGetToWaypoint(PedGroup pedGroup, DefenseZone zone)
        {
            return pedGroup.Where(p => !p.IsDead).Any(p => p.Position.DistanceTo2D(zone.TargetLocation) < 3);
        }

        public static void DeletePedAndBlip(Ped p)
        {
            var b = p.AttachedBlip;
            UI.RemoveBlip(ref b);

            p.LeaveGroup();
            PedPool.Remove(p);
            p.Delete();
        }

    }

    // ReSharper disable once InconsistentNaming
    internal static class UI
    {
        private static Blip[] _startBlips;
        private static Blip _targetBlip;

        private static void CreateBlip(Vector3 location, BlipSprite sprite, string name, BlipColor color, ref Blip b)
        {
            if (b != null) return;
            
            b = World.CreateBlip(location);
            b.Sprite = sprite;
            b.Name = name;
            b.Color = color;
            BlipPool.Add(b);
        }

        public static void RemoveBlip(ref Blip b)
        {
            if (b == null) return;
            BlipPool.Remove(b);
            b.Delete();
            b = null;
        }

        public static void CreateStartBlips(DefenseZone[] zones)
        {
            _startBlips = new Blip[zones.Length];

            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];

                if (_startBlips[i] != null) continue;
                var b = World.CreateBlip(zone.StartLocation);
                b.Sprite = BlipSprite.Deathmatch;
                b.Name = $"Defense {i + 1}";
                b.Color = BlipColor.Blue;
                BlipPool.Add(b);
                _startBlips[i] = b;
            }
        }
        public static void RemoveStartBlips()
        {
            for (var i = 0; i < _startBlips.Length; i++)
            {
                var b = _startBlips[i];
                if (b == null) continue;
                BlipPool.Remove(b);
                b.Delete();
                _startBlips[i] = null;
            }
        }
        public static void CreateTargetBlip(Vector3 targetLocation)
        {
            CreateBlip(targetLocation, BlipSprite.Standard, "Target", BlipColor.Yellow, ref _targetBlip);
        }
        public static void RemoveTargetBlip()
        {
            RemoveBlip(ref _targetBlip);
        }
        public static void RemoveDeadBlips()
        {
            foreach (var b in from p in PedPool let b = p.AttachedBlip where b != null where p.IsDead select b)
            {
                BlipPool.Remove(b);
                b.Delete();
            }
        }
    }
}
