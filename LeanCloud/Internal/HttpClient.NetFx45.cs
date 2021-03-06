//// Copyright (c) 2015-present, AV, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

//using System;
//using System.IO;
//using System.Net;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace LeanCloud.Internal
//{
//    internal class HttpClient : IHttpClient
//    {
//        public Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
//            IProgress<AVUploadProgressEventArgs> uploadProgress,
//            IProgress<AVDownloadProgressEventArgs> downloadProgress,
//            CancellationToken cancellationToken)
//        {
//            HttpWebRequest request = HttpWebRequest.Create(httpRequest.Uri) as HttpWebRequest;
//            request.Method = httpRequest.Method;
//            cancellationToken.Register(() => request.Abort());
//            uploadProgress = uploadProgress ?? new Progress<AVUploadProgressEventArgs>();
//            downloadProgress = downloadProgress ?? new Progress<AVDownloadProgressEventArgs>();

//            // Fill in zero-length data if method is post.
//            Stream data = httpRequest.Data;
//            if (httpRequest.Data == null && httpRequest.Method.ToLower().Equals("post"))
//            {
//                data = new MemoryStream(new byte[0]);
//            }

//            // Fill in the headers
//            if (httpRequest.Headers != null)
//            {
//                foreach (var header in httpRequest.Headers)
//                {
//                    if (header.Key == "Content-Type")
//                    {
//                        // Move over Content-Type header into Content.
//                        request.ContentType = header.Value;
//                    }
//                    else if (header.Key == "Content-Length")
//                    {
//                        request.ContentLength = long.Parse(header.Value);
//                    }
//                    else
//                    {
//                        request.Headers[header.Key] = header.Value;
//                    }
//                }
//            }
//            // Avoid aggresive caching on Windows Phone 8.1.
//            request.Headers["Cache-Control"] = "no-cache";

//            Task uploadTask = null;

//            if (data != null)
//            {
//                Task copyTask = null;
//                long totalLength = -1;

//                try
//                {
//                    totalLength = data.Length;
//                }
//                catch (NotSupportedException)
//                {
//                }

//                // If the length can't be determined, read it into memory first.
//                if (totalLength == -1)
//                {
//                    var memStream = new MemoryStream();
//                    copyTask = data.CopyToAsync(memStream).OnSuccess(_ =>
//                    {
//                        memStream.Seek(0, SeekOrigin.Begin);
//                        totalLength = memStream.Length;

//                        data = memStream;
//                    });
//                }

//                uploadProgress.Report(new AVUploadProgressEventArgs { Progress = 0 });

//                uploadTask = copyTask.Safe().ContinueWith(_ =>
//                {
//                    return request.GetRequestStreamAsync();
//                }).Unwrap()
//                .OnSuccess(t =>
//                {
//                    var requestStream = t.Result;

//                    int bufferSize = 4096;
//                    byte[] buffer = new byte[bufferSize];
//                    int bytesRead = 0;
//                    long readSoFar = 0;

//                    return InternalExtensions.WhileAsync(() =>
//                    {
//                        return data.ReadAsync(buffer, 0, bufferSize, cancellationToken).OnSuccess(readTask =>
//                        {
//                            bytesRead = readTask.Result;
//                            return bytesRead > 0;
//                        });
//                    }, () =>
//                    {
//                        cancellationToken.ThrowIfCancellationRequested();
//                        return requestStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).OnSuccess(_ =>
//                        {
//                            cancellationToken.ThrowIfCancellationRequested();
//                            readSoFar += bytesRead;
//                            uploadProgress.Report(new AVUploadProgressEventArgs { Progress = 1.0 * readSoFar / totalLength });
//                        });
//                    }).ContinueWith(_ =>
//                    {
//                //requestStream.Flush();
//                requestStream.Dispose();
//                    });
//                }).Unwrap();
//            }

//            return uploadTask.Safe().ContinueWith(_ =>
//            {
//                return request.GetResponseAsync();
//            }).Unwrap()
//            .ContinueWith(t =>
//            {
//          // Handle canceled
//          cancellationToken.ThrowIfCancellationRequested();

//                var resultStream = new MemoryStream();
//                HttpWebResponse response = null;
//                if (t.IsFaulted)
//                {
//                    if (t.Exception.InnerException is WebException)
//                    {
//                        var webException = t.Exception.InnerException as WebException;
//                        response = (HttpWebResponse)webException.Response;
//                    }
//                    else
//                    {
//                        TaskCompletionSource<Tuple<HttpStatusCode, string>> tcs = new TaskCompletionSource<Tuple<HttpStatusCode, string>>();
//                        tcs.TrySetException(t.Exception);

//                        return tcs.Task;
//                    }
//                }
//                else
//                {
//                    response = (HttpWebResponse)t.Result;
//                }

//                var responseStream = response.GetResponseStream();

//                int bufferSize = 4096;
//                byte[] buffer = new byte[bufferSize];
//                int bytesRead = 0;
//                long totalLength = -1;
//                long readSoFar = 0;

//                try
//                {
//                    totalLength = responseStream.Length;
//                }
//                catch (NotSupportedException)
//                {
//                }

//                return InternalExtensions.WhileAsync(() =>
//                {
//                    return responseStream.ReadAsync(buffer, 0, bufferSize, cancellationToken).OnSuccess(readTask =>
//                    {
//                        bytesRead = readTask.Result;
//                        return bytesRead > 0;
//                    });
//                }, () =>
//                {
//                    cancellationToken.ThrowIfCancellationRequested();

//                    return resultStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).OnSuccess(_ =>
//                    {
//                        cancellationToken.ThrowIfCancellationRequested();
//                        readSoFar += bytesRead;

