using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Model
{
    public class LeaderboardItem
    {
        public string CustomerID {  get; set; }

        public decimal Score { get; set; }

        public int Rank { get; set; }
    }
}
