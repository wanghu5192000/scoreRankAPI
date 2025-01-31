using Microsoft.AspNetCore.Mvc;
using Model;
using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController:ControllerBase
    {
        [HttpPost("/customer/{customerid}/score/{score}")]
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

        [HttpGet("/leaderboard")]
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

        [HttpGet("/leaderboard/{customerid}")]
        public ActionResult<List<LeaderboardItem>> GetCustomersByID([FromRoute] Int64 customerid,[FromQuery] int high, [FromQuery] int low)
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
