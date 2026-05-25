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
         
            var contract = new CContract(CLevel.Level2, CDenomination.Spade, CDouble.Doubled);
            var board = new CBoard(north, east, south, west, CPlayer.PlayerNorth, contract);
            return View("~/Views/Home/Rozgrywka.cshtml", board);
        }
    }
}
