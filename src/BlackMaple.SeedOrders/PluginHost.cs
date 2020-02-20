/* Copyright (c) 2017, John Lenz

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
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.IO;

namespace BlackMaple.SeedOrders
{
  /// <summary>
  ///  Class used by SeedTactics to help communication between the plugin and SeedTactics itself
  /// </summary>
  /// <remarks>
  ///  <para>
  ///    If you are implementing an order plugin, you do not need to use this class at all.
  ///    Instead, implement the <c>IBookingDatabase</c> and <c>IWorkorderDatabase</c> interfaces.
  ///  </para>
  ///  <para>
  ///    SeedTactics itself will use this class to communicate across the AppDomain boundary.
  ///    Internally, it uses JSON to communicate classes instead of relying on the Serializable
  ///    attribute from .NET remoting. Using JSON and DataContract allows easier versioning and
  ///    compatibility.
  ///   </para>
  /// </remarks>
  public class PluginHost : MarshalByRefObject
  {
    private IBookingDatabase _bookings;
    private IWorkorderDatabase _workorders;

    public PluginHost(string pluginDll)
    {
      try
      {
        var a = Assembly.LoadFrom(pluginDll);
        foreach (var t in a.GetTypes())
        {
          foreach (var i in t.GetInterfaces())
          {
            if (_bookings == null && i == typeof(IBookingDatabase))
              _bookings = (IBookingDatabase)Activator.CreateInstance(t);
            if (_workorders == null && i == typeof(IWorkorderDatabase))
              _workorders = (IWorkorderDatabase)Activator.CreateInstance(t);
          }
        }
      }
      catch (Exception ex)
      {
        var dir = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "OrderLink-plugin-error.txt"), ex.ToString());
        throw ex;
      }
    }

    private string EncJson<T>(T val) where T : class
    {
      using (var ms = new MemoryStream())
      {
        var s = new DataContractJsonSerializer(typeof(T));
        s.WriteObject(ms, val);
        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
      }
    }

    private T DecJson<T>(string json) where T : class
    {
      using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
      {
        var s = new DataContractJsonSerializer(typeof(T));
        return s.ReadObject(ms) as T;
      }
    }

    public bool HasBookingAPI()
    {
      return _bookings != null;
    }

    public bool HasWorkorderAPI()
    {
      return _workorders != null;
    }

    #region Booking API
    public string LoadUnscheduledStatusJson(int lookaheadDays)
    {
      if (_bookings == null) throw new Exception("Plugin does not implement booking API");
      return EncJson(_bookings.LoadUnscheduledStatus(lookaheadDays));
    }

    public void CreateSchedule(string newScheduleJson)
    {
      if (_bookings == null) throw new Exception("Plugin does not implement booking API");
      _bookings.CreateSchedule(DecJson<NewSchedule>(newScheduleJson));
    }

    public void HandleBackedOutWork(long backoutId, string backedOutParts)
    {
      if (_bookings == null) throw new Exception("Plugin does not implement booking API");
      _bookings.HandleBackedOutWork(
          backoutId,
          DecJson<List<BackedOutPart>>(backedOutParts)
      );
    }
    #endregion

    #region Workorder API
    public string LoadUnfilledWorkordersJson(int lookaheadDays)
    {
      if (_workorders == null) throw new Exception("Plugin does not implement workorder API");
      return EncJson(_workorders.LoadUnfilledWorkorders(lookaheadDays));
    }

    public string LoadUnfilledWorkordersJson(string part)
    {
      if (_workorders == null) throw new Exception("Plugin does not implement workorder API");
      return EncJson(_workorders.LoadUnfilledWorkorders(part));
    }

    public void MarkWorkorderAsFilled(string workorderId, DateTime fillUTC, string resourcesJson)
    {
      if (_workorders == null) throw new Exception("Plugin does not implement workorder API");
      _workorders.MarkWorkorderAsFilled(workorderId, fillUTC, DecJson<WorkorderResources>(resourcesJson));
    }
    #endregion
  }
}