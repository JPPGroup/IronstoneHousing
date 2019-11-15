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
    /// Commands to add level blocks
    /// </summary>
    public static class LevelBlockCommands
    {
        private static readonly string[] GradientKeywords = { "No", "Yes" };

        private static double _level; //cache of previous level
        private static double _gradient; //cache of previous gradient
        private static double _invert; //cache of previous invert
        private static string _includeGradient = GradientKeywords[0]; //cache of previous gradient selection
        

        /// <summary>
        /// Custom command to calculate a level from a given point at gradient
        /// </summary>
        [CommandMethod("C_LevelBlock_FromPointAtGradient")]
        public static void CalculateLevelFromPointAtGradient()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromPointAtGradient));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var startPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectStartPoint);
            if (!startPoint.HasValue) return; //Assume user cancelled

            var startLevel = ed.PromptForDouble(Resources.Command_Prompt_EnterLevel, _level);
            if (!startLevel.HasValue) return; //Assume user cancelled

            var gradient = ed.PromptForDouble(Resources.Command_Prompt_EnterGradient, _gradient);
            if (!gradient.HasValue) return; //Assume user cancelled

            var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
            if (!endPoint.HasValue) return; //Assume user cancelled

            GenerateBlock(startPoint.Value, endPoint.Value, startLevel.Value, gradient.Value, db);

            _level = startLevel.Value;
            _gradient = gradient.Value;
            
            trans.Commit();
        }

        /// <summary>
        /// Custom command to calculate a level from a given block at gradient
        /// </summary>
        [CommandMethod("C_LevelBlock_FromBlockAtGradient")]
        public static void CalculateLevelFromLevelBlockAtGradient()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromLevelBlockAtGradient));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var startObjectId = ed.PromptForEntity(Resources.Command_Prompt_SelectStartLevelBlock, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!startObjectId.HasValue) return; //Assume user cancelled

            var startBlock = LevelBlockHelper.GetBlockReference(startObjectId.Value, trans);
            if (startBlock == null) 
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected);
                return;
            }

            var startLevel = LevelBlockHelper.GetLevelFromBlock(startBlock);
            if (!startLevel.HasValue) 
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_No_Level_Set_On_Block);
                return;
            }

            var startPoint = startBlock.Position;

            var gradient = ed.PromptForDouble(Resources.Command_Prompt_EnterGradient, _gradient);
            if (!gradient.HasValue) return; //Assume user cancelled

            var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
            if (!endPoint.HasValue) return; //Assume user cancelled

            var endBlock = GenerateBlock(startPoint, endPoint.Value, startLevel.Value, gradient.Value, db);

            if (IncludeGradient()) GradientBlockCommands.GenerateGradientBlock(db, startBlock, endBlock);

            _gradient = gradient.Value;

            trans.Commit();
        }

        /// <summary>
        /// Custom command to calculate a level from a given block with a set invert
        /// </summary>
        [CommandMethod("C_LevelBlock_FromBlockWithInvert")]
        public static void CalculateLevelFromLevelBlockWithInvert()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromLevelBlockWithInvert));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var startObjectId = ed.PromptForEntity(Resources.Command_Prompt_SelectStartLevelBlock, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!startObjectId.HasValue) return; //Assume user cancelled

            var startBlock = LevelBlockHelper.GetBlockReference(startObjectId.Value, trans);
            if (startBlock == null) 
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected);
                return;
            }

            var startLevel = LevelBlockHelper.GetLevelFromBlock(startBlock);
            if (!startLevel.HasValue)
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_No_Level_Set_On_Block);
                return;
            }

            var invert = ed.PromptForDouble(Resources.Command_Prompt_EnterInvert, _invert);
            if (!invert.HasValue) return; //Assume user cancelled

            var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
            if (!endPoint.HasValue) return; //Assume user cancelled

            var endLevel = startLevel.Value - (invert.Value / 1000);

            var endBlock = LevelBlockHelper.NewLevelBlockAtPoint(db, endPoint.Value, endLevel);

            if (IncludeGradient()) GradientBlockCommands.GenerateGradientBlock(db, startBlock, endBlock);

            _invert = invert.Value;

            trans.Commit();
        }


        private static BlockReference GenerateBlock(Point3d startPoint, Point3d endPoint, double startLevel, double gradient, Database db)
        {
            //TODO: Convert to 2d points
            var s = new Point3d(startPoint.X, startPoint.Y, 0); //Remove z for line length
            var e = new Point3d(endPoint.X, endPoint.Y, 0); //Remove z for line length
            using (var line = new Line(s, e))
            {
                var endLevel = startLevel + line.Length * (1 / gradient);
                return LevelBlockHelper.NewLevelBlockAtPoint(db, endPoint, endLevel);
            }
        }

        private static bool IncludeGradient()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            var result = ed.PromptForKeywords(Resources.Command_Prompt_IncludeGradientBlock, GradientKeywords, _includeGradient);
            
            _includeGradient = result;

            return result == GradientKeywords[1];
        }
    }
}
