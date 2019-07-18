using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace OnPointDrafting
{
  public class Matchline
  {
	//get the current document and database
	Document acDoc = Application.DocumentManager.MdiActiveDocument;
	Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

	[CommandMethod("mln")]
	public void mln()
	{
	  Editor ed = acDoc.Editor;

	  //prompt select matchline
	  PromptEntityOptions entOpt = new PromptEntityOptions("Select Matchline");
	  entOpt.SetRejectMessage("Line Selected is not Matchline\n");
	  entOpt.AddAllowedClass(typeof(Line), false);

	  //check entity results status
	  PromptEntityResult entRst = ed.GetEntity(entOpt);
	  if (entRst.Status != PromptStatus.OK)
		return;

	  //begin transaction
	  Transaction trans = acCurDb.TransactionManager.StartTransaction();

	  using (trans)
	  {
		//open blockTable for read
		BlockTable blockTable;
		blockTable = trans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

		BlockTableRecord blockTableRecord;
		blockTableRecord = trans.GetObject(blockTable[BlockTableRecord.ModelSpace],
										   OpenMode.ForWrite) as BlockTableRecord;

		//i think i need to cast the entity to a polyline so i can get the point
		Line matchLine = trans.GetObject(entRst.ObjectId, OpenMode.ForWrite) as Line;

		//get the midpoint of line
		Point3d pts = getMidPoint(matchLine);

		//create text
		DBText mText = new DBText();

		//System.Diagnostics.Debugger.Launch();

		mText.Justify = AttachmentPoint.MiddleCenter;
		mText.LayerId = matchLine.LayerId;
		mText.Height = 5;

		//prompt to select side 
		PromptPointOptions ptOpt = new PromptPointOptions("Select Side ");

		PromptPointResult ptResult = ed.GetPoint(ptOpt);

		if (!sideSelected(ptResult.Value, matchLine))
		{
		  //mText.Justify = AttachmentPoint.TopCenter;
		  Line x = matchLine.GetOffsetCurves(-3.75)[0] as Line;
		  mText.AlignmentPoint = getMidPoint(x);
		}
		else
		{
		  Line x = matchLine.GetOffsetCurves(3.75)[0] as Line;
		  mText.AlignmentPoint = getMidPoint(x);
		}

		//prompt user for sheet number
		PromptStringOptions strOpt = new PromptStringOptions("Enter Page #")
		{
		  AllowSpaces = false
		};

		TextStyleTable textStyleTable = trans.GetObject(acCurDb.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

		if (textStyleTable.Has("B"))
		{
		  mText.TextStyleId = trans.GetObject(textStyleTable["B"], OpenMode.ForRead).ObjectId;
		}

		PromptResult sheetNum = ed.GetString(strOpt);
		mText.TextString = "MATCH LINE - SEE SHEET " + sheetNum.StringResult;


		if (matchLine.Angle > (Math.PI / 2) && matchLine.Angle < ((Math.PI * 3) / 2))
		{
		  mText.Rotation = matchLine.Angle + Math.PI;
		}
		else
		{
		  mText.Rotation = matchLine.Angle;
		}



		//append to block table
		blockTableRecord.AppendEntity(mText);
		trans.AddNewlyCreatedDBObject(mText, true);
		trans.Commit();

	  }
	}

	public static PlanarEntity getPlane()
	{
	  return PlanarEntity.Create(IntPtr.Zero, true);
	}

	//calculate the midpoint 
	public static Point3d getMidPoint(Line a)
	{


	  //DBObjectCollection objcoll = a;
	  //Line tempLine = new Line();

	  //foreach (Entity ent in objcoll)
	  //{
	  //    tempLine = ((Line)ent);
	  //}

	  Point3d point3d = new Point3d();
	  Vector3d vector = new Vector3d();
	  vector = a.StartPoint.GetVectorTo(a.EndPoint);
	  point3d = a.StartPoint + (vector / 2);

	  return point3d;
	}

	//create vector with point selected and midpoint of line 
	//crete vector with midpoint and endpoint of line
	public bool sideSelected(Point3d sideSelectedPt, Line line)
	{
	  Line lineTemp = new Line(line.StartPoint, sideSelectedPt);

	  if (line.Angle == 0)
	  {
		if (lineTemp.Angle < Math.PI)
		  return true;
		else { return false; }
	  }
	  else if (lineTemp.Angle >= line.Angle)
	  {
		return true;
	  }
	  else
	  {
		return false;
	  }
	}
  }
}
