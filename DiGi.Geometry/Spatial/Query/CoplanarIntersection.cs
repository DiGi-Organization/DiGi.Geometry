using DiGi.Geometry.Planar.Classes;
using DiGi.Geometry.Planar.Interfaces;
using DiGi.Geometry.Spatial.Classes;
using DiGi.Geometry.Spatial.Interfaces;
using System.Collections.Generic;

namespace DiGi.Geometry.Spatial
{
    public static partial class Query
    {
        /// <summary>
        /// Gets the part two nearly coplanar polygonal faces have in common: the faces of the second one that overlap the first, in the plane of the first.
        /// <para>The faces are nearly coplanar when their planes are parallel within <paramref name="maxAngle"/> - facing either way - and every vertex of the second face lies within <paramref name="maxDistance"/> of the plane of the first. The second face is then projected onto that plane and intersected with the first in its 2D coordinates. Pieces smaller than the square of <paramref name="tolerance"/> are dropped as slivers, so two faces meeting along an edge only have nothing in common.</para>
        /// <para>The cheap tests come first and stop at the first failure - the angle between the normals, then the vertex distances - so the intersection itself is only computed for faces that can overlap.</para>
        /// </summary>
        /// <param name="polygonalFace3D_1">The first face, whose plane the result lies in. This value can be null.</param>
        /// <param name="polygonalFace3D_2">The second face. This value can be null.</param>
        /// <param name="maxDistance">The largest distance between a vertex of the second face and the plane of the first.</param>
        /// <param name="maxAngle">The largest angle [rad] between the planes of the faces.</param>
        /// <param name="tolerance">The distance tolerance.</param>
        /// <returns>The common faces, in the plane of the first face; an empty list when the faces are not nearly coplanar or do not overlap; null when either face is null or has no plane.</returns>
        public static List<PolygonalFace3D>? CoplanarIntersection(this IPolygonalFace3D? polygonalFace3D_1, IPolygonalFace3D? polygonalFace3D_2, double maxDistance, double maxAngle, double tolerance = DiGi.Core.Constants.Tolerance.Distance)
        {
            Plane? plane = polygonalFace3D_1?.Plane;
            Plane? plane_2 = polygonalFace3D_2?.Plane;
            if (plane is null || plane_2 is null)
            {
                return null;
            }

            List<PolygonalFace3D> result = [];

            Vector3D? normal = plane.Normal?.Unit;
            Vector3D? normal_2 = plane_2.Normal?.Unit;
            if (normal is null || normal_2 is null || System.Math.Abs(normal.DotProduct(normal_2)) < System.Math.Cos(maxAngle))
            {
                return result;
            }

            IPolygonal3D? externalEdge_2 = polygonalFace3D_2!.ExternalEdge;
            List<Point3D>? point3Ds_2 = externalEdge_2?.GetPoints();
            if (point3Ds_2 is null || point3Ds_2.Count < 3)
            {
                return result;
            }

            foreach (Point3D point3D in point3Ds_2)
            {
                if (!(plane.Distance(point3D) <= maxDistance))
                {
                    return result;
                }
            }

            IPolygonalFace2D? polygonalFace2D_1 = polygonalFace3D_1!.Geometry2D;
            if (polygonalFace2D_1 is null)
            {
                return result;
            }

            // The vertices are projected onto the plane rather than converted with a planarity check: they may lie up to maxDistance off it.
            Polygon2D? Polygon2D(List<Point3D>? point3Ds)
            {
                if (point3Ds is null || point3Ds.Count < 3)
                {
                    return null;
                }

                List<Point2D> point2Ds = new(point3Ds.Count);
                foreach (Point3D point3D in point3Ds)
                {
                    if (plane.Convert(point3D) is Point2D point2D)
                    {
                        point2Ds.Add(point2D);
                    }
                }

                return point2Ds.Count < 3 ? null : new Polygon2D(point2Ds);
            }

            Polygon2D? polygon2D_External_2 = Polygon2D(point3Ds_2);
            if (polygon2D_External_2 is null)
            {
                return result;
            }

            List<IPolygonal2D>? polygonal2Ds_Internal_2 = null;
            if (polygonalFace3D_2.InternalEdges is List<IPolygonal3D> polygonal3Ds_Internal && polygonal3Ds_Internal.Count != 0)
            {
                polygonal2Ds_Internal_2 = new(polygonal3Ds_Internal.Count);
                foreach (IPolygonal3D polygonal3D_Internal in polygonal3Ds_Internal)
                {
                    if (Polygon2D(polygonal3D_Internal?.GetPoints()) is Polygon2D polygon2D_Internal)
                    {
                        polygonal2Ds_Internal_2.Add(polygon2D_Internal);
                    }
                }
            }

            // The 2D bounding boxes are compared before any face is built for the overlay.
            BoundingBox2D? boundingBox2D_1 = polygonalFace2D_1.GetBoundingBox();
            BoundingBox2D? boundingBox2D_2 = polygon2D_External_2.GetBoundingBox();
            if (boundingBox2D_1 is not null && boundingBox2D_2 is not null && !boundingBox2D_1.InRange(boundingBox2D_2, tolerance))
            {
                return result;
            }

            PolygonalFace2D? polygonalFace2D_Overlay_1 = polygonalFace2D_1 as PolygonalFace2D ?? Planar.Create.PolygonalFace2D(polygonalFace2D_1.ExternalEdge, polygonalFace2D_1.InternalEdges, tolerance);
            PolygonalFace2D? polygonalFace2D_Overlay_2 = Planar.Create.PolygonalFace2D(polygon2D_External_2, polygonal2Ds_Internal_2, tolerance);
            if (polygonalFace2D_Overlay_1 is null || polygonalFace2D_Overlay_2 is null)
            {
                return result;
            }

            List<PolygonalFace2D>? polygonalFace2Ds = Planar.Query.Intersection(polygonalFace2D_Overlay_1, polygonalFace2D_Overlay_2);
            if (polygonalFace2Ds is null)
            {
                return result;
            }

            double area_Min = tolerance * tolerance;
            foreach (PolygonalFace2D polygonalFace2D in polygonalFace2Ds)
            {
                if (polygonalFace2D.GetArea() >= area_Min)
                {
                    result.Add(new PolygonalFace3D(plane, polygonalFace2D));
                }
            }

            return result;
        }
    }
}
