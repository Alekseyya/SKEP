using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Common
{
    public class LongRunningTaskBase
    {
        protected string taskId;
        protected LongRunningTaskReport taskReport = null;

        protected static object syncRoot = new object();

        /// <summary>
                /// Gets or sets the process status.
                /// </summary>
                /// <value>The process status.</value>
        protected static IDictionary<string, int> ProcessStatus { get; set; }
        protected static IDictionary<string, string> ProcessStatusMessage { get; set; }
        protected static IDictionary<string, string> SingleTaskIdByTypeName { get; set; }

        /// <summary>
                /// Initializes a new instance of the <see cref="MyLongRunningClass"/> class.
                /// </summary>
        public LongRunningTaskBase()
        {
            if (ProcessStatus == null)
            {
                ProcessStatus = new Dictionary<string, int>();
            }

            if (ProcessStatusMessage == null)
            {
                ProcessStatusMessage = new Dictionary<string, string>();
            }

            if (SingleTaskIdByTypeName == null)
            {
                SingleTaskIdByTypeName = new Dictionary<string, string>();
            }
        }

        public void SetStatus(int progress, string statusMessage, bool includeInReport = false)
        {
            lock (syncRoot)
            {
                if (String.IsNullOrEmpty(taskId) == false)
                {
                    if (progress != -1)
                    {
                        ProcessStatus[taskId] = progress;
                    }
                    ProcessStatusMessage[taskId] = statusMessage;

                    if (includeInReport == true && taskReport != null)
                    {
                        taskReport.AddReportEvent(statusMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public bool Add(string id, bool allowOnlySingleTask)
        {
            bool result = false;

            lock (syncRoot)
            {
                if (allowOnlySingleTask == true)
                {
                    string typeFullName = this.GetType().FullName;

                    if (SingleTaskIdByTypeName.Keys.Count(x => x == typeFullName) == 1)
                    {
                        result = false;
                    }
                    else
                    {
                        SingleTaskIdByTypeName.Add(typeFullName, id);

                        ProcessStatus.Add(id, 0);
                        ProcessStatusMessage.Add(id, "");

                        result = true;
                    }
                }
                else
                {
                    ProcessStatus.Add(id, 0);
                    ProcessStatusMessage.Add(id, "");

                    result = true;
                }

            }

            return result;
        }

        /// <summary>
        /// Removes the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Remove(string id)
        {
            lock (syncRoot)
            {
                ProcessStatus.Remove(id);
                ProcessStatusMessage.Remove(id);

                string typeFullName = this.GetType().FullName;
                if (SingleTaskIdByTypeName.Keys.Count(x => x == typeFullName) == 1)
                {
                    SingleTaskIdByTypeName.Remove(typeFullName);
                }
            }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <param name="id">The id.</param>
        public static int GetStatus(string id)
        {
            lock (syncRoot)
            {
                if (ProcessStatus.Keys.Count(x => x == id) == 1)
                {
                    return ProcessStatus[id];
                }
                else
                {
                    return -1;
                }
            }
        }

        public string GetIdOfRunningSingleTask()
        {
            lock (syncRoot)
            {
                string typeFullName = this.GetType().FullName;
                if (SingleTaskIdByTypeName.Keys.Count(x => x == typeFullName) == 1)
                {
                    return SingleTaskIdByTypeName[typeFullName];
                }
                else
                {
                    return null;
                }
            }
        }

        public static string GetStatusMessage(string id)
        {
            lock (syncRoot)
            {
                if (ProcessStatusMessage.Keys.Count(x => x == id) == 1)
                {
                    return ProcessStatusMessage[id].ToString();
                }
                else
                {
                    return "";
                }
            }
        }
    }
}
