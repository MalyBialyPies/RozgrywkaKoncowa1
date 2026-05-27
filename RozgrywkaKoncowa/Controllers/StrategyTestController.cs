using Microsoft.AspNetCore.Mvc;
using RozgrywkaKoncowa.Models;
using RozgrywkaKoncowa.Logic;
using System.Linq;

namespace RozgrywkaKoncowa.Controllers
{
    public class StrategyTestController : Controller
    {
        public IActionResult Index()
        {
            var spade = CDenomination.Spade;
            var club = CDenomination.Club;
            // Przykład: N: AQT, S: 23
            var north = new CHand(new[] { new CCard(spade, CRank.RA), new CCard(spade, CRank.RQ), new CCard(spade, CRank.RT) });
            var south = new CHand(new[] { new CCard(spade, CRank.R2), new CCard(spade, CRank.R3) });
            // Dopełnianie krótszej ręki
            int maxLen = System.Math.Max(north.Count, south.Count);
            while (north.Count < maxLen)
                north.Add(new CCard(club, CRank.FromValue(north.Count + 2)));
            while (south.Count < maxLen)
                south.Add(new CCard(club, CRank.FromValue(south.Count + 2)));
            int liczbaLew = maxLen;
            var strategies = StrategyGenerator.GenerateAllStrategies(north, south, liczbaLew);
            var model = strategies
                .Select(s => string.Join("   ", Enumerable.Range(0, liczbaLew).Select(lewa =>
                    string.Join(", ", s.Sequence.Where(x => x.Item3 == lewa).Select(x => $"{(x.Item1 == 0 ? "N" : "S")}:{x.Item2.Rank.Symbol}{(x.Item2.Denomination == club ? "♣" : "")}"))
                )))
                .Distinct()
                .ToList();
            return View(model);
        }
    }
}
