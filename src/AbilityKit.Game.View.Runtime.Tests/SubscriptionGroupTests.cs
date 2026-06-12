using System;
using System.Collections.Generic;
using AbilityKit.Game.View.Foundation;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests;

public sealed class SubscriptionGroupTests
{
    [Fact]
    public void Clear_ReleasesItemsInReverseOrder()
    {
        var released = new List<string>();
        var group = new SubscriptionGroup<TestSubscription>(item => released.Add(item.Id));

        group.Add(new TestSubscription("first"));
        group.Add(new TestSubscription("second"));
        group.Add(new TestSubscription("third"));

        group.Clear();

        Assert.Equal(new[] { "third", "second", "first" }, released);
        Assert.Equal(0, group.Count);
    }

    [Fact]
    public void Clear_ContinuesAfterReleaseFailure()
    {
        var released = new List<string>();
        var failures = new List<Exception>();
        var group = new SubscriptionGroup<TestSubscription>(
            item =>
            {
                released.Add(item.Id);
                if (item.Id == "bad") throw new InvalidOperationException("release failed");
            },
            failures.Add);

        group.Add(new TestSubscription("first"));
        group.Add(new TestSubscription("bad"));
        group.Add(new TestSubscription("last"));

        group.Clear();

        Assert.Equal(new[] { "last", "bad", "first" }, released);
        Assert.Single(failures);
        Assert.Equal(0, group.Count);
    }

    [Fact]
    public void Clear_IgnoresNestedClear()
    {
        var released = new List<string>();
        SubscriptionGroup<TestSubscription>? group = null;
        group = new SubscriptionGroup<TestSubscription>(item =>
        {
            released.Add(item.Id);
            group!.Clear();
        });

        group.Add(new TestSubscription("first"));
        group.Add(new TestSubscription("second"));

        group.Clear();

        Assert.Equal(new[] { "second", "first" }, released);
        Assert.Equal(0, group.Count);
    }

    [Fact]
    public void Remove_CanReleaseSingleItem()
    {
        var released = new List<string>();
        var group = new SubscriptionGroup<TestSubscription>(item => released.Add(item.Id));
        var first = group.Add(new TestSubscription("first"));
        group.Add(new TestSubscription("second"));

        var removed = group.Remove(first);

        Assert.True(removed);
        Assert.Equal(new[] { "first" }, released);
        Assert.Equal(1, group.Count);
    }

    private sealed class TestSubscription
    {
        public TestSubscription(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}
