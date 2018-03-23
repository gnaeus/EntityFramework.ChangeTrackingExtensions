﻿using System;
using System.Linq;

#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.CommonTools
#elif EF_6

using System.Data.Entity;
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;

namespace EntityFramework.CommonTools
#endif
{
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Populate special properties for all Auditable Entities in context.
        /// </summary>
        public static void UpdateAuditableEntities<TUserId>(this DbContext context, TUserId editorUserId)
            where TUserId : struct
        {
            DateTime utcNow = DateTime.UtcNow;

            var changedEntries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added
                         || e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                UpdateAuditableEntity(dbEntry, utcNow, editorUserId);
            }
        }

        /// <summary>
        /// Populate special properties for all Auditable Entities in context.
        /// </summary>
        public static void UpdateAuditableEntities(this DbContext context, string editorUserId)
        {
            DateTime utcNow = DateTime.UtcNow;

            var changedEntries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added
                         || e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                UpdateAuditableEntity(dbEntry, utcNow, editorUserId);
            }
        }

        private static void UpdateAuditableEntity<TUserId>(
            EntityEntry dbEntry, DateTime utcNow, TUserId editorUserId)
            where TUserId : struct
        {
            object entity = dbEntry.Entity;

            switch (dbEntry.State)
            {
                case EntityState.Added:
                    var creationAuditable = entity as ICreationAuditable<TUserId>;
                    if (creationAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        creationAuditable.CreatorUserId = editorUserId;
                    }
                    break;

                case EntityState.Modified:
                    var modificationAuditable = entity as IModificationAuditable<TUserId>;
                    if (modificationAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        modificationAuditable.UpdaterUserId = editorUserId;
                        dbEntry.CurrentValues[nameof(IModificationAuditable<TUserId>.UpdaterUserId)] = editorUserId;
                    }
                    break;

                case EntityState.Deleted:
                    var deletionAuditable = entity as IDeletionAuditable<TUserId>;
                    if (deletionAuditable != null)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        // change CurrentValues after dbEntry.State becomes EntityState.Unchanged
                        deletionAuditable.DeleterUserId = editorUserId;
                        dbEntry.CurrentValues[nameof(IDeletionAuditable<TUserId>.DeleterUserId)] = editorUserId;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private static void UpdateAuditableEntity(
            EntityEntry dbEntry, DateTime utcNow, string editorUserId)
        {
            object entity = dbEntry.Entity;

            switch (dbEntry.State)
            {
                case EntityState.Added:
                    if (entity is ICreationAuditable creationAuditable)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        creationAuditable.CreatorUserId = editorUserId;
                    }
                    else if (entity is ICreationAuditableV1 creationAuditableV1)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        creationAuditableV1.CreatorUser = editorUserId;
                    }
                    break;

                case EntityState.Modified:
                    if (entity is IModificationAuditable modificationAuditable)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        modificationAuditable.UpdaterUserId = editorUserId;
                        dbEntry.CurrentValues[nameof(IModificationAuditable.UpdaterUserId)] = editorUserId;
                    }
                    else if (entity is IModificationAuditableV1 modificationAuditableV1)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        modificationAuditableV1.UpdaterUser = editorUserId;
                        dbEntry.CurrentValues[nameof(IModificationAuditableV1.UpdaterUser)] = editorUserId;
                    }
                    break;

                case EntityState.Deleted:
                    if (entity is IDeletionAuditable deletionAuditable)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        // change CurrentValues after dbEntry.State becomes EntityState.Unchanged
                        deletionAuditable.DeleterUserId = editorUserId;
                        dbEntry.CurrentValues[nameof(IDeletionAuditable.DeleterUserId)] = editorUserId;
                    }
                    else if (entity is IDeletionAuditableV1 deletionAuditableV1)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        deletionAuditableV1.DeleterUser = editorUserId;
                        dbEntry.CurrentValues[nameof(IDeletionAuditableV1.DeleterUser)] = editorUserId;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}