using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using BlackMaple.SeedOrders;

namespace tests
{
    public class SqliteWorkorderTest
    {
        private List<Workorder> initialWorkorders;

        public SqliteWorkorderTest()
        {
            using (var context = new ExampleOrderIntegration.WorkorderContext())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            initialWorkorders = new List<Workorder>();

            using (var context = new ExampleOrderIntegration.WorkorderContext())
            {
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

                foreach (var w in initialWorkorders) context.Workorders.Add(w);

                context.SaveChanges();
            }

        }

        [Fact]
        public void LoadUnfilled()
        {
            var workDB = new ExampleOrderIntegration.ExampleWorkorderDatabase();
            workDB.LoadUnfilledWorkorders()
              .ShouldAllBeEquivalentTo(initialWorkorders);

            workDB.LoadUnfilledWorkorders("part3")
              .ShouldAllBeEquivalentTo(new Workorder[] {initialWorkorders[2]});

            Assert.Null(workDB.LoadLastFilledWorkorderId());
        }

        [Fact]
        public void FillWorkorder()
        {
            var workDB = new ExampleOrderIntegration.ExampleWorkorderDatabase();
            workDB.MarkWorkorderAsFilled("work1", new DateTime(2016, 11, 05),
              new WorkorderResources {
                  Serials = new List<string> {"serial1", "serial2"}               
              });

            workDB.LoadUnfilledWorkorders()
              .ShouldAllBeEquivalentTo(initialWorkorders.GetRange(1, 2));
            workDB.LoadUnfilledWorkorders("part3")
              .ShouldAllBeEquivalentTo(new Workorder[] {initialWorkorders[2]});
            Assert.Equal("work1", workDB.LoadLastFilledWorkorderId());

            var filled = new FilledWorkorderAndResources
              { Workorder = initialWorkorders[0]};
            filled.Workorder.FilledUTC = new DateTime(2016, 11, 05);

            workDB.LoadFilledWorkordersByFilledDate(new DateTime(2016, 01, 01), new DateTime(2016, 12, 01))
              .ShouldAllBeEquivalentTo(new FilledWorkorderAndResources[] {filled});
            workDB.LoadFilledWorkordersByDueDate(new DateTime(2017, 01, 01), new DateTime(2018, 01, 01))
              .ShouldAllBeEquivalentTo(new FilledWorkorderAndResources[] {filled});
        }
    }
}
