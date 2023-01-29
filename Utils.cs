using GTA;
using GTA.Math;
using static LastStand.LastStand;

namespace LastStand
{
    class Utils
    {
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

        public static void CreateStartBlip()
        {
            CreateBlip(StartLocation, BlipSprite.Deathmatch, "Defense", BlipColor.Blue, ref StartBlip);
        }

        public static void RemoveStartBlip()
        {
            RemoveBlip(ref StartBlip);
        }
        public static void CreateTargetBlip()
        {
            CreateBlip(TargetLocation, BlipSprite.Standard, "Target", BlipColor.Yellow, ref TargetBlip);
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
