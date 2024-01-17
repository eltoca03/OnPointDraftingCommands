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
    
        /// <summary>
        /// some stuffffffff
        /// </summary>
	[CommandMethod("sta")]
        public void Sta()
        {
            Editor ed = doc.Editor;

            // Prompt for running line
            Polyline polyline;
            if (!TryGetPolylineEntity(ed, out polyline))
                return;

            // Prompt for text side (Top/Bottom)
            bool flip = PromptForTextSide(ed);

            // Prompt for interval
            int interval = PromptForInterval(ed);

            // Generate stations
            GenerateStations(polyline, flip, interval);
        }

        private bool TryGetPolylineEntity(Editor ed, out Polyline polyline)
        {
            polyline = null;

            PromptEntityOptions peo = new PromptEntityOptions("\nSelect running line: ");
            peo.SetRejectMessage("\nNot a polyline");
            peo.AddAllowedClass(typeof(Polyline), false);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
                return false;


            try
            {
                Transaction trans = database.TransactionManager.StartTransaction();

                using (trans)
                {
                    polyline = (Polyline)trans.GetObject(per.ObjectId, OpenMode.ForRead);
                    return true;
                }
            }
            catch (System.Exception)
            {

                return false;
            }
            
        }

        private bool PromptForTextSide(Editor ed)
        {
            PromptStringOptions pso = new PromptStringOptions("\nSelect text side Bottom <Top>: ");
            PromptResult pr = ed.GetString(pso);

            if (pr.Status != PromptStatus.OK)
                return false;

            while (true)
            {
                if (pr.StringResult.Length > 0)
                {
                    if (pr.StringResult.ToUpper().Contains("T") || pr.StringResult.ToUpper().Contains("TOP"))
                        return false;

                    if (pr.StringResult.ToUpper().Contains("B") || pr.StringResult.ToUpper().Contains("BOTTOM"))
                        return true;

                    pr = ed.GetString(pso);
                    if (pr.Status != PromptStatus.OK)
                        return false;
                }
                break;
            }

            return false;
        }

        private int PromptForInterval(Editor ed)
        {
            int interval = 100;
            PromptStringOptions pso = new PromptStringOptions("\nSelect interval <100>: ");
            PromptResult pr = ed.GetString(pso);

            if (pr.Status != PromptStatus.OK)
                return interval;

            while (true)
            {
                if (pr.StringResult.Length > 0)
                {
                    if (int.TryParse(pr.StringResult, out int result))
                        return result;

                    pso = new PromptStringOptions("\nPlease enter an integer: ");
                    pr = ed.GetString(pso);
                    if (pr.Status != PromptStatus.OK)
                        return interval;
                }
                else
                {
                    break;
                }
            }

            return interval;
        }

        private void GenerateStations(Polyline polyline, bool flip, int interval)
        {
            Transaction trans = database.TransactionManager.StartTransaction();

            using (trans)
            {
                var curSpace = (BlockTableRecord)trans.GetObject(database.CurrentSpaceId, OpenMode.ForWrite);

                Double len = polyline.Length;
                int count = (int)len / interval;

                for (int i = 1; i <= count; i++)
                {
                    Point3d p1 = polyline.GetPointAtDist(i * interval);
                    Vector3d ang = polyline.GetFirstDerivative(polyline.GetParameterAtPoint(p1));

                    // Scale the vector by 1.5
                    ang = ang.GetNormal() * 1.5;
                    // Rotate the vector
                    ang = ang.TransformBy(Matrix3d.Rotation(Math.PI / 2, polyline.Normal, Point3d.Origin));
                    // Create a line by subtracting and adding the vector to the point (displacing the point)
                    Line line = new Line(p1 - ang, p1 + ang);
                    line.Layer = polyline.Layer;

                    // Create mtext placed at the end of the new line
                    MText dBText = new MText();
                    // Format numbers for context
                    dBText.Contents = FormatStation(i * interval);

                    dBText.BackgroundFill = true;
                    dBText.BackgroundScaleFactor = 1.25;
                    if (flip)
                    {
                        if (line.Angle > Math.PI)
                            dBText.Attachment = AttachmentPoint.BottomCenter;
                        else
                            dBText.Attachment = AttachmentPoint.TopCenter;

                        dBText.Location = p1 - (ang.GetNormal() * 2);
                    }
                    else
                    {
                        if (line.Angle > Math.PI)
                            dBText.Attachment = AttachmentPoint.TopCenter;
                        else
                            dBText.Attachment = AttachmentPoint.BottomCenter;

                        dBText.Location = p1 + (ang.GetNormal() * 2);
                    }

                    dBText.Layer = polyline.Layer;
                    dBText.Height = 2.2;
                    dBText.TextStyleId = GetTextStyleId("ROMANS", database);

                    if (line.Angle > (Math.PI / 2) && (line.Angle <= Math.PI))
                        dBText.Rotation = line.Angle - (Math.PI / 2);
                    else if (line.Angle > Math.PI)
                        dBText.Rotation = line.Angle + (Math.PI / 2);
                    else
                        dBText.Rotation = (2 * Math.PI) - Math.Abs(line.Angle - (Math.PI / 2));

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
	public void ft()
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

        private static ObjectId GetTextStyleId(string styleName, Database db)
        {
            TextStyleTable textStyleTable = (TextStyleTable)db.TextStyleTableId.GetObject(OpenMode.ForRead);
            if (textStyleTable.Has(styleName))
            {
                return textStyleTable[styleName];
            }
            else
            {
                // Handle the case when the specified text style doesn't exist
                // You may want to create the text style here or handle it based on your requirements
                throw new System.Exception("Text style '" + styleName + "' not found.");
            }
        }

    }
}
