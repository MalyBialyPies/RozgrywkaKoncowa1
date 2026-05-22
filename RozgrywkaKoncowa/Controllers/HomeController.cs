using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RozgrywkaKoncowa.Models;

namespace RozgrywkaKoncowa.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult GeneracjaRozdania()
        {
            var rng = new Random();

            // Tworzymy taliê 52 kart (tylko kolory karciane, bez NT)
            var denominations = CDenomination.All.Where(d => d.Value > 0 && d != CDenomination.NT).ToArray();
            var deck = (from d in denominations
                        from r in CRank.All
                        select new CCard(d, r)).ToList();

            // Tasujemy
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }

            // Rozdajemy po 13 kart: N, E, S, W
            var north = new CHand(deck.Skip(0).Take(13));
            var east  = new CHand(deck.Skip(13).Take(13));
            var south = new CHand(deck.Skip(26).Take(13));
            var west  = new CHand(deck.Skip(39).Take(13));

            // Losowy Declarer (N/E/S/W)
            var players = CPlayer.All.Where(p => p.Value > 0).ToArray();
            var declarer = players[rng.Next(players.Length)];

            // Losowy kontrakt (poziom 1-7, dowolny kolor + NT, losowe doblowanie)
            var contractDenominations = CDenomination.All.Where(d => d.Value > 0).ToArray();
            var levels = CLevel.All.Where(l => l.Value > 0).ToArray();
            var doubles = CDouble.All.Where(d => d.Value > 0).ToArray();
            var contract = new CContract(
                levels[rng.Next(levels.Length)],
                contractDenominations[rng.Next(contractDenominations.Length)],
                doubles[rng.Next(doubles.Length)]
            );

            var board = new CBoard(north, east, south, west, declarer, contract);
            return View(board);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
