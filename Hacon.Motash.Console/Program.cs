using System.Reflection;

namespace Hacon.Motash.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            Checker chk = new Checker();
            int problems = chk.Check();
            if (problems > 0)
            {
                System.Console.WriteLine("Failed tasks: ");
                System.Console.WriteLine(chk.ProblemText);
                chk.EmailReport();
                System.Console.WriteLine("Email sent");
            }
        }
    }
}