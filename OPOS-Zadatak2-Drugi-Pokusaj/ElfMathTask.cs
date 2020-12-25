using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using OPOS_Zadatak2_Drugi_Pokusaj.Math;

namespace OPOS_Zadatak2_Drugi_Pokusaj
{
    public class ElfMathTask
    {

        public enum ElfState { NotStarted, Running, Pausing, Paused, Complete, Cancelled };

        public delegate void ProgressReporterDelegate(double progress, ElfState state);

        public event ProgressReporterDelegate ProgressChanged;


        private SemaphoreSlim instanceSemaphore = new SemaphoreSlim(1);
        private SemaphoreSlim pauseSemaphore = new SemaphoreSlim(1);
        private SemaphoreSlim updateSemaphore = new SemaphoreSlim(1);//bruh

        private CancellationTokenSource source;
        public string Title { get; private set; }
        public string ExpressionString { get; private set; }
        public double LowerBound { get; private set; }
        public double UpperBound { get; private set; }
        public double Step { get; private set; }
        public int ThreadCount { get; private set; }
        public ElfState State { get; private set; } = ElfState.NotStarted;

        private object ResultLock = new object();
        public double Result { get; private set; } = 0.0;

        public List<(double from, double to, double subResult)> SubResults { get; private set; }

        private bool Restored = false;

        public bool CanStart { get => State == ElfState.NotStarted; }


        public bool DidSpawnAnother { get; set; } = false;


        private OneVariableFunction Function;

        private Task MasterTask;

        private long CompleteSteps = 0;
        private long StepCount = 0;
        private long modFor01Percent;

        public long GetCompleteSteps { get => CompleteSteps; }//ne mijenja mi se sve sad

        public ElfMathTask(XmlElement xmlTask, bool restoring = false, List<(double, double,double)> previousState = null, long completeSteps = 0)
        {
            if (xmlTask.Name == "ElfMathTask" && xmlTask.HasChildNodes && (xmlTask.ChildNodes.Count == 5 || restoring))
            {
                Title = xmlTask.GetAttribute("title");
                ExpressionString = xmlTask.SelectSingleNode("Expression").InnerText;
                Function = new OneVariableFunction(RpnConvertor.ToRPN(RpnConvertor.Tokenize(ExpressionString)));
                LowerBound = double.Parse(xmlTask.SelectSingleNode("LowerBound").InnerText);
                UpperBound = double.Parse(xmlTask.SelectSingleNode("UpperBound").InnerText);
                Step = double.Parse(xmlTask.SelectSingleNode("Step").InnerText);
                modFor01Percent = (long)(1 / Step) / 100;
                ThreadCount = int.Parse(xmlTask.SelectSingleNode("ElfCount").InnerText);
                SubResults = Enumerable.Range(0, ThreadCount).Select(t => (0.0, 0.0, 0.0)).ToList();
                StepCount = System.Math.Abs((long)System.Math.Ceiling((UpperBound - LowerBound) / Step));
                Debug.WriteLine($"Ima {ThreadCount} threadova!");
                if (ThreadCount <= 0)
                    throw new InvalidOperationException("Thread count must be greater than 0!");
            }
            else
                throw new ArgumentException("Malformed XML element!");
            if (restoring)
            {
                Restored = restoring;
                SubResults = previousState;
                CompleteSteps = completeSteps;
            }
        }

