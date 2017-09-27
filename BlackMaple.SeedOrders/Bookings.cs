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
using System.Runtime.Serialization;

namespace BlackMaple.SeedOrders
{

    /// <summary>A <c>BookingDemand</c> is an order for a single part and quantity and is held inside a <c>Booking</c>.</summary>
    [DataContract]
    public class BookingDemand
    {
        [DataMember]
        public string BookingId { get; set; }

        [DataMember]
        public string Part { get; set; }

        [DataMember]
        public int Quantity { get; set; }

        ///<summary>The casting used by this part. Can be null to ignore castings and
        ///just assume castings are always available.</summary>
        [DataMember]
        public string CastingId { get; set; }
    }

    /// <summary>A <c>Booking</c> is an order used for scheduling.</summary>
    /// <remarks>
    ///   <para>
    ///     A booking indicates a demand that parts be produced by the system.  The demand must be immutable; once added the parts
    ///     and quantities to produce must never change (the priority and due date can change).  If the quantities for an order do
    ///     change, do not edit the booking; instead create a new booking which contains the extra parts to produce.
    ///   </para>
    ///   <para>
    ///     Most of the time, <c>Bookings</c> and <c>Workorders</c> are in one-to-one correspondence.  One example where they can differ
    ///     is when a part fails inspection and a new booking containing a single part might be created. Another example is when
    ///     part quantities change on the order, where the workorder is updated but a new booking with the change in demand in created.
    ///   </para>
    /// </remarks>
    [DataContract]
    public class Booking
    {
        /// <summary>The unique identifier for a booking</summary>
        [DataMember]
        public string BookingId { get; set; }

        /// <summary>The due date is used as the primary means to determine which booking to produce first.</summary>
        [DataMember]
        public DateTime DueDate { get; set; }

        /// <summary>Bookings with the same due date are sorted by priority (larger integers are higher priority).</summary>
        [DataMember]
        public int Priority { get; set; }

        ///<summary>The schedule id if this booking has been scheduled.  If the booking has not yet been scheduled, this is null</summary>
        [DataMember]
        public string ScheduleId { get; set; }

        ///<summary>The parts to produce for this booking</summary>
        [DataMember]
        public List<BookingDemand> Parts { get; set; }
    }

    /// <summary>
    ///  Represents a raw material/casting type with its available material.
    /// </summary>
    [DataContract]
    public class Casting
    {
        [DataMember]
        public string CastingId { get; set; }

        [DataMember]
        public int Quantity { get; set; }
    }

    /// <summary>
    ///   A <c>DownloadedPart</c> records the parts and quantities that were downloaded into the machine controller as part of a <c>Schedule</c>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Sometimes a <c>Booking</c> can't be scheduled all at once because there isn't enough capacity to fully complete the booking
    ///     but there is still some unused capacity.  In this case, the <c>Booking</c> will be split and produced over multiple schedules.
    ///     For that reason, the collection of <c>Booking</c>s included in a schedule might not reflect the part quantities downloaded into
    ///     the machine controller.  The list of <c>DownloadedPart</c>s included in a booking therefore shows exactly the quantities sent
    ///     to the machine controller.
    ///   </para>
    /// </remarks>
    [DataContract]
    public class DownloadedPart
    {
        [DataMember]
        public string ScheduleId { get; set; }

        [DataMember]
        public string Part { get; set; }

        [DataMember]
        public int Quantity { get; set; }
    }

