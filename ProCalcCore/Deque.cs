using System.Collections;

namespace ProCalcCore;

public class Deque<T> : IEnumerable<T> where T : struct {
    readonly List<T> _data;
    int _head = 0, _tail = 0;

    internal static int CircularDistance(int from, int to, int size) {
        return (from <= to) ? (to - from) : (size - (from - to));
    }

    public Deque(int capacity) {
        _data = new List<T>(capacity);
        for (int i = 0; i < capacity; i++) {
            _data.Add(default);
        }
    }

    public int Capacity {
        get {
            return _data.Count;
        }
    }

    public int Count {
        get {
            return CircularDistance(_tail, _head, Capacity);
        }
    }

    public int Free {
        get {
            return Capacity - Count - 1;
        }
    }

    void ExpandFront(int amt) {
        _head = (_head + amt) % Capacity;
    }

    void ExpandBack(int amt) {
        _tail = (_tail - amt + Capacity) % Capacity;
    }

    void ShrinkFront(int amt) {
        _head = (_head - amt + Capacity) % Capacity;
    }

    void ShrinkBack(int amt) {
        _tail = (_tail + amt) % Capacity;
    }

    public void PushFront(T value) {
        if (Free < 1)
            throw new InvalidOperationException("Deque is full");
        _data[_head] = value;
        ExpandFront(1);
    }

    public void PushBack(T value) {
        if (Free < 1)
            throw new InvalidOperationException("Deque is full");
        ExpandBack(1);
        _data[_tail] = value;
    }

    public T PopFront() {
        if (Count < 1)
            throw new InvalidOperationException("Deque is empty");
        ShrinkFront(1);
        return _data[_head];
    }

    public T PopBack() {
        if (Count < 1)
            throw new InvalidOperationException("Deque is empty");
        var value = _data[_tail];
        ShrinkBack(1);
        return value;
    }

    public T PeekFront() {
        if (Count < 1)
            throw new InvalidOperationException("Deque is empty");
        return _data[(_head - 1 + Capacity) % Capacity];
    }

    public T PeekBack() {
        if (Count < 1)
            throw new InvalidOperationException("Deque is empty");
        return _data[_tail];
    }

    public void Clear() {
        _head = _tail = 0;
    }

    public T this[int index] {
        get {
            if (index >= 0) {
                if (index >= Count)
                    throw new IndexOutOfRangeException();
                return _data[(_tail + index) % Capacity];
            }
            else {
                if (index < -Count)
                    throw new IndexOutOfRangeException();
                return _data[(_head + index + Capacity) % Capacity];
            }
        }
        set {
            if (index >= 0) {
                if (index >= Count)
                    throw new IndexOutOfRangeException();
                _data[(_tail + index) % Capacity] = value;
            }
            else {
                if (index < -Count)
                    throw new IndexOutOfRangeException();
                _data[(_head + index + Capacity) % Capacity] = value;
            }
        }
    }


    public IEnumerable<T> EnumerateFifo(int amount) {
        if (_tail <= _head) {
            for (int pos = _tail; pos < _tail + Math.Min(amount, Count); pos++) {
                yield return _data[pos];
            }
        }
        else if (amount <= _data.Count - _tail) {
            for (int pos = _tail; pos < _tail + amount; pos++) {
                yield return _data[pos];
            }
        }
        else {
            for (int pos = _tail; pos < Capacity; pos++) {
                yield return _data[pos];
            }
            for (int pos = 0; pos < amount - (Capacity - _tail); pos++) {
                yield return _data[pos];
            }
        }
    }

    public IEnumerable<T> EnumerateLifo(int amount) {
        var limit = Math.Min(amount, Count);
        if (_tail <= _head) {
            for (int pos = _head - 1; pos >= _head - limit; pos--) {
                yield return _data[pos];
            }
        }
        else if (limit <= _head) {
            for (int pos = _head - 1; pos >= _head - limit; pos--) {
                yield return _data[pos];
            }
        }
        else {
            for (int pos = _head - 1; pos >= 0; pos--) {
                yield return _data[pos];
            }
            for (int pos = Capacity - 1; pos >= Capacity - (limit - _head); pos--) {
                yield return _data[pos];
            }
        }
    }

    public IEnumerator<T> GetEnumerator() {
        return EnumerateFifo(Count).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
