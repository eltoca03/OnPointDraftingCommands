using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace OnPointDrafting
{
    public class BlockStations
    {
        /// <summary>
        /// The bsta command add a label station to blocks in the running line.
        /// </summary>
        [CommandMethod("bsta")]
        public void bsta()
        {
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // Prompt the user to select a polyline
                PromptEntityOptions promptPolylineOptions = new PromptEntityOptions("\nSelect a polyline: ");
                promptPolylineOptions.SetRejectMessage("\nPlease select a valid polyline.");
                promptPolylineOptions.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult polylineResult = doc.Editor.GetEntity(promptPolylineOptions);

                if (polylineResult.Status != PromptStatus.OK)
                {
                    doc.Editor.WriteMessage("\nCommand canceled.");
                    return;
                }

                // Open the selected polyline
                Polyline polyline = trans.GetObject(polylineResult.ObjectId, OpenMode.ForRead) as Polyline;

                // Use the FENCE selection to select block references
                SelectionSet selectedObjects = CreateFenceUsingPolyline(polyline, true);

                // Create a list to store block distances
                List<BlockDistance> blockDistances = new List<BlockDistance>();

                // Iterate through the selected objects
                foreach (SelectedObject selectedObject in selectedObjects)
                {
                    if (selectedObject != null)
                    {
                        if (selectedObject.ObjectId.ObjectClass == RXClass.GetClass(typeof(BlockReference)))
                        {
                            DBObject dBObject = trans.GetObject(selectedObject.ObjectId, OpenMode.ForRead);

                            if (dBObject is BlockReference)
                            {
                                BlockReference blockRef = dBObject as BlockReference;
                                Double dist = 0;
                                Double rotation = 0;
                                Vector3d perpedicularVector;
                                //ignore block where insertion point not on running line
                                try
                                {
                                    dist = polyline.GetDistAtPoint(blockRef.Position);
                                    perpedicularVector = GetPerpedicularVector(polyline, blockRef.Position);
                                    rotation = VectorToAngle(perpedicularVector);
                                }
                                catch (Autodesk.AutoCAD.Runtime.Exception e)
                                {
                                    continue; 
                                }


                                blockDistances.Add(new BlockDistance
                                {
                                    BlockReference = blockRef,
                                    Distance = dist,
                                    Rotation = rotation,
                                    PerpedicularVector = perpedicularVector
                                });

                            }
                        }
                    }
                }

                // Create text labels for block distances
                foreach (var blockDistance in blockDistances)
                {
                    CreateTextAtBlockPosition(doc, trans, blockDistance.BlockReference, Convert.ToInt32(blockDistance.Distance), blockDistance.Rotation, blockDistance.PerpedicularVector);
                }

                // Commit the transaction
                trans.Commit();
            }
        }

        // Create MText at the given position
        private void CreateTextAtBlockPosition(Document doc, Transaction trans, BlockReference blockRef, int station, double rotation, Vector3d perpendicularVector)
        {
            // Create MText
            MText mtext = new MText();

            if (rotation == Math.PI)
            {
                // Position the MText relative to the insertion point of the block reference
                Point3d blockPosition = blockRef.Position;
                Vector3d offset = new Vector3d(-5.0, 3.0, 0.0); //  from the insertion point
                Point3d textPosition = blockPosition + offset;
                mtext.Location = textPosition;
                mtext.Rotation = 0;
            }
            else
            {
                mtext.Location = GetInsertionPoint(blockRef.Position, perpendicularVector);
                mtext.Rotation = rotation;
            }
            
            mtext.Layer = "NPLT";
            mtext.TextHeight = 2.0;
            mtext.Attachment = AttachmentPoint.MiddleCenter;
            mtext.Contents = FormatStation(station);

            // Add MText to the current space
            BlockTableRecord currentSpace = trans.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
            currentSpace.AppendEntity(mtext);
            trans.AddNewlyCreatedDBObject(mtext, true);
        }

        private string FormatStation(int v)
        {
            string formattedStation = "";

            if (v.ToString().Length == 1)
            {
                formattedStation = "0+0" + v.ToString();
            }
            else if (v.ToString().Length == 2)
            {
                formattedStation = "0+" + v.ToString();
            }
            else
            {
                char[] array = v.ToString().ToCharArray();

                for (int i = 0; i < array.Length; i++)
                {
                    if (array.Length - i == 2)
                    {
                        formattedStation += "+";
                    }

                    formattedStation += array[i];
                }

            }
            return formattedStation;
        }

        private SelectionSet CreateFenceUsingPolyline(Polyline pl, bool filter)
        {
            Point3dCollection vertices = new Point3dCollection();

            for (double i = 0; i < pl.Length; i += 3)
            {
                vertices.Add(pl.GetPointAtDist(i));
            }

            //check for filter
            SelectionFilter selectionFilter = null;
            if (filter)
            {
                TypedValue[] tv = new TypedValue[1];
                tv.SetValue(new TypedValue((int)DxfCode.Start, "INSERT"), 0);
                selectionFilter = new SelectionFilter(tv);
            }

            PromptSelectionResult promptSelectionResult;
            //get fence selection results
            if (selectionFilter != null)
            {
                promptSelectionResult = Application.DocumentManager.MdiActiveDocument.Editor.SelectFence(vertices, selectionFilter);
            }
            else
            {
                promptSelectionResult = Application.DocumentManager.MdiActiveDocument.Editor.SelectFence(vertices);
            }

            if (promptSelectionResult.Status != PromptStatus.OK)
            {
                return null;
            }

            return promptSelectionResult.Value;
        }
        

        private Vector3d GetPerpedicularVector(Polyline polyline, Point3d point3D)
        {
            Vector3d tanget = polyline.GetFirstDerivative(point3D);
            return  tanget.RotateBy(Math.PI / 2.0, Vector3d.ZAxis);
        }

        private double VectorToAngle(Vector3d vector)
        {
            return Math.Atan2(vector.Y, vector.X);
        }

        private Point3d GetInsertionPoint(Point3d point, Vector3d vector)
        {
            double shiftDistance = 7.0;

            Vector3d shiftVector =  vector.GetNormal() * shiftDistance;

            MText mText = new MText();

            mText.Location = point;

            return mText.Location.Add(shiftVector);
        }

    }



    // Helper class to store block distances
    public class BlockDistance
    {
        public BlockReference BlockReference { get; set; }
        public double Distance { get; set; }
        public double Rotation { get; set; }
        public Vector3d PerpedicularVector { get; set; }
    }
}
