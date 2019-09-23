using System;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Linq;

namespace OnPointDrafting
{
	public class UtilityText
	{
		Document doc = Application.DocumentManager.MdiActiveDocument;
		Database database = Application.DocumentManager.MdiActiveDocument.Database;

		[CommandMethod("UT")]
		public void UT()
		{
			Editor ed = doc.Editor;

			while (true)
			{
				PromptEntityOptions peo = new PromptEntityOptions("\nSelect Utility Line: ");
				peo.SetRejectMessage("Must be a Line not a Polyline!");
				peo.AddAllowedClass(typeof(Line), true);

				PromptEntityResult per = ed.GetEntity(peo);

				if (per.Status != PromptStatus.OK)
					return;

				Transaction trans = database.TransactionManager.StartTransaction();
				using (trans)
				{
					//open blockTable for read
					BlockTable blockTable;
					blockTable = trans.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;

					BlockTableRecord blockTableRecord;
					blockTableRecord = trans.GetObject(blockTable[BlockTableRecord.ModelSpace],
														OpenMode.ForWrite) as BlockTableRecord;

					LayerTable layerTable;
					layerTable = trans.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;

					Line line = trans.GetObject(per.ObjectId, OpenMode.ForWrite) as Line;
					LayerTableRecord ltr = trans.GetObject(line.LayerId,
															OpenMode.ForRead) as LayerTableRecord;

					DBText text = new DBText();
					text.Layer = ltr.Name;
					text.Rotation = line.Angle;
					//always set justify first then AligmentPoint
					text.Justify = AttachmentPoint.MiddleCenter;
					text.AlignmentPoint = getMidPoint(line);
					text.TextString = contentFormat(line.Linetype);

					line.LinetypeId = ltr.LinetypeObjectId;

					Point3d o = new Point3d(0, 0, 0);
					double dist = o.DistanceTo(text.GeometricExtents.MaxPoint) >= o.DistanceTo(text.GeometricExtents.MinPoint) ? o.DistanceTo(text.GeometricExtents.MaxPoint) * 1.2 : o.DistanceTo(text.GeometricExtents.MinPoint) * 1.2;

					if ((line.Angle > Math.PI / 2 && line.Angle < Math.PI) || (line.Angle > Math.PI * 1.5 && line.Angle < Math.PI * 2))
						dist *= 1.2;

					Line line2 = (Line)line.Clone();
					line2.StartPoint = PolarPoints(text.AlignmentPoint, line.Angle, dist);
					line2.EndPoint = line.EndPoint;

					line.EndPoint = PolarPoints(text.AlignmentPoint, line.Angle, -dist);

					blockTableRecord.AppendEntity(line2);
					blockTableRecord.AppendEntity(text);
					trans.AddNewlyCreatedDBObject(line2, true);
					trans.AddNewlyCreatedDBObject(text, true);
					trans.Commit();
				}
			}
		}

		public Point3d getMidPoint(Line a)
		{
			Point3d point3d = new Point3d();
			Vector3d vector = new Vector3d();
			vector = a.StartPoint.GetVectorTo(a.EndPoint);
			point3d = a.StartPoint + (vector / 2);

			return point3d;
		}

		public string contentFormat(string size)
		{
			string temp = new String(size.Where(Char.IsDigit).ToArray());
			int.TryParse(temp, out int i);
			temp = i.ToString();
			return temp + "\"" + new String(size.Where(Char.IsLetter).ToArray());
		}

		[CommandMethod("mtx")]
		public void mtx()
		{
			Editor ed = doc.Editor;

			Transaction trans = database.TransactionManager.StartTransaction();
			using (trans)
			{
				BlockTable blockTable;
				blockTable = trans.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;

				BlockTableRecord blockTableRecord;
				blockTableRecord = trans.GetObject(blockTable[BlockTableRecord.ModelSpace],
												   OpenMode.ForWrite) as BlockTableRecord;

				Line line = new Line();
				line.StartPoint = new Point3d(0, 0, 0);
				line.EndPoint = new Point3d(10, 10, 0);

				Circle circle = new Circle();
				circle.Center = new Point3d(5, 5, 0);
				circle.Radius = 1;

				Point3d pt = PolarPoints(circle.Center, line.Angle, 2);

				line.EndPoint = pt;
				
				blockTableRecord.AppendEntity(line);
				blockTableRecord.AppendEntity(circle);

				trans.AddNewlyCreatedDBObject(line, true);
				trans.AddNewlyCreatedDBObject(circle, true);


				Vector3d vector = circle.Center.GetAsVector();

				trans.Commit();
			}
		}

		static Point3d PolarPoints(Point3d pPt, double dAng, double dDist)
		{
			return new Point3d(pPt.X + dDist * Math.Cos(dAng),
							   pPt.Y + dDist * Math.Sin(dAng),
							   pPt.Z);
		}
	}
}