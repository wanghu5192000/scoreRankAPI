using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    internal class CustomerComparer : IComparer<Customer>
    {
        public int Compare(Customer? x, Customer? y)
        {
            return -RealCompare(x, y);
        }
        private int RealCompare(Customer? x, Customer? y)
        {
            if(x != null && y!=null)
            {
                if (x.Score > y.Score)
                {
                    return 1;
                }
                else if (x.Score < y.Score)
                {
                    return -1;
                }
                else
                {
                    if (x.Id < y.Id)
                    {
                        return 1;
                    }
                    else if (x.Id > y.Id)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            else if(x == null && y != null)
            {
                return -1;
            }
            else if(x != null && y == null)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
