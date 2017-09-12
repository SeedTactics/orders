using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BlackMaple.SeedOrders;
using System.Linq;

namespace ExampleOrderIntegration
{
    public class WorkorderContext : DbContext
    {
        public DbSet<Workorder> Workorders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=workorders.db");
        }

        protected override void OnModelCreating(ModelBuilder m)
        {
            m.Entity<WorkorderDemand>()
              .HasKey(p => new { p.WorkorderId, p.Part });
            m.Entity<WorkorderDemand>()
              .HasIndex(p => p.Part);
            m.Entity<Workorder>()
              .HasIndex(w => w.DueDate);
            m.Entity<Workorder>()
              .HasIndex(w => w.FilledUTC);
        }
    }

    public class ExampleWorkorderDatabase : IWorkorderDatabase
    {
        public IEnumerable<Workorder> LoadUnfilledWorkorders()
        {
            using (var context = new WorkorderContext())
            {
                return context.Workorders
                  .Where(w => w.FilledUTC == null)
                  .Include(w => w.Parts)
                  .AsNoTracking()
                  .ToList();
            }
        }

        public IEnumerable<Workorder> LoadUnfilledWorkorders(string part)
        {
            using (var context = new WorkorderContext())
            {
                return context.Workorders
                  .Include(w => w.Parts)
                  .Where(w => w.Parts.Any(p => p.Part == part) && w.FilledUTC == null)
                  .AsNoTracking()
                  .ToList();
            }
        }

        public void MarkWorkorderAsFilled(string workorderId, DateTime fillUTC, WorkorderResources resources)
        {
            using (var context = new WorkorderContext())
            {
                var work = context.Workorders
                    .Include(w => w.Parts)
                    .Single(x => x.WorkorderId == workorderId);
                if (work != null) {
                    work.FilledUTC = fillUTC;

                    //You will likely also want to store the WorkorderResources somewhere.

                    context.SaveChanges();
                }
            }
        }
    }
}