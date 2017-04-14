/* Copyright (c) 2017, John Lenz

All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above
      copyright notice, this list of conditions and the following
      disclaimer in the documentation and/or other materials provided
      with the distribution.

    * Neither the name of John Lenz, Black Maple Software, SeedTactics,
      nor the names of other contributors may be used to endorse or
      promote products derived from this software without specific
      prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using BlackMaple.SeedOrders;

namespace tests
{
    public class SqliteBookingTest
    {
        private List<Booking> initialBookings;
        private List<ScheduledPartWithoutBooking> initialSchParts;

        public SqliteBookingTest()
        {
            using (var context = new ExampleOrderIntegration.BookingContext())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            initialBookings = new List<Booking>();
            initialSchParts = new List<ScheduledPartWithoutBooking>();

            using (var context = new ExampleOrderIntegration.BookingContext())
            {
                initialBookings.Add(new Booking
                {
                    BookingId = "booking1",
                    Priority = 100,
                    DueDate = new DateTime(2017, 01, 01),
                    ScheduleId = null,
                    Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking1", Part = "part1", Quantity = 44},
                        new BookingDemand { BookingId = "booking1", Part = "part2", Quantity = 66}
                     }
                });
                initialBookings.Add(new Booking
                {
                    BookingId = "booking2",
                    Priority = 200,
                    DueDate = new DateTime(2017, 02, 02),
                    ScheduleId = null,
                    Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking2", Part = "part1", Quantity = 55},
                        new BookingDemand { BookingId = "booking2", Part = "part2", Quantity = 77}
                     }
                });
                initialBookings.Add(new Booking
                {
                    BookingId = "booking3",
                    Priority = 300,
                    DueDate = new DateTime(2017, 03, 03),
                    ScheduleId = null,
                    Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking3", Part = "part1", Quantity = 111},
                        new BookingDemand { BookingId = "booking3", Part = "part3", Quantity = 222}
                     }
                });

                foreach (var b in initialBookings) context.Bookings.Add(b);

                initialSchParts.Add(new ScheduledPartWithoutBooking
                {
                    Part = "part1",
                    Quantity = 1
                });
                initialSchParts.Add(new ScheduledPartWithoutBooking
                {
                    Part = "part3",
                    Quantity = 4
                });

                foreach (var p in initialSchParts) context.ExtraParts.Add(p);

                context.SaveChanges();
            }

        }

        [Fact]
        public void LoadUnscheduledStatus()
        {
            var booking = new ExampleOrderIntegration.ExampleBookingDatabase();
            var status = booking.LoadUnscheduledStatus();
            status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
            Assert.Null(status.MaxScheduleId);
        }

        [Fact]
        public void CreateSchedule()
        {

            var booking = new ExampleOrderIntegration.ExampleBookingDatabase();

            var schParts = new ScheduledPartWithoutBooking[] {
                    new ScheduledPartWithoutBooking {Part = "part1", Quantity = 12},
                    new ScheduledPartWithoutBooking {Part = "part2", Quantity = 52},
                    new ScheduledPartWithoutBooking {Part = "part4", Quantity = 9876}
                };

            var downParts = new DownloadedPart[] {
                    new DownloadedPart {ScheduleId = "12345", Part = "part1", Quantity = 155},
                    new DownloadedPart {ScheduleId = "12345", Part = "part2", Quantity = 166}
                };

            booking.CreateSchedule("12345", new DateTime(2016, 11, 05), TimeSpan.FromMinutes(155),
                new string[] { "booking1", "booking2" },
                downParts,
                schParts);

            using (var ctx = new ExampleOrderIntegration.BookingContext())
            {
                Assert.Equal(
                    ctx.Bookings.Single(b => b.BookingId == "booking1").ScheduleId,
                    "12345"
                );
                Assert.Equal(
                    ctx.Bookings.Single(b => b.BookingId == "booking1").ScheduleId,
                    "12345"
                );

                ctx.ExtraParts.ToList().ShouldAllBeEquivalentTo(schParts);
            }

            //check status
            var status = booking.LoadUnscheduledStatus();
            Assert.Equal("12345", status.MaxScheduleId);
            status.ScheduledParts.ShouldAllBeEquivalentTo(schParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(
                new Booking[] { initialBookings[2] }
            );

            initialBookings[0].ScheduleId = "12345";
            initialBookings[1].ScheduleId = "12345";

            //check loading schedules
            Assert.Empty(booking.LoadSchedulesByDate(
                new DateTime(2016, 01, 01), new DateTime(2016, 02, 02)));
            booking.LoadSchedulesByDate(new DateTime(2016, 01, 01), new DateTime(2017, 12, 31))
              .ShouldAllBeEquivalentTo(new Schedule
              {
                  ScheduleId = "12345",
                  ScheduledTimeUTC = new DateTime(2016, 11, 05),
                  ScheduledHorizon = TimeSpan.FromMinutes(155),
                  Bookings = new List<Booking>(initialBookings.GetRange(0, 2)),
                  DownloadedParts = new List<DownloadedPart>(downParts)
              });
        }
    }
}
