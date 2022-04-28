namespace Triggered.Extensions
{
    /// <summary>
    /// An enumerable extension to help find first missing integer in an <see cref="IEnumerable{T}"/> of <see cref="int"/>s.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Retrieves the first missing integer in an <see cref="IEnumerable{T}"/> of <see cref="int"/>s.
        /// </summary>
        /// <param name="ints">From extension, the <see cref="IEnumerable{T}"/> of <see cref="int"/>s.</param>
        /// <param name="zeroStart">Identifies if the method should consider 0 as the start of the enumeration.</param>
        /// <returns>The first missing integer.</returns>
        public static int GetFirstMissingInteger(this IEnumerable<int> ints, bool zeroStart = false)
        {
            int comparingNumber = (zeroStart ? 0 : 1);
            foreach (int currentNumber in ints.OrderBy(i => i))
            {
                if (currentNumber != comparingNumber)
                    break;
                else if (currentNumber != comparingNumber - 1)
                    comparingNumber++;
            }

            return comparingNumber;
        }
    }
}
