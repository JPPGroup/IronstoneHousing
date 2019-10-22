using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;
using Jpp.Ironstone.Housing.Properties;
using System;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands to add gradient blocks
    /// </summary>
    public static class GradientBlockCommands
    {
        /// <summary>
        /// Custom command to calculate and insert a gradient between two levels
        /// </summary>
        [CommandMethod("C_GradientBlock_BetweenLevels")]
        public static void CalculateGradientBetweenLevels()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(GradientBlockCommands), nameof(CalculateGradientBetweenLevels));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            if (!GradientBlockHelper.HasGradientBlock(db)) throw new ArgumentException(Resources.Exception_NoGradientBlock);

            var startObjectId = ed.PromptForEntity(Resources.Command_Prompt_SelectStartLevelBlock, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!startObjectId.HasValue) return;

            var startBlock = (BlockReference)trans.GetObject(startObjectId.Value, OpenMode.ForRead);
            if (!LevelBlockHelper.IsLevelBlockReference(startBlock)) return;

            var endObjectId = ed.PromptForEntity(Resources.Command_Prompt_SelectEndLevelBlock, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!endObjectId.HasValue) return;

            var endBlock = (BlockReference)trans.GetObject(endObjectId.Value, OpenMode.ForRead);
            if (!LevelBlockHelper.IsLevelBlockReference(endBlock)) return;

            GenerateGradientBlock(db, startBlock, endBlock);

            trans.Commit();
        }

        public static void GenerateGradientBlock(Database database, BlockReference x, BlockReference y)
        {
            var xLevel = LevelBlockHelper.GetLevelFromBlock(x);
            var yLevel = LevelBlockHelper.GetLevelFromBlock(y);

            if(!xLevel.HasValue || !yLevel.HasValue) return;
            if (xLevel.Value.Equals(yLevel.Value)) return;

            Point3d startPoint;
            Point3d endPoint;
            double startLevel;
            double endLevel;

            //Always point downhill
            if (xLevel.Value > yLevel.Value)
            {
                startPoint = x.Position;
                startLevel = xLevel.Value;
                endPoint = y.Position;
                endLevel = yLevel.Value;
            }
            else
            {
                startPoint = y.Position;
                startLevel = yLevel.Value;
                endPoint = x.Position;
                endLevel = xLevel.Value;
            }
            
            var vector = endPoint.GetAsVector() - startPoint.GetAsVector();
            var gradient = 1 / ((startLevel - endLevel) / vector.Length);
            var midPoint = startPoint + vector * 0.5;
            var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            var rotation = vector.AngleOnPlane(plane);


            GradientBlockHelper.NewGradientBlockAtPoint(database, midPoint, gradient, rotation);
        }
    }
}
