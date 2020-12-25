using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace OPOS_Zadatak2_Drugi_Pokusaj
{
    public sealed partial class ElfTaskView : UserControl
    {
        public delegate void TaskCompleteDeleate(ElfMathTask task);
        public ElfMathTask ElfMathTask { get; private set; }

        public event TaskCompleteDeleate OnTaskComplete;

        public ElfTaskView( ElfMathTask elfMathTask )
        {
            this.InitializeComponent();
            ElfMathTask = elfMathTask;
            ElfMathTask.ProgressChanged += this.UpdateProgress;
            TitleTextBlock.Text = $"{ElfMathTask.Title} ({ElfMathTask.ThreadCount} {FixTheWord()})";
            elfMathTask.UpdateProgress();
        }

        public async Task Start() => await ElfMathTask.Start();

        private async void PauseClicked(object sender, RoutedEventArgs e)
        {
            await ElfMathTask.Pause();
            PauseButton.Visibility = Visibility.Collapsed;
            ResumeButton.Visibility = Visibility.Visible;
        }

        private async void CancelClicked(object sender, RoutedEventArgs e) => await ElfMathTask.Cancel();

        private async void ResumeClicked(object sender, RoutedEventArgs e)
        {
            await ElfMathTask.Resume();
            PauseButton.Visibility = Visibility.Visible;
            ResumeButton.Visibility = Visibility.Collapsed;
        }

        private async void UpdateProgress(double progress, ElfMathTask.ElfState state)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (progress <= 1.0 && progress >= 0.0)
                    ElfProgressBar.Value = progress;
                StatusTextBlock.Text = state.ToString();
                if (state == ElfMathTask.ElfState.Complete)
                {
                    OnTaskComplete?.Invoke(ElfMathTask);
                    PauseButton.IsEnabled = ResumeButton.IsEnabled = false;
                }
            });
        }

        private string FixTheWord() => ElfMathTask.ThreadCount.ToString()[0] == '1' ? "Elf" : (ElfMathTask.ThreadCount.ToString()[0] < '5' ? "Elfa" : "Elfova");

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {

        }
    }
}
