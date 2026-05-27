namespace RozgrywkaKoncowa.Models
{
    public class CRank
    {
        public int Value { get; }
        public string Symbol { get; }
        public string PolishSymbol { get; }

        private CRank(int value, string symbol, string polish)
        {
            Value = value;
            Symbol = symbol;
            PolishSymbol = symbol;
        }

        public static readonly CRank R2  = new( 2, "2","2");
        public static readonly CRank R3  = new( 3, "3","3");
        public static readonly CRank R4  = new( 4, "4","4");
        public static readonly CRank R5  = new( 5, "5","5");
        public static readonly CRank R6  = new( 6, "6","6");
        public static readonly CRank R7  = new( 7, "7","7");
        public static readonly CRank R8  = new( 8, "8","8");
        public static readonly CRank R9  = new( 9, "9","9");
        public static readonly CRank RT  = new(10, "T","T");
        public static readonly CRank RJ  = new(11, "J","W");
        public static readonly CRank RQ  = new(12, "Q","D");
        public static readonly CRank RK  = new(13, "K","K");
        public static readonly CRank RA  = new(14, "A","A");

        public static IReadOnlyList<CRank> All => AllHolder.Value;

        private static class AllHolder
        {
            internal static readonly IReadOnlyList<CRank> Value =
                RozgrywkaKoncowa.Utils.SmartEnum<CRank>.All;
        }

        public static CRank? FromValue(int value) =>
            RozgrywkaKoncowa.Utils.SmartEnum<CRank>.FromValue(r => r.Value, value);

        public static CRank? FromSymbol(string symbol) =>
            RozgrywkaKoncowa.Utils.SmartEnum<CRank>.FromValue(r => r.Symbol, symbol);

        public override string ToString() => Symbol;
        public string ToSymbol() => Symbol;
    }
}
