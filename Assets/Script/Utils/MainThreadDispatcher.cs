using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Utils
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();

        public static void Enqueue(Action action)
        {
            lock (ExecutionQueue)
            {
                ExecutionQueue.Enqueue(action);
            }
        }

        void Update()
        {
            lock (ExecutionQueue)
            {
                while (ExecutionQueue.Count > 0)
                {
                    var action = ExecutionQueue.Dequeue();
                    action?.Invoke();
                }
            }
        }
    }
}