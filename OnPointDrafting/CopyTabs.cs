using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace OnPointDrafting
{
    public class CopyTabs
    {
        /// <summary>
        /// Copy selected tab
        /// </summary>
        [CommandMethod("CPT")]
        public void cpt()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Prompt the user for the base layout name
            PromptStringOptions baseLayoutPromptOptions = new PromptStringOptions("\nEnter the last current layout name: ");
            PromptResult baseLayoutPromptResult = ed.GetString(baseLayoutPromptOptions);

            if (baseLayoutPromptResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Command canceled.");
                return;
            }

            string baseLayoutName = baseLayoutPromptResult.StringResult;

            // Prompt the user for the last sheet number
            PromptIntegerOptions lastSheetPromptOptions = new PromptIntegerOptions("\nEnter the last sheet number to create: ");
            PromptIntegerResult lastSheetPromptResult = ed.GetInteger(lastSheetPromptOptions);

            if (lastSheetPromptResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Command canceled.");
                return;
            }

            int lastSheetNumber = lastSheetPromptResult.Value;

            // Start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Open the source layout for read
                    Layout sourceLayout = tr.GetObject(LayoutManager.Current.GetLayoutId(baseLayoutName), OpenMode.ForRead) as Layout;

                    // Get the source layout index
                    int sourceLayoutIndex = sourceLayout.TabOrder;

                    // Get the source layout name index
                    int sourceLayoutNameIndex = int.Parse(baseLayoutName);

                    // Calculate the layout count based on the difference
                    int layoutCount = lastSheetNumber - sourceLayoutNameIndex;

                    // Create layouts based on the calculated layout count
                    for (int i = 1; i <= layoutCount; i++)
                    {
                        string newLayoutName = (sourceLayoutNameIndex + i).ToString("00");

                        // Create a new layout
                        ObjectId destLayoutId = LayoutManager.Current.CreateLayout(newLayoutName);

                        if (!destLayoutId.IsNull)
                        {
                            // Open the new layout for write
                            Layout destLayout = tr.GetObject(destLayoutId, OpenMode.ForWrite) as Layout;

                            // Copy the source layout to the destination layout
                            destLayout.CopyFrom(sourceLayout);

                            // Set the layout order based on the layout index
                            int newLayoutIndex = sourceLayoutIndex + i;
                            destLayout.TabOrder = newLayoutIndex;

                            // Copy contents from the source layout
                            CopyLayoutContents(tr, sourceLayout, destLayout);
                        }
                        else
                        {
                            ed.WriteMessage("Failed to create layout '{0}'.", newLayoutName);
                        }
                    }

                    // Commit the changes to the new layout
                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("Error: " + ex.Message);
                }
            }
        }

        private void CopyLayoutContents(Transaction tr, Layout sourceLayout, Layout destLayout)
        {
            // Copy contents from source to destination layout
            BlockTableRecord sourceBtr = tr.GetObject(sourceLayout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
            BlockTableRecord destBtr = tr.GetObject(destLayout.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;

            foreach (ObjectId sourceId in sourceBtr)
            {
                Entity sourceEntity = tr.GetObject(sourceId, OpenMode.ForRead) as Entity;

                if (sourceEntity != null)
                {
                    // Clone the entity
                    Entity destEntity = sourceEntity.Clone() as Entity;

                    // Add the cloned entity to the destination layout
                    destBtr.AppendEntity(destEntity);
                    tr.AddNewlyCreatedDBObject(destEntity, true);
                }
            }
        }

    }
}
