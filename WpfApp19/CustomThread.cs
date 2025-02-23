using System;
using System.Threading;

namespace WpfApp19
{
    internal class CustomThread
    {
        private int counter;
        private CancellationTokenSource cancellationTokenSource;

        public int Counter
        {
            get { return counter; }
            private set { counter = value; }
        }

        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        public CustomThread()
        {
            cancellationTokenSource = new CancellationTokenSource();
            Counter = 0;
        }

        public void IncrementCounter()
        {
            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException("The task has been cancelled.");
            }

            Counter++;
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }
    }
}
