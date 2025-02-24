using System;
using System.ComponentModel;
using System.Threading;

namespace WpfApp19
{
    public enum ThreadStatus
    {
        Initialized,
        Pending,
        InProgress,
        Terminated
    }

    public class TaskThread : INotifyPropertyChanged
    {
        public int TaskId { get; }
        private ThreadStatus threadStatus;
        public ThreadStatus Status
        {
            get => threadStatus;
            set
            {
                threadStatus = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusInfo));
            }
        }

        private int taskCounter;
        public int Counter
        {
            get => taskCounter;
            set
            {
                taskCounter = value;
                OnPropertyChanged(nameof(Counter));
                OnPropertyChanged(nameof(StatusInfo));
            }
        }

        public DateTime TaskStartTime { get; set; }
        public CancellationTokenSource CancellationToken { get; }
        public bool IsForcedStop { get; set; }

        public string StatusInfo => $"Задача {TaskId} –> Счётчик: {Counter} –> {Status}";

        public TaskThread(int taskId)
        {
            TaskId = taskId;
            Status = ThreadStatus.Initialized;
            Counter = 0;
            CancellationToken = new CancellationTokenSource();
            IsForcedStop = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
