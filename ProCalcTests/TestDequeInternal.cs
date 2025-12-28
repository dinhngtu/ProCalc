using ProCalcCore;
using System.Reflection;

namespace ProCalcTests;

public class TestDequeInternal {

    static void SetState<T>(Deque<T> deque, int head, int tail) where T : struct {
        var type = typeof(Deque<T>);
        type.GetField("_head", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(deque, head);
        type.GetField("_tail", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(deque, tail);
    }

    static void FillIndices(Deque<int> deque) {
        var type = typeof(Deque<int>);
        var field = type.GetField("_data", BindingFlags.NonPublic | BindingFlags.Instance);
        var list = (List<int>)field!.GetValue(deque)!;
        for (int i = 0; i < list.Count; i++) {
            list[i] = i;
        }
    }

    static void CheckState(Deque<int> deque, int expectedCount, int expectedFree) {
        Assert.Equal(expectedCount, deque.Count);
        Assert.Equal(expectedFree, deque.Free);
    }

    [Fact]
    public void TestCase1() {
        // Case: _tail = 9, _head = 4, Capacity = 11
        // Expected: Count = 6, Free = 4
        // Segments: [9, 10] and [0, 1, 2, 3]

        var t = new Deque<int>(11);
        FillIndices(t);
        SetState(t, 4, 9);

        CheckState(t, 6, 4);

        // Check index mapping
        // begin + 1 => index 1 => physical 10
        Assert.Equal(10, t[1]);
        // begin + 2 => index 2 => physical 0
        Assert.Equal(0, t[2]);
        // end - 1 => index 5 => physical 3
        Assert.Equal(3, t[^1]);

        // Verify iteration order
        // Should yield: 9, 10, 0, 1, 2, 3
        var elements = t.EnumerateFifo(t.Count).ToArray();
        Assert.Equal([9, 10, 0, 1, 2, 3], elements);
    }

    [Fact]
    public void TestCase2() {
        // Case: _tail = 0, _head = 6, Capacity = 11
        // Expected: Count = 6, Free = 4
        // Segments: [0, 1, 2, 3, 4, 5] (Contiguous)

        var t = new Deque<int>(11);
        FillIndices(t);
        SetState(t, 6, 0);

        CheckState(t, 6, 4);

        // begin + 1 => index 1 => physical 1
        Assert.Equal(1, t[1]);
        // end - 1 => index 5 => physical 5
        Assert.Equal(5, t[^1]);

        // Verify iteration order
        var elements = t.EnumerateFifo(t.Count).ToArray();
        Assert.Equal([0, 1, 2, 3, 4, 5], elements);
    }

    [Fact]
    public void TestCase3() {
        // Case: _tail = 4, _head = 10, Capacity = 11
        // Expected: Count = 6, Free = 4
        // Segments: [4, 5, 6, 7, 8, 9] (Contiguous)

        var t = new Deque<int>(11);
        FillIndices(t);
        SetState(t, 10, 4);

        CheckState(t, 6, 4);

        // begin + 1 => index 1 => physical 5
        Assert.Equal(5, t[1]);
        // end - 1 => index 5 => physical 9
        Assert.Equal(9, t[^1]);
    }

    [Fact]
    public void TestCase4() {
        // Case: _tail = 5, _head = 0, Capacity = 11
        // Expected: Count = 6, Free = 4
        // Segments: [5, 6, 7, 8, 9, 10] (Contiguous, wrapping at boundary but head is 0 so it stops before wrap effectively or just fills end)
        // _head = 0 means it wrapped around to 0.
        // Elements at: 5, 6, 7, 8, 9, 10.

        var t = new Deque<int>(11);
        FillIndices(t);
        SetState(t, 0, 5);

        CheckState(t, 6, 4);

        // begin + 1 => index 1 => physical 6
        Assert.Equal(6, t[1]);
        // end - 1 => index 5 => physical 10
        Assert.Equal(10, t[^1]);
    }

    [Fact]
    public void TestCase5() {
        // Case: _tail = 0, _head = 0, Capacity = 11
        // Expected: Count = 0, Free = 10

        var t = new Deque<int>(11);
        FillIndices(t);
        SetState(t, 0, 0);

        CheckState(t, 0, 10);
    }

    [Fact]
    public void TestCase6() {
        // Case: _tail = 6, _head = 5, Capacity = 11
        // Expected: Count = 10, Free = 0 (Full)
        // Wraps around.

        var t = new Deque<int>(11);
        FillIndices(t);
        SetState(t, 5, 6);

        Assert.Equal(10, t.Count);
        Assert.Equal(0, t.Free);
    }

    [Fact]
    public void TestCase7() {
        // Case: _tail = 0, _head = 10, Capacity = 11
        // Expected: Count = 10, Free = 0 (Full, contiguous)

        var t = new Deque<int>(11);
        FillIndices(t);
        SetState(t, 10, 0);

        Assert.Equal(10, t.Count);
        Assert.Equal(0, t.Free);
    }
}
