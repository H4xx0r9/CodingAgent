import unittest

from task_manager import (
    DuplicateTaskError,
    Task,
    TaskManager,
    TaskNotFoundError,
)


class TestTask(unittest.TestCase):

    def test_task_defaults(self):
        task = Task(1, "Title")
        self.assertEqual(task.id, 1)
        self.assertEqual(task.title, "Title")
        self.assertEqual(task.description, "")
        self.assertFalse(task.completed)

    def test_task_with_description_and_completed(self):
        task = Task(2, "Title", "Desc", completed=True)
        self.assertEqual(task.description, "Desc")
        self.assertTrue(task.completed)

    def test_title_is_stripped(self):
        self.assertEqual(Task(1, "  Hello  ").title, "Hello")

    def test_empty_title_raises(self):
        with self.assertRaises(ValueError):
            Task(1, "")
        with self.assertRaises(ValueError):
            Task(1, "   ")

    def test_complete_sets_status(self):
        task = Task(1, "Title")
        task.complete()
        self.assertTrue(task.completed)

    def test_repr_shows_status(self):
        self.assertIn("pending", repr(Task(1, "T")))
        self.assertIn("done", repr(Task(1, "T", completed=True)))


class TestTaskManagerAdd(unittest.TestCase):

    def setUp(self):
        self.manager = TaskManager()

    def test_add_task_returns_task(self):
        task = self.manager.add_task(1, "Title", "Desc")
        self.assertIsInstance(task, Task)
        self.assertEqual(task.id, 1)
        self.assertEqual(len(self.manager), 1)

    def test_add_duplicate_id_raises(self):
        self.manager.add_task(1, "First")
        with self.assertRaises(DuplicateTaskError):
            self.manager.add_task(1, "Second")
        self.assertEqual(len(self.manager), 1)
        self.assertEqual(self.manager.get_task(1).title, "First")

    def test_duplicate_id_persists_after_completion(self):
        self.manager.add_task(1, "First")
        self.manager.complete_task(1)
        with self.assertRaises(DuplicateTaskError):
            self.manager.add_task(1, "Again")

    def test_add_empty_title_raises(self):
        with self.assertRaises(ValueError):
            self.manager.add_task(1, "")
        self.assertEqual(len(self.manager), 0)

    def test_id_reusable_after_removal(self):
        self.manager.add_task(1, "First")
        self.manager.remove_task(1)
        task = self.manager.add_task(1, "Reused")
        self.assertEqual(task.title, "Reused")


class TestTaskManagerRemove(unittest.TestCase):

    def setUp(self):
        self.manager = TaskManager()
        self.manager.add_task(1, "One")
        self.manager.add_task(2, "Two")

    def test_remove_returns_task_and_deletes_it(self):
        removed = self.manager.remove_task(1)
        self.assertEqual(removed.title, "One")
        self.assertEqual(len(self.manager), 1)
        with self.assertRaises(TaskNotFoundError):
            self.manager.get_task(1)

    def test_remove_missing_raises(self):
        with self.assertRaises(TaskNotFoundError):
            self.manager.remove_task(99)
        self.assertEqual(len(self.manager), 2)


class TestTaskManagerComplete(unittest.TestCase):

    def setUp(self):
        self.manager = TaskManager()
        self.manager.add_task(1, "One")

    def test_complete_marks_task_done(self):
        task = self.manager.complete_task(1)
        self.assertTrue(task.completed)
        self.assertTrue(self.manager.get_task(1).completed)

    def test_complete_is_idempotent(self):
        self.manager.complete_task(1)
        self.assertTrue(self.manager.complete_task(1).completed)

    def test_complete_missing_raises(self):
        with self.assertRaises(TaskNotFoundError):
            self.manager.complete_task(42)


class TestTaskManagerGetAndList(unittest.TestCase):

    def setUp(self):
        self.manager = TaskManager()
        self.manager.add_task(1, "One")
        self.manager.add_task(2, "Two")
        self.manager.add_task(3, "Three")
        self.manager.complete_task(2)

    def test_get_task(self):
        self.assertEqual(self.manager.get_task(3).title, "Three")

    def test_get_missing_raises(self):
        with self.assertRaises(TaskNotFoundError):
            self.manager.get_task(0)

    def test_list_all(self):
        titles = [t.title for t in self.manager.list_tasks()]
        self.assertEqual(titles, ["One", "Two", "Three"])

    def test_list_all_explicit_none(self):
        self.assertEqual(len(self.manager.list_tasks(completed=None)), 3)

    def test_filter_completed(self):
        tasks = self.manager.list_tasks(completed=True)
        self.assertEqual([t.id for t in tasks], [2])
        self.assertTrue(all(t.completed for t in tasks))

    def test_filter_pending(self):
        tasks = self.manager.list_tasks(completed=False)
        self.assertEqual([t.id for t in tasks], [1, 3])
        self.assertTrue(all(not t.completed for t in tasks))

    def test_list_empty_manager(self):
        self.assertEqual(TaskManager().list_tasks(), [])
        self.assertEqual(TaskManager().list_tasks(completed=True), [])

    def test_list_returns_copy(self):
        self.manager.list_tasks().clear()
        self.assertEqual(len(self.manager), 3)


if __name__ == "__main__":
    unittest.main()
