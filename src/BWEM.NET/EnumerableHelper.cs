using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BWEM.NET
{
    public static class EnumerableHelper
    {
        /// <summary>Gets the maximum number of elements that may be contained in an array.</summary>
        /// <returns>The maximum count of elements allowed in any array.</returns>
        /// <remarks>
        /// <para>This property represents a runtime limitation, the maximum number of elements (not bytes)
        /// the runtime will allow in an array. There is no guarantee that an allocation under this length
        /// will succeed, but all attempts to allocate a larger array will fail.</para>
        /// <para>This property only applies to single-dimension, zero-bound (SZ) arrays.
        /// <see cref="Length"/> property may return larger value than this property for multi-dimensional arrays.</para>
        /// </remarks>
        public static int ArrayMaxLength
        {
            get
            {
                // Keep in sync with `inline SIZE_T MaxArrayLength()` from gchelpers and HashHelpers.MaxPrimeArrayLength.
                return 0X7FFFFFC7;
            }
        }

        public static void AddRepeat<T>(this List<T> list, int n, T item)
        {
            while (n > 0)
            {
                list.Add(item);
                n--;
            }
        }

        public static void FastRemove<T>(this List<T> list, T item)
        {
            var itemIdx = list.IndexOf(item);
            if (itemIdx >= 0)
            {
                list.FastRemoveAt(itemIdx);
            }
        }

        public static void FastRemoveAt<T>(this List<T> list, int index)
        {
            Debug.Assert(index >= 0 && index < list.Count);

            list[index] = list[^1];
            list.RemoveAt(list.Count - 1);
        }

        /// <summary>Converts an enumerable to an array using the same logic as List{T}.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <param name="length">The number of items stored in the resulting array, 0-indexed.</param>
        /// <returns>
        /// The resulting array.  The length of the array may be greater than <paramref name="length"/>,
        /// which is the actual number of elements in the array.
        /// </returns>
        public static T[] ToArray<T>(IEnumerable<T> source, out int length)
        {
            if (source is ICollection<T> ic)
            {
                var count = ic.Count;
                if (count != 0)
                {
                    // Allocate an array of the desired size, then copy the elements into it. Note that this has the same
                    // issue regarding concurrency as other existing collections like List<T>. If the collection size
                    // concurrently changes between the array allocation and the CopyTo, we could end up either getting an
                    // exception from overrunning the array (if the size went up) or we could end up not filling as many
                    // items as 'count' suggests (if the size went down).  This is only an issue for concurrent collections
                    // that implement ICollection<T>, which as of .NET 4.6 is just ConcurrentDictionary<TKey, TValue>.
                    var arr = new T[count];
                    ic.CopyTo(arr, 0);
                    length = count;
                    return arr;
                }
            }
            else
            {
                using var en = source.GetEnumerator();

                if (en.MoveNext())
                {
                    const int DefaultCapacity = 4;
                    var arr = new T[DefaultCapacity];
                    arr[0] = en.Current;
                    var count = 1;

                    while (en.MoveNext())
                    {
                        if (count == arr.Length)
                        {
                            // This is the same growth logic as in List<T>:
                            // If the array is currently empty, we make it a default size.  Otherwise, we attempt to
                            // double the size of the array.  Doubling will overflow once the size of the array reaches
                            // 2^30, since doubling to 2^31 is 1 larger than Int32.MaxValue.  In that case, we instead
                            // constrain the length to be Array.MaxLength (this overflow check works because of the
                            // cast to uint).
                            var newLength = count << 1;
                            if ((uint)newLength > ArrayMaxLength)
                            {
                                newLength = ArrayMaxLength <= count ? count + 1 : ArrayMaxLength;
                            }

                            Array.Resize(ref arr, newLength);
                        }

                        arr[count++] = en.Current;
                    }

                    length = count;
                    return arr;
                }
            }

            length = 0;
            return Array.Empty<T>();
        }
    }
}