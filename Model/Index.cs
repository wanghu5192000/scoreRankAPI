using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    /// <summary>
    /// container of partition(s)
    /// </summary>
    public class Index
    {
        /// <summary>
        /// maximum score precision 
        /// </summary>
        private const int MaxScorePrecision = 5;
        /// <summary>
        /// maximum customer count in each partition
        /// </summary>
        private const int MaxParSize = 25000;
        /// <summary>
        /// lowest score limit
        /// </summary>
        private const decimal LowestParScore = 0;
        /// <summary>
        /// highest score limit
        /// </summary>
        private const decimal HighestParScore = decimal.MaxValue;

        private const string ErrFmt = "cannot find any partition for score:{0}";
        /// <summary>
        /// read-write lock for the index
        /// </summary>
        private static ReaderWriterLockSlim rwLocker = new ReaderWriterLockSlim();
        /// <summary>
        /// to lock the rank when it is dirty
        /// </summary>
        private static object rankLocker = new object();
        /// <summary>
        /// to indicate if the low/high rank in each partition can be reused
        /// </summary>
        private bool rankDirty = true;
        /// <summary>
        /// to contain all the partitions
        /// </summary>
        private readonly List<Partition> partitions;

        protected Index(Partition iniPart)
        {
            partitions = new List<Partition>() {iniPart };
        }

        /// <summary>
        /// initialzie the index by client
        /// </summary>
        /// <returns></returns>
        public static Index Initialize()
        {
            return new Index(new Partition { MaxScore = HighestParScore, MinScore = LowestParScore });
        }

        /// <summary>
        /// remove the customer from partition 
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public void RemoveCustomer(Int64 custId, decimal score)
        {
            rwLocker.EnterWriteLock();
            try
            {
               Partition? par =  partitions.Find(p => p.MaxScore >= score && p.MinScore < score);

               if(par == null)
                {
                    throw new ApplicationException(string.Format(ErrFmt, score));
                }
 
                par.PtCustomers.Remove(new Customer { Id=custId,Score=score });
                rankDirty = true;
            }
            finally 
            { 
                rwLocker.ExitWriteLock(); 
            }
        }

        /// <summary>
        /// add customer to partition
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="score"></param>
        public void AddCustomer(Int64 custId, decimal score)
        {
            rwLocker.EnterWriteLock();
            try
            {
                Partition? par = partitions.Find(p => p.MaxScore >= score && p.MinScore < score);

                if(par == null)
                {
                    throw new ApplicationException(string.Format(ErrFmt, score));
                }
 
                par.PtCustomers.Add(new Customer { Id = custId, Score = score },null);

                if(par.PtCustomers.Count > MaxParSize)
                {
                    SplitPartition(par);
                }

                rankDirty = true;
            }
            finally
            {
                rwLocker.ExitWriteLock();
            }
        }

        /// <summary>
        /// replace the old score with new score for a customer
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="oldScore"> a positive decimal </param>
        /// <param name="newScore"> a positive decimal </param>
        public void ReplaceCustomerScore(Int64 custId, decimal oldScore, decimal newScore)
        {
            rwLocker.EnterWriteLock();
            try
            {
                Partition? par = partitions.Find(p => p.MaxScore >= oldScore && p.MinScore < oldScore);
                if (par == null)
                {
                    throw new ApplicationException(string.Format(ErrFmt, oldScore));
                }

                Customer tmp = new Customer { Id=custId,Score=oldScore };
                par.PtCustomers.Remove(tmp);

                if(par.MaxScore >= newScore && par.MinScore < newScore)
                {
                    tmp.Score = newScore;
                    par.PtCustomers.Add(tmp,null);
                }
                else
                {
                    Partition? newPar = partitions.Find(p => p.MaxScore >= newScore && p.MinScore < newScore);
                    if (newPar == null)
                    {
                        throw new ApplicationException(string.Format(ErrFmt, newScore));
                    }
                    tmp.Score = newScore;
                    newPar.PtCustomers.Add(tmp, null);
                    if (par.PtCustomers.Count > MaxParSize)
                    {
                        SplitPartition(par);
                    }
                    rankDirty = true;
                }
            }
            finally { rwLocker.ExitWriteLock(); }
        }

        /// <summary>
        /// ////////
        /// </summary>
        /// <param name="high">highRank</param>
        /// <param name="low">lowRank</param>
        /// <returns></returns>
        private List<LeaderboardItem> GetLdItemsByRange(int high, int low)
        {
            List<Partition> parts = partitions.Where(item => (item.HighRank >= high && item.LowRank <= low)
                                                           || (item.HighRank <= high && item.LowRank >= high)
                                                           || (item.HighRank <= low && item.LowRank >= low)
                                                              ).ToList();

            if (parts.Count == 0)
            {
                return new List<LeaderboardItem>();
            }

            List<LeaderboardItem> ldbds = new List<LeaderboardItem>();
            for (int i = 0; i < parts.Count; i++)
            {
                Partition part = parts[i];
                int highest = Math.Max(part.HighRank,high);
                ldbds.AddRange(
                    part.PtCustomers.Where( (pair,index) => part.HighRank + index >= high && part.HighRank + index <= low  )
                                    .Select((pair, index) => new LeaderboardItem { CustomerID = pair.Key.Id.ToString(), Score = pair.Key.Score, Rank = index + highest })
                    );
            }

            return ldbds;
        }

        /// <summary>
        /// get leader board by rank range
        /// </summary>
        /// <param name="high"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public List<LeaderboardItem> GetLdboardByRange(int high, int low)
        {
            rwLocker.EnterReadLock();
            try
            {
                if(rankDirty)
                {
                    lock(rankLocker)
                    {
                        if(rankDirty)
                        {// refresh the rank of each partition
                            UpdateIndexRanks();
                            rankDirty = false;
                        }
                    }
                }
                return GetLdItemsByRange(high, low);
            }
            finally
            {
                rwLocker.ExitReadLock();
            }
        }

        /// <summary>
        /// get ranks by customer id
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="score"></param>
        /// <param name="high">the delta number to minus by customer rank</param>
        /// <param name="low">the delta number to add by customer rank</param>
        /// <returns></returns>
        public List<LeaderboardItem> GetCustomerRanksByID(Int64 custId, decimal score, int high=0, int low=0)
        {
            rwLocker.EnterReadLock();

            try
            {
                if(rankDirty)
                {
                    lock (rankLocker)
                    {
                        if(rankDirty)
                        {
                            UpdateIndexRanks();
                            rankDirty = false;
                        }
                    }
                }

                int rank = GetCustomerRank(custId, score);

                if (rank == -1)
                {
                    return new List<LeaderboardItem>();
                }

                LeaderboardItem custItem = new LeaderboardItem { CustomerID= custId.ToString(), Score=score, Rank = rank };

                if (high == 0 && low == 0)
                {
                    return new List<LeaderboardItem>() { custItem };
                }

                int realHigh = custItem.Rank - high;
                int realLow = low + custItem.Rank;

                return GetLdItemsByRange(realHigh, realLow);

            }
            finally
            {
                rwLocker.ExitReadLock();
            }
        }

        public int PartitionCount()
        {
            return partitions.Count;
        }

        public void Clear()
        {
            partitions.Clear();
            partitions.Add(new Partition { MaxScore = HighestParScore, MinScore = LowestParScore });
        }

        /// <summary>
        /// get rank of a customer
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private int GetCustomerRank(Int64 custId, decimal score)
        {
            //the partition range is a left open, right closed range
            Partition? pt = partitions.Where(p => p.MinScore < score && p.MaxScore >= score).SingleOrDefault();

            if (pt == null)
            {
                throw new InvalidDataException(string.Format("could not find the partition of score:{0}", score));
            }

            int innerIndex = pt.PtCustomers.IndexOfKey(new Customer { Id = custId, Score = score });

            if (innerIndex == -1)
            {
                return -1;
            }

            return pt.HighRank + innerIndex;
        }

        /// <summary>
        /// split the partition if its customer count become larger than MaxParSize.
        /// focus on the partition size insead of the score distribution 
        /// </summary>
        /// <param name="curP"></param>
        private void SplitPartition(Partition curP)
        {
            //parallel query here takes a very long time at its first execution
            decimal mid = curP.PtCustomers.Average(kvp => kvp.Key.Score);
            mid = Math.Round(mid, MaxScorePrecision);

            //sequential query here is super faster than parallel one
            Dictionary<Customer,object> largerDic = curP.PtCustomers.Where(c => c.Key.Score > mid).ToDictionary(r=>r.Key,r=>r.Value);
            Partition largerPart = new Partition { MaxScore = curP.MaxScore, MinScore = mid, PtCustomers = new SortedList<Customer, object>(largerDic, new CustomerComparer()) };

            //create a new sorted list here is much faster than deleting one by one from the old sorted list
            //reuse the old partition while create a new sorted list
            Dictionary<Customer, object> smallerDic = curP.PtCustomers.Where(c => c.Key.Score <= mid).ToDictionary(r => r.Key, r => r.Value);
            curP.MaxScore = mid;
            curP.PtCustomers = new SortedList<Customer, object>(smallerDic, new CustomerComparer());

            partitions.Insert(partitions.IndexOf(curP), largerPart);
        }

        /// <summary>
        /// update the high/low ranks of each partition
        /// </summary>
        private void UpdateIndexRanks()
        {
            //clear high/low rank for the empty partition
            List<Partition> empties = partitions.Where(p => p.PtCustomers.Count == 0).ToList();

            for (int i = 0; i < empties.Count; i++)
            {
                empties[i].HighRank = 0;
                empties[i].LowRank = 0;
            }

            int high = 1;
            List<Partition> nonEmpties = partitions.Where(p => p.PtCustomers.Count > 0).ToList();
            for (int i = 0; i < nonEmpties.Count; i++)
            {
                nonEmpties[i].HighRank = high;
                high = high + nonEmpties[i].PtCustomers.Count;
                nonEmpties[i].LowRank = high - 1;
            }
        }

    }
}
