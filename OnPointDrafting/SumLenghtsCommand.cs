using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;

namespace OnPointDrafting
{
    public class SumLengthCommand
    {
        /// <summary>
        /// sum all materials in sheet
        /// </summary>
        [CommandMethod("SUM")]
        public void SUM()
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Prompt the user to select a polyline
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect a polyline: ");
                peo.SetRejectMessage("\nInvalid selection. Please select a polyline.");
                peo.AddAllowedClass(typeof(Polyline), true);

                PromptEntityResult per = doc.Editor.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                {
                    // User canceled or made an invalid selection
                    return;
                }

                // Open the selected polyline for read
                ObjectId polyId = per.ObjectId;
                Polyline poly = tr.GetObject(polyId, OpenMode.ForRead) as Polyline;

                if (poly == null)
                {
                    // Selected entity is not a polyline
                    return;
                }

                // Define the selection fence using 3D points
                Point3dCollection fencePoints = new Point3dCollection();
                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    fencePoints.Add(poly.GetPoint3dAt(i));
                }

                // Create a selection set based on the fence
                PromptSelectionResult psr = doc.Editor.SelectCrossingPolygon(fencePoints);

                if (psr.Status != PromptStatus.OK)
                {
                    // No objects found or user canceled
                    return;
                }

                SelectionSet selectionSet = psr.Value;

                // Process the selected polylines on specific layers and display their lengths
                foreach (SelectedObject selectedObject in selectionSet)
                {
                    ObjectId objectId = selectedObject.ObjectId;

                    // Open the object for read to check if it's a polyline
                    DBObject obj = tr.GetObject(objectId, OpenMode.ForRead);

                    if (obj is Polyline polyline)
                    {
                        // Check if the polyline is on one of the specified layers
                        if (IsValidLayer(polyline.Layer))
                        {
                            // Display the length of the original polyline
                            double originalLength = polyline.Length;
                            doc.Editor.WriteMessage($"\nOriginal Polyline length on layer {polyline.Layer}: {originalLength}");

                            // Create a dummy duplicate of the original polyline
                            Polyline dummyPolyline = polyline.Clone() as Polyline;

                            Point3dCollection intersectingPoints = new Point3dCollection();
                            
                            // Check for intersection with the original polyline
                            dummyPolyline.IntersectWith(poly, Intersect.OnBothOperands, intersectingPoints, IntPtr.Zero, IntPtr.Zero);

                            Polyline trimmedPolyline = null;

                            if (intersectingPoints.Count > 0)
                            {
                                trimmedPolyline = TrimPolylineAtPoints( tr, (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite), dummyPolyline, intersectingPoints);
                            }

                            doc.Editor.WriteMessage($"\nTrimmed Polyline length on layer {dummyPolyline.Layer}: {trimmedPolyline.Length}");


                            //// Trim poly1 at the intersection points
                            //if (intersectingPoints.Count == 1)
                            //{
                            //    // Display the length of the trimmed polyline
                            //    double trimmedLength = GetPolylineLengthToPoint(dummyPolyline, intersectingPoints[0]);
                            //    doc.Editor.WriteMessage($"\nTrimmed Polyline length on layer {dummyPolyline.Layer}: {trimmedLength}");
                            //}

                            //// Trim poly1 at the intersection points
                            //if (intersectingPoints.Count == 2)
                            //{
                            //    // Display the length of the trimmed polyline
                            //    double trimmedLength = GetPolylineLength(dummyPolyline, intersectingPoints[0], intersectingPoints[1]);
                            //    doc.Editor.WriteMessage($"\nTrimmed Polyline length on layer {dummyPolyline.Layer}: {trimmedLength}");
                            //}
                        }
                    }
                }





