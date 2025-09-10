using System.Collections.Generic;

namespace Engine.World
{
    public class WorldState
    {
        private HashSet<string> _flags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        public static WorldState Instance { get; } = new WorldState();
        private WorldState() { }

        public void SetFlag(string name, bool value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (value) _flags.Add(name);
            else _flags.Remove(name);
        }

        public bool IsFlagSet(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return _flags.Contains(name);
        }
    }
}
