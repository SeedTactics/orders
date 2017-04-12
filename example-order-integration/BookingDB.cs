using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BlackMaple.SeedOrders;
using System.Linq;

namespace ExampleOrderIntegration
{
    public class BookingContext : DbContext
    {
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<ScheduledPartWithoutBooking> ExtraParts { get; set; }

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
            m.Entity<Schedule>()
              .HasIndex(s => s.ScheduledTimeUTC);
        }
    }

    public class ExampleBookingDatabase : IBookingDatabase
    {
        public UnscheduledStatus LoadUnscheduledStatus()
        {
            using (var context = new BookingContext())
            {
                var ret = default(UnscheduledStatus);
                ret.UnscheduledBookings = context.Bookings
                    .Where(b => b.ScheduleId == null)
                    .Include(b => b.Parts)
                    .AsNoTracking()
                    .ToList();
                ret.ScheduledParts = context.ExtraParts
                    .AsNoTracking()
                    .ToList();
                ret.MaxScheduleId = context.Schedules
                    .Max(s => s.ScheduleId);
                return ret;
            }
        }

        public void CreateSchedule(string scheduleId, DateTime scheduledTimeUTC, TimeSpan scheduledHorizon, IEnumerable<string> bookingIds, IEnumerable<DownloadedPart> downloadedParts, IEnumerable<ScheduledPartWithoutBooking> scheduledParts)
        {
            using (var context = new BookingContext())
            {
                context.Schedules.Add(new Schedule
                {
                    ScheduleId = scheduleId,
                    ScheduledTimeUTC = scheduledTimeUTC,
                    ScheduledHorizon = scheduledHorizon,
                    DownloadedParts = downloadedParts.ToList(),
                    Bookings = null
                }
                );

                foreach (var bookingId in bookingIds)
                {
                    var booking = context.Bookings.Single(b => b.BookingId == bookingId);
                    booking.ScheduleId = scheduleId;
                }

                var oldParts = context.ExtraParts.ToDictionary(p => p.Part);
                var newParts = scheduledParts.ToDictionary(p => p.Part);

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

                context.SaveChanges();
            }
        }

        public IEnumerable<Schedule> LoadSchedulesByDate(DateTime startUTC, DateTime endUTC)
        {
            using (var context = new BookingContext())
            {
                return context.Schedules
                  .Where(s => s.ScheduledTimeUTC >= startUTC && s.ScheduledTimeUTC <= endUTC)
                  .Include(s => s.Bookings)
                    .ThenInclude(b => b.Parts)
                  .Include(s => s.DownloadedParts)
                  .AsNoTracking()
                  .ToList();
            }
        }


    }
}