    /// <summary>Records a part that has been scheduled into the system but not attached to a <c>Booking</c></summary>
    /// <remarks>
    ///   <para>
    ///   Sometimes a <c>Booking</c> can't be scheduled all at once.  When this is the case, the <c>Booking</c>
    ///   will not be included in the first schedule so the booking will be left in the unscheduled state.
    ///   Instead, some parts from the <c>Booking</c> will be included in the downloaded parts and also recorded in
    ///   a <c>ScheduledPartWithoutBooking</c>.  When the next schedule is generated, the remaining parts will be
    ///   produced and the booking will be marked scheduled at that time.
    ///   </para>
    ///   <para>
    ///   For example, if there is a booking for 100 of part ABC but there is only capacity for 40, the booking will be left unscheduled,
    ///   40 ABC parts will be included in the downloaded parts, and a <c>ScheduledPartWithoutBooking</c> will be created to keep track of the
    ///   40 ABC parts.  When deciding on a future schedule, OrderLink will see the 40 ABC parts from <c>ScheduledPartWithoutBooking</c> and
    ///   realize that only 60 parts of ABC need to be produced.  This future schedule will therefore have 60 ABC parts to download, include
    ///   the booking as scheduled, and delete the <c>ScheduledPartWithoutBooking</c>.  OrderLink handles all this calculation internally,
    ///   so when writing the interface between OrderLink and your ERP system, you just need to store and retrieve this data.
    ///   </para>
    /// </remarks>
    [DataContract]
    public class ScheduledPartWithoutBooking
    {
        [DataMember]
        public string Part { get; set; }

        [DataMember]
        public int Quantity { get; set; }
    }

    /// <summary>Contains information about unscheduled bookings, scheduled parts, and pending transactions.</summary>
    /// <remarks>
    ///  <para>
    ///    This type is used only to return multiple data at once about unscheduled bookings
    ///  </para>
    /// </remarks>
    [DataContract]
    public class UnscheduledStatus
    {
        /// <summary>
        ///   All unscheduled bookings in the system.
        /// </summary>
        [DataMember]
        public IEnumerable<Booking> UnscheduledBookings {get;set;}

        /// <summary>
        ///   All scheduled parts in the system.
        /// </summary>
        [DataMember]
        public IEnumerable<ScheduledPartWithoutBooking> ScheduledParts {get;set;}

        ///<summary>
        ///  The latest backout id, which is used to prevent a backout from being recorded multiple times
        ///</summary>
        ///<remarks>
        ///  <para>
        ///  Can be null if backouts are not used.  See the documentation on <c>HandleBackedOutWork</c>
        ///  for more details.
        ///  </para>
        ///</remarks>
        [DataMember]
        public string LatestBackoutId {get;set;}

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
        [DataMember]
        public IEnumerable<Casting> Castings {get;set;}
    }

    /// <summary>
    ///   The data passed in when creating a new schedule
    /// </summary>
    [DataContract]
    public class NewSchedule
    {
        [DataMember]
        public string ScheduleId {get; set;}

        [DataMember]
        public DateTime ScheduledTimeUTC {get;set;}

        [DataMember]
        public TimeSpan ScheduledHorizon {get;set;}

        [DataMember]
        public List<string> BookingIds {get;set;}

        [DataMember]
        public List<DownloadedPart> DownloadedParts {get;set;}

        [DataMember]
        public List<ScheduledPartWithoutBooking> ScheduledParts {get;set;}
    }

    /// <summary>Records a part and quantity that was removed from the cell controller</summary>
    [DataContract]
    public class BackedOutPart
    {
        [DataMember]
        public string Part { get; set; }

        [DataMember]
        public int Quantity { get; set; }
    }

    /// <summary>
    ///   The main interface to interact with bookings.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Each order plugin must create a class which implements this interface.  Our program will instantiate the class once
    ///     and re-use it for all operations.  Each operation should interact with the ERP database in a single transaction.
    ///   </para>
    /// </remarks>
    public interface IBookingDatabase
    {
        /// <summary>
        ///   Load all information about the unscheduled status.
        /// </summary>
        UnscheduledStatus LoadUnscheduledStatus(int lookaheadDays);

        /// <summary>
        ///   Mark the given bookings as scheduled and replace scheduled parts with the following parts.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Note that all existing scheduled parts should be deleted and only the scheduled parts appearing in this call
        ///     should be stored.
        ///   </para>
        /// </remarks>
        void CreateSchedule(NewSchedule newData);

        /// <summary>
        ///  Deal with scheduled parts that are removed from the cell controller.
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
        void HandleBackedOutWork(string backoutId, IEnumerable<BackedOutPart> backedOutParts);
    }
}
