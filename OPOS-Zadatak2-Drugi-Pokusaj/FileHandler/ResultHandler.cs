using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;

namespace OPOS_Zadatak2_Drugi_Pokusaj.FileHandler
{
    public class ResultHandler
    {
        public static async Task WriteResultToFile(ElfMathTask task)
        {
            try
            {
                using (var streamWriter = new StreamWriter(await (await GetResultsFile()).OpenStreamForWriteAsync()))
                {
                    streamWriter.BaseStream.Seek(0, SeekOrigin.End);
                    await streamWriter.WriteLineAsync($"Integral funkcije {task.ExpressionString} od {task.LowerBound} do {task.UpperBound} je {task.Result}");
                }
            }
            catch(Exception) { }
        }


        private static async Task<StorageFile> GetResultsFile(CreationCollisionOption mode = CreationCollisionOption.OpenIfExists) => await ApplicationData.Current.LocalFolder.CreateFileAsync("Results.txt", mode);

    }
}
