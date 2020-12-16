/* Copyright (c) 2020, John Lenz

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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BlackMaple.SeedOrders;
using System.Globalization;

namespace BlackMaple.CSVOrders
{
  ///<summary>
  ///  Implement management of orders via CSV files for simpler integration into ERP systems
  ///</summary>
  public class CSVBookings
  {
    private string _csvBase = null;
    public string CSVBasePath
    {
      get
      {
        if (string.IsNullOrEmpty(_csvBase))
          return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        else
          return _csvBase;
      }
      set
      {
        _csvBase = value;
      }
    }
    public string ScheduledBookingsPath { get; set; } = "scheduled-bookings";
    public Func<DateTime> GetUtcNow { get; set; } = () => DateTime.UtcNow;

    private class UnscheduledCsvRow
    {
      [CsvHelper.Configuration.Attributes.Optional] public string Id { get; set; }
      public DateTime DueDate { get; set; }
      public int Priority { get; set; }
      public string Part { get; set; }
      public int Quantity { get; set; }
    }

    private class ScheduledBookingCsvRow
    {
      public DateTime ScheduledTimeUTC { get; set; }
      public string Part { get; set; }
      public int Quantity { get; set; }
      public string ScheduleId { get; set; }
    }

    private void CreateEmptyBookingFile(string file)
    {
      using (var f = File.OpenWrite(file))
      using (var s = new StreamWriter(f))
      using (var csv = new CsvHelper.CsvWriter(s, CultureInfo.InvariantCulture))
      {
        csv.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats
                = new string[] { "yyyy-MM-dd" };
        csv.WriteRecords<UnscheduledCsvRow>(new[] {
          new UnscheduledCsvRow()
          {
            Id = "12345",
            DueDate = DateTime.Today.AddDays(10),
            Priority = 100,
            Part = "part1",
            Quantity = 50,
          },
          new UnscheduledCsvRow()
          {
            Id = "98765",
            DueDate = DateTime.Today.AddDays(12),
            Priority = 100,
            Part = "part2",
            Quantity = 77,
          }
        });
      }
    }

    public Dictionary<string, Booking> LoadUnscheduledBookings(int? lookaheadDays = null)
    {
      var bookingMap = new Dictionary<string, Booking>();
      var pth = Path.Combine(CSVBasePath, "bookings.csv");
      if (!File.Exists(pth))
      {
        CreateEmptyBookingFile(pth);
        return bookingMap;
      }

      using (var f = File.OpenRead(pth))
      using (var sreader = new StreamReader(f))
      using (var csv = new CsvHelper.CsvReader(sreader, CultureInfo.InvariantCulture))
      {

        var orderCntr = 0;

        foreach (var row in csv.GetRecords<UnscheduledCsvRow>())
        {
          var bookingId = row.Id;

          if (string.IsNullOrEmpty(bookingId))
          {
            bookingId = "B" + GetUtcNow().ToString("yyyy-MM-dd-HH-mm-ss") + "-" + orderCntr.ToString();
            orderCntr += 1;
          }

          Booking book;
          if (bookingMap.ContainsKey(bookingId))
          {
            book = bookingMap[bookingId];
          }
          else
          {
            book = new Booking
            {
              BookingId = bookingId,
              Priority = row.Priority,
              DueDate = row.DueDate,
              Parts = new List<BookingDemand>(),
              ScheduleId = null
            };
            bookingMap.Add(bookingId, book);
          }
          book.Parts.Add(new BookingDemand
          {
            BookingId = bookingId,
            Part = row.Part,
            Quantity = row.Quantity,
          });

        }

      }

      DateTime? endDate = null;
      if (lookaheadDays.HasValue && lookaheadDays.Value > 0)
      {
        endDate = DateTime.Today.AddDays(lookaheadDays.Value);
      }

      foreach (var id in bookingMap.Keys.ToList())
      {
        var f = Path.Combine(CSVBasePath, Path.Combine(ScheduledBookingsPath, id + ".csv"));
        if (File.Exists(f) || endDate.HasValue && bookingMap[id].DueDate > endDate)
        {
          bookingMap.Remove(id);
        }
      }

      return bookingMap;
    }

    public IEnumerable<ScheduledPartWithoutBooking> LoadScheduledParts()
    {
      var schFile = Path.Combine(CSVBasePath, "scheduled-parts.csv");

      var tempSchFile = Directory.GetFiles(CSVBasePath, "scheduled-parts-temp-*.csv")
          .OrderBy(x => x)
          .LastOrDefault();
      if (!string.IsNullOrEmpty(tempSchFile))
      {
        if (File.Exists(schFile)) File.Delete(schFile);
        File.Move(tempSchFile, schFile);
      }

      if (!File.Exists(schFile)) return new ScheduledPartWithoutBooking[] { };

      using (var f = File.OpenRead(schFile))
      using (var csv = new CsvHelper.CsvReader(new StreamReader(f), CultureInfo.InvariantCulture))
      {
        return csv.GetRecords<ScheduledPartWithoutBooking>().ToArray();
      }
    }

    public long? LoadLatestBackoutId()
    {
      string f = Path.Combine(CSVBasePath, "latest-backout-id");
      if (File.Exists(f))
        return long.Parse(File.ReadAllText(f));
      else
        return null;
    }

    private void WriteScheduledParts(string file, IEnumerable<ScheduledPartWithoutBooking> parts)
    {
      using (var f = File.Open(file, FileMode.Create))
      {
        using (var s = new StreamWriter(f))
        using (var csv = new CsvHelper.CsvWriter(s, CultureInfo.InvariantCulture))
        {
          csv.WriteRecords(parts);
          s.Flush();
          //f.Flush(true);
          f.Flush();
        }
      }
    }

    private void WriteScheduledBookings(string scheduleId, DateTime scheduledTimeUTC, IEnumerable<string> bookingIds)
    {
      var unschBookings = LoadUnscheduledBookings();

      foreach (var bookingId in bookingIds)
      {
        using (var f = File.Open(Path.Combine(CSVBasePath, Path.Combine(ScheduledBookingsPath, bookingId + ".csv")), FileMode.Create))
        using (var stream = new StreamWriter(f))
        using (var csv = new CsvHelper.CsvWriter(stream, CultureInfo.InvariantCulture))
        {
          csv.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats
              = new string[] { "yyyy-MM-ddTHH:mm:ssZ" };
          csv.WriteHeader<ScheduledBookingCsvRow>();
          csv.NextRecord();

          if (unschBookings.ContainsKey(bookingId))
          {
            foreach (var p in unschBookings[bookingId].Parts)
            {
              csv.WriteRecord(new ScheduledBookingCsvRow
              {
                ScheduledTimeUTC = scheduledTimeUTC,
                Part = p.Part,
                Quantity = p.Quantity,
                ScheduleId = scheduleId
              });
              csv.NextRecord();
            }
          }
          else
          {
            csv.WriteRecord(new ScheduledBookingCsvRow
            {
              ScheduledTimeUTC = scheduledTimeUTC,
              Part = "",
              Quantity = 0,
              ScheduleId = scheduleId
            });
            csv.NextRecord();
          }

          stream.Flush();
          //f.Flush(true);
          f.Flush();
        }
      }
    }

    public void CreateSchedule(NewSchedule s)
    {
      if (!Directory.Exists(Path.Combine(CSVBasePath, ScheduledBookingsPath)))
        Directory.CreateDirectory(Path.Combine(CSVBasePath, ScheduledBookingsPath));

      var schTempFile = Path.Combine(CSVBasePath, "scheduled-parts-temp-" + s.ScheduleId + ".csv");
      var schFile = Path.Combine(CSVBasePath, "scheduled-parts.csv");
      WriteScheduledParts(schTempFile, s.ScheduledParts);

      WriteScheduledBookings(s.ScheduleId, s.ScheduledTimeUTC, s.BookingIds);

      if (File.Exists(schFile)) File.Delete(schFile);
      File.Move(schTempFile, schFile);
    }

    public void HandleBackedOutWork(Backout backout)
    {
      var file = Path.Combine(CSVBasePath, "bookings.csv");

      File.WriteAllText(Path.Combine(CSVBasePath, "latest-backout-id"), backout.BackoutId.ToString());

      var orders = new List<UnscheduledCsvRow>();

      if (File.Exists(file))
      {
        using (var f = File.OpenRead(file))
        using (var csv = new CsvHelper.CsvReader(new StreamReader(f), CultureInfo.InvariantCulture))
        {
          orders.AddRange(csv.GetRecords<UnscheduledCsvRow>());
        }
      }

      foreach (var p in backout.Parts)
      {
        orders.Add(new UnscheduledCsvRow()
        {
          Id = "Reschedule-" + p.Part + "-" + DateTime.UtcNow.ToString("yyy-MM-ddTHH-mm-ssZ"),
          DueDate = DateTime.Today,
          Priority = 100,
          Part = p.Part,
          Quantity = p.Quantity
        });
      }

      using (var s = new StreamWriter(file))
      using (var csv = new CsvHelper.CsvWriter(s, CultureInfo.InvariantCulture))
      {
        csv.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats
                = new string[] { "yyyy-MM-dd" };

        csv.WriteRecords(orders);
      }
    }
  }
}
