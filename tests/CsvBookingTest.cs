/* Copyright (c) 2019, John Lenz

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
      if (Directory.Exists("programs"))
        Directory.Delete("programs", true);
      if (File.Exists("latest-backout-id"))
        File.Delete("latest-backout-id");
      foreach (var f in Directory.GetFiles(".", "scheduled-parts-temp*.csv"))
      {
        File.Delete(f);
      }
      initialBookings = new List<Booking>();
      initialSchParts = new List<ScheduledPartWithoutBooking>();

      initialBookings.Add(new Booking
      {
        BookingId = "booking1",
        Priority = 100,
        DueDate = DateTime.Today.AddDays(5),
        ScheduleId = null,
        Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking1", Part = "part1", Quantity = 44, CastingId=null},
                        new BookingDemand { BookingId = "booking1", Part = "part2", Quantity = 66, CastingId=null}
                     }
      });
      initialBookings.Add(new Booking
      {
        BookingId = "booking2",
        Priority = 200,
        DueDate = DateTime.Today.AddDays(15),
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
        DueDate = DateTime.Today.AddDays(30),
        ScheduleId = null,
        Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking3", Part = "part1", Quantity = 111},
                        new BookingDemand { BookingId = "booking3", Part = "part3", Quantity = 222}
                     }
      });

      using (var f = File.Open("bookings.csv", FileMode.Create))
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
      var status = booking.LoadUnscheduledStatus(50);
      status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
      status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
      Assert.Empty(status.Castings);
      Assert.Null(status.LatestBackoutId);
      Assert.Null(status.Programs);

      status = booking.LoadUnscheduledStatus(10);
      status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
      status.UnscheduledBookings.ShouldAllBeEquivalentTo(
          new[] { initialBookings[0] }
      );
      Assert.Empty(status.Castings);
      Assert.Null(status.LatestBackoutId);
      Assert.Null(status.Programs);

      status = booking.LoadUnscheduledStatus(-1);
      status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
      status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
      Assert.Empty(status.Castings);
      Assert.Null(status.LatestBackoutId);
      Assert.Null(status.Programs);
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

      booking.CreateSchedule(
          new NewSchedule
          {
            ScheduleId = "12345",
            ScheduledTimeUTC = new DateTime(2016, 11, 05),
            ScheduledHorizon = TimeSpan.FromMinutes(155),
            BookingIds = new List<string>() { "booking1", "booking2" },
            DownloadedParts = downParts.ToList(),
            ScheduledParts = schParts.ToList()
          });

      //check status
      var status = booking.LoadUnscheduledStatus(50);
      status.ScheduledParts.ShouldAllBeEquivalentTo(schParts);
      status.UnscheduledBookings.ShouldAllBeEquivalentTo(
          new Booking[] { initialBookings[2] }
      );
      Assert.Null(status.LatestBackoutId);

      var sch1 = File.ReadAllLines("scheduled-bookings/booking1.csv");
      var sch2 = File.ReadAllLines("scheduled-bookings/booking2.csv");

      sch1.ShouldAllBeEquivalentTo(new string[] {
                "ScheduledTimeUTC,Part,Quantity,ScheduleId",
                "2016-11-05T00:00:00Z,part1,44,12345",
                "2016-11-05T00:00:00Z,part2,66,12345"
            });
      sch2.ShouldAllBeEquivalentTo(new string[] {
                "ScheduledTimeUTC,Part,Quantity,ScheduleId",
                "2016-11-05T00:00:00Z,part1,55,12345",
                "2016-11-05T00:00:00Z,part2,77,12345"
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
      var status = booking.LoadUnscheduledStatus(50);
      status.ScheduledParts.ShouldAllBeEquivalentTo(new ScheduledPartWithoutBooking[] {
                new ScheduledPartWithoutBooking { Part = "mypart", Quantity = 12},
                new ScheduledPartWithoutBooking { Part = "otherpart", Quantity = 17}
            });
      status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
      Assert.Null(status.LatestBackoutId);
    }

    [Fact]
    public void BackOutOfWork()
    {
      var booking = new BlackMaple.CSVOrders.CSVBookings();
      booking.HandleBackedOutWork(667788, new[] {
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
                    }
                }
      });

      var status = booking.LoadUnscheduledStatus(50);
      status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
      status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
      Assert.Equal(667788, status.LatestBackoutId);
    }

    [Fact]
    public void MissingIdColumn()
    {
      // only works with bookings which have only a single part
      initialBookings.Clear();
      initialBookings.Add(new Booking
      {
        BookingId = "booking1",
        Priority = 100,
        DueDate = DateTime.Today.AddDays(5),
        ScheduleId = null,
        Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking1", Part = "part1", Quantity = 44, CastingId=null},
                     }
      });
      initialBookings.Add(new Booking
      {
        BookingId = "booking2",
        Priority = 200,
        DueDate = DateTime.Today.AddDays(15),
        ScheduleId = null,
        Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking2", Part = "part2", Quantity = 77}
                     }
      });
      initialBookings.Add(new Booking
      {
        BookingId = "booking3",
        Priority = 300,
        DueDate = DateTime.Today.AddDays(30),
        ScheduleId = null,
        Parts = new List<BookingDemand> {
                        new BookingDemand { BookingId = "booking3", Part = "part3", Quantity = 222}
                     }
      });

      using (var f = File.Open("bookings.csv", FileMode.Create))
      {
        using (var s = new StreamWriter(f))
        {
          s.WriteLine("DueDate,Priority,Part,Quantity");
          foreach (var b in initialBookings)
          {
            foreach (var p in b.Parts)
            {
              s.WriteLine(
                  b.DueDate.ToString("yyyy-MM-dd") + ","
                + b.Priority.ToString() + ","
                + p.Part + "," + p.Quantity.ToString());
            }
          }
        }
      }

      var now = new DateTime(2019, 04, 23, 1, 2, 3, DateTimeKind.Utc);
      var nowStr = now.ToString("yyyy-MM-dd-HH-mm-ss");


      initialBookings[0].BookingId = "B:" + nowStr + ":0";
      initialBookings[0].Parts[0].BookingId = "B:" + nowStr + ":0";
      initialBookings[1].BookingId = "B:" + nowStr + ":1";
      initialBookings[1].Parts[0].BookingId = "B:" + nowStr + ":1";
      initialBookings[2].BookingId = "B:" + nowStr + ":2";
      initialBookings[2].Parts[0].BookingId = "B:" + nowStr + ":2";

      var booking = new BlackMaple.CSVOrders.CSVBookings();
      booking.GetUtcNow = () => now;

      var status = booking.LoadUnscheduledStatus(50);
      status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
      status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
      Assert.Empty(status.Castings);
      Assert.Null(status.LatestBackoutId);

    }

    [Fact]
    public void LoadPrograms()
    {
      Directory.CreateDirectory("programs");
      File.WriteAllText(Path.Combine("programs", "part1.NC"), "the part1 program");

      var booking = new BlackMaple.CSVOrders.CSVBookings();
      var status = booking.LoadUnscheduledStatus(-1);
      status.ScheduledParts.ShouldAllBeEquivalentTo(initialSchParts);
      status.UnscheduledBookings.ShouldAllBeEquivalentTo(initialBookings);
      Assert.Empty(status.Castings);
      Assert.Null(status.LatestBackoutId);
      status.Programs.ShouldBeEquivalentTo(new Dictionary<string, PartProgram>() {
        {"part1", new PartProgram() { ProgramName = "part1", ProgramContents = "the part1 program"}}
      });
    }
  }

  public class CsvCreateBookingTest
  {
    [Fact]
    public void CreateSampleBooking()
    {
      var bookFile = System.IO.Path.Combine("create-book-test", "bookings.csv");
      if (!System.IO.Directory.Exists("create-book-test"))
        System.IO.Directory.CreateDirectory("create-book-test");
      if (System.IO.File.Exists(bookFile))
        System.IO.File.Delete(bookFile);
      var booking = new BlackMaple.CSVOrders.CSVBookings();
      booking.CSVBasePath = "create-book-test";
      booking.LoadUnscheduledStatus(1);

      var b = File.ReadAllLines(bookFile);

      b.ShouldAllBeEquivalentTo(new string[] {
        "Id,DueDate,Priority,Part,Quantity",
        "12345," + DateTime.Today.AddDays(10).ToString("yyyy-MM-dd") + ",100,part1,50",
        "98765," + DateTime.Today.AddDays(12).ToString("yyyy-MM-dd") + ",100,part2,77"
      });

    }
  }
}
