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
                var f = Path.Combine(CSVBasePath, Path.Combine(FilledWorkordersPath, id + ".csv"));
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

            if (!Directory.Exists(Path.Combine(CSVBasePath, FilledWorkordersPath)))
            {
                Directory.CreateDirectory(Path.Combine(CSVBasePath, FilledWorkordersPath));
            }

            using (var f = File.OpenWrite(Path.Combine(CSVBasePath, Path.Combine(FilledWorkordersPath, workorderId + ".csv"))))
            {
                using (var stream = new StreamWriter(f))
                {
                    var csv = new CsvHelper.CsvWriter(stream);
                    csv.WriteField("CompletedTimeUTC");
                    csv.WriteField("ID");
                    csv.WriteField("Part");
                    csv.WriteField("Quantity");
                    csv.WriteField("Serials");

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
                    csv.WriteField(filledUTC.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    csv.WriteField(workorderId);
                    csv.WriteField(parts);
                    csv.WriteField(qtys);
                    csv.WriteField(string.Join(";", resources.Serials));

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
        }
    }
}
