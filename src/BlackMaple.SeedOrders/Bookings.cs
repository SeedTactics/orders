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
using System.Runtime.Serialization;

namespace BlackMaple.SeedOrders
{

  /// <summary>A <c>MainProgram</c> contains the program information used to cut a single process on one machine.</summary>
  [DataContract]
  public class MainProgram
  {
    /// <summary>Identifies the process on the part that this program is for.</summary>
    [DataMember]
    public int ProcessNumber { get; set; }

    /// <summary>Identifies which machine stop on the part that this program is for (only needed if a process has multiple
    /// machining stops before unload).</summary>
    [DataMember]
    public string MachineGroup { get; set; }

    /// <summary>The program name, used to find the program contents.</summary>
    [DataMember]
    public string ProgramName { get; set; }

    ///<summary>The program revision to run.  If zero or not specified, the most recent program revision is used.</summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    public long? Revision { get; set; }
  }

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

    ///<summary>The programs to run. If not given, the programs are assumed to be defined in the
    /// flexibility plan and already loaded in the machine.</summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    public IEnumerable<MainProgram> Programs { get; set; }
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

  [DataContract]
  public class ProgramEntry
  {
    [DataMember]
    public string ProgramName { get; set; }
    [DataMember]
    public long? Revision { get; set; }
    [DataMember]
    public string Comment { get; set; }
    [DataMember]
    public string ProgramContent { get; set; }
  }

  /// <summary>
  ///   The data passed in when creating a new schedule
  /// </summary>
  [DataContract]
  public class NewSchedule
  {
    [DataMember]
    public string ScheduleId { get; set; }

    [DataMember]
    public DateTime ScheduledTimeUTC { get; set; }

    [DataMember]
    public TimeSpan ScheduledHorizon { get; set; }

    [DataMember]
    public List<string> BookingIds { get; set; }

    [DataMember]
    public List<DownloadedPart> DownloadedParts { get; set; }

    [DataMember]
    public List<ScheduledPartWithoutBooking> ScheduledParts { get; set; }
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

  ///<summary>The backout created by OrderLink which should be stored into the database.</summary>
  public class Backout
  {
    public long BackoutId { get; set; }
    public List<BackedOutPart> Parts { get; set; }
  }
}
