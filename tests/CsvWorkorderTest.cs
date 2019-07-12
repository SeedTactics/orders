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
      initialWorkorders = new List<Workorder>();

      initialWorkorders.Add(new Workorder
      {
        WorkorderId = "work1",
        Priority = 100,
        DueDate = DateTime.Today.AddDays(5),
        Parts = new List<WorkorderDemand> {
                        new WorkorderDemand { WorkorderId = "work1", Part = "part1", Quantity = 44},
                        new WorkorderDemand { WorkorderId = "work1", Part = "part2", Quantity = 66}
                     }
      });
      initialWorkorders.Add(new Workorder
      {
        WorkorderId = "work2",
        Priority = 200,
        DueDate = DateTime.Today.AddDays(15),
        Parts = new List<WorkorderDemand> {
                        new WorkorderDemand { WorkorderId = "work2", Part = "part1", Quantity = 55},
                        new WorkorderDemand { WorkorderId = "work2", Part = "part2", Quantity = 77}
                     }
      });
      initialWorkorders.Add(new Workorder
      {
        WorkorderId = "work3",
        Priority = 300,
        DueDate = DateTime.Today.AddDays(30),
        Parts = new List<WorkorderDemand> {
                        new WorkorderDemand { WorkorderId = "work3", Part = "part1", Quantity = 111},
                        new WorkorderDemand { WorkorderId = "work3", Part = "part3", Quantity = 222}
                     }
      });

      using (var f = File.Open("workorders.csv", FileMode.Create))
      {
        using (var s = new StreamWriter(f))
        {
          s.WriteLine("Id,DueDate,Priority,Part,Quantity");
          foreach (var w in initialWorkorders)
          {
            foreach (var p in w.Parts)
            {
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
      workDB.LoadUnfilledWorkorders(30)
        .ShouldAllBeEquivalentTo(initialWorkorders);
      workDB.LoadUnfilledWorkorders(10)
        .ShouldAllBeEquivalentTo(new[] { initialWorkorders[0] });
      workDB.LoadUnfilledWorkorders("part3")
        .ShouldAllBeEquivalentTo(new[] { initialWorkorders[2] });
      workDB.LoadUnfilledWorkorders(-1)
        .ShouldAllBeEquivalentTo(initialWorkorders);
    }

    [Fact]
    public void FillWorkorder()
    {
      var workDB = new BlackMaple.CSVOrders.WorkorderCSV();
      workDB.MarkWorkorderAsFilled("work1", new DateTime(2016, 11, 05, 3, 44, 52, DateTimeKind.Utc),
        new WorkorderResources
        {
          Serials = new List<string> { "serial1", "serial2" },
          Parts = new List<WorkorderPartResources>
          {
                    new WorkorderPartResources
                      {
                        Part = "part1",
                        PartsCompleted = 44,
                        ActiveOperationTime = new Dictionary<string, TimeSpan>
                        {
                            { "stat1", TimeSpan.FromMinutes(105)},
                            { "stat2", TimeSpan.FromMinutes(107)}
                        },
                        ElapsedOperationTime = new Dictionary<string, TimeSpan>
                        {
                            { "stat1", TimeSpan.FromMinutes(201)},
                            { "stat2", TimeSpan.FromMinutes(210)},
                            { "stat3", TimeSpan.FromMinutes(222)}
                        }
                      },
                    new WorkorderPartResources
                      {
                        Part = "part2",
                        PartsCompleted = 55,
                        ActiveOperationTime = new Dictionary<string, TimeSpan>
                        {
                            { "stat1", TimeSpan.FromMinutes(301)},
                            { "stat2", TimeSpan.FromMinutes(311)},
                            { "stat3", TimeSpan.FromMinutes(333)}
                        },
                        ElapsedOperationTime = new Dictionary<string, TimeSpan>
                        {
                            { "stat1", TimeSpan.FromMinutes(422)},
                            { "stat2", TimeSpan.FromMinutes(462)}
                        }
                      }
          }
        });

      workDB.LoadUnfilledWorkorders(50)
        .ShouldAllBeEquivalentTo(initialWorkorders.GetRange(1, 2));
      workDB.LoadUnfilledWorkorders("part3")
        .ShouldAllBeEquivalentTo(new Workorder[] { initialWorkorders[2] });

      var lines = File.ReadAllLines("filled-workorders/work1.csv");
      Assert.Equal(3, lines.Count());
      Assert.Equal("CompletedTimeUTC,ID,Part,Quantity,Serials,Active stat1 (minutes),Active stat2 (minutes),Active stat3 (minutes),Elapsed stat1 (minutes),Elapsed stat2 (minutes),Elapsed stat3 (minutes)", lines[0]);
      Assert.Equal("2016-11-05T03:44:52Z,work1,part1,44,serial1;serial2,105,107,0,201,210,222", lines[1]);
      Assert.Equal("2016-11-05T03:44:52Z,work1,part2,55,serial1;serial2,301,311,333,422,462,0", lines[2]);
    }
  }
  public class CsvCreateWorkorderTest
  {
    [Fact]
    public void CreateSampleWorkorders()
    {
      var workFile = System.IO.Path.Combine("create-work-test", "workorders.csv");
      if (!System.IO.Directory.Exists("create-work-test"))
        System.IO.Directory.CreateDirectory("create-work-test");
      if (System.IO.File.Exists(workFile))
        System.IO.File.Delete(workFile);
      var booking = new BlackMaple.CSVOrders.WorkorderCSV();
      booking.CSVBasePath = "create-work-test";
      booking.LoadUnfilledWorkorders(1);

      var b = File.ReadAllLines(workFile);

      b.ShouldAllBeEquivalentTo(new string[] {
        "Id,DueDate,Priority,Part,Quantity",
        "12345," + DateTime.Today.AddDays(10).ToString("yyyy-MM-dd") + ",100,part1,50",
        "98765," + DateTime.Today.AddDays(12).ToString("yyyy-MM-dd") + ",100,part2,77"
      });

    }
  }
}
