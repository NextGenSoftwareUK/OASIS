using System;
using System.Threading;

namespace NextGenSoftware.OASIS.STAR.TestHarness
{
    public class Spinner : IDisposable
    {
        private Thread _thread;
        private int _counter = 0;

        public string Sequence { get; set; } = @"/-\|";
        public int Left { get; set; }
        public int Top { get; set; }
        public int Delay { get; set; } = 100;
        public bool IsActive { get; set; }
        public ConsoleColor Colour { get; set; } = ConsoleColor.Green;

        public Spinner()
        {
            _thread = new Thread(Spin);
        }

        public Spinner(int left, int top, int delay = 100)
        {
            this.Left = left;
            this.Top = top;
            this.Delay = delay;
            _thread = new Thread(Spin);
        }

        public void Start()
        {
            Console.CursorVisible = false;

            IsActive = true;
            if (!_thread.IsAlive)
            {
                if (_thread.ThreadState == ThreadState.Stopped)
                    _thread = new Thread(Spin);
                else
                {
                    try
                    {
                        _thread.Start();
                    }
                    catch (Exception ex)
                    {
                        _thread = new Thread(Spin);
                    }
                }
            }
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
            ConsoleColor existingColour = Console.ForegroundColor;
            Console.ForegroundColor = Colour;
            Console.Write(c);
            Console.ForegroundColor = existingColour;
        }

        private void Turn()
        {
            Draw(Sequence[++_counter % Sequence.Length]);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
