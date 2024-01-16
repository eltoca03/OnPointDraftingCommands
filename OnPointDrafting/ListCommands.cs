
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using Exception = System.Exception;

namespace OnPointDrafting
{
    public class ListCommands
    {
        /// <summary>
        /// The list Command displays all commands in this dll
        /// </summary>
        [CommandMethod("listCom")]
        public void listcom()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                var commandMethods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(CommandMethodAttribute), false).Length > 0)
                    .ToList();

                if (commandMethods.Count > 0)
                {
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Command methods in the current assembly:\n");

                    foreach (var method in commandMethods)
                    {
                        var methodName = method.Name;
                        var summary = GetMethodSummary(method);

                        Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Method: {methodName}\nSummary: {summary}\n\n");
                    }
                }
                else
                {
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("No command methods found in the current assembly.\n");
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"An error occurred: {ex.Message}\n");
            }
        }

        private string GetMethodSummary(MethodInfo method)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var xmlPath = assembly.Location.Replace(".dll", ".xml"); // Assumes the XML file has the same name as the assembly with a .xml extension

                if (System.IO.File.Exists(xmlPath))
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(xmlPath);

                    var memberName = $"M:{method.DeclaringType.FullName}.{method.Name}";
                    var summaryNode = xmlDoc.SelectSingleNode($"//member[@name='{memberName}']/summary");

                    if (summaryNode != null)
                    {
                        return summaryNode.InnerXml.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., file not found, XML parsing error)
                return $"Error reading XML comments: {ex.Message}";
            }

            return "No summary available.";
        }
    }
}

