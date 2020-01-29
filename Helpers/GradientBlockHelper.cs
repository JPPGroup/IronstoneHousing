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
        private const double ARROW_FULL_LENGTH = 2; // Hack to move position based on a known length

        public static void GenerateBlock(Database database, LevelBlockDetails x, LevelBlockDetails y)
        {
            if (!x.IsValid || !y.IsValid) return;

            if (x.Level.Equals(y.Level))
            {
                HousingExtensionApplication.Current.Logger.Entry(Resources.Message_Levels_Are_Equal);
                return;
            }

            Point2d startPoint;
            Point2d endPoint;
            double startLevel;
            double endLevel;

            //Always point downhill
            if (x.Level > y.Level)
            {
                startPoint = x.Point2d;
                startLevel = x.Level;
                endPoint = y.Point2d;
                endLevel = y.Level;
            }
            else
            {
                startPoint = y.Point2d;
                startLevel = y.Level;
                endPoint = x.Point2d;
                endLevel = x.Level;
            }

            var vector = endPoint.GetAsVector() - startPoint.GetAsVector();

            var gradient = 1 / ((startLevel - endLevel) / vector.Length);
            var midPoint = startPoint + vector * 0.5;

            // Hack to move position based on a known length
            var shiftVector = vector.GetNormal() * ARROW_FULL_LENGTH;
            var matrix = Matrix2d.Displacement(shiftVector);
            midPoint.TransformBy(matrix);

            var rotation = vector.Angle;

            NewGradientBlockAtPoint(database, new Point3d(midPoint.X, midPoint.Y, 0), gradient, rotation);

            HousingExtensionApplication.Current.Logger.Entry(string.Format(Resources.Command_Output_GradientLineLength, Math.Round(vector.Length, 3)));
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

            throw new ArgumentException(Resources.Exception_NoGradientBlock);
        }
    }
}
