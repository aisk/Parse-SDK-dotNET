﻿// Copyright (c) 2015-present, Parse, LLC  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Internal {
  internal class AVQueryController : IAVQueryController {
    public Task<IEnumerable<IObjectState>> FindAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject {
      string sessionToken = user != null ? user.SessionToken : null;

      return FindAsync(query.ClassName, query.BuildParameters(), sessionToken, cancellationToken).OnSuccess(t => {
        var items = t.Result["results"] as IList<object>;

        return (from item in items
                select AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance));
      });
    }

    public Task<int> CountAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject {
      string sessionToken = user != null ? user.SessionToken : null;
      var parameters = query.BuildParameters();
      parameters["limit"] = 0;
      parameters["count"] = 1;

      return FindAsync(query.ClassName, parameters, sessionToken, cancellationToken).OnSuccess(t => {
        return Convert.ToInt32(t.Result["count"]);
      });
    }

    public Task<IObjectState> FirstAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject {
      string sessionToken = user != null ? user.SessionToken : null;
      var parameters = query.BuildParameters();
      parameters["limit"] = 1;

      return FindAsync(query.ClassName, parameters, sessionToken, cancellationToken).OnSuccess(t => {
        var items = t.Result["results"] as IList<object>;
        var item = items.FirstOrDefault() as IDictionary<string, object>;

        // Not found. Return empty state.
        if (item == null) {
          return (IObjectState)null;
        }

        return AVObjectCoder.Instance.Decode(item, AVDecoder.Instance);
      });
    }

    private Task<IDictionary<string, object>> FindAsync(string className,
        IDictionary<string, object> parameters,
        string sessionToken,
        CancellationToken cancellationToken) {
      var command = new AVCommand(string.Format("/classes/{0}?{1}",
              Uri.EscapeDataString(className),
              AVClient.BuildQueryString(parameters)),
          method: "GET",
          sessionToken: sessionToken,
          data: null);

      return AVClient.AVCommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return t.Result.Item2;
      });
    }
  }
}
