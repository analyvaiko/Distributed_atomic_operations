using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Distributed_atomic_operations
{
    internal class Program
    {
        static void Main(string[] args)
        {


            //Initialization
            Mutex mut = new Mutex();
            const int numThreads = 8;
            int len = 100;
            int range = 1000;
            Stopwatch sw = Stopwatch.StartNew();
            int[] arr = null;
            int sum = 0;


            void InitArray (int size)
            {
                Random rnd = new Random();
                arr = new int[size];
                for (int i = 0; i < len; i++)
                {
                    arr[i] = rnd.Next(range);
                }
            }

            // Straight forward sum

            void SumArrMod2(int[] a)
            {

                for (int i = 0; i < len; i++)
                {
                    if (a[i] % 2 != 0)
                    {
                        sum = sum ^ a[i];
                    }
                }
 
            }

            // Mutex usage
            void SumArrMod2WithLock(int[] a, int start, int portion)
            {

                for (int i = start; i < len; i += portion)
                {
                    if (a[i] % 2 != 0)
                    {
                        mut.WaitOne();
                        sum = sum ^ a[i];
                        mut.ReleaseMutex();
                    }
                }
            }

            // Compare and exchange usage
            void SumArrMod2WithCAS(int[] a, int start, int portion)
            {

                for (int i = start; i < len; i += portion)
                {
                    if (a[i] % 2 != 0)
                    {
                        sum = AddToTotal(a[i]);
                    }
                }
            }

            /* 
             * C# InterlockedCompareExchange
             * https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked.compareexchange?view=net-8.0
             */

            int AddToTotal(int addend)
            {
                int initialValue, computedValue;
                do
                {
                    // Save the current running total in a local variable.
                    initialValue = sum;

                    // Add the new value to the running total.
                    computedValue = initialValue ^ addend;

                    // CompareExchange compares totalValue to initialValue. If
                    // they are not equal, then another thread has updated the
                    // running total since this loop started. CompareExchange
                    // does not update totalValue. CompareExchange returns the
                    // contents of totalValue, which do not equal initialValue,
                    // so the loop executes again.
                } while (initialValue != Interlocked.CompareExchange(
                    ref sum, computedValue, initialValue));
                // If no other thread updated the running total, then 
                // totalValue and initialValue are equal when CompareExchange
                // compares them, and computedValue is stored in totalValue.
                // CompareExchange returns the value that was in totalValue
                // before the update, which is equal to initialValue, so the 
                // loop ends.

                // The function returns computedValue, not totalValue, because
                // totalValue could be changed by another thread between
                // the time the loop ends and the function returns.
                return computedValue;
            }


            // Do the opearion with multithreading
            void RunWithMT(int[] a, int threadscnt, int mode)
            {
                
                List<Thread> threads = new List<Thread>();

                // Adding threads
                
                for (int i = 0; i < threadscnt; i++)
                {
                    // Use mode value to handle the MT function
                    if (mode == 0)
                    {
                        Thread thread = new Thread(() => SumArrMod2WithLock(a, i, threadscnt));
                        threads.Add(thread);
                        thread.Start();
                    }
                    else
                    {
                        Thread thread = new Thread(() => SumArrMod2WithCAS(a, i, threadscnt));
                        threads.Add(thread);
                        thread.Start();
                    }
                    
                }
 
                // Joining 
                foreach (Thread t in threads)
                    t.Join();
            }


            // Main thread

            while (true)
            {
                sw.Reset();
                sum = 0;

                // First init
                if (arr == null)
                {
                    InitArray(len);
                }

                // Change array size if needed
                Console.WriteLine($"The length of array is {arr.Length}. Is it needed to change the array length? Y/N");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    len = 0;
                    arr = null;
                    do {
                        Console.WriteLine("\nEnter the array length:");
                        try
                        {
                            len = Convert.ToInt32(Console.ReadLine());
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("\nWrong value");
                        }
                    }
                    while (len == 0);

                    InitArray(len);


                }

                Console.WriteLine("\nEnter the program mode: \"S\" - Main thread; Any other - Multithread");

                if (Console.ReadKey().Key == ConsoleKey.S)
                {
                    sw.Start();
                    SumArrMod2(arr);
                    sw.Stop();

                }
                else
                {
                    
                    Console.WriteLine($"\nEnter the MT mode: \"L\" - Mutex lock; Any other - No locks");

                    if (Console.ReadKey().Key == ConsoleKey.L)
                    {
                        sw.Start();
                        RunWithMT(arr, numThreads, 0);
                        sw.Stop();
                    }
                    else
                    {
                        sw.Start();
                        RunWithMT(arr, numThreads, 1);
                        sw.Stop();
                    }
                }
                    
                // Show results
                Console.WriteLine($"\nSum mod 2 of all odd elements is: {sum}");
                Console.WriteLine($"\nTime spent: {sw.ElapsedMilliseconds} ms");
                
                // Exit point
                Console.WriteLine("\nPress Q to exit");
                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    break;
                }
            }

        }
    }
}
