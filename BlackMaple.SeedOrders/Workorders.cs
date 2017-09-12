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
    /// <summary>A <c>WorkorderDemand</c> is an order for a single part and quantity and is held inside a <c>Workorder</c>.</summary>
    [DataContract]
    public class WorkorderDemand
    {
        [DataMember]
        public string WorkorderId { get; set; }

        [DataMember]
        public string Part { get; set; }

        [DataMember]
        public int Quantity { get; set; }
    }

    /// <summary>
    ///   A <c>Workorder</c> is used to track the output of the manufacturing system.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     A <c>Booking</c> is concerned with the demand input to the system while a <c>Workorder</c> focuses on tracking and
    ///     monitoring the output.  As parts are produced by the system, their serials are assigned to workorders.  This assignment
    ///     can either happen automatically or via operator input in the SeedTactic: Tracking software.  Once enough serials
    ///     have been assigned to the workorder, it is marked as filled.  The accumulation of data for all serials assigned to the workorder
    ///     is collected into the <c>WorkorderResources</c>.
    ///   </para>
    ///   <para>
    ///     Typically, <c>Bookings</c> and <c>Workorders</c> are the same but they can differ when parts fail inspections or quantities
    ///     change.  Unfilled workorders can be edited at any time.
    ///   </para>
    /// </remarks>
    [DataContract]
    public class Workorder
    {
        ///<summary>The unique id for this workorder</summary>
        [DataMember]
        public string WorkorderId { get; set; }

        /// <summary>The due date is used as the primary means to determine which workorder to fill first.</summary>
        [DataMember]
        public DateTime DueDate { get; set; }

        /// <summary>Workorders with the same due date are sorted by priority (larger integers are higher priority).</summary>
        [DataMember]
        public int Priority { get; set; }

        /// <summary>The time in coordinated universal time (UTC) when the final part was assigned to this workorder</summary>
        /// <remarks>
        ///  <para>
        ///    This value is null while the workorder is unfilled, and only gets set once the workorder is completed and no more
        ///    parts should be assigned to it again.
        ///  </para>
        /// </remarks>
        [DataMember]
        public DateTime? FilledUTC { get; set; }

        ///<summary>The parts required to fill this workorder</summary>
        [DataMember]
        public List<WorkorderDemand> Parts { get; set; }
    }

    /// <summary>
    /// The resources used by a single part in a filled workorder
    /// </summary>
    [DataContract]
    public class WorkorderPartResources
    {
        [DataMember]
        public string Part { get; set; }

        [DataMember]
        public int PartsCompleted { get; set; }

        /// <summary>The elapsed wall clock time used by the parts in this workorder at each station</summary>
        /// <remarks>
        ///   <para>
        ///   The key of the dictionary is the station name, the value is the sum of the amount of time spent at this
        ///   station over all serials assigned to this workorder.  The time is wall-clock time from cycle start
        ///   to cycle end, so includes time that the station is idle (for example, if the program is interrupted).
        ///   </para>
        /// </remarks>
        [DataMember]
        public Dictionary<string, TimeSpan> ElapsedOperationTime { get; set; }

        /// <summary>The active time used by the parts in this workorder at each station</summary>
        /// <remarks>
        ///   <para>
        ///   The key of the dictionary is the station name, the value is the amount of time
        ///   that a part spends at this station with some active operation occuring, summed over
        ///   all serials assigned to this workorder.  The active time will be smaller than the elapsed time
        ///   if for example the program is interrupted or some other interruption occurs.
        ///   </para>
        /// </remarks>
        [DataMember]
        public Dictionary<string, TimeSpan> ActiveOperationTime { get; set; }
    }

    /// <summary>
    ///   A <c>WorkorderResources</c> summarizes the execution of the parts assigned to the workorder.
    /// </summary>
    [DataContract]
    public class WorkorderResources
    {
        ///<summary>The serials of all parts assigned to this workorder</summary>
        [DataMember]
        public List<string> Serials { get; set; }

        ///<summary>The completed counts and operation times for parts assigned to this workorder</summary>
        [DataMember]
        public List<WorkorderPartResources> Parts { get; set; }
    }

    /// <summary>
    ///   The main interface to interact with workorders.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Each order plugin must create a class which implements this interface.  Our program will instantiate the class once
    ///     and re-use it for all operations.  Each operation should interact with the ERP database in a single transaction.
    ///   </para>
    /// </remarks>
    public interface IWorkorderDatabase
    {
        /// <summary>
        ///   Load all unfilled workorders.
        /// </summary>
        IEnumerable<Workorder> LoadUnfilledWorkorders();

        /// <summary>
        ///   Load all unfilled workorders for the given part.
        /// </summary>
        IEnumerable<Workorder> LoadUnfilledWorkorders(string part);

        /// <summary>
        ///   Mark the given workorder as filled.
        /// </summary>
        void MarkWorkorderAsFilled( string workorderId,
                                    DateTime fillUTC,
                                    WorkorderResources resources
                                  );
    }

}
