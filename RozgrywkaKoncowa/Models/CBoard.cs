namespace RozgrywkaKoncowa.Models
{
    public class CBoard
    {
        // Hands in order N, E, S, W
        public CHand[] Hands { get; set; }

        public CPlayer Declarer { get; set; } = CPlayer.Unknown;

        public CContract Contract { get; set; } = new CContract();

        public CBoard()
        {
            Hands = new CHand[4]
            {
                new CHand(),
                new CHand(),
                new CHand(),
                new CHand()
            };
        }

        public CBoard(CHand north, CHand east, CHand south, CHand west, CPlayer declarer, CContract contract)
        {
            Hands = new CHand[4] { north ?? new CHand(), east ?? new CHand(), south ?? new CHand(), west ?? new CHand() };
            Declarer = declarer;
            Contract = contract ?? new CContract();
        }
    }
}
