using System;

namespace Core.Models
{
    public class Summary
    {
        public Summary()
        {
            LimitAmount = Decimal.Zero;
            LimitAmountReservedAndActuallySpent = Decimal.Zero;
            Month = 0;
        }
        public decimal LimitAmount { get; set; }
        public decimal LimitAmountReservedAndActuallySpent { get; set; }
        public int FactPlanPercent
        {
            get
            {
                if (LimitAmount == Decimal.Zero)
                    return 0;

                return Decimal.ToInt32(LimitAmountReservedAndActuallySpent / LimitAmount * 100);
            }
        }
        public decimal LimitBalance { get { return LimitAmount - LimitAmountReservedAndActuallySpent; } }
        public int Month { get; set; }
    }
}
