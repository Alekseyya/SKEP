using System;

namespace Core
{
    public struct DateTimeRange
    {
        public DateTime Begin { get; }

        public DateTime End { get; }

        public DateTimeRange(DateTime begin, DateTime end)
        {
            if (end < begin)
                throw new ArgumentException("Begin date should be less than or equal to end date");

            Begin = begin;
            End = end;
        }

        public bool Includes(DateTime date)
        {
            return (Begin <= date) && (date <= End);
        }

        public bool Includes(DateTimeRange range)
        {
            return (Begin <= range.Begin) && (range.End <= End);
        }

        public bool Overlaps(DateTimeRange range)
        {
            return Includes(range.Begin) || Includes(range.End) || range.Includes(Begin);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DateTimeRange))
                return false;
            var other = (DateTimeRange)obj;
            return (Begin == other.Begin) && (End == other.End);
        }

        public override int GetHashCode()
        {
            int first = Begin.GetHashCode();
            int second = End.GetHashCode();
            return first ^ (second << 2);
        }

        public static bool operator ==(DateTimeRange first, DateTimeRange second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(DateTimeRange first, DateTimeRange second)
        {
            return !first.Equals(second);
        }
    }
}
