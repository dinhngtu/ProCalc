using ProCalcCore;
using System.Collections;

namespace ProCalcTests;

public class TestDeque {

    [Fact]
    public void TestPushPopFront() {
        // In this implementation, Front is the END of the sequence.
        // PushFront appends, PopFront removes from end.
        var d = new Deque<int>(5);
        d.PushFront(1);
        d.PushFront(2);

        Assert.Equal(2, d.Count);
        Assert.Equal(2, d.PeekFront()); // Last item
        Assert.Equal(1, d.PeekBack());  // First item

        Assert.Equal(2, d.PopFront());
        Assert.Equal(1, d.PopFront());
        Assert.Equal(0, d.Count);
    }

    [Fact]
    public void TestPushPopBack() {
        // In this implementation, Back is the START of the sequence.
        // PushBack prepends, PopBack removes from start.
        var d = new Deque<int>(5);
        d.PushBack(1);
        d.PushBack(2);

        Assert.Equal(2, d.Count);
        Assert.Equal(1, d.PeekFront()); // Last item added via PushBack (prepended) is now at the end of the logical list? No.
        // Logic: 
        // PushBack(1) -> [1]
        // PushBack(2) -> [2, 1]
        // PeekBack is index 0 -> 2.
        // PeekFront is index -1 -> 1.
        Assert.Equal(2, d.PeekBack());
        Assert.Equal(1, d.PeekFront());

        Assert.Equal(2, d.PopBack());
        Assert.Equal(1, d.PopBack());
        Assert.Equal(0, d.Count);
    }

    [Fact]
    public void TestMixedPushPop() {
        var d = new Deque<int>(10);
        d.PushBack(10);  // Prepend 10: [10]
        d.PushFront(20); // Append 20: [10, 20]

        Assert.Equal(20, d.PeekFront());
        Assert.Equal(10, d.PeekBack());

        Assert.Equal(10, d[0]);
        Assert.Equal(20, d[1]);

        Assert.Equal(20, d.PopFront());
        Assert.Equal(10, d.PopBack());
    }

    [Fact]
    public void TestIndexer() {
        var d = new Deque<int>(10);
        // We want logical sequence [1, 2, 3]
        // Since PushFront appends:
        d.PushFront(1);
        d.PushFront(2);
        d.PushFront(3);

        Assert.Equal(1, d[0]);
        Assert.Equal(2, d[1]);
        Assert.Equal(3, d[2]);

        // Negative indices
        Assert.Equal(3, d[-1]);
        Assert.Equal(2, d[-2]);
        Assert.Equal(1, d[-3]);
    }

    [Fact]
    public void TestCircularBehavior() {
        var d = new Deque<int>(5); // Max items 4
        // Logic to wrap:
        d.PushFront(1);
        d.PushFront(2);
        d.PopBack(); // Removes 1
        d.PopBack(); // Removes 2

        // head/tail moved
        d.PushFront(10);
        d.PushFront(20);
        d.PushFront(30);
        d.PushFront(40);

        // Should be [10, 20, 30, 40]
        Assert.Equal(4, d.Count);
        Assert.Equal(10, d[0]);
        Assert.Equal(40, d[3]);
        Assert.Equal(40, d.PeekFront());
        Assert.Equal(10, d.PeekBack());
    }

    [Fact]
    public void TestEnumerationFifo() {
        var d = new Deque<int>(10);
        d.PushFront(1);
        d.PushFront(2);
        d.PushFront(3);

        // EnumerateFifo(amount) yields 'amount' items starting from index 0.
        var list = d.EnumerateFifo(3).ToList();
        Assert.Equal([1, 2, 3], list);
    }

    [Fact]
    public void TestEnumerationLifo() {
        var d = new Deque<int>(10);
        d.PushFront(1);
        d.PushFront(2);
        d.PushFront(3);
        // Logical: [1, 2, 3]
        // EnumerateLifo starts from the end (Front/Head).
        var list = d.EnumerateLifo(3).ToList();
        Assert.Equal([3, 2, 1], list);
    }

    [Fact]
    public void TestIndexerSet() {
        var d = new Deque<int>(5);
        d.PushFront(1);
        d.PushFront(2);

        d[0] = 10;
        d[-1] = 20;

        Assert.Equal(10, d[0]);
        Assert.Equal(20, d[1]);
        Assert.Equal(20, d.PeekFront());
        Assert.Equal(10, d.PeekBack());
    }

    [Fact]
    public void TestFullExceptionAndStateSafety() {
        // Capacity 3 means we can store 2 items (one slot always kept free)
        var d = new Deque<int>(3);
        d.PushFront(10); // Adds to end. d: [10]
        d.PushBack(20);  // Adds to front. d: [20, 10]

        Assert.Equal(2, d.Count);

        // Assert throws generic exception
        Assert.ThrowsAny<Exception>(() => d.PushFront(30));

        // Verify state preserved
        Assert.Equal(2, d.Count);
        Assert.Equal(20, d[0]);
        Assert.Equal(10, d[1]);

        Assert.ThrowsAny<Exception>(() => d.PushBack(30));

        // Verify state preserved
        Assert.Equal(2, d.Count);
        Assert.Equal(20, d[0]);
        Assert.Equal(10, d[1]);
    }

    [Fact]
    public void TestEmptyExceptionAndStateSafety() {
        var d = new Deque<int>(5);
        Assert.Equal(0, d.Count);

        Assert.ThrowsAny<Exception>(() => d.PopFront());
        Assert.Equal(0, d.Count);

        Assert.ThrowsAny<Exception>(() => d.PopBack());
        Assert.Equal(0, d.Count);

        Assert.ThrowsAny<Exception>(() => d.PeekFront());
        Assert.Equal(0, d.Count);

        Assert.ThrowsAny<Exception>(() => d.PeekBack());
        Assert.Equal(0, d.Count);
    }

    [Fact]
    public void TestIndexerExceptionAndStateSafety() {
        var d = new Deque<int>(4);
        d.PushBack(1);
        d.PushBack(2);
        // d: [2, 1]

        Assert.ThrowsAny<Exception>(() => { var x = d[2]; });
        Assert.ThrowsAny<Exception>(() => { var x = d[-3]; });

        Assert.ThrowsAny<Exception>(() => { d[2] = 5; });
        Assert.ThrowsAny<Exception>(() => { d[-3] = 5; });

        // State preserved
        Assert.Equal(2, d.Count);
        Assert.Equal(2, d[0]);
        Assert.Equal(1, d[1]);
    }

    [Fact]
    public void TestIEnumerableExplicit() {
        var d = new Deque<int>(2);
        d.PushBack(1);
        IEnumerable e = d;
        Assert.NotNull(e.GetEnumerator());
    }
}
