using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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

        public static BlockReference GetBlockReference(ObjectId objectId, Transaction transaction)
        {
            var block = transaction.GetObject(objectId, OpenMode.ForRead) as BlockReference;
            return string.Equals(block?.Name, LEVEL_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase) ? block : null;
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

            return level;
        }

        public static BlockReference NewLevelBlockAtPoint(Database database, Point3d point, double level)
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

                    var blockRef = new BlockReference(point, blockId)
                    {
                        ScaleFactors = new Scale3d(0.2, 0.2, 0.2) //Block is annotative, scaled to match as advise by TL.
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

                    return blockRef;
                }
            }

            return null;
        }
    }
}
