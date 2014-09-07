using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AirMedia.Core.Utils
{
    public class LruCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public interface ICacheEntryHandler
        {
            int GetSizeOfValue(TKey key, TValue value);
            void DisposeOfValue(TKey key, TValue value);
        }

        private readonly Dictionary<TKey, Node> _entries;
        private readonly ICacheEntryHandler _entryHandler;
        private readonly int _capacity;
        private int _currentSize;
        private Node _head;
        private Node _tail;

        private class Node
        {
            public Node Next { get; set; }
            public Node Previous { get; set; }
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public int SizeOfValue { get; set; }
        }

        public LruCache(int capacity, ICacheEntryHandler handler)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(
                    "capacity",
                    "Capacity should be greater than zero");
            _capacity = capacity;
            _entryHandler = handler;
            _entries = new Dictionary<TKey, Node>();
        }

        public void DisposeNode(TKey key)
        {
            Node node;
            if (_entries.TryGetValue(key, out node) == false)
                return;

            DisposeNode(node);
        }

        public void Clear()
        {
            var clearingItems = _entries.Values.ToArray();
            foreach (var entry in clearingItems)
            {
                DisposeNode(entry, false);
            }
        }

        private void FreeCache(int requiredCapacity)
        {
            while (_tail != null)
            {
                if (_entries.Count == 0 || (_capacity - _currentSize) >= requiredCapacity)
                    return;

                DisposeNode(_tail);
            }
        }

        private void DisposeNode(Node node, bool invokeDisposeCallback = true)
        {
            _entries.Remove(node.Key);

            if (node.Previous != null)
                node.Previous.Next = node.Next;

            if (node.Next != null)
                node.Next.Previous = node.Previous;

            if (node == _head)
            {
                _head = node.Next;
                if (_head != null)
                    _head.Previous = null;
            }

            if (node == _tail)
            {
                _tail = node.Previous;
                if (_tail != null)
                    _tail.Next = null;
            }

            _currentSize -= node.SizeOfValue;

            if (invokeDisposeCallback)
            {
                _entryHandler.DisposeOfValue(node.Key, node.Value);
            }

            node.Previous = null;
            node.Next = null;
            node.Key = default(TKey);
            node.Value = default(TValue);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="replaceValue"></param>
        /// <returns>removed entry if any</returns>
        public void Set(TKey key, TValue value, bool replaceValue = true)
        {
            if (replaceValue == false)
            {
                if (_entries.ContainsKey(key))
                {
                    MoveToHead(_entries[key]);
                    return;
                }
            }

            DisposeNode(key);

            int sizeofValue = _entryHandler.GetSizeOfValue(key, value);
            FreeCache(sizeofValue);

            var entry = new Node {Key = key, Value = value, SizeOfValue = sizeofValue};
            _entries.Add(key, entry);

            MoveToHead(entry);

            if (_tail == null)
                _tail = entry;

            _currentSize += sizeofValue;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            
            Node entry;
            if (!_entries.TryGetValue(key, out entry)) 
                return false;

            MoveToHead(entry);

            value = entry.Value;

            return true;
        }

        private void MoveToHead(Node entry)
        {
            if (entry == _head || entry == null) return;

            var next = entry.Next;
            var previous = entry.Previous;

            if (next != null) next.Previous = entry.Previous;
            if (previous != null) previous.Next = entry.Next;

            entry.Previous = null;
            entry.Next = _head;

            if (_head != null) _head.Previous = entry;
            _head = entry;

            if (_tail == entry) _tail = previous;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _entries.Select(entry => 
                new KeyValuePair<TKey, TValue>(entry.Value.Key, entry.Value.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}