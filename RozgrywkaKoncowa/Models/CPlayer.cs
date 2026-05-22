namespace RozgrywkaKoncowa.Models
{
    public class CPlayer
    {
        public int Value { get; }
        public string Name { get; }
        public string Symbol { get; }

        private CPlayer(int value, string name,string symbol)
        {
            Value = value;
            Name = name;
            Symbol = symbol;
        }

        public static readonly CPlayer Unknown     = new(-1, "unknown","?");
        public static readonly CPlayer PlayerNorth = new( 1, "north","N");
        public static readonly CPlayer PlayerEast  = new( 2, "east","E");
        public static readonly CPlayer PlayerSouth = new( 3, "south","S");
        public static readonly CPlayer PlayerWest  = new( 4, "west","W");

        public static readonly CPlayer[] All = RozgrywkaKoncowa.Utils.SmartEnum<CPlayer>.All.ToArray();

        public static CPlayer? FromValue(int value) =>
            RozgrywkaKoncowa.Utils.SmartEnum<CPlayer>.FromValue(p => p.Value, value);

        public override string ToString() => Name;
    }
}
