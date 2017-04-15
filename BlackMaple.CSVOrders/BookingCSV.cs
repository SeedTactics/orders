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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BlackMaple.SeedOrders;

namespace BlackMaple.CSVOrders
{
    ///<summary>
    ///  Implement management of orders via CSV files for simpler integration into ERP systems
    ///</summary>
    public class CSVBookings : IBookingDatabase
    {
        public string CSVBasePath { get; set; } = ".";
        public string ScheduledBookingsPath { get; set; } = "scheduled-bookings";

        private class UnscheduledCsvRow
        {
            public string Id { get; set; }
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
            {
                using (var s = new StreamWriter(f))
                {
                    var csv = new CsvHelper.CsvWriter(s);
                    csv.WriteHeader<UnscheduledCsvRow>();
                }
            }
        }

        private Dictionary<string, Booking> LoadUnscheduledBookings()
        {
            var bookingMap = new Dictionary<string, Booking>();
            var pth = Path.Combine(CSVBasePath, "unscheduled-bookings.csv");
            if (!File.Exists(pth))
            {
                CreateEmptyBookingFile(pth);
                return bookingMap;
            }

            using (var f = File.OpenRead(pth))
            {
                var csv = new CsvHelper.CsvReader(new StreamReader(f));

                foreach (var row in csv.GetRecords<UnscheduledCsvRow>())
                {
                    Booking work;
                    if (bookingMap.ContainsKey(row.Id))
                    {
                        work = bookingMap[row.Id];
                    }
                    else
                    {
                        work = new Booking
                        {
                            BookingId = row.Id,
                            Priority = row.Priority,
                            DueDate = row.DueDate,
                            Parts = new List<BookingDemand>(),
                            ScheduleId = null
                        };
                        bookingMap.Add(row.Id, work);
                    }
                    work.Parts.Add(new BookingDemand
                    {
                        BookingId = row.Id,
                        Part = row.Part,
                        Quantity = row.Quantity
                    });
                }

            }

            foreach (var id in bookingMap.Keys.ToList())
            {
                var f = Path.Combine(CSVBasePath, ScheduledBookingsPath, id + ".csv");
                if (File.Exists(f))
                {
                    bookingMap.Remove(id);
                }
            }

            return bookingMap;
        }

        private IEnumerable<ScheduledPartWithoutBooking> LoadScheduledParts()
        {
            var schFile = Path.Combine(CSVBasePath, "scheduled-parts.csv");

            var tempSchFile = Directory.GetFiles(CSVBasePath, "scheduled-parts-temp-*.csv")
                .OrderBy(x => x)
                .LastOrDefault();
            if (!string.IsNullOrEmpty(tempSchFile)) {
                if (File.Exists(schFile)) File.Delete(schFile);
                File.Move(tempSchFile, schFile);
            }

            if (!File.Exists(schFile)) return new ScheduledPartWithoutBooking[] { };

            using (var f = File.OpenRead(schFile))
            {
                var csv = new CsvHelper.CsvReader(new StreamReader(f));
                return csv.GetRecords<ScheduledPartWithoutBooking>().ToArray();
            }
        }

        public UnscheduledStatus LoadUnscheduledStatus()
        {
            var ret = default(UnscheduledStatus);
            ret.UnscheduledBookings = LoadUnscheduledBookings().Values;
            ret.ScheduledParts = LoadScheduledParts();
            return ret;
        }

        private void WriteScheduledParts(string file, IEnumerable<ScheduledPartWithoutBooking> parts)
        {
            using (var f = File.Open(file, FileMode.Create))
            {
                using (var s = new StreamWriter(f))
                {
                    var csv = new CsvHelper.CsvWriter(s);
                    csv.WriteRecords(parts);
                    s.Flush();
                    f.Flush(true);
                }
            }
        }

        private void WriteScheduledBookings(string scheduleId, DateTime scheduledTimeUTC, IEnumerable<string> bookingIds)
        {
            var unschBookings = LoadUnscheduledBookings();

            foreach (var bookingId in bookingIds)
            {
                using (var f = File.Open(Path.Combine(CSVBasePath, ScheduledBookingsPath, bookingId + ".csv"), FileMode.Create))
                {
                    using (var stream = new StreamWriter(f))
                    {
                        var csv = new CsvHelper.CsvWriter(stream);
                        csv.WriteHeader<ScheduledBookingCsvRow>();

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
                        }

                        stream.Flush();
                        f.Flush(true);
                    }
                }
            }
        }

        public void CreateSchedule(string scheduleId, DateTime scheduledTimeUTC, TimeSpan scheduledHorizon, IEnumerable<string> bookingIds, IEnumerable<DownloadedPart> downloadedParts, IEnumerable<ScheduledPartWithoutBooking> scheduledParts)
        {
            if (!Directory.Exists(Path.Combine(CSVBasePath, ScheduledBookingsPath)))
                Directory.CreateDirectory(Path.Combine(CSVBasePath, ScheduledBookingsPath));

            var schTempFile = Path.Combine(CSVBasePath, "scheduled-parts-temp-" + scheduleId + ".csv");
            var schFile = Path.Combine(CSVBasePath, "scheduled-parts.csv");
            WriteScheduledParts(schTempFile, scheduledParts);

            WriteScheduledBookings(scheduleId, scheduledTimeUTC, bookingIds);

            if (File.Exists(schFile)) File.Delete(schFile);
            File.Move(schTempFile, schFile);
        }

        public IEnumerable<Schedule> LoadSchedulesByDate(DateTime startUTC, DateTime endUTC)
        {
            return new Schedule[] { };
        }
    }
}
