using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;
using Jpp.Ironstone.Housing.Properties;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands to add level blocks
    /// </summary>
    public static class LevelBlockCommands
    {
        private static double _level; //cache of previous level
        private static double _gradient; //cache of previous gradient

        /// <summary>
        /// Custom command to calculate a level from a given given point
        /// </summary>
        [CommandMethod("C_LevelBlock_FromPoint")]
        public static void CalculateLevelFromPoint()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromPoint));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var startPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectStartPoint);
            if (!startPoint.HasValue) return;

            var startLevel = ed.PromptForDouble(Resources.Command_Prompt_EnterLevel, _level);
            if (!startLevel.HasValue) return;

            var gradient = ed.PromptForDouble(Resources.Command_Prompt_EnterGradient, _gradient);
            if (!gradient.HasValue) return;

            var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
            if (!endPoint.HasValue) return;

            using var line = new Line(startPoint.Value, endPoint.Value);
            var endLevel = startLevel.Value + line.Length * (1 / gradient.Value);

            LevelBlockHelper.NewLevelBlockAtPoint(db, endPoint.Value, endLevel);

            _level = startLevel.Value;
            _gradient = gradient.Value;
        }
    }
}
