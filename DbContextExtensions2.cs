using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Db.Extensions
{
    /// <summary>
    /// Extension methods for DbContext
    /// </summary>
    public static class DbContextExtensions2
    {
        /// <summary>
        /// Detaches the navigation properties.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <exception cref="ArgumentNullException">entry</exception>
        public static void DetachNavigationProperties(this EntityEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var parentEntities = new HashSet<object>();
            entry.Detach(parentEntities);
        }

        private static void Detach(this EntityEntry entry, ISet<object> objectSet) {
            entry.State = EntityState.Detached;

            if (!objectSet.Add(entry))
                return;

            foreach (var nav in entry.Navigations)
            {
                var isCurrentValueEnumerable = nav.CurrentValue is IEnumerable;

                if (!isCurrentValueEnumerable)
                {
                    var navEntry = nav.EntityEntry.Context.Entry(nav.CurrentValue);
                    if (navEntry.State != EntityState.Detached)
                    {
                        navEntry.Detach(objectSet);
                    }
                }
                else
                {
                    foreach (var navItem in nav.CurrentValue as IEnumerable)
                    {
                        var navEntry = nav.EntityEntry.Context.Entry(navItem);
                        if (navEntry.State != EntityState.Detached)
                        {
                            navEntry.Detach(objectSet);
                        }
                    }
                }
            }

        }


    }
}
