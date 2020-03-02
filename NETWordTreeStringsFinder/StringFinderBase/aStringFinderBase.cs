using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NETWordTreeStringsFinder.StringFinderBase
{
    public abstract class aStringFinderBase
    {
        public static string LastErrorMsg { get; protected set; }
        public static string LastErrorStackTrace { get; protected set; }
        public static Exception LastException { get; protected set; }

        protected static void SetLastException(Exception e)
        {
            if(e == null)
            {
                LastErrorMsg = "";
                LastErrorStackTrace = "";
                LastException = null;
                return;
            }

            LastErrorMsg = e.Message;
            LastErrorStackTrace = e.StackTrace;
            LastException = e;
        }

        protected abstract Task<bool> GetIfStaticDataIsOKAsync();
    }
}
