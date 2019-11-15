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
            if (!startObjectId.HasValue) return; //Assume user cancelled

            var startBlock = LevelBlockHelper.GetBlockReference(startObjectId.Value, trans);
            if (startBlock == null) 
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected);
                return;
            }

            var endObjectId = ed.PromptForEntity(Resources.Command_Prompt_SelectEndLevelBlock, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!endObjectId.HasValue) return; //Assume user cancelled

            var endBlock = LevelBlockHelper.GetBlockReference(endObjectId.Value, trans);
            if (endBlock == null) 
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected);
                return;
            }

            GenerateGradientBlock(db, startBlock, endBlock);

            trans.Commit();
        }

        public static void GenerateGradientBlock(Database database, BlockReference x, BlockReference y)
        {
            var xLevel = LevelBlockHelper.GetLevelFromBlock(x);
            var yLevel = LevelBlockHelper.GetLevelFromBlock(y);

            if (!xLevel.HasValue || !yLevel.HasValue)
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_No_Level_Set_On_Block);
                return;
            }

            if (xLevel.Value.Equals(yLevel.Value))
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Levels_Are_Equal);
                return;
            }

            Point2d startPoint;
            Point2d endPoint;
            double startLevel;
            double endLevel;

            var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);

            //Always point downhill
            if (xLevel.Value > yLevel.Value)
            {
                startPoint = x.Position.Convert2d(plane);
                startLevel = xLevel.Value;
                endPoint = y.Position.Convert2d(plane);
                endLevel = yLevel.Value;
            }
            else
            {
                startPoint = y.Position.Convert2d(plane);
                startLevel = yLevel.Value;
                endPoint = x.Position.Convert2d(plane);
                endLevel = xLevel.Value;
            }
            
            var vector = endPoint.GetAsVector() - startPoint.GetAsVector();
            var gradient = 1 / ((startLevel - endLevel) / vector.Length);
            var midPoint = startPoint + vector * 0.5;
            var rotation = vector.Angle;

            GradientBlockHelper.NewGradientBlockAtPoint(database, new Point3d(plane, midPoint), gradient, rotation);
        }
    }
}
