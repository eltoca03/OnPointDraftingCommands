using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
namespace OnPointDrafting
{
  public class CalloutsDropdown
  {
	public const string TAP_PED = "PLACE TAP PEDESTAL\r\n" +
							  "10\"%%c x 17\"h 18\" STAKE";
	public const string DROP_BUCKET = "PLACE DROP BUCKET\r\n" +
									  "9.5\"%%c x 17\"D, FLUSH";
	public const string SPLITTER_PED = "PLACE SPLITTER PED\r\n" +
									   "10\"%%c x 22\"H 24\" STAKE";
	public const string LE_PED = "PLACE LE PEDESTAL\r\n" +
								 "12\"W x 12\"L x 32\"H";
	public const string AMP_PED = "PLACE AMP PEDESTAL\r\n" +
								  "15\"W x 34\"L x 16\"H";
	public const string NODE_PED = "PLACE NODE PEDESTAL\r\n" +
								   "26\"W x 38\"L x 24\"H";
	public const string POWER_SUPPLY = "PLACE POWER SUPPLY\r\n" +
									   "26\"W x 15\"L x 34\"H";
	public const string UGUARD_RISER = "PLACE U-GUARD RISER\r\n" +
										"ON EXISTING POLE";

	public const int recWidth = 10;
	public const int recLength = 45;


	//Get current document and database
	Document doc = Application.DocumentManager.MdiActiveDocument;
	Database database = Application.DocumentManager.MdiActiveDocument.Database;

