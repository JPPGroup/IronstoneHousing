using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Jpp.Ironstone.Housing.Properties;

namespace Jpp.Ironstone.Housing.Helpers
{
    internal static class CommandHelper
    {
        public static StartEndDetails GetStartEndDetails(Editor ed, Transaction trans)
        {
            var start = LevelBlockHelper.GetPromptedBlockDetails(Resources.Command_Prompt_SelectStartLevelBlock, ed, trans);
            var end = LevelBlockHelper.GetPromptedBlockDetails(Resources.Command_Prompt_SelectEndLevelBlock, ed, trans);

            return new StartEndDetails
            {
                Start = start,
                End = end
            };
        }
    }

    internal struct StartEndDetails
    {
        public bool IsValid => Start.IsValid && End.IsValid;
        public LevelBlockDetails Start { get; set; }
        public LevelBlockDetails End { get; set; }
    }
}
