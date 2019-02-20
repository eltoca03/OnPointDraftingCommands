using System;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace OnPointDrafting
{
  class ConduitLabel
  {
	Document doc = Application.DocumentManager.MdiActiveDocument;
	Database database = Application.DocumentManager.MdiActiveDocument.Database;

	[CommandMethod("cv")]
	public void cd()
	{
	  Editor ed;
	  ed = doc.Editor;

	  //doc.SendStringToExecute("")

	}
  }
}
