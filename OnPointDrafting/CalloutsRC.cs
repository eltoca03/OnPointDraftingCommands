using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using System;


namespace OnPointDrafting

{

  class Commands : IExtensionApplication

  {
	public void Initialize()
	{
	  CountMenu.Attach();
	}

	public void Terminate()
	{
	  CountMenu.Detach();
	}

	//[CommandMethod("SSS", CommandFlags.UsePickSet)]
	//static public void CountSelection()
	//{
	//  Editor ed =
	//	Application.DocumentManager.MdiActiveDocument.Editor;

	//  PromptSelectionResult psr = ed.GetEntity();

	//  if (psr.Status == PromptStatus.OK)

	//  {

	//	ed.WriteMessage(

	//	  "\nSelected {0} entities.",

	//	  psr.Value.Count

	//	);

	 //}

	//}

  }


  public class CountMenu

  {
	private static ContextMenuExtension cme;
	public static void Attach()
	{
	  cme = new ContextMenuExtension();

	  MenuItem mi = new MenuItem("Callouts");
	  mi.MenuItems.Add(new MenuItem("DropBox"));
	  mi.MenuItems.Add(new MenuItem("DropBox"));
	  mi.MenuItems.Add(new MenuItem("DropBox"));
	  mi.MenuItems.Add(new MenuItem("DropBox"));

	  mi.MenuItems[1].Click += new EventHandler(OnCount);

	  cme.MenuItems.Add(mi);

	  RXClass rxc = Entity.GetClass(typeof(Entity));

	  Application.AddObjectContextMenuExtension(rxc, cme);

	}

	public static void Detach()

	{

	  RXClass rxc = Entity.GetClass(typeof(Entity));

	  Application.RemoveObjectContextMenuExtension(rxc, cme);

	}

	private static void OnCount(Object o, EventArgs e)

	{

	  Document doc =

		Application.DocumentManager.MdiActiveDocument;

	  //doc.SendStringToExecute("_.COUNT ", true, false, false);
	  Editor ed =
		Application.DocumentManager.MdiActiveDocument.Editor;

	  while (true)
	  {
		Transaction trans = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction();
		using (trans)
		{
		  
		  

		  
		}
	  }
	}
  }
}