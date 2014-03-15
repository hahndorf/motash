using System;

namespace Hacon.Motash
{
    /// <summary>
    /// Information about a failed task
    /// </summary>
    public class Failure
    {
        /// <summary>
        /// Name of the task
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Full path of the task in the tree, including the name
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// The numeric return value
        /// </summary>
        public int Result { get; set; }
        /// <summary>
        /// The date/time when the task was last running
        /// </summary>
        public DateTime LastRun { get; set; }
    }
}
