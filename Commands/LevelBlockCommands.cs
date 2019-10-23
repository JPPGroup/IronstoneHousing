﻿using Autodesk.AutoCAD.ApplicationServices.Core;
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
        private static double _level; //cache of previous level
        private static double _gradient; //cache of previous gradient

        /// <summary>
        /// Custom command to calculate a level from a given point
        /// </summary>
        [CommandMethod("C_LevelBlock_FromPoint")]
        public static void CalculateLevelFromPoint()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromPoint));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var startPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectStartPoint);
            if (!startPoint.HasValue) return;

            var startLevel = ed.PromptForDouble(Resources.Command_Prompt_EnterLevel, _level);
            if (!startLevel.HasValue) return;

            var gradient = ed.PromptForDouble(Resources.Command_Prompt_EnterGradient, _gradient);
            if (!gradient.HasValue) return;

            var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
            if (!endPoint.HasValue) return;

            GenerateBlock(startPoint.Value, endPoint.Value, startLevel.Value, gradient.Value, db);

            _level = startLevel.Value;
            _gradient = gradient.Value;
            
            trans.Commit();
        }

        /// <summary>
        /// Custom command to calculate a level from a given block
        /// </summary>
        [CommandMethod("C_LevelBlock_FromBlock")]
        public static void CalculateLevelFromLevelBlock()
        {
            HousingExtensionApplication.Current.Logger.LogCommand(typeof(LevelBlockCommands), nameof(CalculateLevelFromLevelBlock));

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using var trans = db.TransactionManager.StartTransaction();

            if (!LevelBlockHelper.HasLevelBlock(db)) throw new ArgumentException(Resources.Exception_NoLevelBlock);

            var startObjectId = ed.PromptForEntity(Resources.Command_Prompt_SelectStartLevelBlock, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!startObjectId.HasValue) return;

            var block = (BlockReference)trans.GetObject(startObjectId.Value, OpenMode.ForRead);

            if (!LevelBlockHelper.IsLevelBlockReference(block)) return;

            var startLevel = LevelBlockHelper.GetLevelFromBlock(block);
            if (!startLevel.HasValue) return;

            var startPoint = block.Position;

            var gradient = ed.PromptForDouble(Resources.Command_Prompt_EnterGradient, _gradient);
            if (!gradient.HasValue) return;

            var endPoint = ed.PromptForPosition(Resources.Command_Prompt_SelectEndPoint);
            if (!endPoint.HasValue) return;

            GenerateBlock(startPoint, endPoint.Value, startLevel.Value, gradient.Value, db);

            _gradient = gradient.Value;

            trans.Commit();
        }

        private static void GenerateBlock(Point3d startPoint, Point3d endPoint, double startLevel, double gradient, Database db)
        {
            using (var line = new Line(startPoint, endPoint))
            {
                var endLevel = startLevel + line.Length * (1 / gradient);
                LevelBlockHelper.NewLevelBlockAtPoint(db, endPoint, endLevel);
            }
        }
    }
}
