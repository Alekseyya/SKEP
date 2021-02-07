using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class Limit
    {
        public decimal LimitAmountReserved { get; set; }
        public decimal LimitAmount { get; set; }
        public decimal LimitAmountActuallySpent { get; set; }
    }
}
