namespace RozgrywkaKoncowa.Models
{
    public class CLevel
    {
        public int Value { get; }
        public string Symbol { get; }

        private CLevel(int value, string symbol)
        {
            Value = value;
            Symbol = symbol;
        }

        public static readonly CLevel Unknown = new(-1, "?");
        public static readonly CLevel Pass    = new( 0, "Pass");
        public static readonly CLevel Level1  = new( 1, "1");
        public static readonly CLevel Level2  = new( 2, "2");
        public static readonly CLevel Level3  = new( 3, "3");
        public static readonly CLevel Level4  = new( 4, "4");
        public static readonly CLevel Level5  = new( 5, "5");
        public static readonly CLevel Level6  = new( 6, "6");
        public static readonly CLevel Level7  = new( 7, "7");

        public static readonly CLevel[] All = RozgrywkaKoncowa.Utils.SmartEnum<CLevel>.All.ToArray();

        public static CLevel? FromValue(int value) =>
            RozgrywkaKoncowa.Utils.SmartEnum<CLevel>.FromValue(l => l.Value, value);

        public override string ToString() => Symbol;
    }
}
