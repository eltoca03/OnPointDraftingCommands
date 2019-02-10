using System;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace OnPointDrafting2012
{
  public class UtilityLine
  {
	Document doc = Application.DocumentManager.MdiActiveDocument;
	Database database = Application.DocumentManager.MdiActiveDocument.Database;

	[CommandMethod("OU")]
	public void OU()
	{
	  //need editor to prompt user
	  Editor ed = doc.Editor;

	  while (true)
	  {
		//Prompt options for utility line
		PromptEntityOptions promptUtilityLineOpt = new PromptEntityOptions("Select Utility Line: \n");
		PromptEntityResult utilityLineResults = ed.GetEntity(promptUtilityLineOpt);

		if (utilityLineResults.Status != PromptStatus.OK)
		{
		  //ed.WriteMessage("\nThe selected object is not utility line");
		  return;
		}

		Transaction trans = database.TransactionManager.StartTransaction();
		using (trans)
		{
		  //access block table and create block table record
		  BlockTable bt = trans.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
		  BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
		  LinetypeTable linetypeTable = trans.GetObject(database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

		  if (!linetypeTable.Has("BLDG"))
		  {
			ed.WriteMessage("Please load Linetype:BLDG \n");
			return;
		  }

		  //get the running line
		  Type type = trans.GetObject(utilityLineResults.ObjectId, OpenMode.ForRead).GetType();

		  String resultString;
		  Line line;
		  Polyline polyline;
		  double offset;

		  if (type == typeof(Line))
		  {
			line = trans.GetObject(utilityLineResults.ObjectId, OpenMode.ForRead) as Line;

			resultString = Regex.Match(line.Linetype, @"\d+").Value;

			if (resultString != "")
			{
			  offset = Double.Parse(resultString);
			}
			else
			{
			  ed.WriteMessage("Line has no width. Utility linetype with size must be used.");
			  break;
			}

			offset = (offset / 12) / 2;

			DBObjectCollection objCollection = line.GetOffsetCurves(offset);

			foreach (Entity entity in objCollection)
			{
			  Line newLine = (Line)entity;
			  newLine.LinetypeScale = .05;
			  if (offset < 1.5)
			  {
				newLine.Linetype = "CONTINUOUS";
			  }
			  else
			  {
				newLine.Linetype = "BLDG";
			  }

			  newLine.LineWeight = LineWeight.LineWeight009;
			  newLine.ReverseCurve();
			  btr.AppendEntity(newLine);
			  trans.AddNewlyCreatedDBObject(newLine, true);
			}

			DBObjectCollection objCollection2 = line.GetOffsetCurves(-offset);

			foreach (Entity entity in objCollection2)
			{
			  Line newLine = (Line)entity;
			  newLine.LinetypeScale = .05;
			  if (offset < 1.5)
			  {
				newLine.Linetype = "CONTINUOUS";
			  }
			  else
			  {
				newLine.Linetype = "BLDG";
			  }

			  newLine.LineWeight = LineWeight.LineWeight009;
			  btr.AppendEntity(newLine);
			  trans.AddNewlyCreatedDBObject(newLine, true);
			}

		  }
		  else if (type == typeof(Polyline))
		  {
			polyline = trans.GetObject(utilityLineResults.ObjectId, OpenMode.ForRead) as Polyline;

			resultString = Regex.Match(polyline.Linetype, @"\d+").Value;

			if (resultString != "")
			{
			  offset = Double.Parse(resultString);
			}
			else
			{
			  ed.WriteMessage("Line has no width. Utility linetype with size must be used.\n");
			  break;
			}

			offset = (offset / 12) / 2;

			DBObjectCollection objCollection = polyline.GetOffsetCurves(offset);

			foreach (Entity entity in objCollection)
			{
			  Polyline newLine = (Polyline)entity;
			  newLine.LinetypeScale = .05;
			  if (offset < 1.5)
			  {
				newLine.Linetype = "CONTINUOUS";
			  }
			  else
			  {
				newLine.Linetype = "BLDG";
			  }

			  newLine.LineWeight = LineWeight.LineWeight009;
			  btr.AppendEntity(newLine);
			  trans.AddNewlyCreatedDBObject(newLine, true);
			}

			DBObjectCollection objCollection2 = polyline.GetOffsetCurves(-offset);

			foreach (Entity entity in objCollection2)
			{
			  Polyline newLine = (Polyline)entity;
			  newLine.LinetypeScale = .05;
			  if (offset < 1.5)
			  {
				newLine.Linetype = "CONTINUOUS";
			  }
			  else
			  {
				newLine.Linetype = "BLDG";
			  }

			  newLine.LineWeight = LineWeight.LineWeight009;
			  newLine.ReverseCurve();
			  btr.AppendEntity(newLine);
			  trans.AddNewlyCreatedDBObject(newLine, true);
			}
		  }
		  else { ed.WriteMessage("Must me Line or Polyline. \n"); }

		  trans.Commit();
		}
	  }
	}
  }
}

