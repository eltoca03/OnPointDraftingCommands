#region Namespaces

using System;
using System.Text;
using System.Linq;
using System.Xml;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Drawing;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;

using MgdAcApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using MgdAcDocument = Autodesk.AutoCAD.ApplicationServices.Document;
using AcWindowsNS = Autodesk.AutoCAD.Windows;

#endregion

namespace AcadNetCSharp
{
	public class MLeaderJigger : EntityJig
	{
		#region Fields

		public int mCurJigFactorIndex = 1;  // Jig Factor Index

		public Autodesk.AutoCAD.Geometry.Point3d mArrowLocation; // Jig Factor #1
		public Autodesk.AutoCAD.Geometry.Point3d mTextLocation; // Jig Factor #2
		public string mMText; // Jig Factor #3


		#endregion

		#region Constructors

		public MLeaderJigger(MLeader ent)
			: base(ent)
		{
			Entity.SetDatabaseDefaults();

			Entity.ContentType = ContentType.MTextContent;
			Entity.MText = new MText();
			Entity.MText.SetDatabaseDefaults();

			Entity.EnableDogleg = true;
			Entity.EnableLanding = true;
			Entity.EnableFrameText = false;

			Entity.AddLeaderLine(mTextLocation);
			Entity.SetFirstVertex(0, mArrowLocation);

			Entity.TransformBy(UCS);
		}

		#endregion

		#region Properties

		private Editor Editor
		{
			get
			{
				return MgdAcApplication.DocumentManager.MdiActiveDocument.Editor;
			}
		}

		private Matrix3d UCS
		{
			get
			{
				return MgdAcApplication.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;
			}
		}

		#endregion

		#region Overrides

		public new MLeader Entity  // Overload the Entity property for convenience.
		{
			get
			{
				return base.Entity as MLeader;
			}
		}

		protected override bool Update()
		{

			switch (mCurJigFactorIndex)
			{
				case 1:
					Entity.SetFirstVertex(0, mArrowLocation);
					Entity.SetLastVertex(0, mArrowLocation);

					break;
				case 2:
					Entity.SetLastVertex(0, mTextLocation);

					break;
				case 3:
					Entity.MText.Contents = mMText;

					break;

				default:
					return false;
			}

			return true;
		}

		protected override SamplerStatus Sampler(JigPrompts prompts)
		{
			switch (mCurJigFactorIndex)
			{
				case 1:
					JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\nArrow Location:");
					// Set properties such as UseBasePoint and BasePoint of the prompt options object if necessary here.
					prOptions1.UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.GovernedByOrthoMode | UserInputControls.GovernedByUCSDetect | UserInputControls.UseBasePointElevation;
					PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
					if (prResult1.Status == PromptStatus.Cancel && prResult1.Status == PromptStatus.Error)
						return SamplerStatus.Cancel;

					if (prResult1.Value.Equals(mArrowLocation))  //Use better comparison method if necessary.
					{
						return SamplerStatus.NoChange;
					}
					else
					{
						mArrowLocation = prResult1.Value;
						return SamplerStatus.OK;
					}
				case 2:
					JigPromptPointOptions prOptions2 = new JigPromptPointOptions("\nLanding Location:");
					// Set properties such as UseBasePoint and BasePoint of the prompt options object if necessary here.
					prOptions2.UseBasePoint = true;
					prOptions2.BasePoint = mArrowLocation;
					prOptions2.UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.GovernedByOrthoMode | UserInputControls.GovernedByUCSDetect | UserInputControls.UseBasePointElevation;
					PromptPointResult prResult2 = prompts.AcquirePoint(prOptions2);
					if (prResult2.Status == PromptStatus.Cancel && prResult2.Status == PromptStatus.Error)
						return SamplerStatus.Cancel;

					if (prResult2.Value.Equals(mTextLocation))  //Use better comparison method if necessary.
					{
						return SamplerStatus.NoChange;
					}
					else
					{
						mTextLocation = prResult2.Value;
						return SamplerStatus.OK;
					}
				case 3:
					JigPromptStringOptions prOptions3 = new JigPromptStringOptions("\nText Content:");
					// Set properties such as UseBasePoint and BasePoint of the prompt options object if necessary here.
					prOptions3.UserInputControls = UserInputControls.AcceptOtherInputString;
					PromptResult prResult3 = prompts.AcquireString(prOptions3);
					if (prResult3.Status == PromptStatus.Cancel && prResult3.Status == PromptStatus.Error)
						return SamplerStatus.Cancel;

					if (prResult3.StringResult.Equals(mMText))  //Use better comparison method if necessary.
					{
						return SamplerStatus.NoChange;
					}
					else
					{
						mMText = prResult3.StringResult;
						return SamplerStatus.OK;
					}

				default:
					break;
			}

			return SamplerStatus.OK;
		}



		#endregion

		#region Methods to Call

		public static MLeader Jig()
		{
			MLeaderJigger jigger = null;
			try
			{
				jigger = new MLeaderJigger(new MLeader());
				PromptResult pr;
				do
				{
					pr = MgdAcApplication.DocumentManager.MdiActiveDocument.Editor.Drag(jigger);
					if (pr.Status == PromptStatus.Keyword)
					{
						// Keyword handling code

					}
					else
					{
						jigger.mCurJigFactorIndex++;
					}
				} while (pr.Status != PromptStatus.Cancel && pr.Status != PromptStatus.Error && jigger.mCurJigFactorIndex <= 3);

				if (pr.Status == PromptStatus.Cancel || pr.Status == PromptStatus.Error)
				{
					if (jigger != null && jigger.Entity != null)
						jigger.Entity.Dispose();

					return null;
				}
				else
				{
					MText text = new MText();
					text.Contents = jigger.mMText;
					text.TransformBy(jigger.UCS);
					jigger.Entity.MText = text;
					return jigger.Entity;
				}
			}
			catch
			{
				if (jigger != null && jigger.Entity != null)
					jigger.Entity.Dispose();

				return null;
			}
		}

		#endregion

		#region Test Commands

		[CommandMethod("TestMLeaderJigger")]
		public static void TestMLeaderJigger_Method()
		{
			try
			{
				Entity jigEnt = MLeaderJigger.Jig();
				if (jigEnt != null)
				{
					Database db = HostApplicationServices.WorkingDatabase;
					using (Transaction tr = db.TransactionManager.StartTransaction())
					{
						BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
						btr.AppendEntity(jigEnt);
						tr.AddNewlyCreatedDBObject(jigEnt, true);
						tr.Commit();
					}
				}
			}
			catch (System.Exception ex)
			{
				MgdAcApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.ToString());
			}
		}

		#endregion
	}
}