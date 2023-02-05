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

        public void Deconstruct(out string name, out Vector3 position, out Vector3 rotation)
        {
            name = this.Name;
            position = this.Position;
            rotation = this.Rotation;
        }
    }
}
