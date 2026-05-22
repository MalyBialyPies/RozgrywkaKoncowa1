namespace RozgrywkaKoncowa.Models
{
    public class CContract
    {
        public CLevel Level { get; set; } = CLevel.Unknown;
        public CDenomination Denomination { get; set; } = CDenomination.Unknown;
        public CDouble Double { get; set; } = CDouble.Unknown;

        public CContract() { }

        public CContract(CLevel level, CDenomination denomination, CDouble dbl)
        {
            Level = level;
            Denomination = denomination;
            Double = dbl;
        }

        public override string ToString()
        {
            if (Level == CLevel.Unknown) return "pass";
            return $"{Level}{Denomination}({Double.ToString()})";
        }
    }
}