        public async Task Start()
        {
            await instanceSemaphore.WaitAsync();//nema duplog starta
            try
            {
                if (State != ElfState.NotStarted)
                    throw new InvalidOperationException("Can't start tasks that aren't waiting for a start");
                Result = 0.0;//samo sve resetuj
                double segmentLength = (UpperBound - LowerBound) / ThreadCount;
                ProgressChanged?.Invoke(0.0, State);
                State = ElfState.Running;
                source = new CancellationTokenSource();
                CancellationToken token = source.Token;
                Barrier pauseBarrier = new Barrier(ThreadCount);
                Func<Task> wrapSection(double from, double to, double step, int index, bool restored = false, double previousResult = 0.0)
                {
                    return async () =>
                    {
                        try
                        {
                            Debug.WriteLine($"[Worker]: od {from} do {to} sa korakom {step}, tj {StepCount}");
                            if (restored)
                                SubResults[index] = (SubResults[index].from, SubResults[index].to, previousResult);
                            else
                                SubResults[index] = (from, to, 0.0);
                            double x = from;
                            for (x = from; x <= to; x += step)
                            {
                                if (token.IsCancellationRequested)
                                    token.ThrowIfCancellationRequested();
                                if (State == ElfState.Pausing)//Paused jer je mozda neko pauzirao vec...
                                {
                                    pauseBarrier.SignalAndWait(token);//sad kad svi prodju barijeru
                                    State = ElfState.Paused;
                                    UpdateProgress();
                                    await pauseSemaphore.WaitAsync();//svi oni ce se poceti otimati oko ovoga
                                    pauseSemaphore.Release();
                                    if (token.IsCancellationRequested)
                                        token.ThrowIfCancellationRequested();
                                    State = ElfState.Running;
                                }
                                double dx = Function.ValueAtX(x) * step;
                                SubResults[index] = (x+step, SubResults[index].to, SubResults[index].subResult + dx);
                                Interlocked.Increment(ref CompleteSteps);//uradio korak, povecaj brojac
                                UpdateProgress();
                            }
                            lock (ResultLock)
                                Result += SubResults[index].subResult;
                        }
                        catch (OperationCanceledException)
                        {
                            State = ElfState.Cancelled;
                            ProgressChanged?.Invoke(1.0, State);
                        }

                    };
                }
                Task[] actualWorkers = new Task[ThreadCount];
                for (int i = 0; i < ThreadCount; ++i)
                    if(Restored)
                        actualWorkers[i] = Task.Run(wrapSection(SubResults[i].from, SubResults[i].to, Step, i, true, SubResults[i].subResult), token);
                    else
                        actualWorkers[i] = Task.Run(wrapSection(LowerBound + i * segmentLength, LowerBound + (i + 1) * segmentLength, Step, i), token);
                MasterTask = Task.Factory.StartNew(async () =>
                {
                    await Task.WhenAll(actualWorkers);
                    if(State != ElfState.Cancelled)
                        State = ElfState.Complete;
                    ProgressChanged?.Invoke(1.0, State);
                },token);
                
            }
            catch (OperationCanceledException)
            {
                State = ElfState.Cancelled;
                ProgressChanged?.Invoke(1.0, State);
            }
            finally
            {
                instanceSemaphore.Release();
            }
        }

        public async Task WaitForTaskToFinish()
        {
            try
            {
                await MasterTask;
            }
            catch (OperationCanceledException)
            {
                State = ElfState.Cancelled;
                ProgressChanged?.Invoke(1.0, State);
            }
            finally
            {
                source.Dispose();
                source = null;
                MasterTask = null;
            }
        }

        //public da mogu pri restore-u starih da uradim refresh
        public void UpdateProgress()
        {
            if (CompleteSteps % modFor01Percent == 0 || State == ElfState.Pausing || State == ElfState.Paused || (State==ElfState.NotStarted&&Restored))
                ProgressChanged?.Invoke((double)CompleteSteps / StepCount, State);
        }

        public async Task Pause()
        {
            Debug.WriteLine("Radim pause!");
            await instanceSemaphore.WaitAsync();
            try
            {
                if (State == ElfState.Running)
                {
                    Debug.WriteLine("Cekam semafor za pauzu!");
                    await pauseSemaphore.WaitAsync();
                    Debug.WriteLine("Uzeo semafor za pauzu");
                    State = ElfState.Pausing;
                }
            }
            finally
            {
                instanceSemaphore.Release();
            }
        }

        public async Task Resume()
        {
            Debug.WriteLine("Radim resume!");
            await instanceSemaphore.WaitAsync();
            try
            {
                if(State == ElfState.Paused)
                {
                    Debug.WriteLine("State je bio paused, vracam sad state");
                    pauseSemaphore.Release();
                    State = ElfState.Running;
                }
            }
            finally
            {
                instanceSemaphore.Release();
            }
        }

        public async Task Cancel()
        {
            await instanceSemaphore.WaitAsync();
            try
            {
                if (State == ElfState.Running || State == ElfState.Pausing || State == ElfState.Running)
                {
                    Debug.WriteLine("Otkazujem task!");
                    State = ElfState.Cancelled;
                    source.Cancel();
                }
                else if (State == ElfState.NotStarted)
                    State = ElfState.Cancelled;
                else if (State == ElfState.Paused)
                {
                    State = ElfState.Cancelled;
                    source.Cancel();
                    pauseSemaphore.Release();
                    UpdateProgress();
                }
            }
            finally
            {
                instanceSemaphore.Release();
            }
        }
    }
}
