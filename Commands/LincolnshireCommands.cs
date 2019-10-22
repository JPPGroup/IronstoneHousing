using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;
using System;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands for use with Lincolnshire only
    /// </summary>
    public static class LincolnshireCommands
    {
        private const string PROMPT_SELECT_FOOTWAY_POINT = "\nSelect back of footway point:";
        private const string PROMPT_SELECT_ROAD_STRING = "\nSelect road string: ";

        /// <summary>
        /// Custom command for Lincolnshire to add 0.177mm to carriageway level for back of footway
        /// </summary>
        [CommandMethod("C_P_BOF_LEVEL_177")]
        public static void AddBackOfFootwayLevel177()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LincolnshireCommands), nameof(AddBackOfFootwayLevel177));
            AddBackOfFootwayLevel(0.177);
        }

        /// <summary>
        /// Custom command for Lincolnshire to add 0.105mm to carriageway level for back of footway
        /// </summary>
        [CommandMethod("C_P_BOF_LEVEL_LEVEL_105")]
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

            var roadString = SelectRoadString(db, ed);
            if (roadString == null) return;

            using var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            var vectorNormal = plane.Normal;

            var point = ed.PromptForPosition(PROMPT_SELECT_FOOTWAY_POINT);

            while (point.HasValue)
            {
                var roadPoint = roadString.GetClosestPointTo(point.Value, vectorNormal, false);
                var roadLevel = Math.Round(roadPoint.Z, 3);
                var footwayLevel = roadLevel + level;

                LevelBlockHelper.NewLevelBlockAtPoint(db, point.Value, footwayLevel);

                point = ed.PromptForPosition(PROMPT_SELECT_FOOTWAY_POINT);
            }
        }

        private static Polyline3d SelectRoadString(Database database, Editor editor)
        {
            var objectId = editor.PromptForEntity(PROMPT_SELECT_ROAD_STRING, typeof(Polyline3d),"Only 3d polylines allowed.", true);

            if (!objectId.HasValue) return null;

            using var trans = database.TransactionManager.StartTransaction();
            return trans.GetObject(objectId.Value, OpenMode.ForRead) as Polyline3d;
        }
    }
}
