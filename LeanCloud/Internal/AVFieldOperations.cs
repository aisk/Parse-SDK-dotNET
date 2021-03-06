﻿// Copyright (c) 2015-present, Parse, LLC  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace LeanCloud.Internal {
  class AVObjectIdComparer : IEqualityComparer<object> {
    bool IEqualityComparer<object>.Equals(object p1, object p2) {
      var parseObj1 = p1 as AVObject;
      var parseObj2 = p2 as AVObject;
      if (parseObj1 != null && parseObj2 != null) {
        return object.Equals(parseObj1.ObjectId, parseObj2.ObjectId);
      }
      return object.Equals(p1, p2);
    }

    public int GetHashCode(object p) {
      var parseObject = p as AVObject;
      if (parseObject != null) {
        return parseObject.ObjectId.GetHashCode();
      }
      return p.GetHashCode();
    }
  }

  static class AVFieldOperations {
    private static AVObjectIdComparer comparer;

    public static IAVFieldOperation Decode(IDictionary<string, object> json) {
      throw new NotImplementedException();
    }

    public static IEqualityComparer<object> AVObjectComparer {
      get {
        if (comparer == null) {
          comparer = new AVObjectIdComparer();
        }
        return comparer;
      }
    }
  }
}
