using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using SM.Media.BackgroundAudio;
using SM.Media.Utility;
using BackgroundAudio.Sample;
using Windows.Media.Playback;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Core;
using System.Threading;
using Windows.Media;
using System.Diagnostics;
using MCGIApp.Common;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace MCGIApp.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BackgroundAudioPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        readonly MediaPlayerHandle _mediaPlayerHandle;
        readonly DispatcherTimer _timer;
        int _refreshPending;
        string _trackName;

        public BackgroundAudioPage()
        {
            InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            _mediaPlayerHandle = new MediaPlayerHandle(Dispatcher);

            _mediaPlayerHandle.MessageReceivedFromBackground += OnMessageReceivedFromBackground;
            _mediaPlayerHandle.CurrentStateChanged += OnCurrentStateChanged;

            NavigationCacheMode = NavigationCacheMode.Required;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.6)
            };

            var count = 0;

            _timer.Tick += (sender, o) =>
            {
                var mediaPlayer = MediaPlayer;

                if (null == mediaPlayer)
                    return;

                var positionText = string.Empty;

                try
                {
                    var position = mediaPlayer.Position;

                    positionText = position.ToString("G");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MainPage position update failed: " + ex.Message);

                    // The COM object is probably dead...
                    CleanupFailedPlayer();
                }

                txtPosition.Text = positionText;

                if (++count < 5)
                    return;

                count = 0;

                _mediaPlayerHandle.NotifyBackground(BackgroundNotificationType.Memory);
            };
        }
        

        MediaPlayer MediaPlayer
        {
            get
            {
                Debug.Assert(Dispatcher.HasThreadAccess, "MediaPlayer requires the dispatcher thread");

                return _mediaPlayerHandle.MediaPlayer;
            }
        }

        bool IsRunning
        {
            get { return _mediaPlayerHandle.IsRunning; }
        }

        void CloseMediaPlayerAndUpdate()
        {
            Debug.WriteLine("MainPage.CloseMediaPlayerAndUpdate()");

            CloseMediaPlayer();

            RequestRefresh();
        }

        void CloseMediaPlayer()
        {
            Debug.WriteLine("MainPage.CloseMediaPlayer()");

            _timer.Stop();

            _mediaPlayerHandle.Close();

            _trackName = null;
        }

        async Task OpenMediaPlayerAsync()
        {
            Debug.WriteLine("MainPage.OpenMediaPlayer()");

            Debug.Assert(Dispatcher.HasThreadAccess, "OpenMediaPlayerAsync() requires the dispatcher thread");

            _timer.Start();

            await _mediaPlayerHandle.OpenAsync();

            var mediaPlayer = MediaPlayer;

            if (null == mediaPlayer)
            {
                Debug.WriteLine("MainPage.OpenMediaPlayer() failed");

                _timer.Stop();

                RequestRefresh();

                return;
            }

            RefreshUi(mediaPlayer.CurrentState, _trackName);
        }

        void CleanupFailedPlayer()
        {
            Debug.WriteLine("MainPage.CleanupFailedPlayer()");

            try
            {
                CloseMediaPlayerAndUpdate();

                _mediaPlayerHandle.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage.CleanupFailedPlayer() failed: " + ex.ExtendedMessage());
            }
        }

        /// <summary>
        ///     Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">
        ///     Event data that describes how this page was reached.
        ///     This parameter is typically used to configure the page.
        /// </param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("MainPage.OnNavigatedTo()");

            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            Application.Current.Suspending += OnSuspending;
            Application.Current.Resuming += OnResuming;
            Application.Current.UnhandledException += OnUnhandledException;

            CloseMediaPlayerAndUpdate();

            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("MainPage.OnNavigatedFrom()");

            Application.Current.Suspending -= OnSuspending;
            Application.Current.Resuming -= OnResuming;
            Application.Current.UnhandledException -= OnUnhandledException;

            OnSuspending(null, null);

            this.navigationHelper.OnNavigatedFrom(e);
        }

        void OnResuming(object sender, object o)
        {
            Debug.WriteLine("MainPage.OnResuming()");

            var awaiter = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var task = ResumeAsync();

                TaskCollector.Default.Add(task, "MainPage ResumeAsync");
            });

            TaskCollector.Default.Add(awaiter.AsTask(), "MainPage ResumeAsync dispatch");
        }

        async Task ResumeAsync()
        {
            Debug.WriteLine("MainPage.ResumeAsync()");

            Debug.Assert(Dispatcher.HasThreadAccess, "ResumeAsync requires the dispatcher thread");

            try
            {
                await _mediaPlayerHandle.ResumeAsync().ConfigureAwait(true);

                RequestRefresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage.ResumeAsync() failed: " + ex.ExtendedMessage());
            }

            try
            {
                var mediaPlayer = _mediaPlayerHandle.MediaPlayer;

                if (null != mediaPlayer)
                {
                    if (MediaPlayerState.Closed != mediaPlayer.CurrentState)
                    {
                        _timer.Stop();
                        _timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage.ResumeAsync() timer restart failed: " + ex.ExtendedMessage());
            }
        }

        void OnSuspending(object sender, SuspendingEventArgs suspendingEventArgs)
        {
            //var deferral = suspendingEventArgs.SuspendingOperation.GetDeferral();

            Debug.WriteLine("MainPage.OnSuspending()");

            _mediaPlayerHandle.Suspend();

            _timer.Stop();

            //deferral.Complete();
        }

        void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine("MainPage.OnUnhandledException()");

            _mediaPlayerHandle.Fail();
        }

        void OnMessageReceivedFromBackground(object sender, ValueSet notification)
        {
            //Debug.WriteLine("MainPage.OnMessageReceivedFromBackground()");

            long? memoryValue = null;
            ulong? appMemoryValue = null;
            ulong? appMemoryLimitValue = null;
            var updateMemory = false;
            string failMessage = null;
            string trackName = null;
            var callShutdown = false;

            foreach (var kv in notification)
            {
                //Debug.WriteLine(" b->f {0}: {1}", kv.Key, kv.Value);

                try
                {
                    if (null == kv.Key)
                    {
                        Debug.WriteLine("*** MainPage.OnMessageReceivedFromBackground() null key");

                        continue; // This does happen.  It shouldn't, but it does.
                    }

                    BackgroundNotificationType type;
                    if (!Enum.TryParse(kv.Key, true, out type))
                        continue;

                    switch (type)
                    {
                        case BackgroundNotificationType.Track:
                            trackName = kv.Value as string ?? string.Empty;

                            break;
                        case BackgroundNotificationType.Fail:
                            callShutdown = true;
                            failMessage = kv.Value as string;

                            Debug.WriteLine("MainPage.OnMessageReceivedFromBackground() fail " + failMessage);

                            break;
                        case BackgroundNotificationType.Memory:
                            memoryValue = kv.Value as long?;
                            if (memoryValue.HasValue)
                                updateMemory = true;

                            break;
                        case BackgroundNotificationType.AppMemory:
                            appMemoryValue = kv.Value as ulong?;
                            if (appMemoryValue.HasValue)
                                updateMemory = true;

                            break;

                        case BackgroundNotificationType.AppMemoryLimit:
                            appMemoryLimitValue = kv.Value as ulong?;
                            if (appMemoryLimitValue.HasValue)
                                updateMemory = true;

                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MainPage.OnMessageReceivedFromBackground() failed: " + ex.Message);
                }
            }

            if (null != failMessage)
                _trackName = null;

            if (null != trackName)
                _trackName = trackName;

            if (null != failMessage || null != trackName)
                RequestRefresh();

            if (updateMemory)
            {
                var memoryString = string.Format("{0:F2}MiB {1:F2}MiB/{2:F2}MiB",
                    memoryValue.BytesToMiB(), appMemoryValue.BytesToMiB(), appMemoryLimitValue.BytesToMiB());

                var awaiter = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => txtMemory.Text = memoryString);
            }

            if (callShutdown)
            {
                var awaiter2 = Dispatcher.RunAsync(CoreDispatcherPriority.Low, CleanupFailedPlayer);
            }
        }

        void OnCurrentStateChanged(object obj, object args)
        {
            Debug.WriteLine("MainPage.OnCurrentStateChanged()");

            RequestRefresh();
        }

        void RefreshUi()
        {
            //Debug.WriteLine("MainPage.RefreshUi()");

            Debug.Assert(Dispatcher.HasThreadAccess, "RefreshUi requires the dispatcher thread");

            while (0 != Interlocked.Exchange(ref _refreshPending, 0))
            {
                try
                {
                    var mediaPlayer = MediaPlayer;

                    if (null == mediaPlayer)
                    {
                        txtPosition.Text = string.Empty;
                        RefreshUi(MediaPlayerState.Closed, null);

                        return;
                    }

                    MediaPlayerState? mediaPlayerState = null;

                    try
                    {
                        mediaPlayerState = mediaPlayer.CurrentState;
                    }
                    catch (Exception mediaPlayerException)
                    {
                        Debug.WriteLine("MainPage.RefreshUi() mediaPlayer failed: " + mediaPlayerException.ExtendedMessage());
                    }

                    if (mediaPlayerState.HasValue)
                        RefreshUi(mediaPlayerState.Value, _trackName);
                    else
                        CleanupFailedPlayer();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MainPage.RefreshUi() failed: " + ex.Message);
                }
            }
        }

        void RefreshUi(MediaPlayerState currentState, string track)
        {
            Debug.WriteLine("MainPage.RefreshUi({0}, {1}) {2}", currentState, track, _mediaPlayerHandle.Id);

            txtCurrentTrack.Text = track ?? "MCGI";
            txtCurrentState.Text = currentState.ToString();

            //playButton.Content = MediaPlayerState.Playing == currentState ? "| |" : ">";

            var imgSource = MediaPlayerState.Playing == currentState ? "ms-appx:///Assets/Icons/pause.png" : "ms-appx:///Assets/Icons/play.png";
            
            backgroundButton.Source = new BitmapImage(new System.Uri(imgSource, UriKind.RelativeOrAbsolute));
            
            if (MediaPlayerState.Opening == currentState)
            {
                //prevButton.IsEnabled = false;
                playButton.IsEnabled = false;
                //nextButton.IsEnabled = false;
            }
            else
            {
                prevButton.IsEnabled = true;
                playButton.IsEnabled = true;
                nextButton.IsEnabled = true;
            }

            var needTimer = MediaPlayerState.Closed != currentState;

            if (needTimer == _timer.IsEnabled)
                return;

            if (needTimer)
                _timer.Start();
            else
                _timer.Stop();
        }

        void RequestRefresh()
        {
            var was = Interlocked.Exchange(ref _refreshPending, 1);

            if (0 != was)
                return;

            var awaiter = Dispatcher.RunAsync(CoreDispatcherPriority.Low, RefreshUi);

            TaskCollector.Default.Add(awaiter.AsTask(), "MainPage RefreshUi");
        }

        Task StartAudioAsync()
        {
            if (IsRunning)
                return TplTaskExtensions.TrueTask;

            return OpenMediaPlayerAsync();
        }

        #region Button Click Event Handlers

        void gcButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainPage gc");

            _mediaPlayerHandle.NotifyBackground(BackgroundNotificationType.Gc);
        }

        void wakeButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainPage wake");

            if (Debugger.IsAttached)
                Debugger.Break();

            RequestRefresh();
        }

        /// <summary>
        ///     Sends message to the background task to skip to the previous track.
        /// </summary>
        async void prevButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainPage prev");

            prevButton.IsEnabled = false;

            try
            {
                await StartAudioAsync();

                _mediaPlayerHandle.NotifyBackground(SystemMediaTransportControlsButton.Previous);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage prevButton Click failed: " + ex.Message);
            }
            finally
            {
                prevButton.IsEnabled = true;
            }

            RequestRefresh();
        }

        async void playButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainPage play");

            playButton.IsEnabled = false;

            try
            {
                if (IsRunning)
                {
                    var mediaPlayer = MediaPlayer;

                    switch (mediaPlayer.CurrentState)
                    {
                        case MediaPlayerState.Playing:
                            mediaPlayer.Pause();
                            return;
                        case MediaPlayerState.Paused:
                            mediaPlayer.Play();
                            return;
                    }
                }

                await StartAudioAsync();

                _mediaPlayerHandle.NotifyBackground(SystemMediaTransportControlsButton.Play);
            }
            catch (OperationCanceledException)
            {
                CloseMediaPlayer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage playButton Click failed: " + ex.Message);
                CloseMediaPlayer();
            }
            finally
            {
                playButton.IsEnabled = true;
            }

            RequestRefresh();
        }

        async void nextButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainPage next");

            nextButton.IsEnabled = false;

            try
            {
                await StartAudioAsync();

                _mediaPlayerHandle.NotifyBackground(SystemMediaTransportControlsButton.Next);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage nextButton Click failed: " + ex.Message);
            }
            finally
            {
                nextButton.IsEnabled = true;
            }

            RequestRefresh();
        }

        void stopButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainPage stop");

            _mediaPlayerHandle.NotifyBackground(SystemMediaTransportControlsButton.Stop);

            RequestRefresh();
        }

        void killButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainPage kill");

            _mediaPlayerHandle.Shutdown();

            RequestRefresh();
        }

        #endregion Button Click Event Handlers

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data.
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
            this.killButton_Click(this, null);
        }
    }
}
