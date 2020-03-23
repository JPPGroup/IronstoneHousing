using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Colors;
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
    /// Commands to add level blocks
    /// </summary>
    public static class LevelBlockCommands
    {
        private static readonly string[] GradientKeywords = { "No", "Yes" };
        private static readonly string[] BlockKeywords = { "New", "Existing" };

        private static double _level; //cache of previous level
        private static double _gradient; //cache of previous gradient
        private static double _invert; //cache of previous invert
        private static string _includeGradient = GradientKeywords[0]; //cache of previous gradient selection

        /// <summary>
        /// Custom command to calculate a level at a given point
        /// </summary>
        [CommandMethod("C_LevelBlock_AtPoint")]
        public static void CalculateLevelAtPoint()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelAtPoint));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var point = ed.PromptForPosition(Resources.Command_Prompt_SelectPoint);
            if (!point.HasValue) return; //Assume user cancelled

            var args = new LevelBlockArgs(point.Value, point.Value.Z);

            LevelBlockHelper.NewLevelBlockAtPoint(db, args);

            trans.Commit();
        }


        /// <summary>
        /// Custom command to calculate a level at given point between two existing levels
        /// </summary>
        [CommandMethod("C_LevelBlock_BetweenLevels")]
        public static void CalculateLevelBetweenLevels()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelBetweenLevels));
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var details = CommandHelper.GetStartEndDetails(ed, trans);
            if (!details.IsValid) return;

            var s = new Point3d(details.Start.Point2d.X, details.Start.Point2d.Y, 0);
            var e = new Point3d(details.End.Point2d.X, details.End.Point2d.Y, 0);

            Point3d? midPoint;
            
            using var line = new Line(s, e) { Color = Color.FromRgb(0, 255, 0) };
            { 
                var acBlkTbl = (BlockTable) trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                var acBlkTblRec = (BlockTableRecord) trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                acBlkTblRec.AppendEntity(line);
                trans.AddNewlyCreatedDBObject(line, true);

                db.TransactionManager.QueueForGraphicsFlush(); // TODO: Review graphics flush in core console.

                midPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectMidPoint);
                while (midPoint.HasValue)
                {
                    var m = new Point3d(midPoint.Value.X, midPoint.Value.Y, 0);
                    if (line.GetGeCurve().IsOn(m)) break;

                    midPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectMidPoint);
                }

                line.Erase();
                db.TransactionManager.QueueForGraphicsFlush(); // TODO: Review graphics flush in core console.
            }

            if (!midPoint.HasValue)
            {
                return; //Assume user cancelled
            }

            var gradient = 1 / ((details.End.Level - details.Start.Level) / line.Length);
            var midLevel = CalculateLevel(details.Start.Point3d, midPoint.Value, details.Start.Level, gradient);

            var args = new LevelBlockArgs(midPoint.Value, midLevel, details.Start.Rotation, details.Start.Rotate);

            LevelBlockHelper.NewLevelBlockAtPoint(db, args);

            trans.Commit();
        }

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

            var endBlock = GenerateOrUpdateBlock(ed, db, new GenerateArgs(startPoint.Value, startLevel.Value, gradient.Value));
            if (endBlock == null) return; //Assume user cancelled

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

            var start = LevelBlockHelper.GetPromptedBlockDetails(Resources.Command_Prompt_SelectStartLevelBlock, ed, trans);
            if (!start.IsValid) return;

            var gradient = ed.PromptForDouble(Resources.Command_Prompt_EnterGradient, _gradient);
            if (!gradient.HasValue) return; //Assume user cancelled

            var endBlock = GenerateOrUpdateBlock(ed, db, new GenerateArgs(start.Point3d, start.Level, gradient.Value, start.Rotation, start.Rotate));
            if (endBlock == null) return; //Assume user cancelled

            if (ShouldIncludeGradient(ed)) GradientBlockHelper.GenerateBlock(db, start, endBlock);

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

            var start = LevelBlockHelper.GetPromptedBlockDetails(Resources.Command_Prompt_SelectStartLevelBlock, ed, trans);
            if(!start.IsValid) return;
            
            var invert = ed.PromptForDouble(Resources.Command_Prompt_EnterInvert, _invert);
            if (!invert.HasValue) return; //Assume user cancelled

            var endLevel = start.Level - invert.Value;

            var end = GenerateOrUpdateBlock(ed, db, new GenerateArgs(endLevel, start.Rotation, start.Rotate));
            if (!end.IsValid) return; //Assume user cancelled

            if (ShouldIncludeGradient(ed)) GradientBlockHelper.GenerateBlock(db, start, end);

            _invert = invert.Value;

            trans.Commit();
        }

        private static LevelBlockDetails GenerateOrUpdateBlock(Editor ed, Database db, GenerateArgs args)
        {
            if (IsNewBlock(ed))
            {
                var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
                if (!endPoint.HasValue) return LevelBlockDetails.CreateEmpty();

                var levelArgs = CreateNewBlockLevelBlockArgs(args, endPoint.Value);
                
                return LevelBlockHelper.NewLevelBlockAtPoint(db, levelArgs);
            }

            var trans = db.TransactionManager.TopTransaction;
            var existing = LevelBlockHelper.GetPromptedBlockDetails(Resources.Command_Prompt_SelectExistingLevelBlock, ed, trans);
            if (!existing.IsValid) return LevelBlockDetails.CreateEmpty();

            if (args.HasLevel)
            {
                return LevelBlockHelper.UpdateExistingLevelBlock(existing.BlockReference, args.BlockLevel);
            }

            var endLevel = CalculateLevel(args.StartPoint, existing.Point3d, args.StartLevel, args.Gradient);
            return LevelBlockHelper.UpdateExistingLevelBlock(existing.BlockReference, endLevel);
        }

        private static LevelBlockArgs CreateNewBlockLevelBlockArgs(GenerateArgs args, Point3d endPoint)
        {
            if (args.HasLevel)
            {
                return new LevelBlockArgs(endPoint, args.BlockLevel, args.Rotation, args.Rotate);
            }

            var endLevel = CalculateLevel(args.StartPoint, endPoint, args.StartLevel, args.Gradient);
            return new LevelBlockArgs(endPoint, endLevel, args.Rotation, args.Rotate);
        }

        private static bool ShouldIncludeGradient(Editor ed)
        {
            var result = ed.PromptForKeywords(Resources.Command_Prompt_IncludeGradientBlock, GradientKeywords, _includeGradient);
            _includeGradient = result;
            return result == GradientKeywords[1];
        }

        private static bool IsNewBlock(Editor ed)
        {
            var result = ed.PromptForKeywords(Resources.Command_Prompt_NewOrExistingLevelBlock, BlockKeywords);
            return result == BlockKeywords[0];
        }

        private static double CalculateLevel(Point3d startPoint, Point3d endPoint, double startLevel, double gradient)
        {
            var s = new Point3d(startPoint.X, startPoint.Y, 0); 
            var e = new Point3d(endPoint.X, endPoint.Y, 0);
            using var line = new Line(s, e);
            
            return startLevel + line.Length * (1 / gradient);
        }
    }

    internal struct GenerateArgs
    {
        private readonly double? _blockLevel;
        private readonly Point3d? _startPoint;
        private readonly double? _startLevel;
        private readonly double? _gradient;

        public bool HasLevel { get; }
        public double BlockLevel => HasLevel && _blockLevel.HasValue ? _blockLevel.Value : default;
        public Point3d StartPoint => !HasLevel && _startPoint.HasValue ? _startPoint.Value : default;
        public double StartLevel => !HasLevel && _startLevel.HasValue ? _startLevel.Value : default;
        public double Gradient => !HasLevel && _gradient.HasValue ? _gradient.Value : default;
        public double? Rotation { get; }
        public double? Rotate { get; }

        public GenerateArgs(Point3d startPoint, double startLevel, double gradient, double? rotation = null, double? rotate = null)
        {
            Rotation = rotation;
            Rotate = rotate;

            _startPoint = startPoint;
            _startLevel = startLevel;
            _gradient = gradient;

            _blockLevel = null;

            HasLevel = false;
        }

        public GenerateArgs(double blockLevel, double? rotation = null, double? rotate = null)
        {
            Rotation = rotation;
            Rotate = rotate;

            _blockLevel = blockLevel;

            _startPoint = null;
            _startLevel = null;
            _gradient = null;

            HasLevel = true;
        }
    }
}
