using System;
using HelloWorld_NetFramework_Attributes;

namespace HelloWorld_NetCore
{
    [HelloWorldAspect]
    static class Program
    {
        /// <summary>
        /// Output:
        /// 
        /// Entered method: Main
        /// Entered method: PrintHelloWorld
        /// Hello World
        /// Exited method: PrintHelloWorld
        /// Exited method: Main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            PrintHelloWorld();
        }

        private static void PrintHelloWorld()
        {
            Console.WriteLine("Hello World");
        }
    }
}