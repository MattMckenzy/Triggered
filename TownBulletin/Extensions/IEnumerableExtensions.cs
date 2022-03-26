namespace TownBulletin.Extensions
{
    public static class IEnumerableExtensions
    {
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
