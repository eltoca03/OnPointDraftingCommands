using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.AutoCAD.DatabaseServices;

using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Runtime;

using Autodesk.AutoCAD.Geometry;



namespace RotatingRectangles

{

	public class Commands

	{

		// Define some constants we'll use to

		// store our XData



		// AppName is our RDS (TTIF, for

		// "Through The InterFace") plus an indicator

		// what it's for (ROTation)



		const string kRegAppName = "TTIF_ROT";

		const int kAppCode = 1001;

		const int kRotCode = 1040;



		class RotateJig : EntityJig

		{

			// Declare some internal state



			double m_baseAngle, m_deltaAngle;

			Point3d m_rotationPoint;

			Matrix3d m_ucs;



			// Constructor sets the state and clones

			// the entity passed in

			// (adequate for simple entities)



			public RotateJig(

			  Entity ent,

			  Point3d rotationPoint,

			  double baseAngle,

			  Matrix3d ucs)

			  : base(ent.Clone() as Entity)

			{

				m_rotationPoint = rotationPoint;

				m_baseAngle = baseAngle;

				m_ucs = ucs;

			}



			protected override SamplerStatus Sampler(

			  JigPrompts jp

			)

			{

				// We acquire a single angular value



				JigPromptAngleOptions jo =

				  new JigPromptAngleOptions(

					"\nAngle of rotation: "

				  );

				jo.BasePoint = m_rotationPoint;

				jo.UseBasePoint = true;



				PromptDoubleResult pdr =

				  jp.AcquireAngle(jo);



				if (pdr.Status == PromptStatus.OK)

				{

					// Check if it has changed or not

					// (reduces flicker)



					if (m_baseAngle == pdr.Value)

					{

						return SamplerStatus.NoChange;

					}

					else

					{

						// Set the change in angle to

						// the new value



						m_deltaAngle = pdr.Value;

						return SamplerStatus.OK;

					}

				}

				return SamplerStatus.Cancel;

			}



			protected override bool Update()

			{

				// Filter out the case where a zero delta is provided



				if (m_deltaAngle > Tolerance.Global.EqualPoint)

				{

					// We rotate the polyline by the change

					// minus the base angle



					Matrix3d trans =

					  Matrix3d.Rotation(

						m_deltaAngle - m_baseAngle,

						m_ucs.CoordinateSystem3d.Zaxis,

						m_rotationPoint);

					Entity.TransformBy(trans);



					// The base becomes the previous delta

					// and the delta gets set to zero



					m_baseAngle = m_deltaAngle;

					m_deltaAngle = 0.0;

				}



				return true;

			}



			public Entity GetEntity()

			{

				return Entity;

			}



			public double GetRotation()

			{

				// The overall rotation is the

				// base plus the delta



				return m_baseAngle + m_deltaAngle;

			}

		}



		[CommandMethod("ROT")]

		public void RotateEntity()

		{

			Document doc =

			  Application.DocumentManager.MdiActiveDocument;

			Editor ed = doc.Editor;

			Database db = doc.Database;



			// First we prompt for the entity to rotate



			PromptEntityOptions peo =

			  new PromptEntityOptions(

				"\nSelect entity to rotate: "

			  );

			PromptEntityResult per =

			  ed.GetEntity(peo);



			if (per.Status == PromptStatus.OK)

			{

				Transaction tr =

				  db.TransactionManager.StartTransaction();

				using (tr)

				{

					DBObject obj =

					  tr.GetObject(per.ObjectId, OpenMode.ForRead);

					Entity ent = obj as Entity;



					// Use the origin as the default center



					Point3d rotationPoint = Point3d.Origin;



					// If the entity is a polyline,

					// assume it is rectangular and then

					// set the rotation point as its center



					Polyline pl = obj as Polyline;

					if (pl != null)

					{

						LineSegment3d ps0 =

						  pl.GetLineSegmentAt(0);

						LineSegment3d ps1 =

						  pl.GetLineSegmentAt(1);

						Vector3d vec =

						  ((ps0.EndPoint - ps0.StartPoint) / 2.0) +

						  ((ps1.EndPoint - ps1.StartPoint) / 2.0);

						rotationPoint = pl.StartPoint + vec;

					}



					// Get the base rotation angle stored with the

					// entity, if there was one (default is 0.0)



					double baseAngle = GetStoredRotation(obj);



					if (ent != null)

					{

						// Get the current UCS, to pass to the Jig



						Matrix3d ucs =

						  ed.CurrentUserCoordinateSystem;



						// Create our jig object



						RotateJig jig =

						  new RotateJig(

							ent,

							rotationPoint,

							baseAngle,

							ucs

						  );



						PromptResult res = ed.Drag(jig);

						if (res.Status == PromptStatus.OK)

						{

							// Get the overall rotation angle

							// and dispose of the temp clone



							double newAngle = jig.GetRotation();

							jig.GetEntity().Dispose();



							// Rotate the original entity



							Matrix3d trans =

							  Matrix3d.Rotation(

								newAngle - baseAngle,

								ucs.CoordinateSystem3d.Zaxis,

								rotationPoint);



							ent.UpgradeOpen();

							ent.TransformBy(trans);



							// Store the new rotation as XData



							SetStoredRotation(ent, newAngle);

						}

					}

					tr.Commit();

				}

			}

		}



		// Helper function to create a RegApp



		static void AddRegAppTableRecord(string regAppName)

		{

			Document doc =

			  Application.DocumentManager.MdiActiveDocument;

			Editor ed = doc.Editor;

			Database db = doc.Database;



			Transaction tr =

			  doc.TransactionManager.StartTransaction();

			using (tr)

			{

				RegAppTable rat =

				  (RegAppTable)tr.GetObject(

					db.RegAppTableId,

					OpenMode.ForRead,

					false

				  );

				if (!rat.Has(regAppName))

				{

					rat.UpgradeOpen();

					RegAppTableRecord ratr =

					  new RegAppTableRecord();

					ratr.Name = regAppName;

					rat.Add(ratr);

					tr.AddNewlyCreatedDBObject(ratr, true);

				}

				tr.Commit();

			}

		}



		// Store our rotation angle as XData



		private void SetStoredRotation(

		  DBObject obj, double rotation)

		{

			AddRegAppTableRecord(kRegAppName);

			ResultBuffer rb = obj.XData;

			if (rb == null)

			{

				rb =

				  new ResultBuffer(

					new TypedValue(kAppCode, kRegAppName),

					new TypedValue(kRotCode, rotation)

				  );

			}

			else

			{

				// We can simply add our values - no need

				// to remove the previous ones, the new ones

				// are the ones that get stored



				rb.Add(new TypedValue(kAppCode, kRegAppName));

				rb.Add(new TypedValue(kRotCode, rotation));

			}

			obj.XData = rb;

			rb.Dispose();

		}



		// Retrieve the existing rotation angle from XData



		private double GetStoredRotation(DBObject obj)

		{

			double ret = 0.0;



			ResultBuffer rb = obj.XData;

			if (rb != null)

			{

				// If we find our group code, it means that on

				// the next iteration, we'll get our rotation



				bool bReadyForRot = false;

				foreach (TypedValue tv in rb)

				{

					if (bReadyForRot)

					{

						if (tv.TypeCode == kRotCode)

							ret = (double)tv.Value;

						bReadyForRot = false;

					}

					if (tv.TypeCode == kAppCode)

						bReadyForRot = true;

				}

				rb.Dispose();

			}

			return ret;

		}

	}

}