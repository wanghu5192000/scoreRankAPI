using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;


namespace Model
{
    public static class Storage
    {
        /// <summary>
        /// the storage of the customers<by/>
        /// store customer's id and score<br/>
        /// change from decimal score to customer object only to enable lock 
        /// </summary>
        private readonly static ConcurrentDictionary<Int64, Customer> allCustomers = new ConcurrentDictionary<Int64, Customer>();

        /// <summary>
        /// partitions to make query more easier
        /// </summary>
        private readonly static Index index = Index.Initialize();

        private const string ErrFmt1 = "Argument out of range";

        /// <summary>
        /// increase the score of customer with customerID by score<br/>
        /// if the customer is not exists insert the customer with the score<br/>
        /// re-arrange the index after updated the score <br/>
        /// </summary>
        /// <param name="customerID"></param>
        /// <param name="score"></param>
        public static decimal UpdateScore(Int64 customerID, decimal score)
        {
            if(customerID < 0 || score < -1000 || score > 1000)
            {
                throw new ArgumentException(ErrFmt1);
            }

            Customer newCust = new Customer { Id = customerID, Score = score };

            if (allCustomers.TryAdd(customerID, newCust))
            {
                //lock to make sure the index of the score is created before moved to other partition
                lock (newCust)
                {
                    if (score <= 0)
                    {
                        return score;
                    }

                    //add to index
                    index.AddCustomer(customerID, score);

                    return score;
                }

            }
            else
                {
                //lock per customer to insure the thread safe of the score addtion
                //and the maximum of the concurrency
                Customer cust = allCustomers[customerID];
                lock (cust)
                {
                    decimal oldScore = cust.Score;
                    decimal newScore = oldScore + score;

                    cust.Score = newScore;

                    if(oldScore > 0 && newScore > 0)
                    {// replace oldScore with newScore
                        index.ReplaceCustomerScore(customerID,oldScore,newScore);
                    }
                    else if (oldScore > 0)
                    {//delete the old index
                        index.RemoveCustomer(customerID, oldScore);     
                    }
                    else if (newScore > 0)
                    {//add a new index
                        index.AddCustomer(customerID, newScore);
                    }

                    return newScore;
                }
            }
        }

        public static List<LeaderboardItem> GetLdboardByRange(int start, int end)
        {
            return index.GetLdboardByRange(start,end);
        }

        public static List<LeaderboardItem> GetCustomerRankByID(Int64 customerid, int high = 0, int low = 0)
        {
            Customer? tmpCust = null;

            if (allCustomers.TryGetValue(customerid, out tmpCust))
            {
                if (tmpCust == null || tmpCust.Score <= 0)
                {
                    return new List<LeaderboardItem>();
                }

                return index.GetCustomerRanksByID(customerid,tmpCust.Score,high,low);
            }
            else
            {
                return new List<LeaderboardItem>();
            }
        }

        public static int PartitionCount()
        {
            return index.PartitionCount();
        }
 
        public static void Clear()
        {
            allCustomers.Clear();
            index.Clear();
        }
    }
}
