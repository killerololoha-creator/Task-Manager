using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManager
{
    // ==================== МОДЕЛИ (Models) ====================

    // Перечисление приоритетов
    public enum Priority
    {
        Low,
        Medium,
        High
    }

    // Перечисление статусов
    public enum Status
    {
        ToDo,
        InProgress,
        Done
    }

    // Базовый класс Task
    public class Task
    {
        private string _title;

        public int Id { get; set; }

        public string Title
        {
            get => _title;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Название задачи не может быть пустым");
                _title = value;
            }
        }

        public string Description { get; set; }
        public Priority Priority { get; set; }
        public Status Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public Task()
        {
            CreatedAt = DateTime.Now;
        }

        public Task(int id, string title, string description, Priority priority, Status status = Status.ToDo)
        {
            Id = id;
            Title = title;
            Description = description ?? string.Empty;
            Priority = priority;
            Status = status;
            CreatedAt = DateTime.Now;
        }

        public virtual string GetDetails()
        {
            return $"ID: {Id} | [{Priority}] | {Status} | {Title}\n   Описание: {Description}\n   Создана: {CreatedAt:dd.MM.yyyy HH:mm}";
        }
    }

    // Подкласс для демонстрации наследования
    public class UrgentTask : Task
    {
        public DateTime Deadline { get; set; }

        public UrgentTask(int id, string title, string description, Priority priority, DateTime deadline, Status status = Status.ToDo)
            : base(id, title, description, priority, status)
        {
            Deadline = deadline;
        }

        public override string GetDetails()
        {
            return base.GetDetails() + $"\n   Дедлайн: {Deadline:dd.MM.yyyy} (СРОЧНАЯ ЗАДАЧА!)";
        }
    }

    // ==================== КОНТРОЛЛЕР (Controller) ====================

    public class TaskManagerController
    {
        private List<Task> _tasks = new List<Task>();
        private int _nextId = 1;
        private Queue<Task> _priorityQueue = new Queue<Task>();
        private Stack<(string Action, Task Task, int Index)> _undoStack = new Stack<(string, Task, int)>();

        public IReadOnlyList<Task> Tasks => _tasks;

        public void AddTask(Task task)
        {
            task.Id = _nextId++;
            _tasks.Add(task);
            _undoStack.Push(("Add", CloneTask(task), _tasks.Count - 1));
            UpdatePriorityQueue();
        }

        public bool UpdateTask(int id, string title, string description, Priority priority, Status status)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;

            int index = _tasks.IndexOf(task);
            var oldTask = CloneTask(task);

            task.Title = title;
            task.Description = description;
            task.Priority = priority;
            task.Status = status;

            _undoStack.Push(("Update", oldTask, index));
            UpdatePriorityQueue();
            return true;
        }

        public bool DeleteTask(int id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;

            int index = _tasks.IndexOf(task);
            _tasks.RemoveAt(index);
            _undoStack.Push(("Delete", task, index));
            UpdatePriorityQueue();
            return true;
        }

        public bool UndoLastAction()
        {
            if (_undoStack.Count == 0) return false;

            var (action, task, index) = _undoStack.Pop();

            switch (action)
            {
                case "Add":
                    _tasks.RemoveAt(index);
                    break;
                case "Update":
                    if (index >= 0 && index < _tasks.Count)
                        _tasks[index] = task;
                    break;
                case "Delete":
                    _tasks.Insert(index, task);
                    break;
            }

            UpdatePriorityQueue();
            return true;
        }

        public List<Task> FilterByStatus(Status status)
        {
            return _tasks.Where(t => t.Status == status).ToList();
        }

        public List<Task> FilterByPriority(Priority priority)
        {
            return _tasks.Where(t => t.Priority == priority).ToList();
        }

        public List<Task> GetTasksByPriorityQueue()
        {
            UpdatePriorityQueue();
            return _priorityQueue.ToList();
        }

        private void UpdatePriorityQueue()
        {
            _priorityQueue.Clear();
            var orderedTasks = _tasks.OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedAt);
            foreach (var task in orderedTasks)
                _priorityQueue.Enqueue(task);
        }

        private Task CloneTask(Task task)
        {
            return new Task(task.Id, task.Title, task.Description, task.Priority, task.Status)
            {
                CreatedAt = task.CreatedAt
            };
        }

        public void SaveToJson(string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_tasks, options);
            File.WriteAllText(filePath, json);
        }

        public void LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath)) return;

            string json = File.ReadAllText(filePath);
            var loadedTasks = JsonSerializer.Deserialize<List<Task>>(json);

            if (loadedTasks != null)
            {
                _tasks = loadedTasks;
                _nextId = _tasks.Count > 0 ? _tasks.Max(t => t.Id) + 1 : 1;
                _undoStack.Clear();
                UpdatePriorityQueue();
            }
        }
    }

    // ==================== ПРЕДСТАВЛЕНИЕ (View) ====================

    public class ConsoleView
    {
        public void ShowMainMenu()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║        TASK MANAGER v2.0            ║");
            Console.WriteLine("╠══════════════════════════════════════╣");
            Console.WriteLine("║  1. Показать все задачи              ║");
            Console.WriteLine("║  2. Добавить задачу                  ║");
            Console.WriteLine("║  3. Редактировать задачу             ║");
            Console.WriteLine("║  4. Удалить задачу                   ║");
            Console.WriteLine("║  5. Фильтрация задач                 ║");
            Console.WriteLine("║  6. Отменить последнее действие      ║");
            Console.WriteLine("║  7. Очередь по приоритету            ║");
            Console.WriteLine("║  8. Добавить срочную задачу          ║");
            Console.WriteLine("║  9. Выход                            ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.Write("\nВыберите опцию: ");
        }

        public void ShowFilterMenu()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║           ФИЛЬТРАЦИЯ ЗАДАЧ           ║");
            Console.WriteLine("╠══════════════════════════════════════╣");
            Console.WriteLine("║  1. По статусу                       ║");
            Console.WriteLine("║  2. По приоритету                    ║");
            Console.WriteLine("║  3. Назад                            ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.Write("\nВыберите опцию: ");
        }

        public void ShowTasks(IEnumerable<Task> tasks, string title = "СПИСОК ЗАДАЧ")
        {
            Console.Clear();
            Console.WriteLine($"╔══════════════════════════════════════╗");
            Console.WriteLine($"║{title,-32}║");
            Console.WriteLine($"╚══════════════════════════════════════╝\n");

            if (!tasks.Any())
            {
                Console.WriteLine("   📭 Задач нет.");
            }
            else
            {
                foreach (var task in tasks)
                {
                    Console.WriteLine(task.GetDetails());
                    Console.WriteLine(new string('-', 50));
                }
            }

            Console.WriteLine($"\n📊 Всего задач: {tasks.Count()}");
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }

        public (string Title, string Description, Priority Priority, Status Status) GetTaskInput()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║           ДОБАВЛЕНИЕ ЗАДАЧИ          ║");
            Console.WriteLine("╚══════════════════════════════════════╝\n");

            string title = GetNonEmptyString("📝 Введите название задачи: ");
            string description = GetString("📄 Введите описание (Enter - пропустить): ");
            Priority priority = GetPriority();
            Status status = GetStatus();

            return (title, description, priority, status);
        }

        public (string Title, string Description, Priority Priority, Status Status) GetUrgentTaskInput()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║        ДОБАВЛЕНИЕ СРОЧНОЙ ЗАДАЧИ     ║");
            Console.WriteLine("╚══════════════════════════════════════╝\n");

            string title = GetNonEmptyString("📝 Введите название срочной задачи: ");
            string description = GetString("📄 Введите описание (Enter - пропустить): ");
            Priority priority = GetPriority();
            Status status = GetStatus();
            DateTime deadline = GetDeadline();

            return (title, description, priority, status);
        }

        public DateTime GetDeadline()
        {
            while (true)
            {
                Console.Write("⏰ Введите дедлайн (дд.мм.гггг): ");
                if (DateTime.TryParse(Console.ReadLine(), out DateTime deadline) && deadline > DateTime.Now)
                    return deadline;

                ShowError("Некорректная дата! Дедлайн должен быть в будущем.");
            }
        }

        public (string Title, string Description, Priority Priority, Status Status) GetTaskUpdateInput(Task oldTask)
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║         РЕДАКТИРОВАНИЕ ЗАДАЧИ        ║");
            Console.WriteLine("╚══════════════════════════════════════╝\n");

            Console.WriteLine($"📌 Текущее название: {oldTask.Title}");
            string title = GetString("✏️ Новое название (Enter - оставить): ");
            if (string.IsNullOrWhiteSpace(title)) title = oldTask.Title;

            Console.WriteLine($"📄 Текущее описание: {oldTask.Description}");
            string description = GetString("✏️ Новое описание (Enter - оставить): ");
            if (string.IsNullOrWhiteSpace(description)) description = oldTask.Description;

            Console.WriteLine($"⭐ Текущий приоритет: {oldTask.Priority}");
            Priority priority = GetPriority(true);
            if (priority == oldTask.Priority && !GetBoolInput("Изменить приоритет? (y/n): "))
                priority = oldTask.Priority;

            Console.WriteLine($"🔄 Текущий статус: {oldTask.Status}");
            Status status = GetStatus(true);
            if (status == oldTask.Status && !GetBoolInput("Изменить статус? (y/n): "))
                status = oldTask.Status;

            return (title, description, priority, status);
        }

        public int GetTaskId(string message = "🔢 Введите ID задачи: ")
        {
            while (true)
            {
                Console.Write(message);
                if (int.TryParse(Console.ReadLine(), out int id) && id > 0)
                    return id;

                ShowError("Некорректный ID. Введите положительное число.");
            }
        }

        private string GetNonEmptyString(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(input))
                    return input;

                ShowError("Поле не может быть пустым!");
            }
        }

        private string GetString(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine()?.Trim();
        }

        private Priority GetPriority(bool allowSkip = false)
        {
            while (true)
            {
                Console.WriteLine("\n⭐ Приоритет:");
                Console.WriteLine("  1. Low (Низкий)");
                Console.WriteLine("  2. Medium (Средний)");
                Console.WriteLine("  3. High (Высокий)");

                Console.Write("Выберите (1-3): ");
                string input = Console.ReadLine();

                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 3)
                    return (Priority)(choice - 1);

                ShowError("Неверный выбор! Выберите 1, 2 или 3.");
            }
        }

        private Status GetStatus(bool allowSkip = false)
        {
            while (true)
            {
                Console.WriteLine("\n🔄 Статус:");
                Console.WriteLine("  1. To Do (Нужно сделать)");
                Console.WriteLine("  2. In Progress (В процессе)");
                Console.WriteLine("  3. Done (Выполнено)");

                Console.Write("Выберите (1-3): ");
                string input = Console.ReadLine();

                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 3)
                    return (Status)(choice - 1);

                ShowError("Неверный выбор! Выберите 1, 2 или 3.");
            }
        }

        private bool GetBoolInput(string prompt)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.ToLower();
            return input == "y" || input == "yes" || input == "д" || input == "да";
        }

        public void ShowMessage(string message, bool isError = false)
        {
            if (isError)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine($"\n{message}");
            Console.ResetColor();
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        public void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ ОШИБКА: {message}");
            Console.ResetColor();
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        public Status GetStatusFilter()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║          ФИЛЬТР ПО СТАТУСУ           ║");
            Console.WriteLine("╚══════════════════════════════════════╝\n");

            while (true)
            {
                Console.WriteLine("  1. To Do");
                Console.WriteLine("  2. In Progress");
                Console.WriteLine("  3. Done");
                Console.Write("\nВыберите статус: ");

                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= 3)
                    return (Status)(choice - 1);

                ShowError("Неверный выбор!");
            }
        }

        public Priority GetPriorityFilter()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║        ФИЛЬТР ПО ПРИОРИТЕТУ          ║");
            Console.WriteLine("╚══════════════════════════════════════╝\n");

            while (true)
            {
                Console.WriteLine("  1. Low");
                Console.WriteLine("  2. Medium");
                Console.WriteLine("  3. High");
                Console.Write("\nВыберите приоритет: ");

                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= 3)
                    return (Priority)(choice - 1);

                ShowError("Неверный выбор!");
            }
        }
    }

    // ==================== ГЛАВНАЯ ПРОГРАММА ====================

    class Program
    {
        private static TaskManagerController _controller = new TaskManagerController();
        private static ConsoleView _view = new ConsoleView();
        private static readonly string DataFile = "tasks.json";

        static void Main(string[] args)
        {
            Console.Title = "Task Manager";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            LoadData();

            while (true)
            {
                _view.ShowMainMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ShowAllTasks();
                        break;
                    case "2":
                        AddTask();
                        break;
                    case "3":
                        UpdateTask();
                        break;
                    case "4":
                        DeleteTask();
                        break;
                    case "5":
                        FilterTasks();
                        break;
                    case "6":
                        UndoAction();
                        break;
                    case "7":
                        ShowPriorityQueue();
                        break;
                    case "8":
                        AddUrgentTask();
                        break;
                    case "9":
                        SaveData();
                        _view.ShowMessage("👋 До свидания!");
                        return;
                    default:
                        _view.ShowError("Неверный выбор! Попробуйте снова (1-9).");
                        break;
                }
            }
        }

        static void ShowAllTasks()
        {
            _view.ShowTasks(_controller.Tasks, "ВСЕ ЗАДАЧИ");
        }

        static void AddTask()
        {
            try
            {
                var (title, description, priority, status) = _view.GetTaskInput();
                var task = new Task(0, title, description, priority, status);
                _controller.AddTask(task);
                SaveData();
                _view.ShowMessage($"✅ Задача \"{title}\" успешно добавлена!");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        static void AddUrgentTask()
        {
            try
            {
                var (title, description, priority, status) = _view.GetUrgentTaskInput();
                DateTime deadline = _view.GetDeadline();
                var task = new UrgentTask(0, title, description, priority, deadline, status);
                _controller.AddTask(task);
                SaveData();
                _view.ShowMessage($"⚠️ Срочная задача \"{title}\" добавлена! Дедлайн: {deadline:dd.MM.yyyy}");
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        static void UpdateTask()
        {
            if (!CheckTasksExist()) return;

            ShowAllTasks();
            int id = _view.GetTaskId();
            var task = _controller.Tasks.FirstOrDefault(t => t.Id == id);

            if (task == null)
            {
                _view.ShowError($"Задача с ID {id} не найдена!");
                return;
            }

            try
            {
                var (title, description, priority, status) = _view.GetTaskUpdateInput(task);
                if (_controller.UpdateTask(id, title, description, priority, status))
                {
                    SaveData();
                    _view.ShowMessage($"✅ Задача обновлена!");
                }
            }
            catch (Exception ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        static void DeleteTask()
        {
            if (!CheckTasksExist()) return;

            ShowAllTasks();
            int id = _view.GetTaskId();

            var task = _controller.Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                _view.ShowError($"Задача с ID {id} не найдена!");
                return;
            }

            if (_controller.DeleteTask(id))
            {
                SaveData();
                _view.ShowMessage($"✅ Задача \"{task.Title}\" удалена!");
            }
        }

        static void FilterTasks()
        {
            if (!CheckTasksExist()) return;

            while (true)
            {
                _view.ShowFilterMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        FilterByStatus();
                        return;
                    case "2":
                        FilterByPriority();
                        return;
                    case "3":
                        return;
                    default:
                        _view.ShowError("Неверный выбор!");
                        break;
                }
            }
        }

        static void FilterByStatus()
        {
            Status status = _view.GetStatusFilter();
            var filtered = _controller.FilterByStatus(status);
            _view.ShowTasks(filtered, $"ЗАДАЧИ СО СТАТУСОМ: {status}");
        }

        static void FilterByPriority()
        {
            Priority priority = _view.GetPriorityFilter();
            var filtered = _controller.FilterByPriority(priority);
            _view.ShowTasks(filtered, $"ЗАДАЧИ С ПРИОРИТЕТОМ: {priority}");
        }

        static void ShowPriorityQueue()
        {
            if (!CheckTasksExist()) return;

            var queue = _controller.GetTasksByPriorityQueue();
            _view.ShowTasks(queue, "ОЧЕРЕДЬ ЗАДАЧ ПО ПРИОРИТЕТУ (High → Low)");
        }

        static void UndoAction()
        {
            if (_controller.UndoLastAction())
            {
                SaveData();
                _view.ShowMessage("↩️ Последнее действие отменено!");
            }
            else
            {
                _view.ShowError("Нет действий для отмены!");
            }
        }

        static bool CheckTasksExist()
        {
            if (!_controller.Tasks.Any())
            {
                _view.ShowError("Нет доступных задач!");
                return false;
            }
            return true;
        }

        static void LoadData()
        {
            try
            {
                _controller.LoadFromJson(DataFile);
                Console.WriteLine($"📁 Загружено {_controller.Tasks.Count} задач");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Ошибка загрузки: {ex.Message}");
            }
        }

        static void SaveData()
        {
            try
            {
                _controller.SaveToJson(DataFile);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Ошибка сохранения: {ex.Message}");
            }
        }
    }
}
