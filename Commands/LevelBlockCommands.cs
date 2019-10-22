using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Helpers;

namespace Jpp.Ironstone.Housing.Commands
{
    /// <summary>
    /// Commands to add level blocks
    /// </summary>
    public static class LevelBlockCommands
    {
        private const string PROMPT_SELECT_START_POINT = "\nSelect start point: ";
        private const string PROMPT_SELECT_END_POINT = "\nSelect end point: ";
        private const string PROMPT_ENTER_LEVEL = "\nEnter a level: ";
        private const string PROMPT_ENTER_GRADIENT = "\nEnter a gradient(1 in) : ";

        private static double _level; //cache of previous level
        private static double _gradient; //cache of previous gradient

        /// <summary>
        /// Custom command to calculate a level from a given given point
        /// </summary>
        [CommandMethod("C_P_LEVEL_FROM_POINT")]
        public static void CalculateLevelFromPoint()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromPoint));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var startPoint = ed.PromptForPosition(PROMPT_SELECT_START_POINT);
            if (!startPoint.HasValue) return;

            var startLevel = ed.PromptForDouble(PROMPT_ENTER_LEVEL, _level);
            if (!startLevel.HasValue) return;

            var gradient = ed.PromptForDouble(PROMPT_ENTER_GRADIENT, _gradient);
            if (!gradient.HasValue) return;

            var endPoint = ed.PromptForPosition(PROMPT_SELECT_END_POINT);
            if (!endPoint.HasValue) return;

            using var line = new Line(startPoint.Value, endPoint.Value);
            var endLevel = startLevel.Value + line.Length * (1 / gradient.Value);

            LevelBlockHelper.NewLevelBlockAtPoint(db, endPoint.Value, endLevel);

            _level = startLevel.Value;
            _gradient = gradient.Value;
        }
    }
}
