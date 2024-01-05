using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace OnPointDrafting
{
    public class CopyTabs
    {
        [CommandMethod("CPT")]
        public static void CopyPSTabsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Prompt the user for the base layout name
            PromptStringOptions baseLayoutPromptOptions = new PromptStringOptions("\nEnter the base layout name: ");
            PromptResult baseLayoutPromptResult = ed.GetString(baseLayoutPromptOptions);

            if (baseLayoutPromptResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Command canceled.");
                return;
            }

            string baseLayoutName = baseLayoutPromptResult.StringResult;

            // Prompt the user for the layout count
            PromptIntegerOptions countPromptOptions = new PromptIntegerOptions("\nEnter the number of layouts to create: ");
            PromptIntegerResult countPromptResult = ed.GetInteger(countPromptOptions);

            if (countPromptResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Command canceled.");
                return;
            }

            int layoutCount = countPromptResult.Value;

            // Start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Open the source layout for read
                    Layout sourceLayout = tr.GetObject((db.LayoutDictionaryId.GetObject(OpenMode.ForRead) as DBDictionary).GetAt(baseLayoutName), OpenMode.ForRead) as Layout;

                    // Get the source layout index
                    int sourceLayoutIndex = sourceLayout.TabOrder;

                    // Get the source layout name index
                    int sourceLayoutNameIndex = int.Parse(baseLayoutName);

                    // Create layouts based on the specified count
                    for (int i = 1; i <= layoutCount; i++)
                    {
                        string newLayoutName = (sourceLayoutNameIndex + i).ToString("00");

                        // Create a new layout
                        Layout destLayout = new Layout();
                        destLayout.LayoutName = newLayoutName;

                        // Open the layout dictionary for write
                        DBDictionary layoutDict = tr.GetObject(db.LayoutDictionaryId.GetObject(OpenMode.ForWrite).ObjectId, OpenMode.ForWrite) as DBDictionary;

                        // Add the new layout to the layouts dictionary
                        layoutDict.SetAt(newLayoutName, destLayout);
                        tr.AddNewlyCreatedDBObject(destLayout, true);

                        // Copy the source layout to the destination layout
                        destLayout.CopyFrom(sourceLayout);

                        // Set the layout order based on the layout index
                        int newLayoutIndex = sourceLayoutIndex + i;
                        destLayout.TabOrder = newLayoutIndex;
                    }

                    // Commit the transaction
                    tr.Commit();

                    doc.Editor.Regen();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("Error: " + ex.Message);
                }
            }
        }
    }
}
