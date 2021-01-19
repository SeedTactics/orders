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
    ///  Load just the unfilled workorders and programs.  A JSON-formatted LoadOrderResponse is written to standard output.
    ///</summary>
    public LoadOrders LoadWorkordersOnly { get; set; }

    ///<summary>
    ///  Mark the given bookings as scheduled and replace scheduled parts with the following parts.
    ///  No response on standard output.
    ///</summary>
    ///<remarks>
    ///  <para>
    ///    Note that all existing scheduled parts should be deleted and only the scheduled parts appearing in this call
    ///    should be stored.
    ///  </para>
    ///</remarks>
    public NewSchedule CreateSchedule { get; set; }

    /// <summary>
    ///  Backout scheduled but not yet produced parts. No response on standard output.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///    At the user's option, before creating a new schedule, any planned but not yet
    ///    started work can be removed from the cell controller.  When that happens,
    ///    this function will be called with the parts and quantities that were removed
    ///    from the cell controller.
    ///  </para>
    ///  <para>
    ///    When backing out of work, the first step is to try and find the bookings
    ///    that were recently scheduled and change them from scheduled to unscheduled.
    ///    The problem is that we are likely only backing out of partial quantities.
    ///    A better method is to first try and find any bookings with quantities smaller
    ///    than what we are backing out and change them from scheduled to unscheduled.
    ///    If there are still partial quantities left over, rather than trying to edit the
    ///    booking, leave the original bookings as scheduled and create a new booking.
    ///    For example, make a booking with a <c>BookingId</c> something such as
    ///    <c>Reschedule:[part]:[datetime]</c> and a high priority and due date.
    ///    Because you are adding a new booking exactly for the quantity removed,
    ///    the same number of parts will be produced as if you never backed out any work.
    ///    Thus workorders can be left unchanged and not even be aware that such a
    ///    reschedule took place.  For bookings which are changed from Scheduled to Unscheduled,
    ///    the ScheduleId should be left unchanged until the booking is scheduled again.
    ///  </para>
    ///  <para>
    ///    There are benifits and downsides to rescheduling, which is why it is a setting
    ///    to allow the user to choose if it happens at all.  Backing out of work does
    ///    complicate the booking process since new reschedule bookings have to be created
    ///    and managed.  Since workorders are unchanged and the reschedule bookings reproduce
    ///    exactly the parts removed, the quantities do work out in the end.  It is just extra
    ///    complexity that is not always needed, because OrderLink does a good job of estimating
    ///    the work for a single day so it is rare that significant quantities are backed out.
    ///  </para>
    ///  <para>
    ///    The main benifit is in the presense of machine downtime or other unforseen problems.
    ///    Say that a machine goes down for 10 hours.  Then there will be a significant backlog
    ///    of work at the end of the day so backing all of that work out and starting fresh
    ///    can allow for better optimization.  For example, consider that the work that was
    ///    supposed to be done today on the single machine which was down for 10 hours might need to
    ///    run on multiple machines tomorrow to be able to meet a due date.  By backing out
    ///    the work and starting fresh, OrderLink can make these calculations and better
    ///    optimize.  Backing out of work on the whole is a good idea to be able to respond
    ///    to uncertianty and recover from any issues that might arise.
    ///  </para>
    ///  <para>
    ///    Finally, each backout is assigned a unique increasing identifier.  This <c>BackoutId</c>
    ///    is primarily intended to prevent a backout from being recorded multiple times.  Therefore,
    ///    the <c>BackoutId</c> should be stored in the same database transaction that records the
    ///    backed out parts, and returned as part of the <c>UnscheduledStatus</c>.
    ///  </para>
    /// </remarks>
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
    ///List of program revisions and their content
    ///</summary>
    ///<remarks>
    /// <para>
    ///   Programs which appear in BookingDemands or WorkorderDemands which are not in this list are assumed
    ///   to already exist in the cell controller.  If programs are managed entirely in the
    ///   cell controller, this list can be empty or null.
    /// </para>
    ///</remarks>
    public IEnumerable<ProgramEntry> Programs { get; set; }

    ///<summary>
    ///If true, the plugin supports marking a workorder as filled.
    ///</summary>
    public bool? AllowWorkorderFilling { get; set; }
  }

}