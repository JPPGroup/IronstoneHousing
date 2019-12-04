using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Jpp.Ironstone.Housing.Properties;
using System;

namespace Jpp.Ironstone.Housing.Helpers
{
    internal static class GradientBlockHelper
    {
        /*
         * Consider moving constants below to setting, or similar.
         * At the moment assuming that if these names are changed, then there might be other breaking changes.
         */
        private const string GRADIENT_BLOCK_NAME = "ProposedGradient";
        private const string GRADIENT_ATTRIBUTE_NAME = "GRADIENT";
        private const string FLIP_ATTRIBUTE_NAME = "Flip state1";

        public static void GenerateBlock(Database database, BlockReference x, BlockReference y)
        {
            if (!HasGradientBlock(database)) throw new ArgumentException(Resources.Exception_NoGradientBlock);

            var xLevel = LevelBlockHelper.GetLevelFromBlock(x);
            var yLevel = LevelBlockHelper.GetLevelFromBlock(y);

            if (!xLevel.HasValue || !yLevel.HasValue) return;

            if (xLevel.Value.Equals(yLevel.Value))
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Levels_Are_Equal);
                return;
            }

            Point2d startPoint;
            Point2d endPoint;
            double startLevel;
            double endLevel;

            var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);

            //Always point downhill
            if (xLevel.Value > yLevel.Value)
            {
                startPoint = x.Position.Convert2d(plane);
                startLevel = xLevel.Value;
                endPoint = y.Position.Convert2d(plane);
                endLevel = yLevel.Value;
            }
            else
            {
                startPoint = y.Position.Convert2d(plane);
                startLevel = yLevel.Value;
                endPoint = x.Position.Convert2d(plane);
                endLevel = xLevel.Value;
            }

            var vector = endPoint.GetAsVector() - startPoint.GetAsVector();
            var gradient = 1 / ((startLevel - endLevel) / vector.Length);
            var midPoint = startPoint + vector * 0.5;
            var rotation = vector.Angle;

            NewGradientBlockAtPoint(database, new Point3d(plane, midPoint), gradient, rotation);
        }

        private static bool HasGradientBlock(Database database)
        {
            using var trans = database.TransactionManager.TopTransaction;
            var bt = (BlockTable)trans.GetObject(database.BlockTableId, OpenMode.ForRead);
            var hasBlock = false;

            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                if (string.Equals(btr.Name, GRADIENT_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase))
                {
                    hasBlock = true;
                    break;
                }
            }

            return hasBlock;
        }

        private static void NewGradientBlockAtPoint(Database database, Point3d point, double gradient, double rotation)
        {
            var flip = rotation > Math.PI / 2 && rotation < Math.PI * 1.5;
            using var trans = database.TransactionManager.TopTransaction;
            var bt = (BlockTable)trans.GetObject(database.BlockTableId, OpenMode.ForRead);
            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                if (string.Equals(btr.Name, GRADIENT_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase))
                {
                    var blockId = btr.ObjectId;
                    var modelSpaceRecord = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    var blockRef = new BlockReference(point, blockId)
                    {
                        ScaleFactors = new Scale3d(0.2, 0.2, 0.2),
                        Rotation = flip ? rotation + Math.PI : rotation,
                        Layer = ObjectModel.Constants.FOR_REVIEW_GRADIENT_LAYER
                    };

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

                                using var acAttRef = new AttributeReference();
                                if (string.Equals(acAtt.Tag, GRADIENT_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    acAttRef.SetAttributeFromBlock(acAtt, blockRef.BlockTransform);
                                    acAttRef.Position = acAtt.Position.TransformBy(blockRef.BlockTransform);

                                    acAttRef.TextString = $"{gradient:0}";
                                    blockRef.AttributeCollection.AppendAttribute(acAttRef);
                                    trans.AddNewlyCreatedDBObject(acAttRef, true);
                                }
                            }
                        }
                    }

                    if (blockRef.IsDynamicBlock)
                    {
                        var props = blockRef.DynamicBlockReferencePropertyCollection;
                        foreach (DynamicBlockReferenceProperty prop in props)
                        {
                            if(string.Equals(prop.PropertyName,FLIP_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase))
                            {
                                prop.Value = Convert.ToInt16(flip);
                            }
                        }
                    }

                    database.TransactionManager.QueueForGraphicsFlush();

                    return;
                }
            }
        }
    }
}