	//
	[CommandMethod("QB")]
	public void QB()
	{
	  PromptEntityOptions selectCalloutOptions;
	  PromptEntityResult selectedCalloutResults;

	  string msAndKwds;
	  string kwds;
	  string verbose = "default";
	  BlockReference blkRef = null;
	  //string station;
	  //string lineNumber;

	  //need editor to prompt user
	  Editor ed = doc.Editor;

	  //Prompt options for running line
	  PromptNestedEntityOptions promptRunningLineOpt = new PromptNestedEntityOptions("\nSelect Running Line");
	  promptRunningLineOpt.AllowNone = false;
	  PromptNestedEntityResult runningLineResults = ed.GetNestedEntity(promptRunningLineOpt);

	  if (runningLineResults.Status != PromptStatus.OK)
	  {
		ed.WriteMessage("\nThe selected object is not running line");
		return;
	  }

	  //prompt for line number
	  PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter Line Number: ");
	  pStrOpts.AllowSpaces = false;
	  PromptResult pStrRes = ed.GetString(pStrOpts);

	  if (pStrRes.Status != PromptStatus.OK)
	  {
		return;
	  }

	  //prompt user to select object only allow DFWT blocks 
	  PromptNestedEntityOptions pneo = new PromptNestedEntityOptions("\nSelect DFWT Block");
	  pneo.AllowNone = false;

	  //check prompt results from user else return
	  PromptNestedEntityResult pner = ed.GetNestedEntity(pneo);

	  if (pner.Status != PromptStatus.OK)
	  {
		return;
	  }

	  while (true)
	  {


		//just adding a comment to this line
		Transaction trans = database.TransactionManager.StartTransaction();
		using (trans)
		{
		  //access block table and create block table record
		  BlockTable bt = trans.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
		  BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

		  //get the running line
		  Polyline runningLine = trans.GetObject(runningLineResults.ObjectId, OpenMode.ForRead) as Polyline;

		  ObjectId[] containerIds = pner.GetContainers();

		  foreach (ObjectId id in containerIds)
		  {
			DBObject container = trans.GetObject(id, OpenMode.ForRead);

			if (container is BlockReference)
			{
			  blkRef = container as BlockReference;
			  ed.WriteMessage("\nContainer: " + blkRef.BlockName);
			  
			  if (blkRef.Name == "CPR")
			  {
				break;
			  }
				
			}	
		  }

		  string str = String.Format("{0:0}", runningLine.GetDistAtPoint(blkRef.Position));
		  switch (str.Length)
		  {
			case 1:
			  str = "0+0" + str;
			  break;
			case 2:
			  str = "0+" + str;
			  break;
			default:
			  str = str.Substring(0, str.Length - 2) + "+" + str.Substring(str.Length - 2);
			  break;
		  }

		  if (pStrRes.StringResult != "")
			str = str + " LINE " + pStrRes.StringResult;

		  //  
		  switch (blkRef.Name.ToUpper())
		  {
			case "CPR":
			  msAndKwds = "\nSelect Type:[Drop Bucket/Tap pedestal/ Splitter Pedestal]";
			  kwds = "'Drop Bucket' 'Tap Pedestal' 'Tap Splitter'";

			  selectCalloutOptions = new PromptEntityOptions(msAndKwds, kwds);
			  selectedCalloutResults = ed.GetEntity(selectCalloutOptions);

			  switch (selectedCalloutResults.StringResult.ToUpper())
			  {
				case "DROP BUCKET":
				  verbose = DROP_BUCKET;
				  break;

				case "TAP PEDESTAL":
				  verbose = TAP_PED;
				  break;

				default:
				  break;
			  }

			  break;

			case "CPS":
			  break;

			default:
			  ed.WriteMessage(blkRef.BlockName);
			  break;
		  }

		  //.string msAndKwds = "\nCPR or [A/B/C]";
		  //string kwds = "Apple Bob Cat";
		  //peo.SetMessageAndKeywords(msAndKwds, kwds);
		  //ed.WriteMessage(per1.StringResult);

		  /************************************************************************************************
		   *			Prompting for callout box insertion point 
		   *			Set geometry for placing box														
		   ***********************************************************************************************/
		  PromptPointOptions pPtOpt = new PromptPointOptions("\nEnter Insertion Point");
		  PromptPointResult pPtRes = ed.GetPoint(pPtOpt);
		  Point3d insPt = pPtRes.Value;

		  CoordinateSystem3d cs = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
		  Plane plane = new Plane(Point3d.Origin, cs.Zaxis);

		  //create polyline
		  Polyline rec = new Polyline();
		  rec.AddVertexAt(0, insPt.Convert2d(plane), 0, 0, 0);

		  bool Xdir = true;
		  bool Ydir = true;

		  if (insPt.X < blkRef.Position.X)
			Xdir = false;
		  //

		  if (insPt.Y < blkRef.Position.Y)
			Ydir = false;

		  if (Xdir)
		  {
			if (Ydir)
			{
			  //quadrant I
			  rec.AddVertexAt(0, new Point2d(insPt.X + recLength, insPt.Y), 0, 0, 0);
			  rec.AddVertexAt(1, new Point2d(insPt.X + recLength, insPt.Y + recWidth), 0, 0, 0);
			  rec.AddVertexAt(2, new Point2d(insPt.X, insPt.Y + recWidth), 0, 0, 0);
			}
			else
			{
			  //quadrant IV
			  rec.AddVertexAt(0, new Point2d(insPt.X + recLength, insPt.Y), 0, 0, 0);
			  rec.AddVertexAt(1, new Point2d(insPt.X + recLength, insPt.Y - recWidth), 0, 0, 0);
			  rec.AddVertexAt(2, new Point2d(insPt.X, insPt.Y - recWidth), 0, 0, 0);
			}
		  }
		  else
		  {
			if (Ydir)
			{
			  //quadrant II
			  rec.AddVertexAt(0, new Point2d(insPt.X - recLength, insPt.Y), 0, 0, 0);
			  rec.AddVertexAt(1, new Point2d(insPt.X - recLength, insPt.Y + recWidth), 0, 0, 0);
			  rec.AddVertexAt(2, new Point2d(insPt.X, insPt.Y + recWidth), 0, 0, 0);
			}
			else
			{
			  //quadrant III
			  rec.AddVertexAt(0, new Point2d(insPt.X - recLength, insPt.Y), 0, 0, 0);
			  rec.AddVertexAt(1, new Point2d(insPt.X - recLength, insPt.Y - recWidth), 0, 0, 0);
			  rec.AddVertexAt(2, new Point2d(insPt.X, insPt.Y - recWidth), 0, 0, 0);
			}

		  }
		  rec.Closed = true;
		  rec.SetDatabaseDefaults();

		  /************************************************************************************************
		  *			Add object to block table and Commit Transaction
		  *			
		  ***********************************************************************************************/

		  MText mt = new MText();
		  mt.Contents = verbose;

		  btr.AppendEntity(rec);
		  btr.AppendEntity(mt);

		  trans.AddNewlyCreatedDBObject(rec, true);
		  trans.AddNewlyCreatedDBObject(mt, true);
		  trans.Commit();

		}

		return;
	  }
	}

	[CommandMethod("NestEntSelect")]
	public void NestEntSelect()
	{
	  Document doc = Application.DocumentManager.MdiActiveDocument;
	  Editor ed = doc.Editor;

	  PromptNestedEntityOptions pneo = new PromptNestedEntityOptions(
		  "\nSelect nested entity:");

	  PromptNestedEntityResult pner = ed.GetNestedEntity(pneo);

	  if (pner.Status != PromptStatus.OK)
		return;

	  using (Transaction Tx = doc.TransactionManager.StartTransaction())
	  {
		//Containers
		ObjectId[] containerIds = pner.GetContainers();

		foreach (ObjectId id in containerIds)
		{
		  DBObject container = Tx.GetObject(id, OpenMode.ForRead);

		  if (container is BlockReference)
		  {
			BlockReference bref = container as BlockReference;

			ed.WriteMessage("\nContainer: " + bref.BlockName);
		  }
		}

		Entity entity = Tx.GetObject(pner.ObjectId, OpenMode.ForRead)
			as Entity;

		ed.WriteMessage("\nEntity: " + entity.ToString());

		ed.WriteMessage("\nBlock: " + entity.BlockName);

		BlockTableRecord btr = Tx.GetObject(entity.BlockId, OpenMode.ForRead)
			as BlockTableRecord;

		Tx.Commit();
	  }
	}






  }
}
