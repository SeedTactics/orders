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
using Microsoft.EntityFrameworkCore;
using Xunit;
using BlackMaple.SeedOrders;

namespace tests
{
    public class SqliteBookingTest
    {
        private List<Booking> initialBookings;
        private List<ScheduledPartWithoutBooking> initialSchParts;
        private List<Casting> initialCastings;

        public SqliteBookingTest()
        {
            using (var context = new ExampleOrderIntegration.BookingContext())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            initialBookings = new List<Booking>();
            initialSchParts = new List<ScheduledPartWithoutBooking>();
            initialCastings = new List<Casting>();

            using (var context = new ExampleOrderIntegration.BookingContext())
            {
                initialBookings.Add(new Booking
                {
                    BookingId = "booking1",
                    Priority = 100,
                    DueDate = DateTime.Today.AddDays(5),
                    ScheduleId = null,
                    Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking1", Part = "part1", Quantity = 44, CastingId = "abc"},
                        new BookingDemand { BookingId = "booking1", Part = "part2", Quantity = 66, CastingId = null}
                     }
                });
                initialBookings.Add(new Booking
                {
                    BookingId = "booking2",
                    Priority = 200,
                    DueDate = DateTime.Today.AddDays(15),
                    ScheduleId = null,
                    Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking2", Part = "part1", Quantity = 55, CastingId = "xyz"},
                        new BookingDemand { BookingId = "booking2", Part = "part2", Quantity = 77, CastingId = "jjj"}
                     }
                });
                initialBookings.Add(new Booking
                {
                    BookingId = "booking3",
                    Priority = 300,
                    DueDate = DateTime.Today.AddDays(30),
                    ScheduleId = null,
                    Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking3", Part = "part1", Quantity = 111, CastingId = "abc"},
                        new BookingDemand { BookingId = "booking3", Part = "part3", Quantity = 222, CastingId = "zzz"}
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

                initialCastings.Add(new Casting {CastingId = "abc", Quantity = 15});
                initialCastings.Add(new Casting {CastingId = "xyz", Quantity = 10});
                initialCastings.Add(new Casting {CastingId = "jjj", Quantity = 0});
                initialCastings.Add(new Casting {CastingId = "zzz", Quantity = 177});

                foreach (var c in initialCastings) context.Castings.Add(c);

                context.SaveChanges();
            }

        }

        private IEnumerable<Booking> LoadScheduledBookings()
        {
            using (var context = new ExampleOrderIntegration.BookingContext())
            {
                return context.Bookings
                    .Where(x => x.ScheduleId != null)
                    .Include(x => x.Parts)
                    .AsNoTracking()
                    .ToList();
            }
        }

        [Fact]
        public void LoadUnscheduledStatus()
        {
            var booking = new ExampleOrderIntegration.ExampleBookingDatabase();
            var status = booking.LoadUnscheduledStatus(50);
            status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
            status.Castings.ShouldAllBeEquivalentTo(initialCastings);
            Assert.Null(status.LatestBackoutId);

            status = booking.LoadUnscheduledStatus(10);
            status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(
                new [] {initialBookings[0]}
            );
            status.Castings.ShouldAllBeEquivalentTo(initialCastings);
            Assert.Null(status.LatestBackoutId);

            status = booking.LoadUnscheduledStatus(-1);
            status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
            status.Castings.ShouldAllBeEquivalentTo(initialCastings);
            Assert.Null(status.LatestBackoutId);
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

            booking.CreateSchedule(
                new NewSchedule
                {
                    ScheduleId = "12345",
                    ScheduledTimeUTC = new DateTime(2016, 11, 05),
                    ScheduledHorizon = TimeSpan.FromMinutes(155),
                    BookingIds = new List<string> { "booking1", "booking2" },
                    DownloadedParts = downParts.ToList(),
                    ScheduledParts = schParts.ToList()
                });

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
            var status = booking.LoadUnscheduledStatus(50);
            status.ScheduledParts.ShouldAllBeEquivalentTo(schParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(
                new Booking[] { initialBookings[2] }
            );
            Assert.Null(status.LatestBackoutId);

            initialBookings[0].ScheduleId = "12345";
            initialBookings[1].ScheduleId = "12345";

            LoadScheduledBookings().ShouldAllBeEquivalentTo(
                new Booking[] {initialBookings[0], initialBookings[1]}
            );
        }

        [Fact]
        public void BackOutOfWork()
        {
            var booking = new ExampleOrderIntegration.ExampleBookingDatabase();
            booking.HandleBackedOutWork("thebackoutid", new[] {
                new BackedOutPart
                {
                    Part = "abc",
                    Quantity = 23
                },
                new BackedOutPart
                {
                    Part = "def",
                    Quantity = 193
                }
            });

            var bookingId = "Reschedule:abc:" + DateTime.UtcNow.ToString("yyy-MM-ddTHH-mm-ssZ");
            initialBookings.Add(new Booking
            {
                BookingId = bookingId,
                DueDate = DateTime.Today,
                Priority = 100,
                Parts = new List<BookingDemand> {
                    new BookingDemand
                    {
                        BookingId = bookingId,
                        Part = "abc",
                        Quantity = 23,
                        CastingId = null
                    }
                }
            });

            bookingId = "Reschedule:def:" + DateTime.UtcNow.ToString("yyy-MM-ddTHH-mm-ssZ");
            initialBookings.Add(new Booking
            {
                BookingId = bookingId,
                DueDate = DateTime.Today,
                Priority = 100,
                Parts = new List<BookingDemand> {
                    new BookingDemand
                    {
                        BookingId = bookingId,
                        Part = "def",
                        Quantity = 193,
                        CastingId = null
                    }
                }
            });

            var status = booking.LoadUnscheduledStatus(50);
            status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
            Assert.Equal("thebackoutid", status.LatestBackoutId);
        }
    }
}