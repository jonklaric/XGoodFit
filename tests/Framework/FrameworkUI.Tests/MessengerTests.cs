using System.Runtime.CompilerServices;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;

namespace FrameworkUI.Tests;

/// <summary>
/// XGoodFit addition: FrameworkInterfaces' Messenger, which the shell's Message Window shows, forgets a source
/// once it has no messages, so it doesn't keep an element (and through it, its project) alive.
/// </summary>
[TestClass]
public sealed class MessengerTests
{
    [TestMethod]
    public void ASourceWithoutMessages_IsForgotten_SoItCanBeCollected()
    {
        HeadlessSession.Run(() =>
        {
            Messenger messenger = Messenger.GetInstance();
            WeakReference removed = AddMessage(messenger, removeWithClear: false);
            WeakReference cleared = AddMessage(messenger, removeWithClear: true);
            for (int i = 0; i < 3 && (removed.IsAlive || cleared.IsAlive); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.IsFalse(removed.IsAlive, "a source whose last message was removed");
            Assert.IsFalse(cleared.IsAlive, "a source whose messages were cleared");
        });
    }

    [TestMethod]
    public void ASourcesMessages_AreKept_UntilTheLastIsRemoved()
    {
        HeadlessSession.Run(() =>
        {
            Messenger messenger = Messenger.GetInstance();
            var source = new object();
            var first = new BasicMessageItem(MessageType.Warning, "First", source, "Collection", "Element", code: "XGF-TEST-001");
            var second = new BasicMessageItem(MessageType.Warning, "Second", source, "Collection", "Element", code: "XGF-TEST-002");
            messenger.Add(first);
            messenger.Add(second);
            try
            {
                Assert.IsTrue(messenger.Remove(first));
                CollectionAssert.Contains(messenger.AllMessageItems(), second, "the source's other message stays");

                // A message added after the source was forgotten is kept again.
                Assert.IsTrue(messenger.Remove(second));
                messenger.Add(first);
                CollectionAssert.Contains(messenger.AllMessageItems(), first);
            }
            finally
            {
                messenger.Clear(source);
            }
        });
    }

    // Adds a message for a new source and removes it, keeping only a weak reference to the source.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddMessage(Messenger messenger, bool removeWithClear)
    {
        var source = new object();
        var message = new BasicMessageItem(MessageType.Warning, "Test", source, "Collection", "Element", code: "XGF-TEST-000");
        messenger.Add(message);
        if (removeWithClear)
            messenger.Clear(source);
        else
            messenger.Remove(message);
        return new WeakReference(source);
    }
}
