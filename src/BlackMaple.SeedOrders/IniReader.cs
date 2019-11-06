/* Copyright (c) 2019, John Lenz

All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above
      copyright notice, this list of conditions and the following
      disclaimer in the documentation and/or other materials provided
      with the distribution.

    * Neither the name of John Lenz, Black Maple Software, SeedTactics,
      nor the names of other contributors may be used to endorse or
      promote products derived from this software without specific
      prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.IO;
using System.Collections.Generic;

namespace BlackMaple.SeedOrders
{
  public static class IniReader
  {
    public static Dictionary<(string section, string key), string> Parse(string path)
    {
      if (!System.IO.File.Exists(path))
      {
        return new Dictionary<(string section, string key), string>();
      }
      using (var f = File.OpenRead(path))
      {
        return Parse(f);
      }
    }

    public static Dictionary<(string section, string key), string> Parse(Stream s)
    {
      var entries = new Dictionary<(string section, string key), string>();
      var f = new StreamReader(s);
      string line = null;
      string curSection = null;
      while ((line = f.ReadLine()) != null)
      {
        line = line.Trim();
        if (string.IsNullOrEmpty(line)) continue;
        if (line.StartsWith(";") || line.StartsWith("#")) continue;
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
          var idx = line.IndexOf(']');
          curSection = line.Substring(1, idx - 1).Trim();

        }
        else if (line.Contains("="))
        {
          var idx = line.IndexOf('=');
          var key = line.Substring(0, idx).Trim();
          var val = line.Substring(idx + 1).Trim();
          if (val.StartsWith("\"") && val.EndsWith("\""))
          {
            val = val.Substring(1, val.Length - 2);
          }
          entries[(curSection, key)] = val;
        }

      }
      return entries;
    }
  }

}