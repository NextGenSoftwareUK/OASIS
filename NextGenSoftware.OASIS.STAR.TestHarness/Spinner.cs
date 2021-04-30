using System;
using System.Threading;

namespace NextGenSoftware.OASIS.STAR.TestHarness
{
    public class Spinner : IDisposable
    {
        private readonly Thread thread;
        private int counter = 0;

        public string Sequence { get; set; } = @"/-\|";
        public int Left { get; set; }
        public int Top { get; set; }
        public int Delay { get; set; } = 100;
        public bool IsActive { get; set; }
        public ConsoleColor Colour { get; set; } = ConsoleColor.Green;

        public Spinner()
        {
            thread = new Thread(Spin);
        }

        public Spinner(int left, int top, int delay = 100)
        {
            this.Left = left;
            this.Top = top;
            this.Delay = delay;
            thread = new Thread(Spin);
        }

        public void Start()
        {
            Console.CursorVisible = false;

            IsActive = true;
            if (!thread.IsAlive)
                thread.Start();
        }

        public void Stop()
        {
            IsActive = false;
            Draw(' ');
        }

        private void Spin()
        {
            while (IsActive)
            {
                Turn();
                Thread.Sleep(Delay);
            }
        }

        private void Draw(char c)
        {
            Console.SetCursorPosition(Left, Top);
            Console.ForegroundColor = Colour;
            Console.Write(c);
        }

        private void Turn()
        {
            Draw(Sequence[++counter % Sequence.Length]);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
