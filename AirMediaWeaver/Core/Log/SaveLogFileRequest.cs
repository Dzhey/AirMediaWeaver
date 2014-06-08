using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Log
{
    public class SaveLogFileRequest : AbsRequest
    {
        public class RequestResult : Requests.Model.RequestResult
        {
            public const int ResultCodeErrorCantCreateFile = 1000;

            public string FilePath { get; set; }

            public RequestResult(int resultCode) : base(resultCode)
            {
            }

            internal RequestResult(int resultCode, Exception risenException) : 
                base(resultCode, risenException)
            {
            }
        }

        public string OutputPath { get; private set; }
        public string FileName { get; private set; }

        /// <summary>
        /// </summary>
        /// <param name="outputPath">log file output path including trailing slash</param>
        public SaveLogFileRequest(string outputPath)
        {
            OutputPath = outputPath;
            FileName = string.Format("tmlog_{0:dd.MM.yyyy_HH-mm-ss}_.txt", DateTime.Now);
        }

        protected virtual Stream CreateLogStream()
        {
            var dao = (LogEntryDao) DatabaseHelper.Instance.GetDao<LogEntryRecord>();

            List<LogEntryRecord> records;
            lock (InsertLogEntriesRequest.InsertMutex)
            {
                records = dao.QueryForLogLevel(LogLevel.Verbose);
            }

            var stream = new BufferedStream(new MemoryStream());

            var writer = new StreamWriter(stream);
            foreach (var record in records)
            {
                writer.WriteLine(record.ToLogEntry());
            }

            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        protected override Requests.Model.RequestResult ExecuteImpl(out RequestStatus status)
        {
            status = RequestStatus.Failed;

            string outputPath = OutputPath ?? Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);
            if (outputPath[outputPath.Length - 1] != Path.DirectorySeparatorChar)
            {
                outputPath = outputPath + Path.DirectorySeparatorChar;
                AmwLog.Verbose(LogTag, string.Format("trailing path separator automatically added to path \"{0}\"", OutputPath));
            }

            string filePath = outputPath + FileName;

            try
            {
                var input = CreateLogStream();
                using (var writer = new FileStream(filePath, FileMode.CreateNew))
                {
                    input.CopyTo(writer);
                }
                input.Dispose();

                status = RequestStatus.Ok;

                AmwLog.Verbose(LogTag, string.Format("log file sucessfully saved at \"{0}\"", filePath));

                return new RequestResult(Requests.Model.RequestResult.ResultCodeOk)
                    {
                        FilePath = filePath
                    };
            }
            catch (Exception e)
            {
                AmwLog.Error(LogTag, e, "Can't create file at \"{0}\"", filePath);

                return new RequestResult(RequestResult.ResultCodeErrorCantCreateFile, e)
                    {
                        ErrorMessage = e.Message,
                    };
            }
        }
    }
}
