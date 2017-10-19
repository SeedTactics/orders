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
            var path = Path.Combine(CSVBasePath, "workorders.csv");
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

        public IEnumerable<Workorder> LoadUnfilledWorkorders(int lookaheadDays)
        {
            if (lookaheadDays > 0)
            {
                var endDate = DateTime.Today.AddDays(lookaheadDays);
                return LoadUnfilledWorkordersMap()
                    .Values
                    .Where(x => x.DueDate <= endDate);
            }
            else
            {
                return LoadUnfilledWorkordersMap().Values;
            }
        }

        public IEnumerable<Workorder> LoadUnfilledWorkorders(string part)
        {
            return LoadUnfilledWorkordersMap()
                .Values
                .Where(w => w.Parts.Any(p => p.Part == part));
        }

        public void MarkWorkorderAsFilled(string workorderId,
                                          DateTime filledUTC,
                                          WorkorderResources resources)
        {
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

                    var activeStations = new HashSet<string>();
                    var elapsedStations = new HashSet<string>();
                    foreach (var p in resources.Parts)
                    {
                        foreach (var k in p.ActiveOperationTime.Keys)
                            activeStations.Add(k);
                        foreach (var k in p.ElapsedOperationTime.Keys)
                            elapsedStations.Add(k);
                    }

                    var actualKeys = activeStations.OrderBy(x => x).ToList();
                    foreach (var k in actualKeys)
                    {
                        csv.WriteField("Active " + k + " (minutes)");
                    }
                    var plannedKeys = elapsedStations.OrderBy(x => x).ToList();
                    foreach (var k in plannedKeys)
                    {
                        csv.WriteField("Elapsed " + k + " (minutes)");
                    }
                    csv.NextRecord();

                    foreach (var p in resources.Parts)
                    {
                        csv.WriteField(filledUTC.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        csv.WriteField(workorderId);
                        csv.WriteField(p.Part);
                        csv.WriteField(p.PartsCompleted);
                        csv.WriteField(string.Join(";", resources.Serials));

                        foreach (var k in actualKeys)
                        {
                            if (p.ActiveOperationTime.ContainsKey(k))
                                csv.WriteField(p.ActiveOperationTime[k].TotalMinutes);
                            else
                                csv.WriteField(0);
                        }
                        foreach (var k in plannedKeys)
                        {
                            if (p.ElapsedOperationTime.ContainsKey(k))
                                csv.WriteField(p.ElapsedOperationTime[k].TotalMinutes);
                            else
                                csv.WriteField(0);
                        }
                        csv.NextRecord();
                    }
                }
            }
        }
    }
}