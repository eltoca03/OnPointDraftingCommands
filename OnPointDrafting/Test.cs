using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.AutoCAD.DatabaseServices;

using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Runtime;

using Autodesk.AutoCAD.Geometry;


namespace OnPointDrafting

{

	class BlockJig : EntityJig

	{

		Point3d mCenterPt, mActualPoint;


		public BlockJig(BlockReference br)

		  : base(br)

		{

			mCenterPt = br.Position;

		}


		protected override SamplerStatus Sampler(JigPrompts prompts)

		{

			JigPromptPointOptions jigOpts =

			  new JigPromptPointOptions();

			jigOpts.UserInputControls =

			  (UserInputControls.Accept3dCoordinates

			  | UserInputControls.NoZeroResponseAccepted

			  | UserInputControls.NoNegativeResponseAccepted);


			jigOpts.Message =

			  "\nEnter insert point: ";


			PromptPointResult dres =

			  prompts.AcquirePoint(jigOpts);


			if (mActualPoint == dres.Value)

			{

				return SamplerStatus.NoChange;

			}

			else

			{

				mActualPoint = dres.Value;

			}

			return SamplerStatus.OK;

		}


		protected override bool Update()

		{

			mCenterPt = mActualPoint;

			try

			{

				((BlockReference)Entity).Position = mCenterPt;

			}

			catch (System.Exception)

			{

				return false;

			}

			return true;

		}


		public Entity GetEntity()

		{

			return Entity;

		}

	}


	public class test

	{

		[CommandMethod("BJIG")]

		public void CreateBlockWithJig()

		{

			Document doc =

			  Application.DocumentManager.MdiActiveDocument;

			Database db = doc.Database;

			Editor ed = doc.Editor;


			// First let's get the name of the block

			PromptStringOptions opts =

			  new PromptStringOptions("\nEnter block name: ");

			PromptResult pr = ed.GetString(opts);

			if (pr.Status == PromptStatus.OK)

			{

				Transaction tr =

				  doc.TransactionManager.StartTransaction();

				using (tr)

				{

					// Then open the block table and check the

					// block definition exists

					BlockTable bt =

					  (BlockTable)tr.GetObject(

						db.BlockTableId,

						OpenMode.ForRead

					  );

					if (!bt.Has(pr.StringResult))

					{

						ed.WriteMessage("\nBlock not found.");

					}

					else

					{

						ObjectId blockId = bt[pr.StringResult];


						// We loop until the jig is cancelled

						while (pr.Status == PromptStatus.OK)

						{

							// Create the block reference and

							// add it to the jig

							Point3d pt = new Point3d(0, 0, 0);

							BlockReference br =

							  new BlockReference(pt, blockId);

							BlockJig entJig = new BlockJig(br);


							// Perform the jig operation

							pr = ed.Drag(entJig);

							if (pr.Status == PromptStatus.OK)

							{

								// If all is OK, let's go and add the

								// entity to the modelspace

								BlockTableRecord btr =

								  (BlockTableRecord)tr.GetObject(

									bt[BlockTableRecord.ModelSpace],

									OpenMode.ForWrite

								  );

								btr.AppendEntity(

								  entJig.GetEntity()

								);

								tr.AddNewlyCreatedDBObject(

								  entJig.GetEntity(),

								  true

								);

								// Call a function to make the graphics display

								// (otherwise it will only do so when we Commit)

								doc.TransactionManager.QueueForGraphicsFlush();

							}

						}

					}

					tr.Commit();

				}

			}

		}

	}

}