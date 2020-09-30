using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MBM.Classes
{
    public class Helper
    {
        /// <summary>
        /// Used to run tasks and methods in the background queue and prevent UI freezing with large data loads etc
        /// </summary>
        public class BackgroundQueue
        {
            private Task previousTask = Task.FromResult(true);
            private readonly object key = new object();

            internal Task QueueTask(Action action)
            {
                lock (key)
                {
                    previousTask = previousTask.ContinueWith(
                      t => action(),
                      CancellationToken.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);
                    return previousTask;
                }
            }
            internal Task<T> QueueTask<T>(Func<T> work)
            {
                lock (key)
                {
                    var task = previousTask.ContinueWith(
                      t => work(),
                      CancellationToken.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);
                    previousTask = task;
                    return task;
                }
            }
        }

        /// <summary>
        /// Helper method used when looping through datagrid column items
        /// </summary>
        internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        /// <summary>
        /// Helper method used when looping through datagrid column items
        /// </summary>
        internal static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            foreach (childItem child in FindVisualChildren<childItem>(obj))
            {
                return child;
            }
            return null;
        }
    }
}
