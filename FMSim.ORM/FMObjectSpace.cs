using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;

namespace FMSim.ORM
{
    public class FMObjectSpace
    {
        private ObservableCollection<FMObject> allObjects;
        PropertyChangedEventHandler AllObjectsCollectionChanged;
        private Dictionary<FMObject.FMAttribute_derived, int> InstanceSubscribers;
        public FMSubscriptionHandler SubscriptionHandler;
        public FMExpressionHandler ExpressionHandler;

        public ObservableCollection<FMObject> AllObjects
        {
            get
            {
                if (SubscriptionHandler.IsSubscribing)
                {
                    Subscribe(SubscriptionHandler.CurrentSubscriber);
                }
                return allObjects;
            }
        }

        public void Subscribe(FMObject.FMAttribute_derived aSubscriber)
        {
            if (!InstanceSubscribers.ContainsKey(aSubscriber))
            {
                InstanceSubscribers.Add(aSubscriber, 0);
                this.AllObjectsCollectionChanged += aSubscriber.ObservedItemChanged;
                aSubscriber.ObjectSpaceSubscriptions.Add(this);
            }
        }

        public void UnSubscribe(FMObject.FMAttribute_derived aSubscriber)
        {
            if (InstanceSubscribers.ContainsKey(aSubscriber))
            {
                InstanceSubscribers.Remove(aSubscriber);
                this.AllObjectsCollectionChanged -= aSubscriber.ObservedItemChanged;
                aSubscriber.ObjectSpaceSubscriptions.Remove(this);
            }
        }

        private void OnAllObjectsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.AllObjectsCollectionChanged != null)
                this.AllObjectsCollectionChanged(this, new PropertyChangedEventArgs("AllObjects"));
        }

       

        public FMObjectSpace()
        {
            InstanceSubscribers = new Dictionary<FMObject.FMAttribute_derived,int>();
            ExpressionHandler = new FMExpressionHandler(this);
            SubscriptionHandler = new FMSubscriptionHandler(this);
            allObjects = new ObservableCollection<FMObject>();
            allObjects.CollectionChanged += OnAllObjectsCollectionChanged;
        }


        void UpdateDatebase()
        {
            throw new NotImplementedException();
        }
    }
}
