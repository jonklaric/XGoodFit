using Xunit;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using System.Windows.Media;
using System.IO;

namespace FrameworkInterfaces.Tests.Messaging
{
    /// <summary>
    /// Test class for the Messenger singleton implementation, providing comprehensive tests for message management,
    /// event notification, property configuration, and file export functionality.
    /// </summary>
    public class MessengerTests
    {
        /// <summary>
        /// Gets a fresh Messenger instance with cleared message collection for testing.
        /// </summary>
        /// <returns>A Messenger instance with no messages.</returns>
        private Messenger GetFreshMessenger()
        {
            var messenger = Messenger.GetInstance();
            messenger.Clear();
            return messenger;
        }

        #region Singleton Tests

        /// <summary>
        /// Verifies that the Messenger GetInstance method returns the same singleton instance on multiple calls.
        /// </summary>
        [Fact]
        public void GetInstance_ReturnsSameInstance()
        {
            var instance1 = Messenger.GetInstance();
            var instance2 = Messenger.GetInstance();

            Assert.Same(instance1, instance2);
        }

        #endregion

        #region Add Message Tests

        /// <summary>
        /// Verifies that adding a single message successfully adds it to the Messenger collection.
        /// </summary>
        [Fact]
        public void Add_SingleMessage_AddsToCollection()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            var message = new BasicMessageItem(MessageType.Error, "Test error", source, "Collection", "Name", code: "TST-001");

            messenger.Add(message);

            var allMessages = messenger.AllMessageItems();
            Assert.Single(allMessages);
            Assert.Same(message, allMessages[0]);
        }

        /// <summary>
        /// Verifies that attempting to add a null message throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        [Fact]
        public void Add_NullMessage_ThrowsArgumentNullException()
        {
            var messenger = GetFreshMessenger();

            Assert.Throws<ArgumentNullException>(() => messenger.Add((IMessageItem)null!));
        }

        /// <summary>
        /// Verifies that a message with a null source is not added to the collection.
        /// </summary>
        [Fact]
        public void Add_MessageWithNullSource_DoesNotAdd()
        {
            var messenger = GetFreshMessenger();
            var message = new BasicMessageItem { Source = null, Code = "TST-001" };

            messenger.Add(message);

            Assert.Empty(messenger.AllMessageItems());
        }

        /// <summary>
        /// Verifies that attempting to add a duplicate message (same code and source) ignores the duplicate.
        /// </summary>
        [Fact]
        public void Add_DuplicateMessage_IgnoresDuplicate()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            var message1 = new BasicMessageItem(MessageType.Error, "Error 1", source, "Collection", "Name", code: "TST-001");
            var message2 = new BasicMessageItem(MessageType.Error, "Error 2", source, "Collection", "Name", code: "TST-001");

            messenger.Add(message1);
            messenger.Add(message2);

