"""A simple task management module using only the Python standard library."""


class DuplicateTaskError(ValueError):
    """Raised when adding a task whose ID already exists."""


class TaskNotFoundError(LookupError):
    """Raised when an operation refers to a task ID that does not exist."""


class Task:
    """A single task with an ID, title, description, and completion status."""

    def __init__(self, task_id, title, description="", completed=False):
        if not title or not str(title).strip():
            raise ValueError("Task title must be a non-empty string.")
        self.id = task_id
        self.title = str(title).strip()
        self.description = str(description)
        self.completed = bool(completed)

    def complete(self):
        """Mark the task as completed."""
        self.completed = True

    def __repr__(self):
        status = "done" if self.completed else "pending"
        return f"Task(id={self.id!r}, title={self.title!r}, status={status})"


class TaskManager:
    """Manages a collection of tasks with unique IDs."""

    def __init__(self):
        self._tasks = {}  # task_id -> Task, preserves insertion order

    def add_task(self, task_id, title, description=""):
        """Create and store a new task. Raises DuplicateTaskError if the ID exists."""
        if task_id in self._tasks:
            raise DuplicateTaskError(f"Task with ID {task_id!r} already exists.")
        task = Task(task_id, title, description)
        self._tasks[task_id] = task
        return task

    def remove_task(self, task_id):
        """Remove a task by ID. Raises TaskNotFoundError if it does not exist."""
        task = self.get_task(task_id)
        del self._tasks[task_id]
        return task

    def complete_task(self, task_id):
        """Mark a task as completed. Raises TaskNotFoundError if it does not exist."""
        task = self.get_task(task_id)
        task.complete()
        return task

    def get_task(self, task_id):
        """Return the task with the given ID. Raises TaskNotFoundError if missing."""
        try:
            return self._tasks[task_id]
        except KeyError:
            raise TaskNotFoundError(f"No task with ID {task_id!r}.") from None

    def list_tasks(self, completed=None):
        """Return tasks in insertion order.

        completed=None  -> all tasks
        completed=True  -> only completed tasks
        completed=False -> only pending tasks
        """
        if completed is None:
            return list(self._tasks.values())
        return [t for t in self._tasks.values() if t.completed == bool(completed)]

    def __len__(self):
        return len(self._tasks)


if __name__ == "__main__":
    manager = TaskManager()
    manager.add_task(1, "Buy groceries", "Milk, eggs, bread")
    manager.add_task(2, "Write report", "Quarterly summary")
    manager.complete_task(1)
    print("All tasks:", manager.list_tasks())
    print("Pending:  ", manager.list_tasks(completed=False))
    print("Completed:", manager.list_tasks(completed=True))
