using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;
using Jpp.Ironstone.Housing.Properties;

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

            var startObjectId = ed.PromptForEntity(Resources.Command_Prompt_SelectStartLevelBlock, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!startObjectId.HasValue) return; //Assume user cancelled

            var startBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectStartLevelBlock, ed, trans);
            if (startBlock == null) return; //Assume user cancelled

            var endBlock = LevelBlockHelper.GetPromptedBlock(Resources.Command_Prompt_SelectEndLevelBlock, ed, trans);
            if (endBlock == null) return; //Assume user cancelled

            GradientBlockHelper.GenerateBlock(db, startBlock, endBlock);

            trans.Commit();
        }


    }
}
