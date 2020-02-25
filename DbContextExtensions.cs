using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Db.Extensions
{
    /// <summary>
    /// Extension methods for DbContext
    /// </summary>
    public static class DbContextExtensions
    {

        private const string ErrorMessage = "The type '{0}' doesn't exist in current DbContext '{1}'.";
        /// <summary>
        /// Detaches entity from db context if it is loaded.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbContext">The database context.</param>
        /// <param name="entity">The entity.</param>
        /// <exception cref="ArgumentNullException">dbContext</exception>
        public static void TryDetach<TEntity>(this DbContext dbContext, TEntity entity) where TEntity : class
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));

            if (!dbContext.Model.GetEntityTypes().Any(t => t.ClrType.Equals(entity.GetType())))
            {
                throw new ArgumentException(string.Format(ErrorMessage, entity.GetType().Name, dbContext.GetType().Name));
            }

            if (entity == null || dbContext.ChangeTracker == null || dbContext.ChangeTracker.Entries().Count() == 0)
            {
                return;
            }

            var parentEntities = new HashSet<object>();
            dbContext.Detach(entity, parentEntities);
        }

        private static void Detach<TEntity>(this DbContext dbContext, TEntity entity, ISet<object> objectSet) where TEntity : class
        {
            if (entity == null)
            {
                return;
            }

            // This is to prevent an infinite recursion when the child object has a navigation property
            // that points back to the parent
            if (!objectSet.Add(entity))
                return;

            var properties = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => !typeof(string).IsAssignableFrom(property.PropertyType) &&
                    (property.PropertyType.IsClass || typeof(IEnumerable).IsAssignableFrom(property.PropertyType)));

            foreach (var property in properties)
            {
                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    if (!(property.GetValue(entity, null) is IEnumerable<object> collection))
                    {
                        continue;
                    }

                    foreach (var child in collection.ToList())
                    {
                        dbContext.Detach(child, objectSet);
                    }
                }
                else
                {
                    dbContext.Detach(property.GetValue(entity, null), objectSet);
                }
            }

            dbContext.Detach<TEntity>(entity);
        }

        private static void Detach<TEntity>(this DbContext dbContext, TEntity entity) where TEntity : class
        {
            if (entity == null)
            {
                return;
            }

            var entityEntry = dbContext.Entry(entity);
            if (entityEntry != null)
            {
                entityEntry.State = EntityState.Detached;
            }
        }
    }
}
