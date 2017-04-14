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
    public class WorkorderCSV : IWorkorderDatabase
    {
        public string CSVBasePath { get; set; } = ".";

        public const string FilledWorkordersPath = "filled-workorders";

        private class UnscheduledCsvRow
        {
            public string Id { get; set; }
            public DateTime DueDate { get; set; }
            public int Priority { get; set; }
            public string Part { get; set; }
            public int Quantity { get; set; }
        }

        private void CreateEmptyWorkorderFile(string file)
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

        private Dictionary<string, Workorder> LoadUnfilledWorkordersMap()
        {
            var path = Path.Combine(CSVBasePath, "unscheduled-workorders.csv");
            var workorderMap = new Dictionary<string, Workorder>();
            if (!File.Exists(path))
            {
                CreateEmptyWorkorderFile(path);
                return workorderMap;
            }

            using (var f = File.OpenRead(path))
            {
                var csv = new CsvHelper.CsvReader(new StreamReader(f));

                foreach (var row in csv.GetRecords<UnscheduledCsvRow>())
                {
                    Workorder work;
                    if (workorderMap.ContainsKey(row.Id))
                    {
                        work = workorderMap[row.Id];
                    }
                    else
                    {
                        work = new Workorder
                        {
                            WorkorderId = row.Id,
                            Priority = row.Priority,
                            DueDate = row.DueDate,
                            Parts = new List<WorkorderDemand>()
                        };
                        workorderMap.Add(row.Id, work);
                    }
                    work.Parts.Add(new WorkorderDemand
                    {
                        WorkorderId = row.Id,
                        Part = row.Part,
                        Quantity = row.Quantity
                    });
                }
            }

            foreach (var id in workorderMap.Keys.ToList())
            {
                var f = Path.Combine(CSVBasePath, FilledWorkordersPath, id + ".csv");
                if (File.Exists(f))
                {
                    workorderMap.Remove(id);
                }
            }
            return workorderMap;
        }

        public IEnumerable<Workorder> LoadUnfilledWorkorders()
        {
            return LoadUnfilledWorkordersMap().Values;
        }

        public string LoadLastFilledWorkorderId()
        {
            var lastFile = Path.Combine(CSVBasePath, "last-filled-workorder.txt");
            if (File.Exists(lastFile))
            {
                return File.ReadAllLines(lastFile)[0];
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<Workorder> LoadUnfilledWorkorders(string part)
        {
            return LoadUnfilledWorkorders().Where(w => w.Parts.Any(p => p.Part == part));
        }

        public void MarkWorkorderAsFilled(string workorderId,
                                          DateTime filledUTC,
                                          WorkorderResources resources)
        {
            var workorders = LoadUnfilledWorkordersMap();
            if (!workorders.ContainsKey(workorderId)) return;
            var work = workorders[workorderId];
            var now = DateTime.UtcNow;

            if (!Directory.Exists(Path.Combine(CSVBasePath, FilledWorkordersPath)))
            {
                Directory.CreateDirectory(Path.Combine(CSVBasePath, FilledWorkordersPath));
            }

            using (var f = File.OpenWrite(Path.Combine(CSVBasePath, FilledWorkordersPath, workorderId + ".csv")))
            {
                using (var stream = new StreamWriter(f))
                {
                    var csv = new CsvHelper.CsvWriter(stream);
                    csv.WriteField("CompletedTimeUTC");
                    csv.WriteField("ID");
                    csv.WriteField("Part");
                    csv.WriteField("Quantity");

                    var actualKeys = resources.ActualOperationTimes.Keys.ToList();
                    foreach (var k in actualKeys)
                    {
                        csv.WriteField("Actual " + k + " (minutes)");
                    }
                    var plannedKeys = resources.PlannedOperationTimes.Keys.ToList();
                    foreach (var k in plannedKeys)
                    {
                        csv.WriteField("Planned " + k + " (minutes)");
                    }
                    csv.NextRecord();

                    string parts = "";
                    string qtys = "";
                    foreach (var p in work.Parts)
                    {
                        if (parts == "")
                        {
                            parts = p.Part;
                            qtys = p.Quantity.ToString();
                        }
                        else
                        {
                            parts += ";" + p.Part;
                            qtys += ";" + p.Quantity.ToString();
                        }
                    }
                    csv.WriteField(now.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    csv.WriteField(workorderId);
                    csv.WriteField(parts);
                    csv.WriteField(qtys);

                    foreach (var k in actualKeys)
                    {
                        csv.WriteField(resources.ActualOperationTimes[k].TotalMinutes);
                    }
                    foreach (var k in plannedKeys)
                    {
                        csv.WriteField(resources.PlannedOperationTimes[k].TotalMinutes);
                    }
                    csv.NextRecord();
                }
            }

            using (var f = File.Open(Path.Combine(CSVBasePath, "last-filled-workorder.txt"), FileMode.Create))
            {
                using (var s = new StreamWriter(f))
                {
                    s.WriteLine(workorderId);
                    s.Flush();
                    f.Flush(true);
                }
            }
        }

        public IEnumerable<FilledWorkorderAndResources> LoadFilledWorkordersByFilledDate(DateTime startUTC, DateTime endUTC)
        {
            return new FilledWorkorderAndResources[] { };
        }

        public IEnumerable<FilledWorkorderAndResources> LoadFilledWorkordersByDueDate(DateTime startUTC, DateTime endUTC)
        {
            return new FilledWorkorderAndResources[] { };
        }
    }
}
