using System;

namespace AirMedia.Core.Utils
{
    public class Range<T> where T : IComparable
    {
        public T Start { get; set; }
        public T End { get; set; }

        public Range(T start, T end)
        {
            Start = start;
            End = end;

            if (End.CompareTo(Start) < 0)
            {
                throw new InvalidOperationException("range End should be >= range start");
            }
        }

        public bool Contains(T element)
        {
            return Start.CompareTo(element) <= 0 && element.CompareTo(End) <= 0;
        }

        public bool IntersectsWith(Range<T> range)
        {
            return !(Start.CompareTo(range.End) >= 0 || End.CompareTo(range.Start) <= 0);
        }

        public Range<T> IntersectWith(Range<T> range)
        {
            if (IntersectsWith(range) == false) return null;

            var start = Start.CompareTo(range.Start) > 0 ? Start : range.Start;
            var end = End.CompareTo(range.End) > 0 ? range.End : End;

            return new Range<T>(start, end);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var range = obj as Range<T>;
            
            return range != null && range.Start.Equals(Start) && range.End.Equals(End);
        }

        public override int GetHashCode()
        {
            return Start.GetHashCode() + End.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1})", Start, End);
        }
    }
}