                // Commit the transaction
                tr.Commit();
            }
        }

        public Polyline TrimPolylineAtPoints(Transaction tr, BlockTableRecord currentSpace, Polyline originalPolyline, Point3dCollection trimPoints)
        {
            Polyline trimmedPolyline = null;

            // Iterate through trim points
            foreach (Point3d trimPoint in trimPoints)
            {
                // Get the closest point on the original polyline to the trim point
                Point3d closestPoint = originalPolyline.GetClosestPointTo(trimPoint, false);

                // Split the polyline at the closest point
                DBObjectCollection splitCurves = originalPolyline.GetSplitCurves(new Point3dCollection() { closestPoint });

                // If there are split curves, replace the original polyline with the split curve
                if (splitCurves.Count > 0)
                {
                    currentSpace.AppendEntity((Entity)splitCurves[0]);
                    tr.AddNewlyCreatedDBObject((DBObject)splitCurves[0], true);

                    // Store the reference to the trimmed polyline
                    trimmedPolyline = splitCurves[0] as Polyline;
                }
            }

            // Erase the original polyline
            originalPolyline.UpgradeOpen();
            originalPolyline.Erase();

            // Return the newly trimmed polyline
            return trimmedPolyline;
        }

        private bool IsValidLayer(string layerName)
        {
            // Specify the valid layer names
            string[] validLayers = { "D-OHN", "D-OHO", "D-UGB", "D-UGT", "D-PTC", "D-PTS", "D-PTL" };

            // Check if the layer name is in the list of valid layers
            return Array.IndexOf(validLayers, layerName) != -1;
        }


        private double GetPolylineLengthToPoint(Polyline polyline, Point3d endPoint)
        {
            double length = 0.0;
            Point3d currentPoint = polyline.StartPoint;

            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                Curve segment = GetCurveSegment(polyline, i);

                if (segment != null)
                {
                    double segmentLength = segment.GetDistanceAtParameter(segment.EndParam);
                    length += segmentLength;

                    if (currentPoint.DistanceTo(endPoint) < length)
                    {
                        // The desired point is reached within this segment
                        length -= (length - currentPoint.DistanceTo(endPoint));
                        break;
                    }

                    currentPoint = segment.EndPoint;
                }
            }

            return length;
        }

        private double GetPolylineLength(Polyline polyline, Point3d startPoint, Point3d endPoint)
        {
            double length = 0.0;
            Point3d currentPoint = polyline.StartPoint;

            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                Curve segment = GetCurveSegment(polyline, i);

                if (segment != null)
                {
                    double segmentLength = segment.GetDistanceAtParameter(segment.EndParam);
                    length += segmentLength;

                    if (currentPoint.DistanceTo(endPoint) < length)
                    {
                        // The desired point is reached within this segment
                        length -= (length - currentPoint.DistanceTo(endPoint));
                        break;
                    }

                    currentPoint = segment.EndPoint;
                }
            }

            return length;
        }

        private Curve GetCurveSegment(Polyline polyline, int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= polyline.NumberOfVertices - 1)
            {
                return null;
            }

            Point3d startPoint = polyline.GetPoint3dAt(segmentIndex);
            Point3d endPoint = polyline.GetPoint3dAt(segmentIndex + 1);

            if (polyline.GetSegmentType(segmentIndex) == SegmentType.Line)
            {
                // Create a Line object from the LineSegment3d
                return new Line(startPoint, endPoint);
            }
            else if (polyline.GetSegmentType(segmentIndex) == SegmentType.Arc)
            {
                double bulge = polyline.GetBulgeAt(segmentIndex);
                Point3d startPoint3d = startPoint;
                Point3d endPoint3d = endPoint;

                // Calculate the center and radius of the arc
                Point3d center = GetArcCenter(startPoint3d, endPoint3d, bulge);
                double radius = startPoint3d.DistanceTo(center);

                // Calculate start and end angles
                Vector3d startVector = (startPoint3d - center).GetNormal();
                Vector3d endVector = (endPoint3d - center).GetNormal();
                double startAngle = Math.Atan2(startVector.Y, startVector.X);
                double endAngle = Math.Atan2(endVector.Y, endVector.X);

                // Create an Arc object using the calculated center, start, and end angles, and radius
                return new Arc(startPoint, radius, startAngle, endAngle);
            }

            return null;
        }

        // Helper method to calculate the center of the arc
        private Point3d GetArcCenter(Point3d start, Point3d end, double bulge)
        {
            double angle = 4 * Math.Atan(bulge);
            double chordLength = start.DistanceTo(end);
            double radius = chordLength / (2 * Math.Sin(angle / 2));

            Vector3d direction = (end - start).GetNormal();
            Vector3d perpendicular = direction.RotateBy(Math.PI / 2, Vector3d.ZAxis);

            return start + direction * (chordLength / 2) + perpendicular * (radius * Math.Cos(angle / 2));
        }
    }
}
