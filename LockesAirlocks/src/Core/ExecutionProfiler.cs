using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Rolling window of the script's instruction-count and run-time usage, used to report
    /// the script's load so players can see it stays well within the programmable block budget.
    /// </summary>
    public class ExecutionProfiler
    {
        const int Size = 10;
        readonly int[] _instructionCounts = new int[Size];
        readonly double[] _runtimeMs = new double[Size];
        int _index;

        /// <summary>Record the most recent tick's instruction count and run time.</summary>
        public void Sample(IMyGridProgramRuntimeInfo runtime)
        {
            if (_index >= Size) _index = 0;
            _instructionCounts[_index] = runtime.CurrentInstructionCount;
            _runtimeMs[_index] = runtime.LastRunTimeMs;
            _index++;
        }

        public double AverageInstructions => _instructionCounts.Average();
        public int PeakInstructions => _instructionCounts.Max();
        public double AverageRuntimeMs => _runtimeMs.Average();
        public double PeakRuntimeMs => _runtimeMs.Max();
    }
}
