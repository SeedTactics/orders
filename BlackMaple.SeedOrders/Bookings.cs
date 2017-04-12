using System;
using System.Collections.Generic;

namespace BlackMaple.SeedOrders
{

    /// <summary>A <c>BookingDemand</c> is an order for a single part and quantity and is held inside a <c>Booking</c>.</summary>
    public class BookingDemand
    {
        public string BookingId { get; set; }
        public string Part { get; set; }
        public int Quantity { get; set; }
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
    public class Booking
    {
        /// <summary>The unique identifier for a booking</summary>
        public string BookingId { get; set; }

        /// <summary>Bookings with larger integers are produced sooner (larger integers are higher priority).</summary>
        public int Priority { get; set; }

        /// <summary>When bookings have the same <c>Priority</c>, the due date is used to determine which to produce first.</summary>
        public DateTime DueDate { get; set; }

        ///<summary>The schedule id if this booking has been scheduled.  If the booking has not yet been scheduled, this is null</summary>
        public string ScheduleId { get; set; }

        public List<BookingDemand> Parts { get; set; }
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
    public class DownloadedPart
    {
        public string ScheduleId { get; set; }
        public string Part { get; set; }
        public int Quantity { get; set; }
    }

    ///<summary>A <c>Schedule</c> is a collection of part demand that has been downloaded into the machine controller.</summary>
    ///<remarks>
    ///   <para>
    ///   A schedule consists of the bookings completed by this schedule and the part quantities actually downloaded
    ///   into the machine controller.  Sometimes a <c>Booking</c> can't be scheduled all at once.  In that case, the <c>Booking</c>
    ///   itself will not be included in the <c>Schedule</c>.  Instead, some parts from the <c>Booking</c> will be included in the
    ///   downloaded parts and also recorded in a <c>ScheduledPartWithoutBooking</c>.
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
    public class Schedule
    {
        /// <summary>Each schedule is assigned a unique <c>Id</c> which is monotonically increasing.</summary>
        public string ScheduleId { get; set; }

        /// <summary>The time in coordinated universal time (UTC) that the schedule was created.</summary>
        public DateTime ScheduledTimeUTC { get; set; }

        /// <summary>The expected timespan to complete all demand for this schedule.</summary>
        public TimeSpan ScheduledHorizon { get; set; }

        /// <summary>The bookings which can finally be marked as completed.</summary>
        public List<Booking> Bookings { get; set; }

        /// <summary>The parts downloaded into the machine controller as part of this booking.</summary>
        public List<DownloadedPart> DownloadedParts { get; set; }
    }

    /// <summary>Records a part that has been scheduled into the system but not attached to a <c>Booking</c></summary>
    /// <remarks>
    ///   <para>
    ///   Typically, these parts arise when an entire <c>Booking</c> can't be scheduled all at once.
    ///   </para>
    /// </remarks>
    public class ScheduledPartWithoutBooking
    {
        public string Part { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    ///   Contains information about unscheduled bookings, scheduled parts, and pending transactions.
    /// </summary>
    public struct UnscheduledStatus
    {
        /// <summary>
        ///   All unscheduled bookings in the system.
        /// </summary>
        public IEnumerable<Booking> UnscheduledBookings;

        /// <summary>
        ///   All scheduled parts in the system.
        /// </summary>
        public IEnumerable<ScheduledPartWithoutBooking> ScheduledParts;

        /// <summary>
        ///   Maximum ScheduleId (sorted lexicographically) over all schedules.
        /// </summary>
        public string MaxScheduleId;
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
        UnscheduledStatus LoadUnscheduledStatus();

        /// <summary>
        ///   Mark the given bookings as scheduled and replace scheduled parts with the following parts.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Note that all existing scheduled parts should be deleted and only the scheduled parts appearing in this call
        ///     should be stored.
        ///   </para>
        /// </remarks>
        void CreateSchedule(string scheduleId
                           , DateTime scheduledTimeUTC
                           , TimeSpan scheduledHorizon
                           , IEnumerable<string> bookingIds
                           , IEnumerable<DownloadedPart> downloadedParts
                           , IEnumerable<ScheduledPartWithoutBooking> scheduledParts
                           );

        /// <summary>
        ///   Load history of all scheduled bookings with scheduled date between the given start and end time.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Implementing this is optional.  It is used only for reports and displaying data to the user on some screens.  It is
        ///     not required for the correct operation of the system.
        ///   </para>
        /// </remarks>
        IEnumerable<Schedule> LoadSchedulesByDate(DateTime startUTC, DateTime endUTC);
    }
}
