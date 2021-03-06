// Copyright (c) 2015-present, Parse, LLC  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace LeanCloud.Internal {
  /// <summary>
  /// LeanCloud current config controller.
  /// </summary>
  class AVCurrentConfigController : IAVCurrentConfigController {
    private const string CurrentConfigKey = "CurrentConfig";

    private readonly TaskQueue taskQueue;
    private AVConfig currentConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeanCloud.Internal.AVCurrentConfigController"/> class.
    /// </summary>
    public AVCurrentConfigController() {
      taskQueue = new TaskQueue();
    }

    public Task<AVConfig> GetCurrentConfigAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        if (currentConfig == null) {
          object tmp;
          AVClient.ApplicationSettings.TryGetValue(CurrentConfigKey, out tmp);

          string propertiesString = tmp as string;
          if (propertiesString != null) {
            var dictionary = AVClient.DeserializeJsonString(propertiesString);
            currentConfig = new AVConfig(dictionary);
          } else {
            currentConfig = new AVConfig();
          }
        }

        return currentConfig;
      }), CancellationToken.None);
    }

    public Task SetCurrentConfigAsync(AVConfig config) {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => { 
        currentConfig = config;

        var jsonObject = ((IJsonConvertible)config).ToJSON();
        var jsonString = AVClient.SerializeJsonString(jsonObject);

        AVClient.ApplicationSettings[CurrentConfigKey] = jsonString;
      }), CancellationToken.None);
    }

    public Task ClearCurrentConfigAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        currentConfig = null;
        AVClient.ApplicationSettings.Remove(CurrentConfigKey);
      }), CancellationToken.None);
    }

    public Task ClearCurrentConfigInMemoryAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        currentConfig = null;
      }), CancellationToken.None);
    }
  }
}
