using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BlackMaple.SeedOrders;
using System.Linq;

namespace ExampleOrderIntegration
{
    public class BookingContext : DbContext
    {
        public class LatestBackoutIdSingleton
        {
            public string BackoutId {get; set;}
        }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<ScheduledPartWithoutBooking> ExtraParts { get; set; }
        public DbSet<LatestBackoutIdSingleton> LatestBackoutId {get;set;}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=bookings.db");
        }

        protected override void OnModelCreating(ModelBuilder m)
        {
            m.Entity<BookingDemand>()
              .HasKey(p => new { p.BookingId, p.Part });
            m.Entity<DownloadedPart>()
              .HasKey(p => new { p.ScheduleId, p.Part });
            m.Entity<ScheduledPartWithoutBooking>()
              .HasKey(p => p.Part);
            m.Entity<LatestBackoutIdSingleton>()
              .HasKey(x => x.BackoutId);
        }
    }

    public class ExampleBookingDatabase : IBookingDatabase
    {
        public UnscheduledStatus LoadUnscheduledStatus()
        {
            using (var context = new BookingContext())
            {
                return new UnscheduledStatus
                {
                    UnscheduledBookings = context.Bookings
                        .Where(b => b.ScheduleId == null)
                        .Include(b => b.Parts)
                        .AsNoTracking()
                        .ToList(),
                    ScheduledParts = context.ExtraParts
                        .AsNoTracking()
                        .ToList(),
                    LatestBackoutId = context.LatestBackoutId
                        .AsNoTracking()
                        .Select(x => x.BackoutId)
                        .FirstOrDefault()
                };
            }
        }

        public void CreateSchedule(NewSchedule s)
        {
            using (var context = new BookingContext())
            {
                //Update the bookings and extra parts tables.

                foreach (var bookingId in s.BookingIds)
                {
                    var booking = context.Bookings.Single(b => b.BookingId == bookingId);
                    booking.ScheduleId = s.ScheduleId;
                }

                var oldParts = context.ExtraParts.ToDictionary(p => p.Part);
                var newParts = s.ScheduledParts.ToDictionary(p => p.Part);

                foreach (var p in newParts)
                {
                    if (oldParts.ContainsKey(p.Key))
                    {
                        oldParts[p.Key].Quantity = p.Value.Quantity;
                        oldParts.Remove(p.Key);
                    }
                    else
                    {
                        context.ExtraParts.Add(p.Value);
                    }
                }

                foreach (var p in oldParts)
                {
                    context.ExtraParts.Remove(p.Value);
                }

                //In addition to the above data, you could also store data about the schedule itself
                //in a schedule table, but keeping data about the schedule itself is not required for
                //implementing the booking plugin and would just be for your own internal use.

                context.SaveChanges();
            }
        }

        public void HandleBackedOutWork(string backoutId, IEnumerable<BackedOutPart> backedOutParts)
        {
            using (var context = new BookingContext())
            {
                var latest = context.LatestBackoutId.FirstOrDefault();
                if (latest == null)
                {
                    context.LatestBackoutId.Add(new BookingContext.LatestBackoutIdSingleton { BackoutId = backoutId});
                }
                else
                {
                    latest.BackoutId = backoutId;
                }

                foreach (var p in backedOutParts)
                {
                    var bookingId = "Reschedule:" + p.Part + ":" + DateTime.UtcNow.ToString("yyy-MM-ddTHH-mm-ssZ");
                    context.Bookings.Add(new Booking
                    {
                        BookingId = bookingId,
                        DueDate = DateTime.Today,
                        Priority = 100,
                        Parts = new List<BookingDemand> {
                            new BookingDemand
                            {
                                BookingId = bookingId,
                                Part = p.Part,
                                Quantity = p.Quantity,
                                AvailableMaterial = 0
                            }
                        }
                    });
                }
                context.SaveChanges();
            }
        }
    }
}