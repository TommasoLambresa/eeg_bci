namespace Benchmarking
{
    /// <summary>
    /// Mantém compatibilidade com scripts do pacote demonstrativo
    /// após a remoção das ferramentas de benchmark.
    /// </summary>
    public static class PerformanceTest
    {
        public static bool RunningBenchmark => false;
    }
}