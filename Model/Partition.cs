using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    /// <summary>
    /// a partition is a half open, half closed range:(MinScore,MaxScore]
    /// </summary>
    public class Partition
    {
        public decimal MaxScore { get; set; }
        public decimal MinScore { get; set; }
        public int HighRank { get; set; }
        public int LowRank { get; set; }
        /// <summary>
        /// the customers with score between MinScore and MaxScore
        /// </summary>
        public SortedList<Customer,object> PtCustomers { get; set; } = new SortedList<Customer,object>(new CustomerComparer());
    }
}
