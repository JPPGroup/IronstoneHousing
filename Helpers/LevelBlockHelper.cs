using Autodesk.AutoCAD.DatabaseServices;
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
        public const string LEVEL_BLOCK_NAME = "ProposedLevel";
        public const string LEVEL_ATTRIBUTE_NAME = "LEVEL";
        public const string ROTATE_ATTRIBUTE_NAME = "Rotate";

        public static bool HasLevelBlock(Database database)
        {
            var trans = database.TransactionManager.TopTransaction;
            var bt = (BlockTable)trans.GetObject(database.BlockTableId, OpenMode.ForRead);
            var hasLevelBlock = false;

            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                if (string.Equals(btr.Name, LEVEL_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase))
                {
                    hasLevelBlock = true;
                    break;
                }
            }

            return hasLevelBlock;
        }

        public static LevelBlockDetails GetPromptedBlockDetails(string prompt, Editor ed, Transaction trans)
        {
            var objectId = ed.PromptForEntity(prompt, typeof(BlockReference), Resources.Command_Prompt_RejectBlockReference, true);
            if (!objectId.HasValue) return LevelBlockDetails.CreateEmpty();

            var block = GetBlockReference(objectId.Value, trans);
            var details = new LevelBlockDetails(block);

            if (details.IsValid) return details;

            HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected, Severity.Warning);
            return LevelBlockDetails.CreateEmpty();
        }

        public static LevelBlockDetails NewLevelBlockAtPoint(Database database, LevelBlockArgs arg)
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

                    var blockRef = new BlockReference(arg.Point, blockId)
                    {
                        Layer = ObjectModel.Constants.FOR_REVIEW_LEVEL_LAYER
                    };

                    if (arg.Rotation.HasValue) blockRef.Rotation = arg.Rotation.Value;

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

                                        acAttRef.TextString = $"{arg.Level:0.000}";
                                        blockRef.AttributeCollection.AppendAttribute(acAttRef);
                                        trans.AddNewlyCreatedDBObject(acAttRef, true);
                                    }
                                }
                            }

                        }
                    }

                    if (arg.Rotate.HasValue && blockRef.IsDynamicBlock)
                    {
                        var dynamicProps = blockRef.DynamicBlockReferencePropertyCollection;
                        foreach (DynamicBlockReferenceProperty dynamicProp in dynamicProps)
                        {
                            if (string.Equals(dynamicProp.PropertyName, ROTATE_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                            {
                                dynamicProp.Value = arg.Rotate.Value;
                            }
                        }
                    }


                    database.TransactionManager.QueueForGraphicsFlush();

                    return new LevelBlockDetails(blockRef);
                }
            }

            return LevelBlockDetails.CreateEmpty();
        }

        public static LevelBlockDetails UpdateExistingLevelBlock(BlockReference block, double level)
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
                        return new LevelBlockDetails(block);
                    }
                }
            }

            HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Invalid_Level_Block_Selected, Severity.Warning);
            return LevelBlockDetails.CreateEmpty();
        }

        public static BlockReference GetBlockReference(ObjectId objectId, Transaction transaction)
        {
            var block = transaction.GetObject(objectId, OpenMode.ForRead) as BlockReference;
            return string.Equals(block?.EffectiveName(), LEVEL_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase) ? block : null;
        }
    }

    internal struct LevelBlockArgs
    {
        public LevelBlockArgs(Point3d point, double level, double? rotation = null, double? rotate = null)
        {
            Point = point;
            Level = level;
            Rotation = rotation;
            Rotate = rotate;
        }

        public Point3d Point { get; }
        public double Level { get; }
        public double? Rotation { get; }
        public double? Rotate { get; }
    }

    internal class LevelBlockDetails
    {
        private Point2d? _point2d;
        private double? _level;
        private double? _rotation;
        private double? _rotate;

        public LevelBlockDetails(BlockReference blockReference)
        {
            if (blockReference == null) return;

            BlockReference = blockReference;
            SetProperties();
        }

        public static LevelBlockDetails CreateEmpty() => new LevelBlockDetails(null);

        public BlockReference BlockReference { get; }
        public bool IsValid => GetIsValid();
        public Point2d Point2d => GetPoint2d();
        public Point3d Point3d => GetPoint3d();
        public double Level => GetLevel();
        public double Rotation => GetRotation();
        public double Rotate => GetRotate();

        private void SetProperties()
        {
            _point2d = new Point2d(BlockReference.Position.X, BlockReference.Position.Y);
            _rotation = BlockReference.Rotation;
            SetPropertiesFromAttributeCollection();
            SetPropertiesFromDynamicBlockReferencePropertyCollection();
        }

        private void SetPropertiesFromAttributeCollection()
        {
            var trans = BlockReference.Database.TransactionManager.TopTransaction;

            foreach (ObjectId attObjId in BlockReference.AttributeCollection)
            {
                var attDbObj = trans.GetObject(attObjId, OpenMode.ForRead);
                if (attDbObj is AttributeReference attRef)
                {
                    if (string.Equals(attRef.Tag, LevelBlockHelper.LEVEL_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (double.TryParse(attRef.TextString, out var result))
                        {
                            _level = result;
                        }
                    }
                }
            }
        }

        private void SetPropertiesFromDynamicBlockReferencePropertyCollection()
        {
            if (BlockReference.IsDynamicBlock)
            {
                var dynamicProps = BlockReference.DynamicBlockReferencePropertyCollection;
                foreach (DynamicBlockReferenceProperty dynamicProp in dynamicProps)
                {
                    if (string.Equals(dynamicProp.PropertyName, LevelBlockHelper.ROTATE_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _rotate = Convert.ToDouble(dynamicProp.Value);
                    }
                }
            }
        }

        private bool GetIsValid()
        {
            return _point2d.HasValue && _level.HasValue && _rotation.HasValue;
        }

        private Point2d GetPoint2d()
        {
            return IsValid && _point2d.HasValue
                ? _point2d.Value
                : default;
        }

        private double GetLevel()
        {
            return IsValid && _level.HasValue
                ? _level.Value
                : default;
        }

        private double GetRotate()
        {
            return IsValid && _rotate.HasValue 
                ? _rotate.Value 
                : default;
        }

        private double GetRotation()
        {
            return IsValid && _rotation.HasValue 
                ? _rotation.Value 
                : default;
        }

        private Point3d GetPoint3d()
        {
            return IsValid && _point2d.HasValue && _level.HasValue 
                ? new Point3d(_point2d.Value.X, _point2d.Value.Y, _level.Value) 
                : default;
        }
    }
}
