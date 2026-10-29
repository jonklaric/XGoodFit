using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Themes;
using Xunit;

namespace Themes.Tests.Core
{
    /// <summary>
    /// Regression tests for forensic-audit findings A-006 and A-014.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A-006 — <see cref="ThemeService.SetTheme(Theme)"/> must not deadlock when a
    /// subscriber re-enters the singleton from inside the <c>ThemeChanged</c> handler.
    /// The fix mutates <c>MergedDictionaries</c> and raises the event outside the
    /// internal <c>_lock</c>.
    /// </para>
    /// <para>
    /// A-014 — <see cref="ThemeService.SetTheme(Theme)"/> must marshal background-thread
    /// calls to the WPF UI thread instead of throwing.
    /// </para>
    /// <para>
    /// Both tests run on a single dedicated STA thread that owns
    /// <see cref="Application.Current"/> and runs a real <see cref="Dispatcher"/> loop,
    /// shared via <see cref="DispatcherTestHost"/>. Without that, each StaFact thread
    /// would create a fresh Application with its dispatcher bound to the calling thread —
    /// but Application.Current is process-wide, so the second test sees a Dispatcher
    /// belonging to the first test's thread, which has no message pump and deadlocks.
    /// </para>
    /// </remarks>
    public class ThemeServiceConcurrencyTests
    {
        [Fact]
        public void A006_ThemeService_SwitchesThemeUnderConcurrentAccess_DoesNotDeadlock()
        {
            DispatcherTestHost.Run(() =>
            {
                Theme observed = Theme.Light;
                EventHandler<ThemeChangedEventArgs> handler = (_, _) =>
                {
                    // Synchronously re-enter the service from inside the handler.
                    // Pre-fix this hung because SetTheme still held _lock; post-fix the
                    // mutation/event are outside the lock so this re-entry returns.
                    observed = ThemeService.Instance.CurrentTheme;
                };
                ThemeService.Instance.ThemeChanged += handler;

                try
                {
                    ThemeService.Instance.SetTheme(Theme.Dark);

                    Assert.Equal(Theme.Dark, observed);
                    Assert.Equal(Theme.Dark, ThemeService.Instance.CurrentTheme);
                }
                finally
                {
                    ThemeService.Instance.ThemeChanged -= handler;
                    if (ThemeService.Instance.CurrentTheme != Theme.Light)
                    {
                        ThemeService.Instance.SetTheme(Theme.Light);
                    }
                }
            });
        }

        [Fact]
        public void A014_ThemeService_BackgroundThreadSetTheme_MarshalsToUI()
        {
            DispatcherTestHost.Run(() =>
            {
                // Submit SetTheme from a non-dispatcher thread. The fix marshals to the
                // UI thread via Dispatcher.Invoke. The DispatcherTestHost is running a
                // real Dispatcher.Run() loop on a dedicated STA thread, and its dispatcher
                // is the one bound to Application.Current — so the marshalled call
                // actually reaches a thread that pumps it.
                var task = Task.Run(() => ThemeService.Instance.SetTheme(Theme.Dark));

                // Block the dispatcher thread momentarily while the worker runs. We
                // cannot Dispatcher.PushFrame here because we are already inside the
                // dispatcher; instead we yield via DispatcherFrame which runs queued
                // operations and exits when the worker completes.
                var frame = new DispatcherFrame();
                var watchdog = Task.Run(async () =>
                {
                    var deadline = DateTime.UtcNow.AddSeconds(5);
                    while (!task.IsCompleted && DateTime.UtcNow < deadline)
                    {
                        await Task.Delay(20).ConfigureAwait(false);
                    }
                    frame.Continue = false;
                });
                Dispatcher.PushFrame(frame);
                watchdog.Wait(TimeSpan.FromSeconds(1));

                try
                {
                    Assert.True(task.IsCompleted,
                        "Background-thread SetTheme did not complete within 5s (A-014).");
                    if (task.Exception != null)
                    {
                        Assert.Fail("Background-thread SetTheme threw: " + task.Exception);
                    }
                    Assert.Equal(Theme.Dark, ThemeService.Instance.CurrentTheme);
                }
                finally
                {
                    if (ThemeService.Instance.CurrentTheme != Theme.Light)
                    {
                        ThemeService.Instance.SetTheme(Theme.Light);
                    }
                }
            });
        }
    }

    /// <summary>
    /// Hosts a single dedicated STA thread that owns <see cref="Application.Current"/>
    /// and runs a real <see cref="Dispatcher"/> loop. Tests submit work via
    /// <see cref="Run"/> which executes synchronously on that thread. Sharing one
    /// dispatcher across tests prevents the cross-test Application.Current/Dispatcher
    /// thread mismatch that caused intermittent deadlocks under StaFact.
    /// </summary>
    internal static class DispatcherTestHost
    {
        private static readonly object _gate = new();
        private static Dispatcher? _dispatcher;
        private static Thread? _thread;

        private static Dispatcher EnsureRunning()
        {
            lock (_gate)
            {
                if (_dispatcher != null)
                {
                    return _dispatcher;
                }

                using var ready = new ManualResetEventSlim(false);
                Dispatcher? captured = null;

                _thread = new Thread(() =>
                {
                    // Application.Current is process-wide and binds to the thread that
                    // creates it, so we must do this on the dispatcher thread to ensure
                    // SetTheme's auto-marshal reaches a pumped dispatcher.
                    if (Application.Current == null)
                    {
                        _ = new Application();
                    }

                    if (!ThemeService.Instance.IsInitialized)
                    {
                        ThemeService.Instance.Initialize(Theme.Light);
                    }
                    else if (ThemeService.Instance.CurrentTheme != Theme.Light)
                    {
                        ThemeService.Instance.SetTheme(Theme.Light);
                    }

                    captured = Dispatcher.CurrentDispatcher;
                    ready.Set();
                    Dispatcher.Run();
                })
                {
                    IsBackground = true,
                    Name = "ThemeServiceTestsDispatcher",
                };
                _thread.SetApartmentState(ApartmentState.STA);
                _thread.Start();
                ready.Wait();
                _dispatcher = captured!;
                return _dispatcher;
            }
        }

        public static void Run(Action body)
        {
            var d = EnsureRunning();
            d.Invoke(body);
        }
    }
}
