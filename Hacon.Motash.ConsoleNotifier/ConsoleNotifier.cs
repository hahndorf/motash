using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Hacon.Motash
{
    [Export(typeof(Motash.INotifier))]
    public class ConsoleNotifier : Motash.INotifier
    {
        /// <summary>
        /// Output the failures to the console
        /// </summary>
        public void Send(List<Failure> failures)
        {
            if (failures.Count > 0)
            {
                if (failures.Count == 1)
                {
                    Console.WriteLine("The following task executed with an unexpected return value:");
                }
                else
                {
                    Console.WriteLine("The following task(s) executed with an unexpected return value:");
                }
                
                Console.WriteLine("--------------------------------------------------------------");
                Console.WriteLine(Checker.FailuresAsText(failures, "{0} Result: {1} at: {2}"));
            }
            else
            {
                Console.WriteLine("No problems found");
            }
        }
    }
}