using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.Autocad.DrawingObjects.Primitives;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Housing.ObjectModel;
using Jpp.Ironstone.Housing.ObjectModel.Concept;

namespace Jpp.Ironstone.Housing.Commands
{
    public static class ConceptualPlotCommands
    {
        [CommandMethod("C_ConceptualPlot_Create")]
        public static void CreatePlot()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(ConceptualPlotCommands), nameof(CreatePlot));

            // Get the current document and database
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;

            using (Transaction trans = document.TransactionManager.StartTransaction())
            {
                // Request for objects to be selected in the drawing area
                PromptSelectionResult selectionResult = document.Editor.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selectionResult.Status == PromptStatus.OK)
                {
                    ConceptualPlotManager manager = DataService.Current.GetStore<HousingDocumentStore>(document.Name).GetManager<ConceptualPlotManager>();

                    SelectionSet selectionSet = selectionResult.Value;

                    int skippedObjects = 0;
                    int unclosedPolylines = 0;

                    // Step through the objects in the selection set
                    foreach (SelectedObject selectedObject in selectionSet)
                    {
                        // Check to make sure a valid SelectedObject object was returned
                        if (selectedObject != null)
                        {
                            // Open the selected object for write
                            Entity entity = trans.GetObject(selectedObject.ObjectId,
                                OpenMode.ForWrite) as Entity;

                            PolylineDrawingObject polylineDrawingObject;

                            switch (entity)
                            {
                                case Polyline polyline:
                                    polylineDrawingObject = new PolylineDrawingObject(polyline);
                                    break;

                                case Polyline2d polyline:
                                    polylineDrawingObject = new PolylineDrawingObject(polyline);
                                    break;

                                case Polyline3d polyline:
                                    polylineDrawingObject = new PolylineDrawingObject(polyline);
                                    break;

                                default:
                                    skippedObjects++;
                                    continue;
                            }

                            if (polylineDrawingObject.IsClosed())
                            {
                                manager.Add(new ConceptualPlot(polylineDrawingObject));
                            }
                            else
                            {
                                unclosedPolylines++;
                            }
                        }
                    }

                    if(skippedObjects > 0)
                        document.Editor.WriteMessage($"\n{skippedObjects} objects were not polylines and were skipped.");
                    if (unclosedPolylines > 0)
                        document.Editor.WriteMessage($"\n{unclosedPolylines} polylines were not closed and were skipped.");

                    // Save the new object to the database
                    trans.Commit();
                }
            }
        }
    }
}
