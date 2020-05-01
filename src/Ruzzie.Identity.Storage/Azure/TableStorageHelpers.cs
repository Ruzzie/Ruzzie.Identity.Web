﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;
using Ruzzie.Azure.Storage;

namespace Ruzzie.Identity.Storage.Azure
{
    public static class TableStorageHelpers
    {
        private const string PartitionKeyField = "PartitionKey";
        private const string RowKeyField = "RowKey";
        private const string OpEquals = "eq";
        private const string OpAnd = "and";

        public static bool RowExistsForPartitionKeyAndRowKey(this CloudTablePool tablePool,
            string partitionKey,
            string rowKey)
        {
            return tablePool.Pool.RowExistsForPartitionKeyAndRowKey(partitionKey, rowKey);
        }

        public static string CreatePointQueryFilterForPartitionAndRowKey(string partitionKey, string rowKey)
        {
            var queryFilter = TableQuery.GenerateFilterCondition(PartitionKeyField, OpEquals, partitionKey);
            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                OpAnd,
                TableQuery.GenerateFilterCondition(RowKeyField, OpEquals, rowKey));
            return queryFilter;
        }

        public static T InsertEntity<T>(this CloudTablePool tablePool, T entity) where T: class, ITableEntity
        {
            return tablePool.Pool.ExecuteOnAvailableObject(table =>
            {
                var insertOp = TableOperation.Insert(entity, true);
                var tableResult = table.Execute(insertOp);
                return (T) tableResult.Result;
            });
        }

        public static T InsertOrMergeEntity<T>(this CloudTablePool tablePool, T entity) where T: class, ITableEntity
        {
            return tablePool.Pool.ExecuteOnAvailableObject(table =>
            {
                var insertOp = TableOperation.InsertOrMerge(entity);
                var tableResult = table.Execute(insertOp);
                return (T) tableResult.Result;
            });
        }

        public static T UpdateEntity<T>(this CloudTablePool tablePool, T entity) where T: class, ITableEntity
        {
            return tablePool.Pool.ExecuteOnAvailableObject(table =>
            {
                var updateOp = TableOperation.Merge(entity);

                var tableResult = table.Execute(updateOp);
                return (T) tableResult.Result;
            });
        }

        public static T GetEntity<T>(this CloudTablePool tablePool, string partitionKey, string rowKey) where T: ITableEntity, new()
        {
            return tablePool.Pool.ExecuteOnAvailableObject(table =>
            {
                var filter =
                    CreatePointQueryFilterForPartitionAndRowKey(partitionKey, rowKey);

                var entity = table.ExecuteQuery(new TableQuery<T>().Where(filter)).FirstOrDefault();

                return entity;
            });
        }

        public static void Delete(this CloudTablePool tablePool, string partitionKey, string rowKey)
        {
            tablePool.Pool.ExecuteOnAvailableObject(table =>
            {
                table.Execute(TableOperation.Delete(new DynamicTableEntity(partitionKey, rowKey, "*", new Dictionary<string, EntityProperty>())));
                return true;
            });
        }

        public static ReadOnlyCollection<T> GetAllEntitiesInPartition<T>(this CloudTablePool tablePool, string partitionKey) where T: ITableEntity, new()
        {
            return tablePool.Pool.ExecuteOnAvailableObject(table =>
            {
                using var loader =
                    new AzureStorageTableLoader<T, T>(table, DefaultMap, new[] {partitionKey});
                return loader.AllEntities;
            });

            static T DefaultMap(T val)
            {
                return val;
            }
        }
    }
}