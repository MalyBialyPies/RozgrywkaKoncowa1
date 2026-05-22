namespace RozgrywkaKoncowa.Models
{
    public class CDenomination
    {
        public int Value { get; }
        public string Name { get; }
        public string Symbol { get; }

        private CDenomination(int value, string name, string symbol)
        {
            Value = value;
            Name = name;
            Symbol = symbol;
        }

        public static readonly CDenomination Unknown  = new(-1, "unknown",  "?");
        public static readonly CDenomination Club     = new( 1, "club",     "♣");
        public static readonly CDenomination Diamond  = new( 2, "diamond",  "♦");
        public static readonly CDenomination Heart    = new( 3, "heart",    "♥");
        public static readonly CDenomination Spade    = new( 4, "spade",    "♠");
        public static readonly CDenomination NT       = new( 5, "nt",       "NT");

        public static readonly CDenomination[] All = RozgrywkaKoncowa.Utils.SmartEnum<CDenomination>.All.ToArray();

        public static CDenomination? FromValue(int value) =>
            RozgrywkaKoncowa.Utils.SmartEnum<CDenomination>.FromValue(d => d.Value, value);

        public override string ToString() => Name;
        public string ToSymbol() => Symbol;
    }
}
