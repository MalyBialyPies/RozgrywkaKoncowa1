using System.Collections.Generic;

namespace RozgrywkaKoncowa.Models
{
    public class CHand : List<CCard>
    {
        public CHand() : base() { }

        public CHand(IEnumerable<CCard> cards) : base(cards) { }
    }
}
