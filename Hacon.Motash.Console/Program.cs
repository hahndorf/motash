using System.Reflection;

namespace Hacon.Motash.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            Checker chk = new Checker();
            chk.Check();
            chk.Notify();

            //if (problems > 0)
            //{
            //    System.Console.WriteLine("Failed tasks: ");
            //    System.Console.WriteLine(Checker.FailuresAsText(chk.Failures));
            //    //   chk.EmailReport();
            //    System.Console.WriteLine("Email sent");
            //}
            //else
            //{
            //    System.Console.WriteLine("No problems found");
            //}
        }
    }
}