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
using System.IO;
using FluentAssertions;
using Xunit;
using BlackMaple.SeedOrders;

namespace tests
{
    public class CsvBookingTest
    {
        private List<Booking> initialBookings;
        private List<ScheduledPartWithoutBooking> initialSchParts;

        public CsvBookingTest()
        {
            if (Directory.Exists("scheduled-bookings"))
                Directory.Delete("scheduled-bookings", true);
            foreach (var f in Directory.GetFiles(".", "scheduled-parts-temp*.csv")) {
                File.Delete(f);
            }
            initialBookings = new List<Booking>();
            initialSchParts = new List<ScheduledPartWithoutBooking>();

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

            using (var f = File.Open("unscheduled-bookings.csv", FileMode.Create))
            {
                using (var s = new StreamWriter(f))
                {
                    s.WriteLine("Id,DueDate,Priority,Part,Quantity");
                    foreach (var b in initialBookings)
                    {
                        foreach (var p in b.Parts)
                        {
                            s.WriteLine(b.BookingId + ","
                              + b.DueDate.ToString("yyyy-MM-dd") + ","
                              + b.Priority.ToString() + ","
                              + p.Part + "," + p.Quantity.ToString());
                        }
                    }
                }
            }

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

            using (var f = File.Open("scheduled-parts.csv", FileMode.Create))
            {
                using (var s = new StreamWriter(f))
                {
                    s.WriteLine("Part,Quantity");
                    foreach (var b in initialSchParts)
                    {
                        s.WriteLine(b.Part + "," + b.Quantity.ToString());
                    }
                }
            }


        }

        [Fact]
        public void LoadUnscheduledStatus()
        {
            var booking = new BlackMaple.CSVOrders.CSVBookings();
            var status = booking.LoadUnscheduledStatus();
            status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
        }

        [Fact]
        public void CreateSchedule()
        {

            var booking = new BlackMaple.CSVOrders.CSVBookings();

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

            //check status
            var status = booking.LoadUnscheduledStatus();
            status.ScheduledParts.ShouldAllBeEquivalentTo(schParts);
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(
                new Booking[] { initialBookings[2] }
            );

            var sch1 = File.ReadAllLines("scheduled-bookings/booking1.csv");
            var sch2 = File.ReadAllLines("scheduled-bookings/booking2.csv");

            sch1.ShouldAllBeEquivalentTo(new string[] {
                "ScheduledTimeUTC,Part,Quantity,ScheduleId",
                "11/5/16 12:00:00 AM,part1,44,12345",
                "11/5/16 12:00:00 AM,part2,66,12345"
            });
            sch2.ShouldAllBeEquivalentTo(new string[] {
                "ScheduledTimeUTC,Part,Quantity,ScheduleId",
                "11/5/16 12:00:00 AM,part1,55,12345",
                "11/5/16 12:00:00 AM,part2,77,12345"
            });
        }

        [Fact]
        public void RecoverBadFileCopy()
        {
            File.WriteAllLines("scheduled-parts-temp-abc.csv", new string[]{
                "Part,Quantity",
                "mypart,12",
                "otherpart,17"
            });

            var booking = new BlackMaple.CSVOrders.CSVBookings();
            var status = booking.LoadUnscheduledStatus();
            status.ScheduledParts.ShouldAllBeEquivalentTo(new ScheduledPartWithoutBooking[] {
                new ScheduledPartWithoutBooking { Part = "mypart", Quantity = 12},
                new ScheduledPartWithoutBooking { Part = "otherpart", Quantity = 17}
            });
            status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
        }
    }
}
