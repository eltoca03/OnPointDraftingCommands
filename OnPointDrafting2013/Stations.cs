using System;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace OnPointDrafting
{
  public class Stations
  {
	Document doc = Application.DocumentManager.MdiActiveDocument;
	Database database = Application.DocumentManager.MdiActiveDocument.Database;

	[CommandMethod("sta")]
	public void Sta()
	{
	  Editor ed;
	  ed = doc.Editor;

	  PromptEntityOptions peo = new PromptEntityOptions("\nSelect running line: ");
	  peo.SetRejectMessage("\nNot a polyline");
	  peo.AddAllowedClass(typeof(Polyline), false);
	  PromptEntityResult per = ed.GetEntity(peo);

	  if (per.Status != PromptStatus.OK) return;

	  PromptStringOptions pso = new PromptStringOptions("\nSelect text side Bottom <Top>: ");
	  PromptResult pr = ed.GetString(pso);

	  if (pr.Status != PromptStatus.OK) return;

	  bool flip = false;

	  while (true)
	  {
		if (pr.StringResult.Length > 0)
		{
		  if (pr.StringResult.ToUpper().Contains("T") || pr.StringResult.ToUpper().Contains("TOP"))
		  {
			break;
		  }
		  else if (pr.StringResult.ToUpper().Contains("B") || pr.StringResult.ToUpper().Contains("BOTTOM"))
		  {
			flip = true;
			break;
		  }
		  else
		  {
			pr = ed.GetString(pso);
			if (pr.Status != PromptStatus.OK) return;
		  }
		}
		break; 
	  }


	  /****************************************/
	  int interval = 100;
	  pso = new PromptStringOptions("\nSelect interval <100>: ");
	  pr = ed.GetString(pso);

	  if (pr.Status != PromptStatus.OK) return;

	  while (true)
	  {
		if (pr.StringResult.Length > 0)
		{
		  if (int.TryParse(pr.StringResult, out int ignoreme))
		  {
			interval = int.Parse(pr.StringResult);
			break;
		  }
		  else
		  {
			pso = new PromptStringOptions("\nPlease enter an interger: ");
			pr = ed.GetString(pso);
			if (pr.Status != PromptStatus.OK) return;
		  }
		}
		else break;
	  }
	  /****************************************/
	  //busca este commentario online eltoca03..drafting
	  Transaction trans = database.TransactionManager.StartTransaction();

	  using (trans)
	  {
		var curSpace = (BlockTableRecord)trans.GetObject(database.CurrentSpaceId, OpenMode.ForWrite);
		Polyline polyline = (Polyline)trans.GetObject(per.ObjectId, OpenMode.ForRead);

		Double len = polyline.Length;
		int count = (int)len / interval;

		for (int i = 1; i <= count; i++)
		{
		  Point3d p1 = polyline.GetPointAtDist(i * interval);
		  Vector3d ang = polyline.GetFirstDerivative(polyline.GetParameterAtPoint(p1));
		  //scale the vector by 1.5
		  ang = ang.GetNormal() * 1.5;
		  //rotate the vector
		  ang = ang.TransformBy(Matrix3d.Rotation(Math.PI / 2, polyline.Normal, Point3d.Origin));
		  // create a line by substracting and adding the vector to the point (displacing the point
		  Line line = new Line(p1 - ang, p1 + ang);
		  line.Layer = "D-UG";
		  //create mtext place end of new line 
		  MText dBText = new MText();
		  //format numbers for context
		  dBText.Contents = FormatStation(i* interval);
		  
		  dBText.BackgroundFill = true;
		  dBText.BackgroundScaleFactor = 1.25;
		  if (flip)
		  {
			if (line.Angle > Math.PI)
			{
			  dBText.Attachment = AttachmentPoint.BottomCenter;
			}
			else
			{
			  dBText.Attachment = AttachmentPoint.TopCenter;
			}
			
			dBText.Location = p1 - (ang.GetNormal() * 2);
		  }
		  else
		  {
			if (line.Angle > Math.PI)
			{
			  dBText.Attachment = AttachmentPoint.TopCenter;
			}
			else
			{
			  dBText.Attachment = AttachmentPoint.BottomCenter;
			}
			
			dBText.Location = p1 + (ang.GetNormal() * 2);
		  }
		  
		  dBText.Layer = "D-UG";
		  dBText.Height = 2.2;

		  if (line.Angle > (Math.PI / 2) && (line.Angle <= Math.PI))
		  {
			dBText.Rotation = line.Angle - (Math.PI / 2);
		  }
		  else if (line.Angle > Math.PI)
		  {
			dBText.Rotation = line.Angle + (Math.PI / 2);
		  }
		  else
		  {
			dBText.Rotation = (2 * Math.PI) - Math.Abs(line.Angle - (Math.PI / 2));
		  }

		  curSpace.AppendEntity(dBText);
		  trans.AddNewlyCreatedDBObject(dBText, true);

		  curSpace.AppendEntity(line);
		  trans.AddNewlyCreatedDBObject(line, true);
		}
		trans.Commit();
	  }
	  
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

	[CommandMethod("ft")]
	public void Flip()
	{
	  Editor ed = doc.Editor;
	  
	  PromptEntityOptions peo = new PromptEntityOptions("\nSelect station to flip: ");
	  peo.SetRejectMessage("\nStation must be MTEXT");
	  peo.AddAllowedClass(typeof(MText), false);
	  PromptEntityResult per = ed.GetEntity(peo);
	  if (per.Status != PromptStatus.OK) return;

	  //PromptStringOptions pso = new PromptStringOptions("Up <Down>: ");
	  //PromptResult pr = ed.GetString(pso);

	  //if (pr.Status != PromptStatus.OK) return;

	  Transaction trans = database.TransactionManager.StartTransaction();

	  using (trans)
	  {
		MText station = (MText)trans.GetObject(per.ObjectId, OpenMode.ForWrite);

		//if (pr.StringResult.ToUpper().Contains("U"))
		//{
		//  station.Location = GetPoint(station.Location.X, station.Location.Y, station.Rotation + (Math.PI * -1.5));
		//}
		//else
		//{
		//  station.Location = GetPoint(station.Location.X, station.Location.Y, station.Rotation + (Math.PI * 1.5));
		//}

		if (station.Attachment == AttachmentPoint.TopCenter)
		{
		  station.Location = GetPoint(station.Location.X, station.Location.Y, station.Rotation + (Math.PI * -1.5));
		  station.Attachment = AttachmentPoint.BottomCenter;
		}
		else
		{
		  station.Location = GetPoint(station.Location.X, station.Location.Y, station.Rotation + (Math.PI * 1.5));
		  station.Attachment = AttachmentPoint.TopCenter;
		}
		
		trans.Commit();
	  }
	  
	}

	public Point3d GetPoint(double x, double y, double ang)
	{
	  Point3d pt = new Point3d(new double[] { 4.2 * Math.Cos(ang) + x, 4.2 * Math.Sin(ang) + y, 0 });

	  return pt;
	}
	
  }
}
