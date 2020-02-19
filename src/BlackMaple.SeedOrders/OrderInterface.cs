/* Copyright (c) 2020, John Lenz

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

namespace BlackMaple.SeedOrders
{
  public class LoadOrders
  {
    public int? LookaheadDays { get; set; }
  }

  /// <summary>
  ///   A JSON-formatted OrderRequest is written to the plugin on standard input.
  //    Only a single member will be non-null to specify the request, all others will
  //    be null.
  /// </summary>
  public class OrderRequest
  {
    ///<summary>
    ///  Load bookings, workorders, backouts, castings, programs, etc.  A JSON-formatted LoadOrderResponse is
    ///  written to standard output.
    ///</summary>
    public LoadOrders LoadAll { get; set; }

    ///<summary>
    ///  Load just the unfilled workorders.  A JSON-formatted LoadOrderResponse is written to standard output.
    ///</summary>
    public LoadOrders LoadWorkordersOnly { get; set; }

    ///<summary>
    ///  Create a schedule. No response on standard output.
    ///</summary>
    public NewSchedule CreateSchedule { get; set; }

    ///<summary>
    ///  Backout scheduled but not yet produced parts.null  No response on standard output.
    ///</summary>
    public Backout BackoutWork { get; set; }

    ///<summary>
    ///  Mark a workorder as filled.  No response on standard output.
    ///</summary>
    public FilledWorkorder MarkWorkorderFilled { get; set; }
  }

  public class LoadOrderResponse
  {
    /// <summary>
    ///   All unscheduled bookings in the system.
    /// </summary>
    public IEnumerable<Booking> UnscheduledBookings { get; set; }

    /// <summary>
    ///   All scheduled parts in the system.
    /// </summary>
    public IEnumerable<ScheduledPartWithoutBooking> ScheduledParts { get; set; }

    /// <summary>
    ///   All unfilled workorders in the system.
    /// </summary>
    public IEnumerable<Workorder> UnfilledWorkorders { get; set; }

    ///<summary>
    ///  The latest backout id, which is used to prevent a backout from being recorded multiple times
    ///</summary>
    ///<remarks>
    ///  <para>
    ///  Can be null if backouts are not used.  See the documentation on <c>HandleBackedOutWork</c>
    ///  for more details.
    ///  </para>
    ///</remarks>
    public long? LatestBackoutId { get; set; }

    ///<summary>
    ///List of castings and quantities.
    ///</summary>
    ///<remarks>
    /// <para>
    ///   Castings restrict the available orders which are examined during daily schedule
    ///   generation, because only parts that have available castings will be scheduled.
    ///   If castings are always available, they can be ignored by leaving the
    ///   <c>CastingId</c> in <c>BookingDemand</c> null, and not including any casting
    ///   data here.
    /// </para>
    ///</remarks>
    public IEnumerable<Casting> Castings { get; set; }

    ///<summary>
    ///List of program revisions and their content
    ///</summary>
    ///<remarks>
    /// <para>
    ///   Programs which appear in BookingDemands which are not in this list are assumed
    ///   to already exist in the cell controller.  If programs are managed entirely in the
    ///   cell controller, this list can be empty or null.
    /// </para>
    ///</remarks>
    public IEnumerable<ProgramEntry> Programs { get; set; }
  }

}