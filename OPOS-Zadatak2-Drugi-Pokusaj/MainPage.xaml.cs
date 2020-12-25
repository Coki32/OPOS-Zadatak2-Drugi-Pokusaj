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

using OPOS_Zadatak2_Drugi_Pokusaj.Math;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using System.Threading.Tasks;
using Windows.Storage;
using System.Xml.Linq;
using System.Xml;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace OPOS_Zadatak2_Drugi_Pokusaj
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static readonly string XML_ROOT_NAME = "ElfMathTasks";

        private int ConcurrentTasksCount = int.MaxValue;
        private int CurrentlyRunning = 0;

        public  MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            List<ElfMathTask> storedTasks = await FileHandler.ElfPersistencyManager.Instance.Restore();
            FileHandler.ElfPersistencyManager.Instance.RemoveAll();//Kad su ucitani izbaci ih sve
            await AddAllTasksToTheWindow(storedTasks);
            if (e.Parameter is FileActivatedEventArgs)
            {
                var args = e.Parameter as FileActivatedEventArgs;
                await AddAllTasksToTheWindow(await SelectTasks(await GetTasksFromFiles(args.Files.OfType<IStorageFile>())));
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async Task<IEnumerable<ElfMathTask>> SelectTasks(IEnumerable<ElfMathTask> tasks)
        {
            PickTasksDialog pickTasksDialog = new PickTasksDialog();
            pickTasksDialog.DisplayTasksAsync(tasks);
            ContentDialogResult dialogResult = await pickTasksDialog.ShowAsync();
            if (dialogResult == ContentDialogResult.Primary)
                return pickTasksDialog.SelectedTasks(); 
            return null;
        }

        private async void openFile_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.FileTypeFilter.Add(".emt");
            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
                await AddAllTasksToTheWindow(await SelectTasks(await GetTasksFromFiles(files)));
        }

        //Ocajan sam sa imenima....
        private async Task AddAllTasksToTheWindow(IEnumerable<ElfMathTask> tasks)
        {
            if (tasks != null)
                foreach (var task in tasks)
                {
                    FileHandler.ElfPersistencyManager.Instance.AddTask(task);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => ElfTasksPanel.Children.Add(new ElfTaskView(task)));
                }
        }

        private async Task<IEnumerable<ElfMathTask>> SelectTasksFromList(IEnumerable<ElfMathTask> tasks)
        {
            PickTasksDialog pickTasksDialog = new PickTasksDialog();
            pickTasksDialog.DisplayTasksAsync(tasks);
            ContentDialogResult dialogResult = await pickTasksDialog.ShowAsync();
            if (dialogResult == ContentDialogResult.Primary)
            {
                return pickTasksDialog.SelectedTasks();
            }
            else
                return null;
        }

        private async Task<IEnumerable<ElfMathTask>> GetTasksFromFiles(IEnumerable<IStorageFile> files)
        {
            List<ElfMathTask> result = new List<ElfMathTask>();
            Debug.WriteLine("Pokrecem citanje");
            var tasks = files.Select(f => GetTasksFromSingleFileAsync(f));
            Debug.WriteLine("Citam, cekam sad da se svi zavrse");
            foreach (var task in tasks)
            {
                var singleFileTasks = await task;
                if (singleFileTasks != null)
                    result.AddRange(singleFileTasks);
            }
            return result;
        }

        private async Task<IEnumerable<ElfMathTask>> GetTasksFromSingleFileAsync(IStorageFile file)
        {
            var token = new System.Threading.CancellationToken();
            XmlDocument xmlDocument = null;
            var stream = await file.OpenReadAsync();
            try
            {
                var xdocument = await XDocument.LoadAsync(stream.AsStream(), LoadOptions.None, token);
                xmlDocument = new XmlDocument();
                using (var reader = xdocument.CreateReader())
                    await Task.Run(() => xmlDocument.Load(reader));
            }
            catch (Exception ex)//prazan fajl, nikom nista
            {
                return null;
            }

            if (xmlDocument.DocumentElement.Name != XML_ROOT_NAME)
                return null;

            return await Task.Run(() =>
            {
                List<ElfMathTask> tasks = new List<ElfMathTask>();

                foreach (XmlElement child in xmlDocument.DocumentElement.ChildNodes)
                    if (child.ParentNode == xmlDocument.DocumentElement)
                    {
                        try { tasks.Add(new ElfMathTask(child)); }
                        catch (Exception)
                        {
                            Debug.WriteLine("Greska u tasku:");
                            Debug.WriteLine(child.InnerXml.Replace("<","\n<").Replace(">",">\n"));
                        }
                    }
                return tasks;
            });

        }

        private async void StartAllButton_Click(object sender, RoutedEventArgs e)
        {
            await StartTasks();
        }


        //Cim hoces da pokrenes samo N njih, neces koristiti start all vise
        private async void StartFewButton_Click(object sender, RoutedEventArgs e)
        {
            int concurrentJobs;
            if(!int.TryParse(JobsNumberBox.Value.ToString(), out concurrentJobs))
            {
                var errorDialog = new Windows.UI.Popups.MessageDialog("Unesena vrijednost mora biti cijeli broj!")
                {
                    Title = "Greska!"
                };
                await errorDialog.ShowAsync();
                return;
            }
            ConcurrentTasksCount = concurrentJobs;
            await StartTasks();
        }

        private async void SpawnNewTasksOnComplete(ElfMathTask task)
        {
            if (!task.DidSpawnAnother)
            {
                task.DidSpawnAnother = true;
                Interlocked.Decrement(ref CurrentlyRunning);
                await StartTasks();
            }
        }

        private async void WriteResultToFile(ElfMathTask task) => await FileHandler.ResultHandler.WriteResultToFile(task);

        private async Task StartTasks()
        {
            Debug.WriteLine("Pokrecem nove taskove");
            while(CurrentlyRunning < ConcurrentTasksCount)
            {
                var taskToStart = ElfTasksPanel.Children.OfType<ElfTaskView>().DefaultIfEmpty(null).FirstOrDefault(etv => etv.ElfMathTask.CanStart);
                if (taskToStart == null)
                    break;
                Interlocked.Increment(ref CurrentlyRunning);
                taskToStart.OnTaskComplete += SpawnNewTasksOnComplete;
                taskToStart.OnTaskComplete += WriteResultToFile;
                await taskToStart.Start();
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e) => FileHandler.ElfPersistencyManager.Instance.RemoveAll();

        private void ClearFinished_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tv in ElfTasksPanel.Children.OfType<ElfTaskView>().Where(etv => etv.ElfMathTask.State == ElfMathTask.ElfState.Complete))
                ElfTasksPanel.Children.Remove(tv);
        }
    }
}
