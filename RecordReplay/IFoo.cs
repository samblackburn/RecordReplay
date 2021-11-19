using System;
using System.Threading;

namespace RecordReplay
{
    public interface IFoo
    {
        int Append(string input);
    }

    internal class SlowFoo : IFoo
    {
        private string _state = "";

        public int Append(string input)
        {
            Console.WriteLine($"Appending {input} slowly");
            Thread.Sleep(1000);
            _state += input;
            return _state.Length;
        }
    }
}
