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
