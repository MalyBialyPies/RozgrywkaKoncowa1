using Microsoft.AspNetCore.Mvc;
using RozgrywkaKoncowa.Models;
using RozgrywkaKoncowa.Logic;

namespace RozgrywkaKoncowa.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MinimaxTestController : ControllerBase
    {
        [HttpPost("solve")]
        public IActionResult Solve([FromBody] CBoard board)
        {
            var result = MinimaxSolver.Solve(board);
            return Ok(new
            {
                Minimaks = result.NSWins,
                Sekwencja = result.SequenceLog
            });
        }

        [HttpGet("sample")]
        public IActionResult SampleSolve()
        {
            var spade = CDenomination.Spade;
            var north = new CHand(new[] {
                new CCard(spade, CRank.RA),
                new CCard(spade, CRank.RQ),
                new CCard(spade, CRank.RJ)
            });
            var east = new CHand(new[] {
                new CCard(spade, CRank.RT),
                new CCard(spade, CRank.R6),
                new CCard(spade, CRank.R5)
            });
            var south = new CHand(new[] {
                new CCard(spade, CRank.R2),
                new CCard(spade, CRank.R3),
                new CCard(spade, CRank.R4)
            });
            var west = new CHand(new[] {
                new CCard(spade, CRank.RK),
                new CCard(spade, CRank.R7),
                new CCard(spade, CRank.R8)
            });
            var contract = new CContract(CLevel.Level2, CDenomination.Spade, CDouble.Doubled);
            var board = new CBoard(north, east, south, west, CPlayer.PlayerNorth, contract);
            var result = ProbabilisticSolver.Solve(board);
            return Ok(new
            {
                PrawdopodobienstwoWszystkichLew = result.Probability,
                DrzewoStrategii = result
            });
        }

      }
}
