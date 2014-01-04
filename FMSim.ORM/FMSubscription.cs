using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FMSim.ORM
{
    public class FMSubscriptionHandler
    {
        public FMSim.ORM.FMObject.FMAttribute_derived CurrentSubscriber
        {
            get
            {
                return Subscribers.Peek();
            }
        }

        public bool IsSubscribing
        {
            get
            {
                return Subscribers.Count > 0;
            }
        }

        Stack<FMSim.ORM.FMObject.FMAttribute_derived> Subscribers;
        private FMObjectSpace objectSpace;
        public FMSubscriptionHandler(FMObjectSpace aObjectSpace)
        {
            objectSpace = aObjectSpace;
            Subscribers = new Stack<FMSim.ORM.FMObject.FMAttribute_derived>();
        }

        public void StartSubscriptionSession(FMSim.ORM.FMObject.FMAttribute_derived aSubscriber)
        {
            Subscribers.Push(aSubscriber);
        }

        public void EndSubscriptionSession()
        {
            Subscribers.Pop();
        }
    }
}
