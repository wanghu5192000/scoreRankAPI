using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;
using static System.Formats.Asn1.AsnWriter;

namespace Test
{
    public class StorageTest
    {
        private const decimal MaxInputScore = 1000;
        private const decimal MinInputScore = -1000;
        private const int MaxParSize = 25000;
        [SetUp]
        public void Setup()
        {
        
        }

        //do not share state between each test case
        [TearDown] 
        public void TearDown() 
        {
            Storage.Clear();
        }

        [Test]
        [TestCase(12345678,1001)]
        [TestCase(12345678, 1002)]
        [TestCase(12345678, -1001)]
        [TestCase(12345678, -1002)]
        public void ShowException(Int64 custId,decimal score)
        {
            Assert.Throws<ArgumentException>(() => { Storage.UpdateScore(custId, score); });
        }

        [TestCase(12345678, 1000)]
        [TestCase(12345678, 999)]
        [TestCase(12345678, -1000)]
        [TestCase(12345678, -999)]
        public void ShowNoException(Int64 custId, decimal score)
        {
            Assert.DoesNotThrow(() => { Storage.UpdateScore(custId, score); });
        }

        [Test]
        [TestCase(12345678,30.3,ExpectedResult=30.3)]
        [TestCase(12345678, -20, ExpectedResult = -20)]
        [TestCase(12345678, 1000, ExpectedResult = 1000)]
        [TestCase(12345678, -1000, ExpectedResult = -1000)]
        public decimal CustomerInsertTest(Int64 custId, decimal score)
        {
            return Storage.UpdateScore(custId, score);
        }

        [Test]
        [TestCase(12345678, 30.3, -30.3, ExpectedResult = 0)]
        [TestCase(12345679, 1000, 1000, ExpectedResult = 2000)]
        [TestCase(12345670, -1000, -1000, ExpectedResult = -2000)]
        public decimal CustomerUpdateTest(Int64 custId, decimal oldScore, decimal addScore)
        {
            Storage.UpdateScore(custId, oldScore);
            return Storage.UpdateScore(custId, addScore);
        }

       [Test]
        [TestCase(123456789,20,ExpectedResult =1)]
       public int RankTest(Int64 custId,decimal score)
        {
            Storage.UpdateScore(custId, score);
            List<LeaderboardItem> boards = Storage.GetCustomerRankByID(custId, 2, 2);
            Assert.That(boards != null);
            Assert.That(boards.Count == 1);
            return boards[0].Rank;
        }

        [Test]
        [TestCase(123456789, 20,123456788,20, ExpectedResult = 2)]
        public int RanksWithSameScoreTest(Int64 custId1, decimal score1, Int64 custId2, decimal score2)
        {
            Storage.UpdateScore(custId1, score1);
            Storage.UpdateScore(custId2, score2);
            List<LeaderboardItem> boards = Storage.GetCustomerRankByID(123456789);
            Assert.That(boards != null);
            Assert.That(boards.Count == 1);
            return boards[0].Rank;
        }

        [Test]
        [TestCase(123456789, 19.9, 123456788, 20, ExpectedResult = 2)]
        public int RanksTestWithDiffScore(Int64 custId1, decimal score1, Int64 custId2, decimal score2)
        {
            Storage.UpdateScore(custId1, score1);
            Storage.UpdateScore(custId2, score2);
            List<LeaderboardItem> boards = Storage.GetCustomerRankByID(custId1);
            Assert.That(boards != null);
            Assert.That(boards.Count == 1);
            return boards[0].Rank;
        }

        [Test]
        [TestCase(1234567,30,12345678,ExpectedResult =0)]
        public int RankNotFoundTest(Int64 custId1,decimal score,Int64 custId2)
        {
            Storage.UpdateScore(custId1, score);
            List<LeaderboardItem> boards = Storage.GetCustomerRankByID(custId2);
            Assert.That(boards != null);
            return boards.Count;
        }

