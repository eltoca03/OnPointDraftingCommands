using System;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace OnPointDrafting
{
    public class Span
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database database = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("AER")]
        public void AER ()
        {
            Editor ed;
            ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\nSelect aerial line: ");
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

            Transaction trans = database.TransactionManager.StartTransaction();

            using (trans)
            {
                
                var curSpace = (BlockTableRecord)trans.GetObject(database.CurrentSpaceId, OpenMode.ForWrite);
                Polyline polyline = (Polyline)trans.GetObject(per.ObjectId, OpenMode.ForRead);

				BringPolesForward(polyline);

                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    if (i+1 != polyline.NumberOfVertices)
                    {
                        Point3d pt = Matchline.getMidPoint(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1)));
                        
                        MText dBText = GetMText(polyline, pt, flip);
                        dBText.Layer = "D-OH";
                        dBText.TextHeight = 2.2;
                        dBText.Contents = Math.Round(polyline.GetPoint3dAt(i).GetVectorTo(polyline.GetPoint3dAt(i + 1)).Length).ToString() + "'";

                        curSpace.AppendEntity(dBText);
                        trans.AddNewlyCreatedDBObject(dBText, true);
                    }                 
                }
                polyline.UpgradeOpen();
                polyline.ConstantWidth = 1;
                polyline.Layer = "D-OH";
                polyline.Plinegen = false;
                                
                trans.Commit();
            }
        }

        public static MText GetMText(Polyline pol, Point3d p1, bool t)
        {
            
            Vector3d ang = pol.GetFirstDerivative(pol.GetParameterAtPoint(p1));
            //scale the vector by 1.5
            ang = ang.GetNormal() * 1.5;
            //rotate the vector
            ang = ang.TransformBy(Matrix3d.Rotation(Math.PI / 2, pol.Normal, Point3d.Origin));
            // create a line by substracting and adding the vector to the point (displacing the point
            
            MText dBText = new MText();
           

            dBText.BackgroundFill = true;
            dBText.BackgroundScaleFactor = 1.25;
            if (t)
            {
                if (ang.AngleOnPlane(pol.GetPlane()) > Math.PI)
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
                if (ang.AngleOnPlane(pol.GetPlane()) > Math.PI)
                {
                    dBText.Attachment = AttachmentPoint.TopCenter;
                }
                else
                {
                    dBText.Attachment = AttachmentPoint.BottomCenter;
                }

                dBText.Location = p1 + (ang.GetNormal() * 2);
            }


            if (ang.AngleOnPlane(pol.GetPlane()) > (Math.PI / 2) && (ang.AngleOnPlane(pol.GetPlane()) <= Math.PI))
            {
                dBText.Rotation = ang.AngleOnPlane(pol.GetPlane()) - (Math.PI / 2);
            }
            else if (ang.AngleOnPlane(pol.GetPlane()) > Math.PI)
            {
                dBText.Rotation = ang.AngleOnPlane(pol.GetPlane()) + (Math.PI / 2);
            }
            else
            {
                dBText.Rotation = (2 * Math.PI) - Math.Abs(ang.AngleOnPlane(pol.GetPlane()) - (Math.PI / 2));
            }

            return dBText;
        }

		public void BringPolesForward(Polyline polyline)
		{
			Point3dCollection vertices = new Point3dCollection();

			for (int i = 0; i < polyline.NumberOfVertices; i++)
			{
				vertices.Add(polyline.GetPoint3dAt(i));
			}

			//get fence selection results
			PromptSelectionResult promptSelectionResult = doc.Editor.SelectFence(vertices);
			if (promptSelectionResult.Status != PromptStatus.OK)
			{
				return;
			}

			Transaction trans = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();

			using (trans)
			{
				//Open current space
				var curSpace = (BlockTableRecord)trans.GetObject(database.CurrentSpaceId, OpenMode.ForRead);
				var drawOrder = (DrawOrderTable)trans.GetObject(curSpace.DrawOrderTableId, OpenMode.ForWrite);

				ObjectIdCollection idCollection = new ObjectIdCollection();

				//iterate troughth selection set and poles along line
				foreach (SelectedObject selectedObject in promptSelectionResult.Value)
				{
					BlockReference blockReference = null;
					try
					{
						blockReference = (BlockReference)trans.GetObject(selectedObject.ObjectId, OpenMode.ForRead);
					}
					catch
					{
						//this means that the selected object was not a BlockReference type....so continue to next
						continue;
					}

					if (blockReference != null && blockReference.Name.ToLower() == "pp")
					{
						idCollection.Add(blockReference.ObjectId);
					}
					
				}

				drawOrder.MoveToTop(idCollection);
				trans.Commit();
			}
		}
	}
}
