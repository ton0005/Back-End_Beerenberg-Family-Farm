using System;

namespace FarmManagement.Application.Configuration
{
    /// <summary>
    /// Payroll system constants for status values and rate types.
    /// 
    /// NOTE: Runtime payroll configuration (pay frequency, thresholds, rates, etc.) 
    /// is managed via the PayrollOptions table (seeded from payroll_options_seed.json)
    /// and accessed through IPayrollOptionsProvider. Hourly rates are stored in the
    /// PayRates table (seeded from payroll_seed.json).
    /// </summary>
    public static class PayrollSettings
    {
        /// <summary>
        /// Payroll run status values
        /// </summary>
        public static class PayrollRunStatus
        {
            public const string Draft = "Draft";
            public const string Pending = "Pending";
            public const string Approved = "Approved";
            public const string Paid = "Paid";
            public const string Cancelled = "Cancelled";
        }

        /// <summary>
        /// Pay calendar status values
        /// </summary>
        public static class PayCalendarStatus
        {
            public const string Active = "Active";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        /// <summary>
        /// Rate types for PayRate entity
        /// </summary>
        public static class RateTypes
        {
            public const string Regular = "Regular";
            public const string Overtime = "Overtime";
            public const string Weekend = "Weekend";
            public const string PublicHoliday = "PublicHoliday";
        }
    }
}
