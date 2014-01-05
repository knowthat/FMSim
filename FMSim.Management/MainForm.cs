using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FMSim.ORM;

namespace FMSim.Management
{
    public partial class MainForm : Form
    {
        FMObjectSpace objSpace;
        FMExpressionHandler expressionHandler;
        List<ObjectForm> vObjectForms = new List<ObjectForm>();

        public MainForm()
        {
            InitializeComponent();
            objSpace = new FMObjectSpace();
            expressionHandler = new FMExpressionHandler(objSpace); 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FMObject vObj = new FMObject(objSpace);
            vObjectForms.Add(ObjectForm.OpenObject(vObj));
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Object Result = expressionHandler.Evaluate(null, textBox1.Text);
                MessageBox.Show(Result.ToString());

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            objSpace.PersistenceHandler.PersistObjects();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            foreach (ObjectForm OF in vObjectForms)
                OF.Close();
            objSpace.AllObjects.Clear();
            objSpace.PersistenceHandler.LoadPersistedObjects();
            foreach(FMObject FMO in objSpace.AllObjects)
                vObjectForms.Add(ObjectForm.OpenObject(FMO));
        }
    }
}
