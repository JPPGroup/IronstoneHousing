using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace Jpp.Ironstone.Housing.Helpers
{
    internal class LevelBlockHelper
    {
        private const string LEVEL_BLOCK_NAME = "ProposedLevel";
        private const string LEVEL_ATTRIBUTE_NAME = "LEVEL";

        public static void NewLevelBlockAtPoint(Database database, Point3d point, double level)
        {
            using var trans = database.TransactionManager.StartTransaction();

            var bt = (BlockTable)trans.GetObject(database.BlockTableId, OpenMode.ForRead);
            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)trans.GetObject(btrId, OpenMode.ForRead);
                if (!string.Equals(btr.Name, LEVEL_BLOCK_NAME, StringComparison.CurrentCultureIgnoreCase)) continue;

                var blockId = btr.ObjectId;
                var modelSpaceRecord = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var blockRef = new BlockReference(point, blockId)
                {
                    ScaleFactors = new Scale3d(0.2, 0.2, 0.2)
                };

                modelSpaceRecord.AppendEntity(blockRef);
                trans.AddNewlyCreatedDBObject(blockRef, true);

                if (btr.HasAttributeDefinitions)
                {
                    foreach (var objId in btr)
                    {
                        var dbObj = trans.GetObject(objId, OpenMode.ForRead);

                        if (!(dbObj is AttributeDefinition acAtt)) continue;
                        if (acAtt.Constant) continue;

                        using var acAttRef = new AttributeReference();
                        if (!string.Equals(acAtt.Tag, LEVEL_ATTRIBUTE_NAME, StringComparison.CurrentCultureIgnoreCase)) continue;

                        acAttRef.SetAttributeFromBlock(acAtt, blockRef.BlockTransform);
                        acAttRef.Position = acAtt.Position.TransformBy(blockRef.BlockTransform);

                        acAttRef.TextString = $"{level:0.000}";
                        blockRef.AttributeCollection.AppendAttribute(acAttRef);
                        trans.AddNewlyCreatedDBObject(acAttRef, true);
                    }
                }

                database.TransactionManager.QueueForGraphicsFlush();
                trans.Commit();

                return;
            }

            trans.Abort();
        }
    }
}
