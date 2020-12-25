using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OPOS_Zadatak2_Drugi_Pokusaj
{
    public sealed partial class PickTasksDialog : ContentDialog
    {
        public PickTasksDialog()
        {
            this.InitializeComponent();
        }

        public void DisplayTasksAsync(IEnumerable<ElfMathTask> tasks)
        {
            int i = 0;
            foreach (var task in tasks)
            {
                TasksStackPanel.Children.Add(new CheckBox() { Content = $"{task.ExpressionString} u granicama [{task.LowerBound}, {task.UpperBound}] s korakom {task.Step}", Tag = task, IsChecked = false });
            }
        }

        public ElfMathTask[] SelectedTasks() => TasksStackPanel.Children.OfType<CheckBox>().Where(cb=>cb.IsChecked.HasValue && cb.IsChecked.Value).Select(cb => cb.Tag as ElfMathTask).ToArray();
    }
}
