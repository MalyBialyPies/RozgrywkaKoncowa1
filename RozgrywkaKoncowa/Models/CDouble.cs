namespace RozgrywkaKoncowa.Models
{
    public class CDouble
    {
        public int Value { get; }
        public string Symbol { get; }

        private CDouble(int value, string symbol)
        {
            Value = value;
            Symbol = symbol;
        }

        public static readonly CDouble Unknown   = new(-1, "?");
        public static readonly CDouble Doubled   = new( 1, "X");
        public static readonly CDouble Redoubled = new( 2, "XX");
        public static readonly CDouble Pass      = new( 3, "Pass");

        public static readonly CDouble[] All = RozgrywkaKoncowa.Utils.SmartEnum<CDouble>.All.ToArray();

        public static CDouble? FromValue(int value) =>
            RozgrywkaKoncowa.Utils.SmartEnum<CDouble>.FromValue(d => d.Value, value);

        public override string ToString() => Symbol;
    }
}