//                        if (totalLength > -1)
//                        {
//                            downloadProgress.Report(new AVDownloadProgressEventArgs { Progress = 1.0 * readSoFar / totalLength });
//                        }
//                    });
//                }).ContinueWith(_ =>
//                {
//                    responseStream.Dispose();

//              // If getting stream size is not supported, then report download only once.
//              if (totalLength == -1)
//                    {
//                        downloadProgress.Report(new AVDownloadProgressEventArgs { Progress = 1.0 });
//                    }

//              // Assume UTF-8 encoding.
//              var resultAsArray = resultStream.ToArray();
//                    var resultString = Encoding.UTF8.GetString(resultAsArray, 0, resultAsArray.Length);
//                    resultStream.Dispose();
//                    return new Tuple<HttpStatusCode, string>(response.StatusCode, resultString);
//                });
//            }).Unwrap();
//        }
//    }
//}

// Copyright (c) 2015-present, AV, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetHttpClient = System.Net.Http.HttpClient;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net.Http;

namespace LeanCloud.Internal
{
    internal class HttpClient : IHttpClient
    {
        private static HashSet<string> HttpContentHeaders = new HashSet<string> {
            { "Allow" },
            { "Content-Disposition" },
            { "Content-Encoding" },
            { "Content-Language" },
            { "Content-Length" },
            { "Content-Location" },
            { "Content-MD5" },
            { "Content-Range" },
            { "Content-Type" },
            { "Expires" },
            { "Last-Modified" }
        };

        public HttpClient() : this(new NetHttpClient())
        {
        }

        public HttpClient(NetHttpClient client)
        {
            this.client = client;
        }

        private NetHttpClient client;

        public Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
            IProgress<AVUploadProgressEventArgs> uploadProgress,
            IProgress<AVDownloadProgressEventArgs> downloadProgress,
            CancellationToken cancellationToken)
        {
            try
            {
                uploadProgress = uploadProgress ?? new Progress<AVUploadProgressEventArgs>();
                downloadProgress = downloadProgress ?? new Progress<AVDownloadProgressEventArgs>();

                HttpMethod httpMethod = new HttpMethod(httpRequest.Method);
                HttpRequestMessage message = new HttpRequestMessage(httpMethod, httpRequest.Uri);

                // Fill in zero-length data if method is post.
                Stream data = httpRequest.Data;
                if (httpRequest.Data == null && httpRequest.Method.ToLower().Equals("post"))
                {
                    data = new MemoryStream(new byte[0]);
                }

                if (data != null)
                {
                    message.Content = new StreamContent(data);
                }

                if (httpRequest.Headers != null)
                {
                    foreach (var header in httpRequest.Headers)
                    {
                        if (HttpContentHeaders.Contains(header.Key))
                        {
                            message.Content.Headers.Add(header.Key, header.Value);
                        }
                        else
                        {
                            message.Headers.Add(header.Key, header.Value);
                        }
                    }
                }

                // Avoid aggressive caching on Windows Phone 8.1.
                message.Headers.Add("Cache-Control", "no-cache");
                message.Headers.IfModifiedSince = DateTimeOffset.UtcNow;

                // TODO: (richardross) investigate progress here, maybe there's something we're missing in order to support this.
                uploadProgress.Report(new AVUploadProgressEventArgs { Progress = 0 });

                return client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                  .ContinueWith(httpMessageTask =>
                  {
                      var response = httpMessageTask.Result;

                      uploadProgress.Report(new AVUploadProgressEventArgs { Progress = 1 });

                      return response.Content.ReadAsStreamAsync().ContinueWith(streamTask =>
                      {
                          var resultStream = new MemoryStream();
                          var responseStream = streamTask.Result;

                          int bufferSize = 4096;
                          byte[] buffer = new byte[bufferSize];
                          int bytesRead = 0;
                          long totalLength = -1;
                          long readSoFar = 0;

                          try
                          {
                              totalLength = responseStream.Length;
                          }
                          catch (NotSupportedException)
                          {
                          }

                          return InternalExtensions.WhileAsync(() =>
                          {
                              return responseStream.ReadAsync(buffer, 0, bufferSize, cancellationToken).OnSuccess(readTask =>
                              {
                                  bytesRead = readTask.Result;
                                  return bytesRead > 0;
                              });
                          }, () =>
                          {
                              cancellationToken.ThrowIfCancellationRequested();

                              return resultStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).OnSuccess(_ =>
                              {
                                  cancellationToken.ThrowIfCancellationRequested();
                                  readSoFar += bytesRead;

                                  if (totalLength > -1)
                                  {
                                      downloadProgress.Report(new AVDownloadProgressEventArgs { Progress = 1.0 * readSoFar / totalLength });
                                  }
                              });
                          }).ContinueWith(_ =>
                          {
                              responseStream.Dispose();
                              return _;
                          }).Unwrap().OnSuccess(_ =>
                          {
                              // If getting stream size is not supported, then report download only once.
                              if (totalLength == -1)
                              {
                                  downloadProgress.Report(new AVDownloadProgressEventArgs { Progress = 1.0 });
                              }

                              // Assume UTF-8 encoding.
                              var resultAsArray = resultStream.ToArray();
                              var resultString = Encoding.UTF8.GetString(resultAsArray, 0, resultAsArray.Length);
                              resultStream.Dispose();
                              return new Tuple<HttpStatusCode, string>(response.StatusCode, resultString);
                          });
                      });
                  }).Unwrap().Unwrap();
            }
            catch (Exception)
            {
                var localError = new Dictionary<string, object>();
                localError["error"] = "local error when build http request";
                var localErrorStr = Json.Encode(localError);
                return Task.FromResult(new Tuple<HttpStatusCode, string>(HttpStatusCode.ExpectationFailed, localErrorStr));
            }

        }
    }
}