            var allMessages = messenger.AllMessageItems();
            Assert.Single(allMessages);
            Assert.Equal("Error 1", allMessages[0].Description);
        }

        /// <summary>
        /// Verifies that adding event messages with the same code auto-increments the code for subsequent events.
        /// </summary>
        [Fact]
        public void Add_EventMessage_AutoIncrementsCode()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            var event1 = new BasicMessageItem(MessageType.Event, "Event 1", source, "Collection", "Name", code: "EVT-001");
            var event2 = new BasicMessageItem(MessageType.Event, "Event 2", source, "Collection", "Name", code: "EVT-001");

            messenger.Add(event1);
            messenger.Add(event2);

            var allMessages = messenger.AllMessageItems();
            Assert.Equal(2, allMessages.Count);
            Assert.Equal("EVT-001", event1.Code);
            Assert.Equal("EVT-0011", event2.Code);
        }

        /// <summary>
        /// Verifies that adding a message raises the MessagesAdded event with the correct message.
        /// </summary>
        [Fact]
        public void Add_RaisesMessagesAddedEvent()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            var message = new BasicMessageItem(MessageType.Warning, "Warning", source, "Collection", "Name", code: "WRN-001");
            IMessageItem[]? addedMessages = null;

            messenger.MessagesAdded += msgs => addedMessages = msgs;
            messenger.Add(message);

            Assert.NotNull(addedMessages);
            Assert.Single(addedMessages);
            Assert.Same(message, addedMessages[0]);
        }

        /// <summary>
        /// Verifies that adding multiple messages in a single call adds all messages to the collection.
        /// </summary>
        [Fact]
        public void Add_MultipleMessages_AddsAll()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            var messages = new[]
            {
                new BasicMessageItem(MessageType.Error, "Error 1", source, "Collection", "Name", code: "ERR-001"),
                new BasicMessageItem(MessageType.Warning, "Warning 1", source, "Collection", "Name", code: "WRN-001"),
                new BasicMessageItem(MessageType.Message, "Message 1", source, "Collection", "Name", code: "MSG-001")
            };

            messenger.Add(messages);

            Assert.Equal(3, messenger.AllMessageItems().Count);
        }

        /// <summary>
        /// Verifies that attempting to add a null enumerable of messages throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when messages collection is null.</exception>
        [Fact]
        public void Add_NullEnumerable_ThrowsArgumentNullException()
        {
            var messenger = GetFreshMessenger();

            Assert.Throws<ArgumentNullException>(() => messenger.Add((IEnumerable<IMessageItem>)null!));
        }

        #endregion

        #region Remove Message Tests

        /// <summary>
        /// Verifies that removing an existing message returns true and removes it from the collection.
        /// </summary>
        /// <returns>True if the message was successfully removed.</returns>
        [Fact]
        public void Remove_ExistingMessage_ReturnsTrue()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            var message = new BasicMessageItem(MessageType.Error, "Error", source, "Collection", "Name", code: "ERR-001");
            messenger.Add(message);

            var result = messenger.Remove(message);

            Assert.True(result);
            Assert.Empty(messenger.AllMessageItems());
        }

        /// <summary>
        /// Verifies that attempting to remove a non-existent message returns false.
        /// </summary>
        /// <returns>False if the message was not found in the collection.</returns>
        [Fact]
        public void Remove_NonExistentMessage_ReturnsFalse()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            var message = new BasicMessageItem(MessageType.Error, "Error", source, "Collection", "Name", code: "ERR-001");

            var result = messenger.Remove(message);

            Assert.False(result);
        }

        /// <summary>
        /// Verifies that attempting to remove a null message throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        [Fact]
        public void Remove_NullMessage_ThrowsArgumentNullException()
        {
            var messenger = GetFreshMessenger();

            Assert.Throws<ArgumentNullException>(() => messenger.Remove(null!));
        }

        /// <summary>
        /// Verifies that attempting to remove a message with a null source returns false.
        /// </summary>
        /// <returns>False since messages with null sources cannot be tracked.</returns>
        [Fact]
        public void Remove_MessageWithNullSource_ReturnsFalse()
        {
            var messenger = GetFreshMessenger();
            var message = new BasicMessageItem { Source = null };

            Assert.False(messenger.Remove(message));
        }

        /// <summary>
        /// Verifies that removing a message raises the MessagesRemoved event.
        /// </summary>
        [Fact]
        public void Remove_RaisesMessagesRemovedEvent()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            var message = new BasicMessageItem(MessageType.Error, "Error", source, "Collection", "Name", code: "ERR-001");
            messenger.Add(message);
            IMessageItem[]? removedMessages = null;

            messenger.MessagesRemoved += msgs => removedMessages = msgs;
            messenger.Remove(message);

            Assert.NotNull(removedMessages);
            Assert.Single(removedMessages);
        }

        #endregion

        #region Clear Tests

        /// <summary>
        /// Verifies that Clear removes all messages from the collection.
        /// </summary>
        [Fact]
        public void Clear_RemovesAllMessages()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            messenger.Add(new BasicMessageItem(MessageType.Error, "Error 1", source, "Collection", "Name", code: "ERR-001"));
            messenger.Add(new BasicMessageItem(MessageType.Error, "Error 2", source, "Collection", "Name", code: "ERR-002"));

            messenger.Clear();

            Assert.Empty(messenger.AllMessageItems());
        }

        /// <summary>
        /// Verifies that Clear with a specific source only removes messages from that source.
        /// </summary>
        [Fact]
        public void Clear_WithSource_RemovesOnlySourceMessages()
        {
            var messenger = GetFreshMessenger();
            var source1 = new object();
            var source2 = new object();
            messenger.Add(new BasicMessageItem(MessageType.Error, "Error 1", source1, "Collection", "Name", code: "ERR-001"));
            messenger.Add(new BasicMessageItem(MessageType.Error, "Error 2", source2, "Collection", "Name", code: "ERR-002"));

            messenger.Clear(source1);

            var allMessages = messenger.AllMessageItems();
            Assert.Single(allMessages);
            Assert.Equal("Error 2", allMessages[0].Description);
        }

        /// <summary>
        /// Verifies that attempting to clear messages with a null source throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
        [Fact]
        public void Clear_WithNullSource_ThrowsArgumentNullException()
        {
            var messenger = GetFreshMessenger();

            Assert.Throws<ArgumentNullException>(() => messenger.Clear(null!));
        }

        /// <summary>
        /// Verifies that clearing messages for a non-existent source does not throw an exception.
        /// </summary>
        [Fact]
        public void Clear_WithNonExistentSource_DoesNotThrow()
        {
            var messenger = GetFreshMessenger();
            var source = new object();

            var exception = Record.Exception(() => messenger.Clear(source));

            Assert.Null(exception);
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Verifies that the ShowErrors property defaults to true.
        /// </summary>
        [Fact]
        public void ShowErrors_DefaultTrue()
        {
            var messenger = Messenger.GetInstance();
            Assert.True(messenger.ShowErrors);
        }

        /// <summary>
        /// Verifies that the ShowErrors property can be set and retrieved correctly.
        /// </summary>
        /// <param name="value">The value to set for ShowErrors.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShowErrors_SetAndGet(bool value)
        {
            var messenger = Messenger.GetInstance();
            messenger.ShowErrors = value;
            Assert.Equal(value, messenger.ShowErrors);
        }

        /// <summary>
        /// Verifies that changing the ShowErrors property raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void ShowErrors_WhenChanged_RaisesPropertyChanged()
        {
            var messenger = Messenger.GetInstance();
            var originalValue = messenger.ShowErrors;
            var propertyChanged = false;
            messenger.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Messenger.ShowErrors))
                    propertyChanged = true;
            };

            messenger.ShowErrors = !originalValue;

            Assert.True(propertyChanged);
            messenger.ShowErrors = originalValue;
        }

        /// <summary>
        /// Verifies that the ShowWarnings property defaults to true.
        /// </summary>
        [Fact]
        public void ShowWarnings_DefaultTrue()
        {
            var messenger = Messenger.GetInstance();
            Assert.True(messenger.ShowWarnings);
        }

        /// <summary>
        /// Verifies that the ShowMessages property defaults to true.
        /// </summary>
        [Fact]
        public void ShowMessages_DefaultTrue()
        {
            var messenger = Messenger.GetInstance();
            Assert.True(messenger.ShowMessages);
        }

        /// <summary>
        /// Verifies that the ShowEvents property defaults to true.
        /// </summary>
        [Fact]
        public void ShowEvents_DefaultTrue()
        {
            var messenger = Messenger.GetInstance();
            Assert.True(messenger.ShowEvents);
        }

        /// <summary>
        /// Verifies that the ErrorColor property defaults to the custom red.
        /// </summary>
        [Fact]
        public void ErrorColor_DefaultRed()
        {
            var messenger = Messenger.GetInstance();
            Assert.Equal(Color.FromRgb(228, 20, 0), messenger.ErrorColor.Color);
        }

        /// <summary>
        /// Verifies that the WarningColor property defaults to the custom orange.
        /// </summary>
        [Fact]
        public void WarningColor_DefaultDarkOrange()
        {
            var messenger = Messenger.GetInstance();
            Assert.Equal(Color.FromRgb(229, 160, 0), messenger.WarningColor.Color);
        }

        /// <summary>
        /// Verifies that the MessageColor property defaults to the custom blue.
        /// </summary>
        [Fact]
        public void MessageColor_DefaultBlue()
        {
            var messenger = Messenger.GetInstance();
            Assert.Equal(Color.FromRgb(26, 161, 226), messenger.MessageColor.Color);
        }

        /// <summary>
        /// Verifies that the EventColor property defaults to dark turquoise.
        /// </summary>
        [Fact]
        public void EventColor_DefaultBlack()
        {
            var messenger = Messenger.GetInstance();
            Assert.Equal(Color.FromRgb(0, 206, 209), messenger.EventColor.Color);
        }

        /// <summary>
        /// Verifies that setting the ErrorColor property to null does not change the value.
        /// </summary>
        [Fact]
        public void ErrorColor_SetNull_DoesNotChange()
        {
            var messenger = Messenger.GetInstance();
            var originalColor = messenger.ErrorColor;

            messenger.ErrorColor = null!;

            Assert.Same(originalColor, messenger.ErrorColor);
        }

        /// <summary>
        /// Verifies that the TextFileName property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void TextFileName_SetAndGet()
        {
            var messenger = Messenger.GetInstance();
            var testPath = @"C:\test\messages.txt";

            messenger.TextFileName = testPath;

            Assert.Equal(testPath, messenger.TextFileName);
        }

        /// <summary>
        /// Verifies that setting the TextFileName property to null defaults it to an empty string.
        /// </summary>
        [Fact]
        public void TextFileName_SetNull_DefaultsToEmpty()
        {
            var messenger = Messenger.GetInstance();
            messenger.TextFileName = "test.txt";

            messenger.TextFileName = null!;

            Assert.Equal(string.Empty, messenger.TextFileName);
        }

        /// <summary>
        /// Verifies that the WriteToFile property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void WriteToFile_SetAndGet()
        {
            var messenger = Messenger.GetInstance();

            messenger.WriteToFile = true;
            Assert.True(messenger.WriteToFile);

            messenger.WriteToFile = false;
            Assert.False(messenger.WriteToFile);
        }

        /// <summary>
        /// Verifies that the ErrorBeep property can be set and retrieved correctly.
        /// </summary>
        /// <param name="value">The value to set for ErrorBeep.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ErrorBeep_SetAndGet(bool value)
        {
            var messenger = Messenger.GetInstance();
            messenger.ErrorBeep = value;
            Assert.Equal(value, messenger.ErrorBeep);
        }

        /// <summary>
        /// Verifies that the WarningBeep property can be set and retrieved correctly.
        /// </summary>
        /// <param name="value">The value to set for WarningBeep.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WarningBeep_SetAndGet(bool value)
        {
            var messenger = Messenger.GetInstance();
            messenger.WarningBeep = value;
            Assert.Equal(value, messenger.WarningBeep);
        }

        /// <summary>
        /// Verifies that the MessageBeep property can be set and retrieved correctly.
        /// </summary>
        /// <param name="value">The value to set for MessageBeep.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MessageBeep_SetAndGet(bool value)
        {
            var messenger = Messenger.GetInstance();
            messenger.MessageBeep = value;
            Assert.Equal(value, messenger.MessageBeep);
        }

        /// <summary>
        /// Verifies that the EventBeep property can be set and retrieved correctly.
        /// </summary>
        /// <param name="value">The value to set for EventBeep.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EventBeep_SetAndGet(bool value)
        {
            var messenger = Messenger.GetInstance();
            messenger.EventBeep = value;
            Assert.Equal(value, messenger.EventBeep);
        }

        #endregion

        #region Export Tests

        /// <summary>
        /// Verifies that attempting to export to a text file with a null filename throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
        [Fact]
        public void ExportToTextFile_NullFileName_ThrowsArgumentNullException()
        {
            var messenger = GetFreshMessenger();

            Assert.Throws<ArgumentNullException>(() => messenger.ExportToTextFile(null!));
        }

        /// <summary>
        /// Verifies that attempting to export to a text file with an empty filename throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when filename is empty.</exception>
        [Fact]
        public void ExportToTextFile_EmptyFileName_ThrowsArgumentNullException()
        {
            var messenger = GetFreshMessenger();

            Assert.Throws<ArgumentNullException>(() => messenger.ExportToTextFile(string.Empty));
        }

        /// <summary>
        /// Verifies that exporting messages to a text file writes the messages correctly.
        /// </summary>
        [Fact]
        public void ExportToTextFile_WritesMessages()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            messenger.Add(new BasicMessageItem(MessageType.Error, "Test error message", source, "Collection", "Element", code: "ERR-001"));

            var tempFile = Path.Combine(Path.GetTempPath(), $"messenger_test_{Guid.NewGuid()}.txt");
            try
            {
                messenger.ExportToTextFile(tempFile);

                Assert.True(File.Exists(tempFile));
                var content = File.ReadAllText(tempFile);
                Assert.Contains("Test error message", content);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that exporting to a text file creates the directory if it does not exist.
        /// </summary>
        [Fact]
        public void ExportToTextFile_CreatesDirectoryIfNotExists()
        {
            var messenger = GetFreshMessenger();
            var source = new object();
            messenger.Add(new BasicMessageItem(MessageType.Message, "Test message", source, "Collection", "Element", code: "MSG-001"));

            var tempDir = Path.Combine(Path.GetTempPath(), $"messenger_test_{Guid.NewGuid()}");
            var tempFile = Path.Combine(tempDir, "messages.txt");
            try
            {
                messenger.ExportToTextFile(tempFile);

                Assert.True(Directory.Exists(tempDir));
                Assert.True(File.Exists(tempFile));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        #endregion
    }
}
