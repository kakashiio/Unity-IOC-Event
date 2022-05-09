using System;

namespace IO.Unity3D.Source.IOCEvent
{
    //******************************************
    //  
    //
    // @Author: Kakashi
    // @Email: john.cha@qq.com
    // @Date: 2022-05-09 23:11
    //******************************************
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class Event : Attribute
    {
        public string Evt;

        public Event(string evt)
        {
            Evt = evt;
        }
    }
}