        [Test]
        public void RankRangeTest()
        {
            Int64[] custIds = new Int64[10];
            decimal[] decimals = new decimal[10];
            int[] signs = new int[2] { -1, 1 };
            Random rnd = new Random();
            Int64 startId = rnd.NextInt64(0, Int64.MaxValue);

            for (int i = 0; i < custIds.Length; i++)
            {
                custIds[i] = startId++;
            }

            for(int j = 0; j < decimals.Length; j++)
            {
                decimals[j] = Convert.ToDecimal((rnd.NextDouble() * 1000 * signs[rnd.Next(0, 2)]).ToString(".0"));
            }

            for(int i = 0; i < custIds.Length; i++)
            {
                Storage.UpdateScore(custIds[i], decimals[i]);
            }

            decimal max = decimals.Max();
            List<LeaderboardItem> boards = Storage.GetLdboardByRange(1, custIds.Length);

            if (max <= 0)
            {
                Assert.That(boards.Count == 0);
            }
            else
            {
                Assert.That(boards[0].Rank == 1);
                Assert.That(boards[0].Score == max);
            }
        }

        [Test]
        [TestCase(12345678,15,12345679,12,12345670,11)]
        public void RankByCustIdTest(Int64 custId1,decimal score1,Int64 custId2, decimal score2, Int64 custId3, decimal score3)
        {
            Storage.UpdateScore(custId1,score1);
            Storage.UpdateScore(custId2, score2);
            Storage.UpdateScore(custId3, score3);

            List<LeaderboardItem> items = Storage.GetCustomerRankByID(custId2, 1, 1);

            Assert.That(items.Count == 3);
            Assert.That(items[0].Rank == 1);
            Assert.That(items[1].Rank == 2);
            Assert.That(items[2].Rank == 3);

            Assert.That(items[1].CustomerID == custId2.ToString());
        }

        [Test]
        [TestCase(MaxParSize * 100,123456,10,20,5000)]
        public void MoveFromOnePartitionToAnotherTest(int totalSize,Int64 custId,decimal start, decimal step, int repeat)
        {
            Assert.DoesNotThrow(()=>{
                GetPartitionCountByTotalSize(totalSize);
                UpdateCustomerRepeatly(custId, start, step, repeat);
            });

            List<LeaderboardItem> boards = Storage.GetCustomerRankByID(custId);
            Assert.That(1==boards.Count);
            Assert.That(1==boards[0].Rank);
        }

        [Test]
        [TestCase(MaxParSize * 200, 123456, 1000, -10, 5000)]
        public void MoveFromOnePartitionToAnother2Test(int totalSize, Int64 custId, decimal start, decimal step, int repeat)
        {
            Assert.DoesNotThrow(() => {
                GetPartitionCountByTotalSize(totalSize);
                UpdateCustomerRepeatly(custId, start, step, repeat);
            });

            List<LeaderboardItem> boards = Storage.GetCustomerRankByID(custId);
            Assert.That(0==boards.Count);
        }

        [Test]
        [TestCase(MaxParSize, ExpectedResult =1) ]
        public int PartitionCountOneTest(int size)
        {
            return GetPartitionCountByTotalSize(size);
        }

        [Test]
        [TestCase(MaxParSize + 1, ExpectedResult = 2)]
        public int PartitionCountTwoTest(int size)
        {
            return GetPartitionCountByTotalSize(size);
        }

        [Test]
        [TestCase(MaxParSize * 2 + 1, ExpectedResult = 3)]
        public int PartitionCountThreeTest(int size)
        {
            return GetPartitionCountByTotalSize(size);
        }

        private int GetPartitionCountByTotalSize(int size)
        {
            Int64[] custIds = new Int64[size];
            decimal[] decimals = new decimal[size];
            Random rnd = new Random();
            Int64 startId = rnd.NextInt64(1, Int64.MaxValue);

            for (int i = 0; i < custIds.Length; i++)
            {
                custIds[i] = startId++;
            }

            for (int j = 0; j < decimals.Length; j++)
            {
                decimal tmp = Convert.ToDecimal((rnd.NextDouble() * 1000).ToString(".0"));
                if(tmp == 0)
                {
                    tmp = 0.1M;
                }
                decimals[j] = tmp;
            }

            for (int i = 0; i < custIds.Length; i++)
            {
                Storage.UpdateScore(custIds[i], decimals[i]);
            }

            return Storage.PartitionCount();
        }

        private void UpdateCustomerRepeatly(Int64 custId, decimal start, decimal step, int repeat)
        {
            for(int i=1; i<=repeat; i++)
            {
                decimal score = start + (i - 1) * step;
                if(score > MaxInputScore)
                {
                    score = MaxInputScore;
                }
                else if(score < MinInputScore)
                {
                    score = MinInputScore;
                }
 
                Storage.UpdateScore(custId, score);
            }
        }
    }
}
