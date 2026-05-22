using Microsoft.AspNetCore.Mvc;
using RozgrywkaKoncowa.Models;
using System;
using System.Linq;

namespace RozgrywkaKoncowa.Controllers
{
    public class PlayController : Controller
    {
        public IActionResult Rozgrywka()
        {
            var spade = CDenomination.Spade;
            var north = new CHand(new[] {
                new CCard(spade, CRank.RA),
                new CCard(spade, CRank.RQ)
            });
            var east = new CHand(new[] {
                new CCard(spade, CRank.R4),
                new CCard(spade, CRank.R5)
            });
            var south = new CHand(new[] {
                new CCard(spade, CRank.R2),
                new CCard(spade, CRank.R3)
            });
            var west = new CHand(new[] {
                new CCard(spade, CRank.RK),
                new CCard(spade, CRank.R6)
            });
            var used = new HashSet<string>(north.Concat(east).Concat(south).Concat(west).Select(c => c.ToString()));
            var deck = (from d in CDenomination.All.Where(d => d.Value > 0 && d != CDenomination.NT)
                        from r in CRank.All
                        select new CCard(d, r)).ToList();
            var rng = new Random();
            var rest = deck.Where(c => !used.Contains(c.ToString())).OrderBy(_ => rng.Next()).ToList();
            void Fill(CHand hand)
            {
                while (hand.Count < 13)
                {
                    hand.Add(rest[0]);
                    rest.RemoveAt(0);
                }
            }
            Fill(north); Fill(east); Fill(south); Fill(west);
            var contract = new CContract(CLevel.Level2, CDenomination.Spade, CDouble.Doubled);
            var board = new CBoard(north, east, south, west, CPlayer.PlayerNorth, contract);
            return View("~/Views/Home/Rozgrywka.cshtml", board);
        }
    }
}
