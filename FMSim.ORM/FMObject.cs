using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FMSim.ORM
{
    public enum PersistenceState
    {
        New, Modified, Current
    }

    public class FMObject: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Dictionary<string, FMAttribute> attributes;
        public Dictionary<string, FMAttribute> attributes_DeletedPersisted;

        public PersistenceState persState;
        public FMObjectSpace objectSpace;
        private string fMClass;
        public string FMClass 
        {
            get { return fMClass; }
            set { 
                    fMClass = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("FMClass"));
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
            public FMAbstractAttribute(FMObject aThisObject, string aName)
            {
                thisObject = aThisObject;
                name = aName;
            }

            public void Changed()
            {
                if (persState != PersistenceState.New) // If it is persisted
                    persState = PersistenceState.Modified;
                thisObject.UpdateDirtyOSList();
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("FMValue"));
            }

            public FMObject thisObject;
            public PersistenceState persState;
            public string name;
        }

        public class FMAttribute: FMAbstractAttribute
        {
             public FMAttribute(FMObject aThisObject, string aName, Object aValue)
                : base(aThisObject, aName)
            {
                this.fMValue = aValue;
            }

            private Object fMValue;
            public Object FMValue
            {
                get
                {
                    return fMValue;
                }
                set
                {
                    fMValue = value;
                    this.Changed();
                }
            }
        }

        public class FMAttribute_derived : FMAbstractAttribute
        {
            public FMAttribute_derived(FMObject aThisObject, string aName, string aExpression)
                : base(aThisObject, aName)
            {
                this.fMExpression = aExpression;
            }
            private string fMExpression; // Write only on creation
            public object FMValue
            {
                get
                {
                    return null;
                }               
            }
        }

        bool GetIsDirty()        
        {
            foreach (KeyValuePair<string, FMAttribute> KVP in attributes)
                if (KVP.Value.persState != PersistenceState.Current)
                    return false;
            return true;
        }
        
        void UpdateDirtyOSList()
        {
            bool vIsInDirtyList = objectSpace.DirtyObjects.Contains(this);
            bool vIsDirty = GetIsDirty();
            if (vIsDirty && !vIsInDirtyList)
                objectSpace.DirtyObjects.Add(this);
            else if (!vIsDirty && vIsInDirtyList)
                objectSpace.DirtyObjects.Remove(this);
        }
        
        public FMObject(FMObjectSpace aObjectSpace)
        {
            attributes = new Dictionary<string, FMAttribute>();
            attributes_DeletedPersisted = new Dictionary<string, FMAttribute>();
            objectSpace = aObjectSpace;
            aObjectSpace.AllObjects.Add(this);
        }

        public void CreateAttribute(string aName, Object aValue)
        {            
            if (! attributes.ContainsKey(aName))
            {
                attributes.Add(aName.ToLower(), new FMAttribute(this, aName, aValue));
                if (!objectSpace.DirtyObjects.Contains(this))
                    objectSpace.DirtyObjects.Add(this); // optimization 
                Changed();
            }
        }        

        public void DeleteAttribute(string aName)
        {
            if (attributes[aName].persState != PersistenceState.New)
                attributes_DeletedPersisted.Add(aName, attributes[aName]);
            attributes.Remove(aName);
            UpdateDirtyOSList();
            Changed();
        }       
  }
}
