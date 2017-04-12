using System;
using System.Collections.Generic;

namespace BlackMaple.SeedOrders
{
    /// <summary>A <c>WorkorderDemand</c> is an order for a single part and quantity and is held inside a <c>Workorder</c>.</summary>
    public class WorkorderDemand
    {
        public string WorkorderId { get; set; }
        public string Part { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    ///   A <c>Workorder</c> is used to track the output of the manufacturing system.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     A <c>Booking</c> is concerned with the demand input to the system while a <c>Workorder</c> focuses on tracking and
    ///     monitoring the output.  As parts are produced by the system, their serials are assigned to workorders.  This assignment
    ///     can be viewed in the log of system events and can either happen automatically or via operator input.  Once enough serials
    ///     have been assigned to the workorder, it is marked as filled.  The accumulation of data for all serials assigned to the workorder
    ///     is collected into the <c>FilledWorkorder</c>.
    ///   </para>
    ///   <para>
    ///     Typically, <c>Bookings</c> and <c>Workorders</c> are the same but they can differ when parts fail inspections or quantities
    ///     change.  Unfilled workorders can be edited at any time.
    ///   </para>
    /// </remarks>
    public class Workorder
    {
        ///<summary>The unique id for this workorder</summary>
        public string WorkorderId { get; set; }

        /// <summary>Workorders with larger integers are filled sooner (larger integers are higher priority).</summary>
        public int Priority { get; set; }

        /// <summary>When workorders have the same <c>Priority</c>, the due date is used to determine which to fill first.</summary>
        public DateTime DueDate { get; set; }

        public List<WorkorderDemand> Parts { get; set; }
    }

    /// <summary>
    ///   A <c>FilledWorkorder</c> summarizes the execution of the parts assigned to the workorder via the part serials.
    /// </summary>
    public class FilledWorkorder : Workorder
    {
        ///<summary>The time in coordinated universal time (UTC) that the final part was assigned to the workorder</summary>
        public DateTime FilledUTC { get; set; }

        ///<summary>The serials of all parts assigned to this workorder</summary>
        public List<string> Serials { get; set; }

        /// <summary>The actual times used by the parts in this workorder at each station</summary>
        /// <remarks>
        ///   <para>
        ///   The key of the dictionary is the station name, the value is the sum of the amount of time spent at this station
        ///   over all serials assigned to this workorder.
        ///   </para>
        /// </remarks>
        public Dictionary<string, TimeSpan> ActualOperationTimes { get; set; }

        /// <summary>The actual times used by the parts in this workorder at each station</summary>
        /// <remarks>
        ///   <para>
        ///   The key of the dictionary is the station name, the value is the sum of the amount of time spent at this station
        ///   over all serials assigned to this workorder.
        ///   </para>
        /// </remarks>
        public Dictionary<string, TimeSpan> PlannedOperationTimes { get; set; }
    }

    ///<summary>Contains the workorder with the largest FilledUTC of all workorders</summary>
    ///<remarks>
    ///  <para>
    ///    This is used to determine which workorders have been copied into the ERP system.  Once the operator
    ///    assigns the last part to a workorder and marks it filled, OrderLink records the workorder as filled
    ///    internally.  OrderLink then attempts to call <c>MarkWorkorderAsFilled</c> from the plugin.  If there
    ///    is an error, OrderLink will periodically attempt to recall <c>MarkWorkorderAsFilled</c> until the workorder
    ///    appears in <c>LastFilledWorkorder</c>.
    ///  </para>
    ///</remarks>
    public class LastFilledWorkorder
    {
        public string WorkorderId { get; set; }
        public DateTime FilledUTC { get; set; }
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

        /// <summary>Load the filled workorder with the largest FilledUTC</summary>
        LastFilledWorkorder LoadLastFilledWorkorder();

        /// <summary>
        ///   Mark the given workorder as filled.
        /// </summary>
        void MarkWorkorderAsFilled( string workorderId,
                                    DateTime fillUTC,
                                    IEnumerable<string> serials,
                                    IDictionary<string, TimeSpan> actualTime,
                                    IDictionary<string, TimeSpan> plannedTime
                                  );

        /// <summary>
        ///   Load history of all filled workorders with filled date between the given start and end time.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Implementing this is optional.  It is used only for reports and displaying data to the user on some screens.  It is
        ///     not required for the correct operation of the system.  If not all information about the <c>FilledWorkorder</c> is
        ///     stored, that is OK.  Load only what is stored.
        ///   </para>
        /// </remarks>
        IEnumerable<FilledWorkorder> LoadFilledWorkordersByFilledDate(DateTime startUTC, DateTime endUTC);

        /// <summary>
        ///   Load history of all filled workorders with due date between the given start and end time.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Implementing this is optional.  It is used only for reports and displaying data to the user on some screens.  It is
        ///     not required for the correct operation of the system.  If not all information about the <c>FilledWorkorder</c> is
        ///     stored, that is OK.  Load only what is stored.
        ///   </para>
        /// </remarks>
        IEnumerable<FilledWorkorder> LoadFilledWorkordersByDueDate(DateTime startUTC, DateTime endUTC);
    }

}
