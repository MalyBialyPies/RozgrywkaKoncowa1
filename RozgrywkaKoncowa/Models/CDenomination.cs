namespace RozgrywkaKoncowa.Models
{
    public enum CDenomination
    {
        Unknown = -1,
        Club = 1,
        Diamond = 2,
        Heart = 3,
        Spade = 4,
        NT = 5
    }

    public static class CDenominationExtensions
    {
        // Returns a single-character suit symbol or "NT" for no-trump
        public static string ToSymbol(this CDenomination d) => d switch
        {
            CDenomination.Club => "♣",
            CDenomination.Diamond => "♦",
            CDenomination.Heart => "♥",
            CDenomination.Spade => "♠",
            CDenomination.NT => "NT",
            _ => "?"
        };

        // Returns the lowercase name of the denomination ("club", "diamond", ...)
        public static string ToName(this CDenomination d) => d switch
        {
            CDenomination.Club => "club",
            CDenomination.Diamond => "diamond",
            CDenomination.Heart => "heart",
            CDenomination.Spade => "spade",
            CDenomination.NT => "nt",
            _ => "unknown"
        };
    }
}
