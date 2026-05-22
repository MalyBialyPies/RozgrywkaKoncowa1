namespace RozgrywkaKoncowa.Models
{
    public class CCard
    {
        public CCard() { }

        public CCard(CDenomination denomination, CRank rank)
        {
            Denomination = denomination;
            Rank = rank;
        }

        public CDenomination Denomination { get; set; } = CDenomination.Unknown;
        public CRank Rank { get; set; } = CRank.R2;

        public override string ToString() => $"{Denomination.Symbol}{Rank.Symbol}";
    }
}
