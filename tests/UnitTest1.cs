using System;
using System.Collections.Generic;
using Xunit;
using BlackMaple.SeedOrders;

namespace tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            using (var context = new ExampleOrderIntegration.BookingContext())
            {
                //context.Database.OpenConnection();
                context.Database.EnsureCreated();
            }

            using (var context = new ExampleOrderIntegration.BookingContext())
            {
                var b = new Booking
                {
                    BookingId = "booking1",
                    Priority = 100,
                    DueDate = new DateTime(2017, 01, 01),
                    ScheduleId = null,
                    Parts = new List<BookingDemand>(new BookingDemand[] {
                        new BookingDemand { BookingId = "booking1", Part = "part1", Quantity = 44},
                        new BookingDemand { BookingId = "booking1", Part = "part2", Quantity = 66}
                     })
                };
                context.Bookings.Add(b);
                context.Bookings.Add(new Booking
                {
                    BookingId = "booking2",
                    Priority = 200,
                    DueDate = new DateTime(2017, 02, 02),
                    ScheduleId = null,
                    Parts = new List<BookingDemand>(new BookingDemand[] {
                        new BookingDemand { BookingId = "booking2", Part = "part1", Quantity = 55},
                        new BookingDemand { BookingId = "booking2", Part = "part2", Quantity = 77}
                     })
                });
                context.Bookings.Add(new Booking
                {
                    BookingId = "booking3",
                    Priority = 300,
                    DueDate = new DateTime(2017, 03, 03),
                    ScheduleId = null,
                    Parts = new List<BookingDemand>(new BookingDemand[] {
                        new BookingDemand { BookingId = "booking3", Part = "part1", Quantity = 111},
                        new BookingDemand { BookingId = "booking3", Part = "part2", Quantity = 222}
                     })
                });

                context.SaveChanges();
            }

            var booking = new ExampleOrderIntegration.ExampleBookingDatabase();

            booking.CreateSchedule("12345", DateTime.UtcNow, TimeSpan.FromMinutes(155),
                new string[] { "booking1", "booking2" },
                new DownloadedPart[] {
                    new DownloadedPart {ScheduleId = "12345", Part = "part1", Quantity = 155},
                    new DownloadedPart {ScheduleId = "12345", Part = "part2", Quantity = 166}
                },
                new ScheduledPartWithoutBooking[] {
                    new ScheduledPartWithoutBooking {Part = "part1", Quantity = 12},
                    new ScheduledPartWithoutBooking {Part = "part2", Quantity = 52}
                });
        }
    }
}
