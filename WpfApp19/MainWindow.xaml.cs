using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

    public partial class MainWindow : Window
    {
        private int nextTaskId = 1;
        private int maxConcurrentTasks;
        private SemaphoreSlim threadSemaphore;
        private ObservableCollection<TaskThread> initializedThreads;
        private ObservableCollection<TaskThread> pendingThreads;
        private ObservableCollection<TaskThread> activeThreads;

        public MainWindow()
        {
            InitializeComponent();

            initializedThreads = new ObservableCollection<TaskThread>();
            pendingThreads = new ObservableCollection<TaskThread>();
            activeThreads = new ObservableCollection<TaskThread>();

            lbInitialized.ItemsSource = initializedThreads;
            lbPending.ItemsSource = pendingThreads;
            lbActive.ItemsSource = activeThreads;

            maxConcurrentTasks = GetMaxConcurrentTasks();
            threadSemaphore = new SemaphoreSlim(maxConcurrentTasks, int.MaxValue);
        }

        private int GetMaxConcurrentTasks()
        {
            return int.TryParse(tbMaxConcurrent.Text, out var max) ? max : 3;
        }

        private void btnCreateThread_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var task = new TaskThread(nextTaskId++);
                initializedThreads.Add(task);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        private void lbCreated_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lbInitialized.SelectedItem is TaskThread task)
            {
                MoveTaskToPending(task);
            }
        }

        private void lbWorking_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lbActive.SelectedItem is TaskThread task)
            {
                StopTask(task, false);
            }
        }

        private void MoveTaskToPending(TaskThread task)
        {
            initializedThreads.Remove(task);
            task.Status = ThreadStatus.Pending;
            pendingThreads.Add(task);

            StartTaskFromPending(task);
        }

        private async void StartTaskFromPending(TaskThread task)
        {
            await threadSemaphore.WaitAsync(task.CancellationToken.Token);

            Dispatcher.Invoke(() =>
            {
                if (pendingThreads.Contains(task))
                {
                    pendingThreads.Remove(task);
                    task.Status = ThreadStatus.InProgress;
                    task.TaskStartTime = DateTime.Now;
                    activeThreads.Add(task);
                }
            });

            RunTaskLoop(task);
        }

        private async void RunTaskLoop(TaskThread task)
        {
            try
            {
                while (!task.CancellationToken.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, task.CancellationToken.Token);
                    Dispatcher.Invoke(() => task.Counter++);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                threadSemaphore.Release();

                Dispatcher.Invoke(() =>
                {
                    activeThreads.Remove(task);
                    task.Status = ThreadStatus.Terminated;
                });
            }
        }

        private void StopTask(TaskThread task, bool forced)
        {
            if (task == null) return;

            task.IsForcedStop = forced;
            task.CancellationToken.Cancel();
        }

        private void btnUpdateMax_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int newMax = GetMaxConcurrentTasks();
                UpdateMaxConcurrentTasks(newMax);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        private void UpdateMaxConcurrentTasks(int newMax)
        {
            if (newMax == maxConcurrentTasks) return;

            int oldMax = maxConcurrentTasks;
            maxConcurrentTasks = newMax;

            if (newMax > oldMax)
            {
                int diff = newMax - oldMax;
                threadSemaphore.Release(diff);
            }
            else
            {
                HandleDecreaseInMaxTasks(oldMax, newMax);
            }
        }

        private void HandleDecreaseInMaxTasks(int oldMax, int newMax)
        {
            int excess = activeThreads.Count - newMax;

            if (excess > 0)
            {
                var toStop = new List<TaskThread>(activeThreads);
                toStop.Sort((t1, t2) => t1.TaskStartTime.CompareTo(t2.TaskStartTime));

                for (int i = 0; i < excess; i++)
                {
                    StopTask(toStop[i], true);
                }
            }
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
