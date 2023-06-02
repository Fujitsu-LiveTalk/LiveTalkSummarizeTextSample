using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LiveTalkSummarizeTextSample.Models
{
    internal class SummarizeModel : INotifyPropertyChanged
    {
        private const string UrlString = "https://{0}.cognitiveservices.azure.com/text/analytics/v3.2-preview.2/analyze";
        private BlockingCollection<string> Queue = new BlockingCollection<string>();
        private CancellationTokenSource TokenSource = new CancellationTokenSource();
        private HttpClient SendClient = null;
        private long LineCount = 0;
        private string Resource;
        private string AccessKey;

        /// <summary>
        /// 連携ファイル名
        /// </summary>
        private string _FileName = Common.Config.GetConfig("FileName");
        public string FileName
        {
            get { return this._FileName; }
            internal set
            {
                if (this._FileName != value)
                {
                    this._FileName = value;
                    OnPropertyChanged();
                    Common.Config.SetConfig("FileName", value);
                }
            }
        }

        /// <summary>
        /// 要約率
        /// </summary>
        private int _Ratio = int.Parse(Common.Config.GetConfig("Ratio"));
        public int Ratio
        {
            get { return this._Ratio; }
            internal set
            {
                if (this._Ratio != value)
                {
                    this._Ratio = value;
                    OnPropertyChanged();
                    Common.Config.SetConfig("Ratio", value.ToString());
                }
            }
        }

        /// <summary>
        /// 処理中メッセージ
        /// </summary>
        private string _Message = string.Empty;
        public string Message
        {
            get { return this._Message; }
            internal set
            {
                if (this._Message != value)
                {
                    this._Message = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 要約
        /// </summary>
        private string _Result = String.Empty;
        public string Result
        {
            get { return this._Result; }
            set
            {
                if (this._Result != value)
                {
                    this._Result = value;
                    OnPropertyChanged();
                }
            }
        }

        public SummarizeModel()
        {
            this.Resource = Common.Config.GetConfig("APIResourceName");
            this.AccessKey = Common.Config.GetConfig("APIKey");
        }

        internal async Task Convert()
        {
            var source = this.FileName;

            try
            {
                // ファイルからの入力は非同期に実施する
                await Task.Run(async () =>
                {
                    long seqNo = 0;
                    var reg = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    var context = string.Empty;

                    try
                    {
                        //　CSV入力
                        using (var fs = new System.IO.FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var sr = new StreamReader(fs, Encoding.UTF8))
                            {

                                // ファイルの終わりまで入力する
                                while (!sr.EndOfStream)
                                {
                                    var s = sr.ReadLine();
                                    var items = reg.Split(s);
                                    var messageTime = DateTime.Parse(items[0].Substring(1, items[0].Length - 2));
                                    var name = items[1].Substring(1, items[1].Length - 2);
                                    var message = items[2].Substring(1, items[2].Length - 2);
                                    var translateText = items[3].Substring(1, items[3].Length - 2);

                                    // シーケンス番号
                                    this.Message = $"Read CSV File : SeqNo={++seqNo}";

                                    if (!string.IsNullOrEmpty(translateText)) message = translateText;
                                    context += message + Environment.NewLine;
                                }
                                sr.Close();
                            }
                        }
                        this.Message = "Get Summarize";

                        // 要約
                        {
                            var sentenceCount = (int)(seqNo * ((double)this.Ratio / 100D));
                            var result = await GetSummarizeAsync(context, sentenceCount);

                            sentenceCount = sentenceCount < 1 ? 1 : sentenceCount > 20 ? 20 : sentenceCount;
                            this.Result = !string.IsNullOrEmpty(result) ? result : this.Result;

                            this.Message = $"Summarized {seqNo} -> {sentenceCount}";
                        }
                    }
                    catch (Exception ex)
                    {
                        OnThrew(ex);
                    }
                });
            }
            catch { }
        }

        internal async Task<string> GetSummarizeAsync(string inputData, int sentenceCount)
        {
            var summarizeText = string.Empty;
            var body = new TRequest.TRootobject()
            {
                analysisInput = new TRequest.TAnalysisinput()
                {
                    documents = new TRequest.TDocument[]
                    {
                        new TRequest.TDocument()
                        {
                        language = "ja",
                        id = "1",
                        text = inputData,
                        }
                    }
                },
                tasks = new TRequest.TTasks()
                {
                    extractiveSummarizationTasks = new TRequest.TExtractivesummarizationtask[]
                    {
                        new TRequest.TExtractivesummarizationtask()
                        {
                            kind = "AbstractiveSummarization", //"ExtractiveSummarization",
                            parameters = new TRequest.TParameters
                            {
                                modelversion = "latest",
                                sentenceCount = sentenceCount,
                                sortBy = "offset",
                            }
                        }
                    }
                }
            };

            try
            {
                if (this.SendClient == null)
                {
                    this.SendClient = new HttpClient();
                }
                if (this.SendClient != null)
                {
                    var requestBody = "";
                    var jobLocation = "";

                    using (var ms = new MemoryStream())
                    {
                        var serializer = new DataContractJsonSerializer(typeof(TRequest.TRootobject));
                        using (var sr = new StreamReader(ms))
                        {
                            serializer.WriteObject(ms, body);
                            ms.Position = 0;
                            requestBody = sr.ReadToEnd();
                        }
                    }

                    using (var request = new HttpRequestMessage())
                    {
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri(string.Format(UrlString, Resource));
                        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                        request.Headers.Add("Ocp-Apim-Subscription-Key", AccessKey);

                        using (var response = await this.SendClient.SendAsync(request))
                        {
                            response.EnsureSuccessStatusCode();

                            // 返却用ロケーションを取得する
                            jobLocation = response.Headers.GetValues("operation-location").ToList()[0];
                        }
                    }

                    if (!string.IsNullOrEmpty(jobLocation))
                    {
                        while (true)
                        {
                            using (var request = new HttpRequestMessage())
                            {
                                request.Method = HttpMethod.Get;
                                request.RequestUri = new Uri(jobLocation);
                                request.Headers.Add("Ocp-Apim-Subscription-Key", AccessKey);

                                using (var response = await this.SendClient.SendAsync(request))
                                {
                                    response.EnsureSuccessStatusCode();
                                    var jsonString = await response.Content.ReadAsStringAsync();
                                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                                    {
                                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TResult.TRootobject));
                                        {
                                            var result = ser.ReadObject(json) as TResult.TRootobject;
                                            if (result.tasks.completed == 1)
                                            {
                                                foreach (var item in result.tasks.extractiveSummarizationTasks[0].results.documents[0].sentences)
                                                {
                                                    summarizeText += item.text + Environment.NewLine;
                                                }
                                                json.Close();
                                                break;
                                            }
                                        }
                                    }
                                }
                                await Task.Delay(1000);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.SendClient = null;
            }
            return summarizeText;
        }

        #region "TRequest"
        public class TRequest
        {
            [DataContract]
            public class TRootobject
            {
                [DataMember]
                public TAnalysisinput analysisInput { get; set; }
                [DataMember]
                public TTasks tasks { get; set; }
            }

            [DataContract]
            public class TAnalysisinput
            {
                [DataMember]
                public TDocument[] documents { get; set; }
            }

            [DataContract]
            public class TDocument
            {
                [DataMember]
                public string language { get; set; }
                [DataMember]
                public string id { get; set; }
                [DataMember]
                public string text { get; set; }
            }

            [DataContract]
            public class TTasks
            {
                [DataMember]
                public TExtractivesummarizationtask[] extractiveSummarizationTasks { get; set; }
            }

            [DataContract]
            public class TExtractivesummarizationtask
            {
                [DataMember]
                public string kind { get; set; }
                [DataMember]
                public TParameters parameters { get; set; }
            }

            [DataContract]
            public class TParameters
            {
                [DataMember(Name = "model-version")]
                public string modelversion { get; set; }
                [DataMember]
                public int sentenceCount { get; set; }
                [DataMember]
                public string sortBy { get; set; }
            }
        }

        public class TResult
        {

            [DataContract]
            public class TRootobject
            {
                [DataMember]
                public string jobId { get; set; }
                [DataMember]
                public string lastUpdateDateTime { get; set; }
                [DataMember]
                public string createdDateTime { get; set; }
                [DataMember]
                public string expirationDateTime { get; set; }
                [DataMember]
                public string status { get; set; }
                [DataMember]
                public object[] errors { get; set; }
                [DataMember]
                public string displayName { get; set; }
                [DataMember]
                public TTasks tasks { get; set; }
            }

            [DataContract]
            public class TTasks
            {
                [DataMember]
                public int completed { get; set; }
                [DataMember]
                public int failed { get; set; }
                [DataMember]
                public int inProgress { get; set; }
                [DataMember]
                public int total { get; set; }
                [DataMember]
                public TExtractivesummarizationtask[] extractiveSummarizationTasks { get; set; }
            }

            [DataContract]
            public class TExtractivesummarizationtask
            {
                [DataMember]
                public string lastUpdateDateTime { get; set; }
                [DataMember]
                public string taskName { get; set; }
                [DataMember]
                public string state { get; set; }
                [DataMember]
                public TResults results { get; set; }
            }

            [DataContract]
            public class TResults
            {
                [DataMember]
                public TDocument[] documents { get; set; }
                [DataMember]
                public object[] errors { get; set; }
                [DataMember]
                public string modelVersion { get; set; }
            }

            [DataContract]
            public class TDocument
            {
                [DataMember]
                public string id { get; set; }
                [DataMember]
                public TSentence[] sentences { get; set; }
                [DataMember]
                public object[] warnings { get; set; }
            }

            [DataContract]
            public class TSentence
            {
                [DataMember]
                public string text { get; set; }
                [DataMember]
                public float rankScore { get; set; }
                [DataMember]
                public int offset { get; set; }
                [DataMember]
                public int length { get; set; }
            }
        }
        #endregion

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public event ErrorEventHandler Threw;
        protected virtual void OnThrew(Exception ex)
        {
            this.Threw?.Invoke(this, new ErrorEventArgs(ex));
        }

        /// <summary>
        /// プロパティ変更通知
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
