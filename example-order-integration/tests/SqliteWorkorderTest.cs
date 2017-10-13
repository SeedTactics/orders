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
using FluentAssertions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
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

                foreach (var w in initialWorkorders) context.Workorders.Add(w);

                context.SaveChanges();
            }

        }

        private IEnumerable<Workorder> LoadFilledWorkorders()
        {
            using (var context = new ExampleOrderIntegration.WorkorderContext())
            {
                return context.Workorders
                  .Where(w => w.FilledUTC != null)
                  .Include(w => w.Parts)
                  .AsNoTracking()
                  .ToList();
            }
        }

        [Fact]
        public void LoadUnfilled()
        {
            var workDB = new ExampleOrderIntegration.ExampleWorkorderDatabase();
            workDB.LoadUnfilledWorkorders(50)
              .ShouldAllBeEquivalentTo(initialWorkorders);
            workDB.LoadUnfilledWorkorders(10)
              .ShouldAllBeEquivalentTo(new [] { initialWorkorders[0] });
            workDB.LoadUnfilledWorkorders("part3")
              .ShouldAllBeEquivalentTo(new Workorder[] {initialWorkorders[2]});
            workDB.LoadUnfilledWorkorders(-1)
              .ShouldAllBeEquivalentTo(initialWorkorders);
        }

        [Fact]
        public void FillWorkorder()
        {
            var workDB = new ExampleOrderIntegration.ExampleWorkorderDatabase();
            workDB.MarkWorkorderAsFilled("work1", new DateTime(2016, 11, 05),
              new WorkorderResources {
                  Serials = new List<string> {"serial1", "serial2"}               
              });

            workDB.LoadUnfilledWorkorders(50)
              .ShouldAllBeEquivalentTo(initialWorkorders.GetRange(1, 2));
            workDB.LoadUnfilledWorkorders("part3")
              .ShouldAllBeEquivalentTo(new Workorder[] {initialWorkorders[2]});

            initialWorkorders[0].FilledUTC = new DateTime(2016, 11, 05);

            LoadFilledWorkorders()
              .ShouldAllBeEquivalentTo(new [] {initialWorkorders[0]});
        }
    }
}
