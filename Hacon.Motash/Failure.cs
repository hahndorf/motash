using System;

namespace Hacon.Motash
{
    /// <summary>
    /// Information about a failed task
    /// </summary>
    public class Failure
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int Result { get; set; }
        public DateTime LastRun { get; set; }
    }
}
