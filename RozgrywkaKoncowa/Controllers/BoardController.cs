using Microsoft.AspNetCore.Mvc;
using RozgrywkaKoncowa.Models;
using System;
using System.Linq;

namespace RozgrywkaKoncowa.Controllers
{
    public class BoardController : Controller
    {
        public IActionResult GeneracjaRozdania()
        {
            var rng = new Random();
            var denominations = CDenomination.All.Where(d => d.Value > 0 && d != CDenomination.NT).ToArray();
            var deck = (from d in denominations
                        from r in CRank.All
                        select new CCard(d, r)).ToList();
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            var north = new CHand(deck.Skip(0).Take(13));
            var east  = new CHand(deck.Skip(13).Take(13));
            var south = new CHand(deck.Skip(26).Take(13));
            var west  = new CHand(deck.Skip(39).Take(13));
            var players = CPlayer.All.Where(p => p.Value > 0).ToArray();
            var declarer = players[rng.Next(players.Length)];
            var contractDenominations = CDenomination.All.Where(d => d.Value > 0).ToArray();
            var levels = CLevel.All.Where(l => l.Value > 0).ToArray();
            var doubles = CDouble.All.Where(d => d.Value > 0).ToArray();
            var contract = new CContract(
                levels[rng.Next(levels.Length)],
                contractDenominations[rng.Next(contractDenominations.Length)],
                doubles[rng.Next(doubles.Length)]
            );
            var board = new CBoard(north, east, south, west, declarer, contract);
            return View("~/Views/Home/GeneracjaRozdania.cshtml", board);
        }
    }
}
