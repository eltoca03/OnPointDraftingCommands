using System;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using System.Diagnostics;

namespace OnPointDrafting
{
    public class UtilityLine
    {
        private readonly Document doc = Application.DocumentManager.MdiActiveDocument;
        /// <summary>
        /// Need some description
        /// </summary>
        [CommandMethod("OU")]
        public void OU()
        {
            Editor ed = doc.Editor;

            PromptSelectionOptions promptUtilityLineOpt = new PromptSelectionOptions();
            promptUtilityLineOpt.MessageForAdding = "Select Utility Lines or Polylines:";
            PromptSelectionResult utilityLineResults = ed.GetSelection(promptUtilityLineOpt);

            if (utilityLineResults.Status != PromptStatus.OK || utilityLineResults.Value.Count == 0)
            {
                ed.WriteMessage("No valid entities selected. Command aborted.\n");
                return;
            }

            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = GetModelSpace(trans);

                EnsureLayer("STORM2", trans);

                foreach (ObjectId selectedId in utilityLineResults.Value.GetObjectIds())
                {
                    Entity originalEntity = trans.GetObject(selectedId, OpenMode.ForRead) as Entity;

                    if (originalEntity == null)
                    {
                        ed.WriteMessage("Invalid entity selected. Skipping.\n");
                        continue;
                    }

                    // Check if the selected object is in the STORM layer
                    string originalLayer = originalEntity.Layer;
                    bool isInStormLayer = originalLayer.Equals("STORM", StringComparison.OrdinalIgnoreCase);

                    double offset = GetOffset(originalEntity);

                    if (offset > 0)
                    {
                        DBObjectCollection objCollection = GetOffsetCurves(originalEntity, offset);
                        AddEntitiesToModelSpace(trans, btr, objCollection, isInStormLayer);

                        DBObjectCollection objCollection2 = GetOffsetCurves(originalEntity, -offset);
                        AddEntitiesToModelSpace(trans, btr, objCollection2, isInStormLayer);
                    }
                    else
                    {
                        continue;
                    }
                }

                trans.Commit();
            }
        }


        private BlockTableRecord GetModelSpace(Transaction trans)
        {
            BlockTable bt = trans.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
            return trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
        }

        private void EnsureLayer(string layerName, Transaction trans)
        {
            LayerTable layerTable = trans.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;

            if (!layerTable.Has(layerName))
            {
                LayerTableRecord layer = new LayerTableRecord();
                layer.Name = layerName;
                layer.Color = Color.FromColorIndex(ColorMethod.ByAci, 26);
                layer.LinetypeObjectId = EnsureLinetypeExists(trans, "CONTINUOUS");
                layer.LineWeight = LineWeight.LineWeight009;

                layerTable.UpgradeOpen();
                layerTable.Add(layer);
                trans.AddNewlyCreatedDBObject(layer, true);
            }
        }

        private ObjectId EnsureLinetypeExists(Transaction trans, string linetypeName)
        {
            LinetypeTable linetypeTable = trans.GetObject(doc.Database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

            if (!linetypeTable.Has(linetypeName))
            {
                doc.Editor.WriteMessage($"Please load Linetype: {linetypeName} \n");
                throw new InvalidOperationException($"Linetype {linetypeName} not found.");
            }

            return linetypeTable[linetypeName];
        }

        private double GetOffset(Entity entity)
        {
            string resultString = Regex.Match(entity.Linetype, @"\d+").Value;

            if (resultString == "" || !double.TryParse(resultString, out double offset))
            {
                doc.Editor.WriteMessage("Line has no width. Utility linetype with size must be used. Skipping entity.\n");
                return 0.0; // Return a default value or use 0 based on your logic
            }

            return (offset / 12) / 2;
        }

        private DBObjectCollection GetOffsetCurves(Entity entity, double offset)
        {
            DBObjectCollection objCollection = new DBObjectCollection();

            if (entity is Line line)
            {
                objCollection = line.GetOffsetCurves(offset);
            }
            else if (entity is Polyline polyline)
            {
                objCollection = polyline.GetOffsetCurves(offset);
            }
            else
            {
                // Handle other types of entities or throw an exception
                throw new ArgumentException("Entity is not a Line or Polyline.", nameof(entity));
            }

            return objCollection;
        }

        private void AddEntitiesToModelSpace(Transaction trans, BlockTableRecord btr, DBObjectCollection objCollection, bool isInStormLayer)
        {
            foreach (Entity newEntity in objCollection)
            {
                SetEntityProperties(newEntity, isInStormLayer);
                btr.AppendEntity(newEntity);
                trans.AddNewlyCreatedDBObject(newEntity, true);
            }
        }

        private void SetEntityProperties(Entity entity, bool isInStormLayer)
        {
            if (isInStormLayer)
            {
                entity.Layer = "STORM2";
                entity.Linetype = "BYLAYER";
                entity.LinetypeScale = 1.0;
                entity.LineWeight = LineWeight.ByLayer;
                entity.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
            }
            else
            {
                entity.Linetype = "CONTINUOUS";
            }
        }

    }
}
