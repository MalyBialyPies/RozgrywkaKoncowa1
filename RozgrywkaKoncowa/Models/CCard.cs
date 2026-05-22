namespace RozgrywkaKoncowa.Models
{
    public class CCard
    {
        public CCard() { }

        public CCard(CDenomination denomination, string rank)
        {
            Denomination = denomination;
            Rank = rank;
        }

        public CDenomination Denomination { get; set; } = CDenomination.Unknown;
        public string Rank { get; set; } = string.Empty; // use values from CRank.Values

        public override string ToString() => $"{Rank} of {Denomination}";
    }
}
