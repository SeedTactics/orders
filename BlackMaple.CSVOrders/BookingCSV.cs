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
        public string CSVBasePath { get; set; } = "";
        public string ScheduledBookingsPath { get; set; } = "scheduled-bookings";

        private class UnscheduledCsvRow
        {
            public string Id;
            public int Priority;
            public DateTime DueDate;
            public string Part;
            public int Quantity;
        }

        private class ScheduledBookingCsvRow
        {
            public DateTime ScheduledTimeUTC;
            public string Part;
            public int Quantity;
            public string ScheduleId;
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
            if (!File.Exists(pth)) {
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
            var schFile = Path.Combine(CSVBasePath, "schedulued-parts.csv");
            if (!File.Exists(schFile)) return new ScheduledPartWithoutBooking[] { };

            using (var f = File.OpenRead(schFile))
            {
                var csv = new CsvHelper.CsvReader(new StreamReader(f));
                return csv.GetRecords<ScheduledPartWithoutBooking>();
            }
        }

        private string LastSchedule()
        {
            var lastSchFile = Directory.GetFiles(Path.Combine(CSVBasePath, "scheduled-booking-temp*.csv"))
                     .OrderByDescending(x => x)
                     .FirstOrDefault();

            string lastFile = Path.Combine(CSVBasePath, "last-schedule-id.txt");
            string lastId = "";
            if (File.Exists(lastSchFile))
            {
                lastId = File.ReadAllLines(lastFile)[0];
            }

            if (!string.IsNullOrEmpty(lastSchFile))
            {
                //check for bad copy
                if (Path.GetFileName(lastFile) == "scheduled-parts-temp-" + lastId + ".csv")
                {
                    var schPartFile = Path.Combine(CSVBasePath, "scheduled-parts.csv");
                    if (File.Exists(schPartFile)) File.Delete(schPartFile);
                    File.Move(lastFile, schPartFile);
                }
            }
            return lastId;
        }

        public UnscheduledStatus LoadUnscheduledStatus()
        {
            var ret = default(UnscheduledStatus);
            ret.UnscheduledBookings = LoadUnscheduledBookings().Values;
            //call LastSchedule() before LoadScheduledParts() because the scheduled parts file might need to be updated
            ret.MaxScheduleId = LastSchedule();
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
                }
                f.Flush(true);
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
                    }
                    f.Flush(true);
                }
            }
        }

        public void CreateSchedule(string scheduleId, DateTime scheduledTimeUTC, TimeSpan scheduledHorizon, IEnumerable<string> bookingIds, IEnumerable<DownloadedPart> downloadedParts, IEnumerable<ScheduledPartWithoutBooking> scheduledParts)
        {
            WriteScheduledBookings(scheduleId, scheduledTimeUTC, bookingIds);

            var schTempFile = Path.Combine(CSVBasePath, "scheduled-parts-temp-" + scheduleId + ".csv");
            var schFile = Path.Combine(CSVBasePath, "scheduled-parts.csv");
            var lastSchFile = Path.Combine(CSVBasePath, "last-schedule-id.txt");

            WriteScheduledParts(schTempFile, scheduledParts);

            using (var f = File.Open(lastSchFile, FileMode.Create))
            {
                using (var s = new StreamWriter(f))
                {
                    s.WriteLine(scheduleId);
                    s.Flush();
                }
                f.Flush(true);
            }

            if (File.Exists(schFile)) File.Delete(schFile);
            File.Move(schTempFile, schFile);
        }

        public IEnumerable<Schedule> LoadSchedulesByDate(DateTime startUTC, DateTime endUTC)
        {
            return new Schedule[] { };
        }
    }
}
