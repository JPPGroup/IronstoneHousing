using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;
using Jpp.Ironstone.Housing.Properties;
using System;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands for use with Lincolnshire only
    /// </summary>
    public static class LincolnshireCommands
    {
        /// <summary>
        /// Custom command for Lincolnshire to add 0.177mm to carriageway level for back of footway
        /// </summary>
        [CommandMethod("C_Lincolnshire_BackOfFootwayLevel_177")]
        public static void AddBackOfFootwayLevel177()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LincolnshireCommands), nameof(AddBackOfFootwayLevel177));
            AddBackOfFootwayLevel(0.177);
        }

        /// <summary>
        /// Custom command for Lincolnshire to add 0.105mm to carriageway level for back of footway
        /// </summary>
        [CommandMethod("C_Lincolnshire_BackOfFootwayLevel_105")]
        public static void AddBackOfFootwayLevel105()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LincolnshireCommands), nameof(AddBackOfFootwayLevel105));
            AddBackOfFootwayLevel(0.105);
        }

        private static void AddBackOfFootwayLevel(double level)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var roadString = SelectRoadString(db, ed);
            if (roadString == null) 
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_No_Road_String_Selected);
                return;
            }

            using (var plane = new Plane(Point3d.Origin, Vector3d.ZAxis))
            {
                var point = ed.PromptForPosition(Resources.Command_Prompt_SelectFootwayPoint);

                while (point.HasValue)
                {
                    var roadPoint = roadString.GetClosestPointTo(point.Value, plane.Normal, false);
                    var roadLevel = Math.Round(roadPoint.Z, 3);
                    var footwayLevel = roadLevel + level;

                    LevelBlockHelper.NewLevelBlockAtPoint(db, new LevelBlockProps(point.Value, footwayLevel));

                    point = ed.PromptForPosition(Resources.Command_Prompt_SelectFootwayPoint);
                }
            }

            trans.Commit();
        }

        private static Polyline3d SelectRoadString(Database database, Editor editor)
        {
            var objectId = editor.PromptForEntity(Resources.Command_Prompt_SelectRoadString, typeof(Polyline3d),Resources.Command_Prompt_Reject3dPolyline, true);
            if (!objectId.HasValue) return null;

            var trans = database.TransactionManager.TopTransaction;
            return trans.GetObject(objectId.Value, OpenMode.ForRead) as Polyline3d;
        }
    }
}
