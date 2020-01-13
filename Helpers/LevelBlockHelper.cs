﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Jpp.Ironstone.Core.Autocad;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Core.UI.Autocad;
using Jpp.Ironstone.Housing.Properties;
using System;

namespace Jpp.Ironstone.Housing.Helpers
{
    internal static class LevelBlockHelper
    {
        /*
         * Consider moving constants below to setting, or similar.
         * At the moment assuming that if these names are changed, then there might be other breaking changes.
         */
        private const string LEVEL_BLOCK_NAME = "ProposedLevel"; 
        private const string LEVEL_ATTRIBUTE_NAME = "LEVEL";
        private const string ROTATE_ATTRIBUTE_NAME = "Rotate";

        public static bool HasLevelBlock(Database database)
        {
            var trans = database.TransactionManager.TopTransaction;
            var bt = (BlockTable)trans.GetObject(database.BlockTableId, OpenMode.ForRead);
            var hasLevelBlock = false;

            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord) trans.GetObject(btrId, OpenMode.ForRead);
                if (string.Equals(btr.Name, LEVEL_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase))
                {
                    hasLevelBlock = true;
                    break;
                }
            }

            return hasLevelBlock;
        }

        public static BlockReference GetPromptedBlock(string prompt, Editor ed, Transaction trans)
        {
            var objectId = ed.PromptForEntity(prompt, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!objectId.HasValue) return null;

            var block = GetBlockReference(objectId.Value, trans);
            if (block != null) return block;

            HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected, Severity.Warning);
            return null;
        }

        public static BlockReference GetBlockReference(ObjectId objectId, Transaction transaction)
        {
            var block = transaction.GetObject(objectId, OpenMode.ForRead) as BlockReference;
            return string.Equals(block?.EffectiveName(), LEVEL_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase) ? block : null;
        }

        public static double? GetLevelFromBlock(BlockReference block)
        {
            var trans = block.Database.TransactionManager.TopTransaction;
            double? level = null;

            foreach (ObjectId attObjId in block.AttributeCollection)
            {
                var attDbObj = trans.GetObject(attObjId, OpenMode.ForRead);
                if (attDbObj is AttributeReference attRef)
                {
                    if (string.Equals(attRef.Tag, LEVEL_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (double.TryParse(attRef.TextString, out var result))
                        {
                            level = result;
                        }
                    }
                }
            }
            if (!level.HasValue) HousingExtensionApplication.Current.Logger.Entry(Resources.Message_No_Level_Set_On_Block, Severity.Warning);
            return level;
        }

        public static BlockReference UpdateExistingLevelBlock(BlockReference block, double level)
        {
            //Update level value, but not adjust any other properties.
            var trans = block.Database.TransactionManager.TopTransaction;

            foreach (ObjectId attObjId in block.AttributeCollection)
            {
                var attDbObj = trans.GetObject(attObjId, OpenMode.ForRead);
                if (attDbObj is AttributeReference attRef)
                {
                    if (string.Equals(attRef.Tag, LEVEL_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                    {
                        attRef.UpgradeOpen();

                        attRef.TextString = $"{level:0.000}";
                        return block;
                    }
                }
            }

            HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected, Severity.Warning);
            return null;
        }

        public static BlockReference NewLevelBlockAtPoint(Database database, LevelBlockProps props)
        {
            var trans = database.TransactionManager.TopTransaction;
            var bt = (BlockTable)trans.GetObject(database.BlockTableId, OpenMode.ForRead);
            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                if (string.Equals(btr.Name, LEVEL_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase))
                {
                    var blockId = btr.ObjectId;
                    var modelSpaceRecord = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    var blockRef = new BlockReference(props.Point, blockId)
                    {
                        Layer = ObjectModel.Constants.FOR_REVIEW_LEVEL_LAYER
                    };
                    
                    if (props.Rotation.HasValue) blockRef.Rotation = props.Rotation.Value;

                    modelSpaceRecord.AppendEntity(blockRef);
                    trans.AddNewlyCreatedDBObject(blockRef, true);

                    if (btr.HasAttributeDefinitions)
                    {
                        foreach (var objId in btr)
                        {
                            var dbObj = trans.GetObject(objId, OpenMode.ForRead);
                            if (dbObj is AttributeDefinition acAtt)
                            {
                                if (acAtt.Constant) continue;

                                using (var acAttRef = new AttributeReference())
                                {
                                    if (string.Equals(acAtt.Tag, LEVEL_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        acAttRef.SetAttributeFromBlock(acAtt, blockRef.BlockTransform);
                                        acAttRef.Position = acAtt.Position.TransformBy(blockRef.BlockTransform);

                                        acAttRef.TextString = $"{props.Level:0.000}";
                                        blockRef.AttributeCollection.AppendAttribute(acAttRef);
                                        trans.AddNewlyCreatedDBObject(acAttRef, true);
                                    }
                                }
                            }
                                    
                        }
                    }

                    if (props.RotateGrip.HasValue && blockRef.IsDynamicBlock)
                    {
                        var dynamicProps = blockRef.DynamicBlockReferencePropertyCollection;
                        foreach (DynamicBlockReferenceProperty dynamicProp in dynamicProps)
                        {
                            if (string.Equals(dynamicProp.PropertyName, ROTATE_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                            {
                                dynamicProp.Value = props.RotateGrip.Value;
                            }
                        }
                    }


                    database.TransactionManager.QueueForGraphicsFlush();

                    return blockRef;
                }
            }

            return null;
        }

        internal static double? GetRotateFromBlock(BlockReference block)
        {
            double? rotate = null;

            if (block.IsDynamicBlock)
            {
                var dynamicProps = block.DynamicBlockReferencePropertyCollection;
                foreach (DynamicBlockReferenceProperty dynamicProp in dynamicProps)
                {
                    if (string.Equals(dynamicProp.PropertyName, ROTATE_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                    {
                        rotate = Convert.ToDouble(dynamicProp.Value);
                    }
                }
            }

            if (!rotate.HasValue) HousingExtensionApplication.Current.Logger.Entry(Resources.Message_No_Rotate_Set_On_Block, Severity.Warning);
            return rotate;
        }
    }

    internal struct LevelBlockProps
    {
        public LevelBlockProps(Point3d point, double level, double? rotation = null, double? rotateGrip = null)
        {
            Point = point;
            Level = level;
            Rotation = rotation;
            RotateGrip = rotateGrip;
        }

        public Point3d Point { get; }
        public double Level { get; }
        public double? Rotation { get; }
        public double? RotateGrip { get; }
    }
}
