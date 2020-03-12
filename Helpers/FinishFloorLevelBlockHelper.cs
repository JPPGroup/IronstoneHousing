using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace Jpp.Ironstone.Housing.Helpers
{
    internal static class FinishFloorLevelBlockHelper
    {
        public const string FLL_BLOCK_NAME = "FFL Block";
        public const string LEVEL_ATTRIBUTE_NAME = "FFL";

        public static void NewFinishFloorLevelAtPoint(Database database, Point3d position, double level)
        {
            var trans = database.TransactionManager.TopTransaction;
            var bt = (BlockTable)trans.GetObject(database.BlockTableId, OpenMode.ForRead);
            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                if (string.Equals(btr.Name, FLL_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase))
                {
                    var blockId = btr.ObjectId;
                    var modelSpaceRecord = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    var blockRef = new BlockReference(position, blockId);
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

                                        acAttRef.TextString = $"{level:0.000}";
                                        blockRef.AttributeCollection.AppendAttribute(acAttRef);
                                        trans.AddNewlyCreatedDBObject(acAttRef, true);
                                    }
                                }
                            }

                        }
                    }

                    database.TransactionManager.QueueForGraphicsFlush();
                }
            }
        }
    }
}
