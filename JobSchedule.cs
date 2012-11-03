using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Dynamic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Specialized;

public class ServiceClassAttribute : Attribute { }
public class ServiceMethodAttribute : Attribute { }
public enum RecursiveFrequencyTypeEnum { OnceAt, EveryHour, EveryMinute, EverySecond }
public enum ScheduleFrequencyType { DailySchedule, WeeklySchedule, MonthlySchedule }
public class JobScheduleHost
{
    public EventLog eventLog;
    private object[] assemblyPaths;
    private List<dynamic> timerList;
    public JobScheduleHost(string eventLogSourceName = "Job Schedule", string eventLogLogName = "Job Schedule", params dynamic[] assemblyPaths)
    {
        this.eventLog = new EventLog();
        if (!System.Diagnostics.EventLog.SourceExists(eventLogSourceName))
            System.Diagnostics.EventLog.CreateEventSource(eventLogSourceName, eventLogLogName);
        eventLog.Source = eventLogSourceName;
        eventLog.Log = eventLogLogName;
        this.assemblyPaths = assemblyPaths;
        timerList = new List<dynamic>();
        int scheduleCount = 0;
        foreach (var assemblyPath in assemblyPaths)
        {
            foreach (var serviceMethod in assemblyPath.ServiceMethods)
            {
                foreach (var schedule in serviceMethod.Schedules)
                {
                    var scheduleDict = ((object)schedule).ToDictionary();
                    if (!scheduleDict.ContainsKey("Enabled"))
                        schedule.Enabled = true;
                    else
                    {
                        bool enabled;
                        bool.TryParse(schedule.Enabled, out enabled);
                        if (!enabled) continue;
                    }
                    schedule.IsFirstTimeWork = true;
                    schedule.LastExecuted = (DateTime?)null;
                    schedule.PreviousElapsedTime = (DateTime?)null;
                    if (!scheduleDict.ContainsKey("DurationStartDate"))
                        schedule.DurationStartDate = (DateTime?)null;
                    else
                        schedule.DurationStartDate = ((object)schedule.DurationStartDate).To<DateTime>();
                    if (!scheduleDict.ContainsKey("DurationEndDate"))
                        schedule.DurationEndDate = (DateTime?)null;
                    else
                        schedule.DurationEndDate = schedule.DurationEndDate == null ? (DateTime?)null : ((object)schedule.DurationEndDate).To<DateTime>();
                    if (!scheduleDict.ContainsKey("Name"))
                        schedule.Name = string.Format("Schedule {0}", ++scheduleCount);
                    dynamic newTimer = new ExpandoObject();
                    newTimer.AssemblyPath = assemblyPath;
                    newTimer.ServiceMethod = serviceMethod;
                    newTimer.Schedule = schedule;
                    System.Timers.Timer timer = new System.Timers.Timer(500);
                    timer.Enabled = false;
                    timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                    newTimer.Timer = timer;
                    newTimer.Task = null;
                    timerList.Add(newTimer);
                }
            }
        }
    }
    void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        System.Timers.Timer timer = sender as System.Timers.Timer;
        dynamic getTimer = timerList.Where(f => f.Timer == timer).SingleOrDefault();
        dynamic schedule = getTimer.Schedule;
        dynamic serviceMethod = getTimer.ServiceMethod;
        dynamic assemblyPath = getTimer.AssemblyPath;
        Task task = getTimer.Task;
        bool isWork = IsRunSchedule(schedule, e.SignalTime);
        if (isWork)
        {
            string methodName = serviceMethod.Method;
            string className = serviceMethod.Class;
            if (task == null || task.Status == TaskStatus.RanToCompletion)
            {
                task = Task.Factory.StartNew(f =>
                {
                    var parameters = new Dictionary<string, object>();
                    dynamic dynoObject = f;
                    parameters.Add("SignalTime", dynoObject.SignalTime);
                    parameters.Add("EventLog", eventLog);
                    parameters.Add("Schedule", schedule);
                    InvokeMethod(dynoObject.FilePath, dynoObject.Class, dynoObject.Method, parameters);
                }, new { FilePath = assemblyPath.FilePath, Class = className, Method = methodName, SignalTime = e.SignalTime }
                );
            }
        }
    }
    private bool IsRunSchedule(dynamic schedule, DateTime now)
    {
        var scheduleDict = ((object)schedule).ToDictionary();
        DateTime? durationStartDate = schedule.DurationStartDate;
        DateTime? durationEndDate = schedule.DurationEndDate;
        if (durationStartDate == null) return false;
        if (now.Date < durationStartDate.Value.Date || (durationEndDate != null && now.Date > durationEndDate.Value.Date))
            return false;
        DateTime? lastExecuted = schedule.LastExecuted;
        DateTime? previousElapsedTime = schedule.PreviousElapsedTime;
        schedule.PreviousElapsedTime = now;
        ScheduleFrequencyType scheduleFrequencyType;
        Enum.TryParse<ScheduleFrequencyType>(schedule.ScheduleFrequencyType, out scheduleFrequencyType);
        long ScheduleFrequencyInterval = schedule.ScheduleFrequencyInterval;
        bool isWork = false;
        if (scheduleDict.ContainsKey("OccursOnceAt"))
        {
            var occursOnceAt = TimeSpan.Zero;
            TimeSpan.TryParse(schedule.OccursOnceAt, out occursOnceAt);
            if (now.TimeOfDay.Hours == occursOnceAt.Hours && now.TimeOfDay.Minutes == occursOnceAt.Minutes && now.TimeOfDay.Seconds == occursOnceAt.Seconds)
            {
                if (lastExecuted != null)
                {
                    if (scheduleFrequencyType == ScheduleFrequencyType.DailySchedule)
                        if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddDays(ScheduleFrequencyInterval).Date == now.Date)
                            lastExecuted = now;
                        else if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddDays(ScheduleFrequencyInterval).Date != now.Date)
                            return false;
                    else if (scheduleFrequencyType == ScheduleFrequencyType.WeeklySchedule)
                        if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddDays(ScheduleFrequencyInterval * 7).Date == now.Date)
                            lastExecuted = now;
                        else if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddDays(ScheduleFrequencyInterval * 7).Date != now.Date)
                            return false;
                    else if (scheduleFrequencyType == ScheduleFrequencyType.MonthlySchedule)
                        if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddMonths((int)ScheduleFrequencyInterval).Date == now.Date)
                            lastExecuted = now;
                        else if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddMonths((int)ScheduleFrequencyInterval).Date != now.Date)
                            return false;
                    isWork = lastExecuted.Value.Date == now.Date && now.Hour == lastExecuted.Value.Hour && now.Minute == lastExecuted.Value.Minute && now.Second == lastExecuted.Value.Second;
                }
                if (isWork || lastExecuted == null)
                {
                    schedule.LastExecuted = now;
                    return previousElapsedTime == null || (now.Second != previousElapsedTime.Value.Second);
                }
            }
        }
        else
        {
            RecursiveFrequencyTypeEnum recursiveFrequencyType;
            Enum.TryParse<RecursiveFrequencyTypeEnum>(schedule.RecuriveFrequencyType, out recursiveFrequencyType);
            var startingAt = TimeSpan.Zero;
            var endingAt = TimeSpan.Zero;
            TimeSpan.TryParse(schedule.StartingAt, out startingAt);
            TimeSpan.TryParse(schedule.EndingAt, out endingAt);
            var recursiveFrequencyInterval = schedule.RecuriveFrequencyInterval;
            DateTime newDate = now;
            if (now.TimeOfDay >= startingAt && new TimeSpan(now.TimeOfDay.Hours, now.TimeOfDay.Minutes, now.TimeOfDay.Seconds) <= endingAt)
            {
                if (lastExecuted != null)
                {
                    if (scheduleFrequencyType == ScheduleFrequencyType.DailySchedule)
                        if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddDays(ScheduleFrequencyInterval).Date == now.Date)
                            lastExecuted = now;//now.AddSeconds(-recursiveFrequencyInterval);
                        else if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddDays(ScheduleFrequencyInterval).Date != now.Date)
                            return false;
                    else if (scheduleFrequencyType == ScheduleFrequencyType.WeeklySchedule)
                        if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddDays(ScheduleFrequencyInterval * 7).Date == now.Date)
                            lastExecuted = now;//now.AddSeconds(-recursiveFrequencyInterval);
                        else if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddDays(ScheduleFrequencyInterval * 7).Date != now.Date)
                            return false;
                    else if (scheduleFrequencyType == ScheduleFrequencyType.MonthlySchedule)
                        if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddMonths((int)ScheduleFrequencyInterval).Date == now.Date)
                            lastExecuted = now;//now.AddSeconds(-recursiveFrequencyInterval);
                        else if (lastExecuted.Value.Date < now.Date && lastExecuted.Value.AddMonths((int)ScheduleFrequencyInterval).Date != now.Date)
                            return false;
                    if (lastExecuted != now)
                        if (recursiveFrequencyType == RecursiveFrequencyTypeEnum.EveryHour)
                            newDate = lastExecuted.Value.AddHours(recursiveFrequencyInterval);
                        else if (recursiveFrequencyType == RecursiveFrequencyTypeEnum.EveryMinute)
                            newDate = lastExecuted.Value.AddMinutes(recursiveFrequencyInterval);
                        else if (recursiveFrequencyType == RecursiveFrequencyTypeEnum.EverySecond)
                            newDate = lastExecuted.Value.AddSeconds(recursiveFrequencyInterval);
                    isWork = now.Date == newDate.Date && now.Hour == newDate.Hour && now.Minute == newDate.Minute && (now.Second == newDate.Second || now.TimeOfDay.Subtract(previousElapsedTime.Value.TimeOfDay).Seconds == recursiveFrequencyInterval);
                }
                if (isWork || (lastExecuted == null && previousElapsedTime != null))
                {
                    schedule.LastExecuted = now;
                    return now.Second != previousElapsedTime.Value.Second && now.Millisecond != previousElapsedTime.Value.Millisecond;
                }
            }
        }
        return false;
    }
    private void InvokeMethod(string assemblyPath, string className, string methodName, Dictionary<string, object> parameters)
    {
        try
        {
            Assembly asm = Assembly.LoadFrom(assemblyPath);
            Type[] typeArray = asm.GetTypes().
                Where(
                f => f.GetCustomAttributes(true).
                    Where(k => k.GetType().Name == typeof(ServiceClassAttribute).Name).Count() > 0
                    ).ToArray();
            Type type = typeArray.Where(f => f.Name == className).SingleOrDefault();
            MethodInfo[] miArray = type.GetMethods().Where(p => p.GetCustomAttributes(true).Where(k => k.GetType().Name == typeof(ServiceMethodAttribute).Name).Count() > 0).ToArray();
            MethodInfo mi = miArray.Where(f => f.Name == methodName).SingleOrDefault();
            if (mi != null)
                mi.Invoke(Activator.CreateInstance(type), new object[] { parameters });
        }
        catch (Exception ex)
        {
            eventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
        }
    }
    public void Start()
    {
        foreach (var item in timerList)
        {
            System.Timers.Timer timer = item.Timer as System.Timers.Timer;
            timer.Enabled = true;
            GC.KeepAlive(timer);
        }
    }
    public void Stop()
    {
        foreach (var item in timerList)
        {
            var timer = item.Timer as System.Timers.Timer;
            timer.Enabled = false;
        }
    }
}
public class JobSchedule
{
    public static List<dynamic> GetServiceMethods(dynamic assemblyPath, params dynamic[] schedules)
    {
        var serviceMethods = new List<dynamic>();
        if (!((object)assemblyPath).ToDictionary().ContainsKey("ServiceMethods"))
            assemblyPath.ServiceMethods = serviceMethods;
        else
            serviceMethods = assemblyPath.ServiceMethods;
        if (serviceMethods.Count > 0) return serviceMethods;
        Assembly asm = Assembly.LoadFrom(assemblyPath.FilePath);
        var types = asm.GetTypes().Where(f => f.GetCustomAttributes(true).Where(k => k.GetType().Name == typeof(ServiceClassAttribute).Name).Count() > 0).ToArray();
        foreach (var type in types)
        {
            assemblyPath.Description = type.ToAttribute<DescriptionAttribute>().Description;
            var methods = type.GetMethods().Where(p => p.GetCustomAttributes(true).Where(k => k.GetType().Name == typeof(ServiceMethodAttribute).Name).Count() > 0);
            foreach (var method in methods)
            {
                dynamic serviceMethod = new ExpandoObject();
                serviceMethod.Class = type.Name;
                serviceMethod.Method = method.Name;
                serviceMethod.Description = method.ToAttribute<DescriptionAttribute>().Description;
                var scheduleList = new List<dynamic>();
                foreach (var item in schedules.Where(f => f.Class == type.Name && f.Method == method.Name))
                {
                    var scheduleDict = ((object)item).ToDictionary();
                    var newScheduleDict = new Dictionary<string, object>();
                    scheduleDict.Where(f => f.Key != "Class" && f.Key != "Method").ToList().ForEach(f => newScheduleDict.Add(f.Key, f.Value));
                    scheduleList.Add(newScheduleDict.ToExpando());
                }
                serviceMethod.Schedules = scheduleList;
                serviceMethods.Add(serviceMethod);
            }
        }
        return serviceMethods;
    }
}
public static class Extensions
{
    public static dynamic ToExpando(this object obj)
    {
        if (obj.GetType() == typeof(ExpandoObject)) return obj;
        var result = new ExpandoObject();
        var dict = result as IDictionary<string, object>;
        int i = 0;
        if (obj.GetType() == typeof(NameValueCollection) || obj.GetType().IsSubclassOf(typeof(NameValueCollection)))
        {
            var nv = (NameValueCollection)obj;
            nv.Cast<string>().Select(key => new KeyValuePair<string, object>(key, nv[key])).ToList().ForEach(f => dict.Add(f));
        }
        else if (typeof(IEnumerable<dynamic>).IsAssignableFrom(obj.GetType()))
            obj.ToEnumerable<dynamic>().ToList().ForEach(f => dict.Add("Item" + (i++).ToString(), f));
        else if (typeof(IDictionary<string, object>).IsAssignableFrom(obj.GetType()))
            obj.ToDictionary().ToList().ForEach(f => dict.Add(f.Key, f.Value));
        else
            obj.GetType().GetProperties().ToList().ForEach(f => dict.Add(f.Name, f.GetValue(obj, null)));
        return result;
    }
    public static IDictionary<string, object> ToDictionary(this object obj)
    {
        if (typeof(Dictionary<string, object>).IsAssignableFrom(obj.GetType()))
            return (IDictionary<string, object>)obj;
        else
            return (IDictionary<string, object>)obj.ToExpando();
    }
    public static T To<T>(this object obj)
    {
        if (obj == DBNull.Value)
            return (T)default(T);
        else
            return (T)Convert.ChangeType(obj, typeof(T));
    }
    public static IEnumerable<T> ToEnumerable<T>(this object obj)
    {
        return obj as IEnumerable<T>;
    }
    public static T ToAttribute<T>(this object obj)
    {
        object retValue = null;
        if (obj is Type)
        {
            Type type = obj as Type;
            retValue = type.GetCustomAttributes(true).Where(k => k.GetType().Name == typeof(T).Name).SingleOrDefault();
        }
        else if (obj is MethodInfo)
        {
            MethodInfo mi = obj as MethodInfo;
            retValue = mi.GetCustomAttributes(true).Where(k => k.GetType().Name == typeof(T).Name).SingleOrDefault();
        }
        return retValue == null ? Activator.CreateInstance<T>() : (T)retValue;
    }
}