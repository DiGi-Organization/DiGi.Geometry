using DiGi.Geometry.Planar.Classes;
using System.Collections.Generic;

namespace DiGi.Geometry.Planar
{
    public static partial class Create
    {
        /// <summary>
        /// Creates an open <see cref="Classes.Polyline2D"/> from a collection of <see cref="Point2D"/> objects, dropping any point which repeats the one before it.
        /// <para>Use this rather than the constructor whenever the points come from outside the library - a file, a service, a user - and are not already known to be free of repeats. A point which repeats its predecessor contributes a segment of no length, and such a segment has no direction.</para>
        /// <para>The polyline is open, so its last point is not compared with its first: a polyline which returns to where it started keeps both ends.</para>
        /// <para>The constructor stays a plain assignment and does none of this, so a caller who already holds clean points pays nothing.</para>
        /// </summary>
        /// <param name="point2Ds">The <see cref="IEnumerable{T}"/> of <see cref="Point2D"/> objects used to define the polyline.</param>
        /// <param name="tolerance">The <see cref="double"/> distance within which two points are treated as one.</param>
        /// <returns>A <see cref="Classes.Polyline2D"/> object if a valid polyline can be created; otherwise, null.</returns>
        public static Polyline2D? Polyline2D(this IEnumerable<Point2D?>? point2Ds, double tolerance = DiGi.Core.Constants.Tolerance.Distance)
        {
            if (point2Ds == null)
            {
                return null;
            }

            List<Point2D> point2Ds_Temp = [];
            foreach (Point2D? point2D in point2Ds)
            {
                if (point2D != null)
                {
                    point2Ds_Temp.Add(point2D);
                }
            }

            // The count is checked after the repeats are removed, so a polyline which only reaches two positions
            // because it repeats a point is rejected rather than turned into a line of no length.
            point2Ds_Temp.RemoveDuplicates(false, tolerance);

            if (point2Ds_Temp.Count < 2)
            {
                return null;
            }

            return new Polyline2D(point2Ds_Temp);
        }
    }
}
