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
                                //ignore block where insertion point not on running line
                                try
                                {
                                    dist = polyline.GetDistAtPoint(blockRef.Position); 
                                }
                                catch (Autodesk.AutoCAD.Runtime.Exception e)
                                {
                                    continue; 
                                }

                                blockDistances.Add(new BlockDistance
                                {
                                    BlockReference = blockRef,
                                    Distance = dist
                                });

                            }
                        }
                    }
                }

                // Create text labels for block distances
                foreach (var blockDistance in blockDistances)
                {
                    CreateTextAtBlockPosition(doc, trans, blockDistance.BlockReference, Convert.ToInt32(blockDistance.Distance));
                }

                // Commit the transaction
                trans.Commit();
            }
        }

        // Create MText at the given position
        private void CreateTextAtBlockPosition(Document doc, Transaction trans, BlockReference blockRef, int station)
        {
            // Create MText
            MText mtext = new MText();

            // Position the MText relative to the insertion point of the block reference
            Point3d blockPosition = blockRef.Position;
            Vector3d offset = new Vector3d(-5.0, 5.0, 0.0); // 5 units left and 5 units up from the insertion point
            Point3d textPosition = blockPosition + offset;
            mtext.Location = textPosition;
            mtext.Layer = "NPLT";
            mtext.TextHeight = 2.0;
            mtext.Attachment = AttachmentPoint.BottomCenter;
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

    }



    // Helper class to store block distances
    public class BlockDistance
    {
        public BlockReference BlockReference { get; set; }
        public double Distance { get; set; }
    }
}
