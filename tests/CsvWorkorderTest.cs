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
using System.IO;
using FluentAssertions;
using Xunit;
using BlackMaple.SeedOrders;

namespace tests
{
    public class CsvWorkorderTest
    {
        private List<Workorder> initialWorkorders;

        public CsvWorkorderTest()
        {
            if (Directory.Exists("filled-workorders"))
                Directory.Delete("filled-workorders", true);
            if (File.Exists("last-filled-workorder.txt"))
                File.Delete("last-filled-workorder.txt");
            initialWorkorders = new List<Workorder>();

            initialWorkorders.Add(new Workorder
            {
                WorkorderId = "work1",
                Priority = 100,
                DueDate = new DateTime(2017, 01, 01),
                Parts = new List<WorkorderDemand> {
                        new WorkorderDemand { WorkorderId = "work1", Part = "part1", Quantity = 44},
                        new WorkorderDemand { WorkorderId = "work1", Part = "part2", Quantity = 66}
                     }
            });
            initialWorkorders.Add(new Workorder
            {
                WorkorderId = "work2",
                Priority = 200,
                DueDate = new DateTime(2017, 02, 02),
                Parts = new List<WorkorderDemand> {
                        new WorkorderDemand { WorkorderId = "work2", Part = "part1", Quantity = 55},
                        new WorkorderDemand { WorkorderId = "work2", Part = "part2", Quantity = 77}
                     }
            });
            initialWorkorders.Add(new Workorder
            {
                WorkorderId = "work3",
                Priority = 300,
                DueDate = new DateTime(2017, 03, 03),
                Parts = new List<WorkorderDemand> {
                        new WorkorderDemand { WorkorderId = "work3", Part = "part1", Quantity = 111},
                        new WorkorderDemand { WorkorderId = "work3", Part = "part3", Quantity = 222}
                     }
            });

            using (var f = File.Open("unscheduled-workorders.csv", FileMode.Create))
            {
                using (var s = new StreamWriter(f))
                {
                    s.WriteLine("Id,DueDate,Priority,Part,Quantity");
                    foreach (var w in initialWorkorders)
                    {
                        foreach (var p in w.Parts) {
                            s.WriteLine(w.WorkorderId + "," 
                              + w.DueDate.ToString("yyyy-MM-dd") + ","
                              + w.Priority.ToString() + ","
                              + p.Part + "," + p.Quantity.ToString());
                        }
                    }
                }
            }

        }

        [Fact]
        public void LoadUnfilled()
        {
            var workDB = new BlackMaple.CSVOrders.WorkorderCSV();
            workDB.LoadUnfilledWorkorders()
              .ShouldAllBeEquivalentTo(initialWorkorders);

            workDB.LoadUnfilledWorkorders("part3")
              .ShouldAllBeEquivalentTo(new Workorder[] { initialWorkorders[2] });

            Assert.Null(workDB.LoadLastFilledWorkorderId());
        }

        [Fact]
        public void FillWorkorder()
        {
            var workDB = new BlackMaple.CSVOrders.WorkorderCSV();
            workDB.MarkWorkorderAsFilled("work1", new DateTime(2016, 11, 05),
              new WorkorderResources
              {
                  Serials = new List<string> { "serial1", "serial2" },
                  ActualOperationTimes = new Dictionary<string, TimeSpan>
                    {
                        { "stat1", TimeSpan.FromMinutes(15)},
                        { "stat2", TimeSpan.FromMinutes(20)}
                    },
                    PlannedOperationTimes = new Dictionary<string, TimeSpan>
                    {
                        { "stat1", TimeSpan.FromMinutes(105)},
                        { "stat2", TimeSpan.FromMinutes(200)}
                    }
              });

            workDB.LoadUnfilledWorkorders()
              .ShouldAllBeEquivalentTo(initialWorkorders.GetRange(1, 2));
            workDB.LoadUnfilledWorkorders("part3")
              .ShouldAllBeEquivalentTo(new Workorder[] { initialWorkorders[2] });
            Assert.Equal("work1", workDB.LoadLastFilledWorkorderId());

            var lines = File.ReadAllLines("filled-workorders/work1.csv");
            Assert.Equal(2, lines.Count());
            Assert.Equal("CompletedTimeUTC,ID,Part,Quantity,Actual stat1 (minutes),Actual stat2 (minutes),Planned stat1 (minutes),Planned stat2 (minutes)", lines[0]);
            
            //only check date, not time
            int idx = lines[1].IndexOf(",");
            Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM-ddT"), lines[1].Substring(0, 11));
            Assert.Equal("work1,part1;part2,44;66,15,20,105,200", lines[1].Substring(idx+1));
        }
    }
}
