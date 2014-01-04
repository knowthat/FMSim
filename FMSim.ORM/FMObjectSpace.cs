using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace FMSim.ORM
{
    public class FMObjectSpace
    {
        public ObservableCollection<FMObject> AllObjects = new ObservableCollection<FMObject>();
        public ObservableCollection<FMObject> DirtyObjects = new ObservableCollection<FMObject>();

        public FMExpressionHandler expressionHandler;

        public FMObjectSpace()
        {
            expressionHandler = new FMExpressionHandler(this);
        }

        void UpdateDatebase()
        {
            throw new NotImplementedException();
        }
    }
}
