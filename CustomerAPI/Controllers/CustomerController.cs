using Microsoft.AspNetCore.Mvc;
using Model;
using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.Controllers
{
    /// <summary>
    /// Provides score update and leaderboard endpoints.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class CustomerController:ControllerBase
    {
        /// <summary>
        /// Adds a score delta to a customer, creating the customer if needed.
        /// </summary>
        /// <param name="customerid">Positive customer identifier.</param>
        /// <param name="score">Score delta, from -1000 to 1000 inclusive.</param>
        /// <returns>The customer's resulting total score.</returns>
        /// <response code="200">The score was updated.</response>
        /// <response code="400">The customer identifier or score delta is invalid.</response>
        [HttpPost("/customer/{customerid}/score/{score}")]
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<decimal> UpdateScore(Int64 customerid,decimal score)
        {
            if(customerid <= 0)
            {
                return BadRequest("illegal value of parameter:'customerid'");
            }

            if(score > 1000 || score < -1000)
            {
                return BadRequest("illegal value of parameter:'score'");
            }

            return Storage.UpdateScore(customerid, score);
        }

        /// <summary>
        /// Returns leaderboard entries for an inclusive rank range.
        /// </summary>
        /// <param name="start">One-based first rank to return.</param>
        /// <param name="end">Inclusive final rank to return.</param>
        /// <returns>Leaderboard entries ordered by score descending.</returns>
        /// <response code="200">The requested leaderboard entries.</response>
        /// <response code="400">A rank is invalid or start is greater than end.</response>
        /// <response code="404">No leaderboard entries exist in the requested range.</response>
        [HttpGet("/leaderboard")]
        [ProducesResponseType(typeof(List<LeaderboardItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<List<LeaderboardItem>> GetCustomersByRank([FromQuery] int start, [FromQuery] int end)
        {
            if(start <= 0 || end <= 0) 
            {
                return BadRequest("illegal value of parameter:'start' or 'end'");
            }

            if(start > end)
            {
                return BadRequest("value of parameter:'start' should not be greater than value of parameter:'end'");
            }

            List<LeaderboardItem> ranks = Storage.GetLdboardByRange(start, end);

            if(ranks.Count == 0)
            {
                return NotFound();
            }

            return ranks;
        }

        /// <summary>
        /// Returns a customer's rank and optional neighboring leaderboard entries.
        /// </summary>
        /// <param name="customerid">Positive customer identifier.</param>
        /// <param name="high">Number of preceding ranks to include; defaults to zero.</param>
        /// <param name="low">Number of following ranks to include; defaults to zero.</param>
        /// <returns>The customer's leaderboard neighborhood.</returns>
        /// <response code="200">The customer and requested neighbors.</response>
        /// <response code="400">The customer identifier or neighbor count is invalid.</response>
        /// <response code="404">The customer has no leaderboard rank.</response>
        [HttpGet("/leaderboard/{customerid}")]
        [ProducesResponseType(typeof(List<LeaderboardItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<List<LeaderboardItem>> GetCustomersByID([FromRoute] Int64 customerid,[FromQuery] int high = 0, [FromQuery] int low = 0)
        {
            if (customerid <= 0)
            {
                return BadRequest("illegal value of parameter:'customerid'");
            }

            if(high < 0)
            {
                return BadRequest("illegal value of parameter:'high'");
            }

            if(low < 0)
            {
                return BadRequest("illegal value of parameter:'low'");
            }

            List<LeaderboardItem> ctmRanks = Storage.GetCustomerRankByID(customerid, high, low);

            if(ctmRanks.Count==0)
            {
                return NotFound();
            }

            return ctmRanks;
        }
    }
}
