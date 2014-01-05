using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace FMSim.ORM
{
    public class FMPersistenceHandler
    {        
        SqlConnection fDBConnection;
        FMObjectSpace objectSpace;
        public readonly string[] SupportedTypes = { "Int32", "Decimal", "String", "DateTime", "Boolean" };      

        DataTable ConnectAndEvaluateSQL(string aQuery)
        {
            SqlDataAdapter vDA = new SqlDataAdapter(aQuery, fDBConnection);
            DataTable vDT = new DataTable();
            fDBConnection.Open();
            try
            {
                vDA.Fill(vDT);                
            }
            finally
            {
                fDBConnection.Close();
            }
            return vDT;
        }

        DataTable EvaluateSQL(string aQuery)
        {
            SqlDataAdapter vDA = new SqlDataAdapter(aQuery, fDBConnection);
            DataTable vDT = new DataTable();
            vDA.Fill(vDT);                            
            return vDT;
        }

        public FMPersistenceHandler(FMObjectSpace aObjectSpace)
        {
            objectSpace = aObjectSpace;
            fDBConnection = new SqlConnection(@"Data Source=OPTIPLEX\SQLEXPRESS;Initial Catalog=FMSim;Integrated Security=True");
        }

        string GetTableForAttributeType(FMObject.FMAbstractAttribute aAttribute)
        {
            if (aAttribute is FMObject.FMAttribute_derived)
                return "FMDerived";
            else 
            {
                return "FM" + aAttribute.Type;
            }
        }

        bool AttributeExistsInPS(FMObject.FMAbstractAttribute aAttribute)
        {
            return EvaluateSQL("select ObjectGUID from " + GetTableForAttributeType(aAttribute) + " where ObjectGUID = '" + aAttribute.thisObject.ObjectGUID + "' and Attribute = '" + aAttribute.name + "'").Rows.Count > 0;
        }

        void PersistObject(FMObject aObject)
        {
            try
            {
                fDBConnection.Open();
                #region Begin transaction
                SqlCommand vSQLCMD = new SqlCommand("Begin transaction", fDBConnection);
                vSQLCMD.ExecuteNonQuery();
                #endregion
            

                string vSQL = "";
                bool vOjectInDB  = EvaluateSQL("select ObjectGUID from FMObject where ObjectGUID = '" + aObject.ObjectGUID + "'").Rows.Count > 0;
                if (vOjectInDB)
                {
                     vSQL = "update FMObject set classname = '" + aObject.FMClass + "' where ObjectGUID = '" + aObject.ObjectGUID + "'";
                }
                else
                {
                    vSQL = "insert into FMObject values ('" + aObject.ObjectGUID + "', '" + aObject.FMClass + "')";
                }
                new SqlCommand(vSQL, fDBConnection).ExecuteNonQuery();

                foreach (KeyValuePair<String, FMObject.FMAbstractAttribute> KVP in aObject.attributes)
                {
                    vSQL = "";
                    if (KVP.Value is FMObject.FMAttribute_derived)
                    {
                        FMObject.FMAttribute_derived vDerAttr = KVP.Value as FMObject.FMAttribute_derived;                       
                        if (AttributeExistsInPS(vDerAttr))
                            vSQL = "update " + GetTableForAttributeType(vDerAttr) + " set query = '" + vDerAttr.Expression + "' where attribute = '" + vDerAttr.name + "' and objectguid = '" + aObject.ObjectGUID + "' and Type = '" + vDerAttr.Type + "'";
                        else
                            vSQL = "insert into " + GetTableForAttributeType(vDerAttr) + " values ('" + vDerAttr.Expression + "', '" + vDerAttr.name + "', '" + aObject.ObjectGUID + "', '" + vDerAttr.Type + "')";
                    }                         

                    else
                    {
                        FMObject.FMAttribute vAttr = KVP.Value as FMObject.FMAttribute;
                        string vSQLValue = "";
                        switch (KVP.Value.Type)
                        {
                            case "Int32": case "Decimal":
                                vSQLValue = vAttr.FMValue.ToString();
                                break;                            
                            case "String":
                                vSQLValue = "'" + vAttr.FMValue.ToString() + "'";
                                break;
                            case "DateTime":
                                vSQLValue = ((DateTime)vAttr.FMValue).ToString("yyyy-MM-dd HH:mm:ss");
                                break;
                            case "Boolean":
                                vSQLValue = (Boolean)vAttr.FMValue? "1" : "0";
                                break;
                        }
                        if (AttributeExistsInPS(vAttr))
                            vSQL = "update " + GetTableForAttributeType(vAttr) + " set value = " + vSQLValue+ " where attribute = '" + vAttr.name + "' and objectguid = '" + aObject.ObjectGUID + "'";
                        else
                            vSQL = "insert into " + GetTableForAttributeType(vAttr) + " values (" + vSQLValue + ", '" + vAttr.name  + "', '" + aObject.ObjectGUID + "')";
                    }
                    new SqlCommand(vSQL, fDBConnection).ExecuteNonQuery();
                }

                #region Commit transaction
                new SqlCommand("Commit transaction", fDBConnection).ExecuteNonQuery();
                #endregion
            }
            catch (Exception Exc)
            {                
                new SqlCommand("Rollback transaction", fDBConnection).ExecuteNonQuery();
                throw (Exc);
            }
            finally
            {
                fDBConnection.Close();
            }
        }

        public struct AttributeDetails
        {
            public string Attribute;
            public Object Value;
            public string Type;
        }

        public struct DerivedAttributeDetails
        {
            public string Query;
            public string Attribute;
            public string Type;
        }

        public class ObjectDetails
        {
            public string ObjectGUID;
            public string ClassName;
            public List<AttributeDetails> Attributes = new List<AttributeDetails>();
            public List<DerivedAttributeDetails> DerivedAttributes = new List<DerivedAttributeDetails>();
        }

        public void LoadPersistedObjects()
        {   
            Dictionary<string, ObjectDetails> vObjectDetailDictionary = new Dictionary<string, ObjectDetails>();
            try
            {            
                fDBConnection.Open();
                DataTable vFMObjectTable = EvaluateSQL("select * from FMObject");
                foreach (DataRow vRow in vFMObjectTable.Rows)
                {                    
                    ObjectDetails vOD = new ObjectDetails();
                    vOD.ObjectGUID = vRow["ObjectGUID"].ToString();
                    vOD.ClassName = vRow["ClassName"].ToString();
                    vObjectDetailDictionary.Add(vOD.ObjectGUID, vOD);
                }

                foreach (string Type in SupportedTypes)
                {                                                            
                    DataTable vAttributeDataTable = EvaluateSQL("select * from FM" + Type);
                    foreach (DataRow vRow in vAttributeDataTable.Rows)
                    {
                        string vObjectGUID = vRow["objectGUID"].ToString();
                        AttributeDetails vA;
                        vA.Attribute = vRow["Attribute"].ToString();
                        vA.Value = vRow["Value"];
                        vA.Type = Type;                       
                        vObjectDetailDictionary[vObjectGUID].Attributes.Add(vA);
                    }
                }
                DataTable vDerivedAttributeDataTable = EvaluateSQL("select * from FMDerived");
                foreach (DataRow vRow in vDerivedAttributeDataTable.Rows)
                {
                    string vObjectGUID = vRow["objectGUID"].ToString();
                    DerivedAttributeDetails vD;
                    vD.Attribute = vRow["Attribute"].ToString();
                    vD.Query = vRow["Query"].ToString();
                    vD.Type = vRow["Type"].ToString();
                    vObjectDetailDictionary[vObjectGUID].DerivedAttributes.Add(vD);
                }
            }            
            finally
            {
                fDBConnection.Close();
            }

            foreach (KeyValuePair<string, ObjectDetails> KVP in vObjectDetailDictionary)
            {
                LoadObjectByObjectDetails(KVP.Value);
            }
        }

        void LoadObjectByObjectDetails(ObjectDetails vObjectDetails)
        {
            FMObject vLoadedObject = new FMObject(objectSpace, vObjectDetails.ObjectGUID);
            vLoadedObject.FMClass = vObjectDetails.ClassName;
            foreach (AttributeDetails AD in vObjectDetails.Attributes)
                vLoadedObject.CreateAttribute(AD.Type, AD.Attribute, AD.Value);
            foreach (DerivedAttributeDetails DAD in vObjectDetails.DerivedAttributes)
                vLoadedObject.CreateDerivedAttribute(DAD.Type, DAD.Attribute, DAD.Query);
        }

        public void PersistObjects()
        {
            foreach (FMObject O in objectSpace.AllObjects)
                PersistObject(O);
        }
    }
}
