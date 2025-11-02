using System;
using System.Collections.Generic;

/// <summary>
/// Simple binary heap implementation for A* pathfinding
/// </summary>
public class PriorityQueue<T> where T : IComparable<T>
{
    private List<T> heap = new List<T>();

    public int Count => heap.Count;

    public void Enqueue(T item)
    {
        heap.Add(item);
        int childIndex = heap.Count - 1;
        while (childIndex > 0)
        {
            int parentIndex = (childIndex - 1) / 2;
            if (heap[childIndex].CompareTo(heap[parentIndex]) >= 0)
                break;

            Swap(childIndex, parentIndex);
            childIndex = parentIndex;
        }
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Queue is empty");

        T result = heap[0];
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        int parentIndex = 0;
        while (true)
        {
            int leftChild = parentIndex * 2 + 1;
            int rightChild = parentIndex * 2 + 2;
            int smallest = parentIndex;

            if (leftChild < heap.Count && heap[leftChild].CompareTo(heap[smallest]) < 0)
                smallest = leftChild;
            if (rightChild < heap.Count && heap[rightChild].CompareTo(heap[smallest]) < 0)
                smallest = rightChild;

            if (smallest == parentIndex)
                break;

            Swap(parentIndex, smallest);
            parentIndex = smallest;
        }

        return result;
    }

    public bool Contains(T item)
    {
        return heap.Contains(item);
    }

    private void Swap(int index1, int index2)
    {
        T temp = heap[index1];
        heap[index1] = heap[index2];
        heap[index2] = temp;
    }
}
