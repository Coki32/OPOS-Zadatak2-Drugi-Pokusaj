using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace OPOS_Zadatak2_Drugi_Pokusaj.FileHandler
{
    public class ElfPersistencyManager
    {
        private static ElfPersistencyManager _instance = new ElfPersistencyManager();
        public static ElfPersistencyManager Instance { get => _instance; }

        private List<ElfMathTask> tasks = new List<ElfMathTask>();
        private ElfPersistencyManager()
        {

        }

        public void AddTask(ElfMathTask task) => tasks.Add(task);

        public void RemoveAll() => tasks.Clear();

        public void RemoveComplete() => tasks.RemoveAll(emt => emt.State == ElfMathTask.ElfState.Complete);

        public async Task Save()
        {
            foreach (var t in tasks) await t.Cancel();
            var xml = new XElement(nameof(ElfPersistencyManager));
            foreach(var task in tasks)
            {
                var interruptedTask = new XElement(nameof(ElfMathTask));

                interruptedTask.SetAttributeValue("title", task.Title);
                interruptedTask.Add(new XElement("Expression", task.ExpressionString));
                interruptedTask.Add(new XElement("LowerBound", task.LowerBound));
                interruptedTask.Add(new XElement("UpperBound", task.UpperBound));
                interruptedTask.Add(new XElement("Step", task.Step));
                interruptedTask.Add(new XElement("ElfCount", task.ThreadCount));
                interruptedTask.Add(new XElement("CompleteSteps", task.GetCompleteSteps));
                var stateElement = new XElement("State");
                //foreach(var (from, to, subResult) in task.SubResults)
                for(int i=0; i<task.SubResults.Count; ++i)
                {
                    var (from, to, subResult) = task.SubResults[i];
                    var stateEntry = new XElement("StateEntry");
                    stateEntry.Add(new XElement("from", from));
                    stateEntry.Add(new XElement("to", to));
                    stateEntry.Add(new XElement("subResult", subResult));
                    stateElement.Add(stateEntry);
                }
                interruptedTask.Add(stateElement);
                xml.Add(interruptedTask);
            }
            var outFile = await GetStateFile(CreationCollisionOption.ReplaceExisting);
            using (var stream = await outFile.OpenStreamForWriteAsync())
            {
                XDocument xDocument = new XDocument(xml);
                xDocument.Save(stream);
            }
        }

        public async Task<List<ElfMathTask>> Restore()
        {
            try
            {
                var inFile = await GetStateFile(CreationCollisionOption.OpenIfExists);
                XmlDocument xmlDocument = new XmlDocument();
                using (var stream = await inFile.OpenStreamForReadAsync())
                {
                    var xDocument = XDocument.Load(stream);
                    xmlDocument.Load(xDocument.CreateReader());
                }
                List<ElfMathTask> tasks = new List<ElfMathTask>();
                foreach (XmlElement child in xmlDocument.DocumentElement.ChildNodes)
                {
                    if (child.ParentNode == xmlDocument.DocumentElement)//jeste prvo
                    {
                        var stateElement = child.SelectSingleNode("State");
                        List<(double from, double to, double subResult)> state = new List<(double, double, double)>();
                        foreach(XmlElement stateEntry in stateElement.ChildNodes)
                        {
                            if (stateEntry.ParentNode == stateElement)
                            {
                                double from = double.Parse(stateEntry.SelectSingleNode("from").InnerText);
                                double to = double.Parse(stateEntry.SelectSingleNode("to").InnerText);
                                double subResult = double.Parse(stateEntry.SelectSingleNode("subResult").InnerText);
                                state.Add((from, to, subResult));
                            }
                        }
                        long CompleteSteps = long.Parse(child.SelectSingleNode("CompleteSteps").InnerText);
                        tasks.Add(new ElfMathTask(child, true, state, CompleteSteps));
                    }
                }
                return tasks;
            }
            catch(Exception)
            {
                return null;
            }
        }

        private async Task<StorageFile> GetStateFile(CreationCollisionOption mode) => await ApplicationData.Current.LocalFolder.CreateFileAsync("integrals.xml", mode);

    }
}
