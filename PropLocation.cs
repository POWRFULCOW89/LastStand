using GTA.Math;

namespace LastStand
{
    public readonly struct PropLocation
    {
        public readonly string Name;
        public readonly Vector3 Position;
        public readonly Vector3 Rotation;

        public PropLocation(string name, Vector3 position, Vector3 rotation)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
        }

        public void Deconstruct(out string Name, out Vector3 Position, out Vector3 Rotation)
        {
            Name = this.Name;
            Position = this.Position;
            Rotation = this.Rotation;
        }
    }
}
