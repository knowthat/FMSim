using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FMSim.ORM
{   
    public class FMObject: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string objectGUID;
        public string ObjectGUID
        {
            get 
            {
                return objectGUID;
            }
        }
        public Dictionary<string, FMAbstractAttribute> attributes;
        public Dictionary<FMAttribute_derived, int> Subscribers;
        public FMObjectSpace objectSpace;        

        public FMObject(FMObjectSpace aObjectSpace)
        {
            objectGUID = Guid.NewGuid().ToString();
            InitializeObject(aObjectSpace);
        }

        public FMObject(FMObjectSpace aObjectSpace, string aObjectGUID)
        {
            objectGUID = aObjectGUID;
            InitializeObject(aObjectSpace);
        }

        private void InitializeObject(FMObjectSpace aObjectSpace)
        {
            attributes = new Dictionary<string, FMAbstractAttribute>();
            Subscribers = new Dictionary<FMAttribute_derived, int>();
            objectSpace = aObjectSpace;
            aObjectSpace.AllObjects.Add(this);
        }

        private string fMClass;        
        public string FMClass
        {
            get 
            {
                if (objectSpace.SubscriptionHandler.IsSubscribing)
                    Subscribe(objectSpace.SubscriptionHandler.CurrentSubscriber);
                return fMClass; 
            }
            set
            {
                fMClass = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FMClass"));
            }
        }

        public void Subscribe(FMAttribute_derived aSubscriber)
        {
            if (!Subscribers.ContainsKey(aSubscriber))
            {
                Subscribers.Add(aSubscriber, 0);
                this.PropertyChanged += aSubscriber.ObservedItemChanged;
                    aSubscriber.ObjectSubscriptions.Add(this);
            }
        }

        public void UnSubscribe(FMAttribute_derived aSubscriber)
        {
            if (Subscribers.ContainsKey(aSubscriber))
            {
                Subscribers.Remove(aSubscriber);
                this.PropertyChanged -= aSubscriber.ObservedItemChanged;
                aSubscriber.ObjectSubscriptions.Remove(this);
            }
        }

        void Changed()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("attributes"));
        }

        public class FMAbstractAttribute : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public Dictionary<FMAttribute_derived, int> Subscribers;
            public FMAbstractAttribute(FMObject aThisObject, string aType, string aName)
            {
                Subscribers = new Dictionary<FMAttribute_derived, int>();
                thisObject = aThisObject;
                type = aType; 
                name = aName;
            }

            public void Subscribe(FMAttribute_derived aSubscriber)
            {
                if (!Subscribers.ContainsKey(aSubscriber))
                {
                    Subscribers.Add(aSubscriber, 0);
                    this.PropertyChanged += aSubscriber.ObservedItemChanged;
                    aSubscriber.Subscriptions.Add(this);
                }
            }

            public void UnSubscribe(FMAttribute_derived aSubscriber)
            {
                if (Subscribers.ContainsKey(aSubscriber))
                {
                    Subscribers.Remove(aSubscriber);
                    this.PropertyChanged -= aSubscriber.ObservedItemChanged;
                    aSubscriber.Subscriptions.Remove(this);
                }
            }

            public void Changed()
            {                                
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FMValue"));
            }

            public virtual Object FMValue
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            string type;
            public string Type
            {
                get
                {
                    return type;
                }
            }

            public FMObject thisObject;           
            public string name;
        }

        public class FMAttribute: FMAbstractAttribute
        {
            public FMAttribute(FMObject aThisObject, string aType, string aName, Object aValue)
                : base(aThisObject, aType, aName)
            {
                this.fMValue = aValue;
            }

            private Object fMValue;
            public override Object FMValue
            {
                get
                {
                    if (thisObject.objectSpace.SubscriptionHandler.IsSubscribing)
                        Subscribe(thisObject.objectSpace.SubscriptionHandler.CurrentSubscriber);
                    if (Type == "Int32")
                        return (Int32)fMValue;
                    else
                        return fMValue;
                }
                set
                {
                    if (Type == "Int32" && value is String)
                        fMValue = Int32.Parse(value as String);
                    else
                        fMValue = value;
                    this.Changed();
                }
            }
        }

        public class FMAttribute_derived : FMAbstractAttribute
        {
            public List<FMAbstractAttribute> Subscriptions;
            public List<FMObject> ObjectSubscriptions;
            public List<FMObjectSpace> ObjectSpaceSubscriptions;
            public FMAttribute_derived(FMObject aThisObject, string aType, string aName, string aExpression)
                : base(aThisObject, aType, aName)
            {
                Subscriptions = new List<FMAbstractAttribute>();
                ObjectSubscriptions = new List<FMObject>();
                ObjectSpaceSubscriptions = new List<FMObjectSpace>();
                this.fMExpression = aExpression;
            }
            private string fMExpression; // Write only on creation
            public string Expression { get { return fMExpression; } }
            public override object FMValue
            {
                get
                {
                    try
                    {
                        if (thisObject.objectSpace.SubscriptionHandler.IsSubscribing)
                            this.Subscribe(thisObject.objectSpace.SubscriptionHandler.CurrentSubscriber);
                        thisObject.objectSpace.SubscriptionHandler.StartSubscriptionSession(this);
                        return thisObject.objectSpace.ExpressionHandler.Evaluate(thisObject, fMExpression);
                    }
                    catch
                    {
                        return null;
                    }
                    finally
                    {
                        thisObject.objectSpace.SubscriptionHandler.EndSubscriptionSession();                       
                    }
                }
            }

            public void ObservedItemChanged(Object sender, PropertyChangedEventArgs e)
            {
                this.Changed();
            }

            public void CancelAllSubscriptions()
            {
                foreach (FMAbstractAttribute FMA in Subscriptions)
                    FMA.UnSubscribe(this);
                foreach (FMObject FMO in ObjectSubscriptions)
                    FMO.UnSubscribe(this);
                foreach (FMObjectSpace FMOS in ObjectSpaceSubscriptions)
                    FMOS.UnSubscribe(this);
            }
        }               

        public void CreateAttribute(string aType, string aName, Object aValue)
        {
            if (!attributes.ContainsKey(aName.ToLower()))
            {
                attributes.Add(aName.ToLower(), new FMAttribute(this, aType, aName, aValue));
                Changed();
            }
        }

        public void CreateDerivedAttribute(string aType, string aName, string aExpression)
        {
            if (!attributes.ContainsKey(aName.ToLower()))
            {
                attributes.Add(aName.ToLower(), new FMAttribute_derived(this, aType, aName, aExpression));
                Changed();
            }
        }        

        public void DeleteAttribute(string aName)
        {                
            attributes.Remove(aName);         
            Changed();
        }       
  }
}
