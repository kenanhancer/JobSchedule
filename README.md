JobSchedule
===========

Job Schedule for C#.NET Methods
You can read article from http://www.kenanhancer.com/job-schedule/.

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Dynamic;
    using System.Reflection;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Threading;
    using JobScheduleLib;

    namespace ConsoleApplication_JobScheduleTest
    {
        class Program
        {
            static void Main(string[] args)
            {
                dynamic dailySchedule1 = new ExpandoObject();
                dailySchedule1.Class = "SMS";
                dailySchedule1.Method = "SendCustomerInvoice";
                dailySchedule1.ScheduleFrequencyType = ScheduleFrequencyType.DailySchedule;
                dailySchedule1.ScheduleFrequencyInterval = 1;
                dailySchedule1.RecuriveFrequencyType = RecursiveFrequencyTypeEnum.EverySecond;
                dailySchedule1.RecuriveFrequencyInterval = 2;
                dailySchedule1.StartingAt = new TimeSpan(0, 0, 0);
                dailySchedule1.EndingAt = new TimeSpan(23, 59, 59);
                dailySchedule1.DurationStartDate = DateTime.Now.AddSeconds(-4);
                dailySchedule1.DurationEndDate = null;

                dynamic dailySchedule2 = new ExpandoObject();
                dailySchedule2.Class = "SMS";
                dailySchedule2.Method = "SendCustomerInvoice";
                dailySchedule2.ScheduleFrequencyType = ScheduleFrequencyType.DailySchedule;
                dailySchedule2.ScheduleFrequencyInterval = 1;
                dailySchedule2.RecuriveFrequencyType = RecursiveFrequencyTypeEnum.EverySecond;
                dailySchedule2.RecuriveFrequencyInterval = 2;
                dailySchedule2.StartingAt = new TimeSpan(9, 53, 0);
                dailySchedule2.EndingAt = new TimeSpan(9, 54, 0);
                dailySchedule2.DurationStartDate = DateTime.Now;
                dailySchedule2.DurationEndDate = dailySchedule2.DurationStartDate.AddDays(5);

                dynamic dailySchedule3 = new ExpandoObject();
                dailySchedule3.Class = "SMS";
                dailySchedule3.Method = "SendCustomerInvoice";
                dailySchedule3.ScheduleFrequencyType = ScheduleFrequencyType.DailySchedule;
                dailySchedule3.ScheduleFrequencyInterval = 1;
                dailySchedule3.RecuriveFrequencyType = RecursiveFrequencyTypeEnum.EveryMinute;
                dailySchedule3.RecuriveFrequencyInterval = 1;
                dailySchedule3.StartingAt = new TimeSpan(18, 52, 0);
                dailySchedule3.EndingAt = new TimeSpan(18, 54, 0);
                dailySchedule3.DurationStartDate = DateTime.Now;
                dailySchedule3.DurationEndDate = dailySchedule3.DurationStartDate.AddDays(5);

                dynamic dailySchedule4 = new ExpandoObject();
                dailySchedule4.Class = "SMS";
                dailySchedule4.Method = "SendCustomerInvoice";
                dailySchedule4.ScheduleFrequencyType = ScheduleFrequencyType.DailySchedule;
                dailySchedule4.ScheduleFrequencyInterval = 2;
                dailySchedule4.RecuriveFrequencyType = RecursiveFrequencyTypeEnum.EverySecond;
                dailySchedule4.RecuriveFrequencyInterval = 1;
                dailySchedule4.StartingAt = new TimeSpan(10, 49, 0);
                dailySchedule4.EndingAt = new TimeSpan(10, 49, 15);
                dailySchedule4.DurationStartDate = DateTime.Now;
                dailySchedule4.DurationEndDate = dailySchedule4.DurationStartDate.AddDays(5);

                dynamic dailySchedule5 = new ExpandoObject();
                dailySchedule5.Class = "SMS";
                dailySchedule5.Method = "SendCustomerInvoice";
                dailySchedule5.ScheduleFrequencyType = ScheduleFrequencyType.DailySchedule;
                dailySchedule5.ScheduleFrequencyInterval = 2;
                dailySchedule5.RecuriveFrequencyType = RecursiveFrequencyTypeEnum.EverySecond;
                dailySchedule5.RecuriveFrequencyInterval = 1;
                dailySchedule5.StartingAt = new TimeSpan(18, 48, 21);
                dailySchedule5.EndingAt = new TimeSpan(18, 48, 40);
                dailySchedule5.DurationStartDate = DateTime.Now;
                dailySchedule5.DurationEndDate = dailySchedule5.DurationStartDate.AddDays(5);

                dynamic weeklySchedule6 = new ExpandoObject();
                weeklySchedule6.Class = "SMS";
                weeklySchedule6.Method = "SendCustomerInvoice";
                weeklySchedule6.ScheduleFrequencyType = ScheduleFrequencyType.WeeklySchedule;
                weeklySchedule6.ScheduleFrequencyInterval = 2;
                weeklySchedule6.RecuriveFrequencyType = RecursiveFrequencyTypeEnum.EverySecond;
                weeklySchedule6.RecuriveFrequencyInterval = 1;
                weeklySchedule6.StartingAt = new TimeSpan(4, 21, 0);
                weeklySchedule6.EndingAt = new TimeSpan(4, 21, 10);
                weeklySchedule6.DurationStartDate = DateTime.Now;
                weeklySchedule6.DurationEndDate = weeklySchedule6.DurationStartDate.AddDays(100);

                dynamic monthlySchedule6 = new ExpandoObject();
                monthlySchedule6.Class = "SMS";
                monthlySchedule6.Method = "SendCustomerInvoice";
                monthlySchedule6.ScheduleFrequencyType = ScheduleFrequencyType.MonthlySchedule;
                monthlySchedule6.ScheduleFrequencyInterval = 2;
                monthlySchedule6.RecuriveFrequencyType = RecursiveFrequencyTypeEnum.EverySecond;
                monthlySchedule6.RecuriveFrequencyInterval = 1;
                monthlySchedule6.StartingAt = new TimeSpan(4, 29, 0);
                monthlySchedule6.EndingAt = new TimeSpan(4, 29, 10);
                monthlySchedule6.DurationStartDate = DateTime.Now;
                monthlySchedule6.DurationEndDate = monthlySchedule6.DurationStartDate.AddDays(200);

                dynamic dailyOccursOnceAtSchedule7 = new ExpandoObject();
                dailyOccursOnceAtSchedule7.Class = "SMS";
                dailyOccursOnceAtSchedule7.Method = "SendCustomerInvoice";
                dailyOccursOnceAtSchedule7.ScheduleFrequencyType = ScheduleFrequencyType.DailySchedule;
                dailyOccursOnceAtSchedule7.ScheduleFrequencyInterval = 1;
                dailyOccursOnceAtSchedule7.OccursOnceAt = new TimeSpan(5, 10, 0);
                dailyOccursOnceAtSchedule7.DurationStartDate = DateTime.Now;
                dailyOccursOnceAtSchedule7.DurationEndDate = dailyOccursOnceAtSchedule7.DurationStartDate.AddDays(5);

                var schedules = new List<dynamic>();
                schedules.AddRange(new[] { dailySchedule4 });


                dynamic assemblyPath = new ExpandoObject();
                assemblyPath.FilePath = @"C:\Users\kenan\Documents\Visual Studio 2010\Projects\JobScheduleSolutionV1\SampleLibrary\bin\Debug\SampleLibrary.dll";
                assemblyPath.ServiceMethods = JobSchedule.GetServiceMethods(assemblyPath, schedules.ToArray());

                //You can serialize and deserialize Job Schedule data. So, After serializing data, you can update schedules.
                var serializedAssemblyPath = Newtonsoft.Json.JsonConvert.SerializeObject(assemblyPath, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.Converters.IsoDateTimeConverter(), new Newtonsoft.Json.Converters.StringEnumConverter());
                var deserializedAssemblyPath = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(serializedAssemblyPath);

                var host = new JobScheduleHost("Job Schedule", "Job Schedule", deserializedAssemblyPath);
                host.Start();
                Console.ReadLine();
                host.Stop();
                Console.ReadLine();
            }
        }
    }
