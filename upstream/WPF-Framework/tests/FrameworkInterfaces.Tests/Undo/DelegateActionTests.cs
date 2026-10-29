using Xunit;
using FrameworkInterfaces.Undo.Actions;

namespace FrameworkInterfaces.Tests.Undo
{
    /// <summary>
    /// Test class for the DelegateAction implementation, providing comprehensive tests for delegate-based
    /// undo/redo actions, description management, target tracking, and action merging behavior.
    /// </summary>
    public class DelegateActionTests
    {
        #region Constructor Tests

        /// <summary>
        /// Verifies that the constructor correctly sets the Description property.
        /// </summary>
        [Fact]
        public void Constructor_SetsDescription()
        {
            var action = new DelegateAction("Test Description", () => { }, () => { });

            Assert.Equal("Test Description", action.Description);
        }

        /// <summary>
        /// Verifies that the constructor correctly sets the Target property when provided.
        /// </summary>
        [Fact]
        public void Constructor_SetsTarget()
        {
            var target = new object();
            var action = new DelegateAction("Test", () => { }, () => { }, target);

            Assert.Same(target, action.Target);
        }

        /// <summary>
        /// Verifies that the Target property is null when no target is provided to the constructor.
        /// </summary>
        [Fact]
        public void Constructor_WithNullTarget_TargetIsNull()
        {
            var action = new DelegateAction("Test", () => { }, () => { });

            Assert.Null(action.Target);
        }

        /// <summary>
        /// Verifies that the constructor sets a timestamp within the expected time range.
        /// </summary>
        [Fact]
        public void Constructor_SetsTimestamp()
        {
            var before = DateTime.Now;
            var action = new DelegateAction("Test", () => { }, () => { });
            var after = DateTime.Now;

            Assert.True(action.Timestamp >= before);
            Assert.True(action.Timestamp <= after);
        }

        #endregion

        #region Execute Tests

        /// <summary>
        /// Verifies that executing the action invokes the execute delegate.
        /// </summary>
        [Fact]
        public void Execute_CallsExecuteDelegate()
        {
            var executed = false;
            var action = new DelegateAction("Test", () => executed = true, () => { });

            action.Execute();

            Assert.True(executed);
        }

        /// <summary>
        /// Verifies that the execute delegate can access and modify captured variables.
        /// </summary>
        [Fact]
        public void Execute_WithParameters_PassesParameters()
        {
            var capturedValue = 0;
            var action = new DelegateAction("Test",
                () => capturedValue = 42,
                () => capturedValue = 0);

            action.Execute();

            Assert.Equal(42, capturedValue);
        }

        #endregion

        #region Undo Tests

        /// <summary>
        /// Verifies that undoing the action invokes the undo delegate.
        /// </summary>
        [Fact]
        public void Undo_CallsUndoDelegate()
        {
            var undone = false;
            var action = new DelegateAction("Test", () => { }, () => undone = true);

            action.Undo();

            Assert.True(undone);
        }

        /// <summary>
        /// Verifies that executing and then undoing the action correctly applies both delegates.
        /// </summary>
        [Fact]
        public void Execute_ThenUndo_WorksCorrectly()
        {
            var value = "initial";
            var action = new DelegateAction("Test",
                () => value = "executed",
                () => value = "undone");

            action.Execute();
            Assert.Equal("executed", value);

            action.Undo();
            Assert.Equal("undone", value);
        }

        #endregion

        #region CanMergeWith Tests

        /// <summary>
        /// Verifies that DelegateAction does not support merging with other actions by default.
        /// </summary>
        /// <returns>False, indicating the action cannot be merged.</returns>
        [Fact]
        public void CanMergeWith_ByDefault_ReturnsFalse()
        {
            var action1 = new DelegateAction("Test1", () => { }, () => { });
            var action2 = new DelegateAction("Test2", () => { }, () => { });

            Assert.False(action1.CanMergeWith(action2));
        }

        /// <summary>
        /// Verifies that CanMergeWith returns false when passed a null action.
        /// </summary>
        /// <returns>False when action is null.</returns>
        [Fact]
        public void CanMergeWith_Null_ReturnsFalse()
        {
            var action = new DelegateAction("Test", () => { }, () => { });

            Assert.False(action.CanMergeWith(null!));
        }

        #endregion

        #region MergeWith Tests

        /// <summary>
        /// Verifies that attempting to merge returns the original action unchanged by default.
        /// </summary>
        /// <returns>The original action instance.</returns>
        [Fact]
        public void MergeWith_ByDefault_ReturnsSelf()
        {
            var action1 = new DelegateAction("Test1", () => { }, () => { });
            var action2 = new DelegateAction("Test2", () => { }, () => { });

            var result = action1.MergeWith(action2);

            Assert.Same(action1, result);
        }

        #endregion
    }
}
