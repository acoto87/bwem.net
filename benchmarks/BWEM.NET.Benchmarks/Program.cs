using BenchmarkDotNet.Running;

namespace BWEM.NET.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<InitializationBenchmarks>();
        }
    }
}
