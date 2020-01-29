using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
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

            var details = CommandHelper.GetStartEndDetails(ed, trans);
            if (details.IsValid)
            {
                GradientBlockHelper.GenerateBlock(db, details.Start, details.End);
                trans.Commit();
            }
        }
    }
}
