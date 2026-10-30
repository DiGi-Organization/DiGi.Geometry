using DiGi.Geometry.Planar.Classes;
using System.Collections.Generic;

namespace DiGi.Geometry.Planar
{
    public static partial class Query
    {
        /// <summary>
        /// Gets a comparison that orders keys along a Z-order (Morton) curve of their bounding boxes, so that consecutive keys lie close together.
        /// <para>Each box is placed in a square cell of <paramref name="cellSize"/> by its lower-left corner, measured from the lower-left corner of all the boxes, and the cells are ordered by the interleaved bits of their column and row. A run of consecutive keys then covers a compact area rather than a strip, which is what batching spatial data needs. Keys of one cell are ordered by <see cref="Comparer{T}.Default"/>, and keys with no box come last, so the order is total and repeatable.</para>
        /// <para>The Morton code of every key is computed once, when the comparison is created; a comparison then costs one lookup per key.</para>
        /// </summary>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <param name="boundingBox2Ds">The bounding boxes, by key. This value can be null.</param>
        /// <param name="cellSize">The side of a cell.</param>
        /// <returns>The comparison.</returns>
        public static System.Comparison<TKey> ZOrderComparison<TKey>(IDictionary<TKey, BoundingBox2D>? boundingBox2Ds, double cellSize) where TKey : notnull
        {
            Dictionary<TKey, ulong> keys = [];

            if (boundingBox2Ds is not null && boundingBox2Ds.Count != 0 && cellSize > 0)
            {
                double x_Min = double.MaxValue;
                double y_Min = double.MaxValue;
                foreach (BoundingBox2D boundingBox2D in boundingBox2Ds.Values)
                {
                    if (boundingBox2D?.Min is Point2D point2D_Min)
                    {
                        x_Min = System.Math.Min(x_Min, point2D_Min.X);
                        y_Min = System.Math.Min(y_Min, point2D_Min.Y);
                    }
                }

                // The 32 bits of a cell index spread over the even bits of a 64-bit code.
                static ulong Interleave(uint value)
                {
                    ulong result = value;
                    result = (result | (result << 16)) & 0x0000FFFF0000FFFFUL;
                    result = (result | (result << 8)) & 0x00FF00FF00FF00FFUL;
                    result = (result | (result << 4)) & 0x0F0F0F0F0F0F0F0FUL;
                    result = (result | (result << 2)) & 0x3333333333333333UL;
                    result = (result | (result << 1)) & 0x5555555555555555UL;
                    return result;
                }

                foreach (KeyValuePair<TKey, BoundingBox2D> keyValuePair in boundingBox2Ds)
                {
                    if (keyValuePair.Value?.Min is not Point2D point2D_Min)
                    {
                        continue;
                    }

                    uint column = (uint)System.Math.Max(0, System.Math.Min(uint.MaxValue, System.Math.Floor((point2D_Min.X - x_Min) / cellSize)));
                    uint row = (uint)System.Math.Max(0, System.Math.Min(uint.MaxValue, System.Math.Floor((point2D_Min.Y - y_Min) / cellSize)));
                    keys[keyValuePair.Key] = Interleave(column) | (Interleave(row) << 1);
                }
            }

            Comparer<TKey> comparer = Comparer<TKey>.Default;

            return (x, y) =>
            {
                bool hasKey_X = keys.TryGetValue(x, out ulong key_X);
                bool hasKey_Y = keys.TryGetValue(y, out ulong key_Y);

                if (hasKey_X != hasKey_Y)
                {
                    return hasKey_X ? -1 : 1;
                }

                if (hasKey_X && key_X != key_Y)
                {
                    return key_X.CompareTo(key_Y);
                }

                return comparer.Compare(x, y);
            };
        }
    }
}
