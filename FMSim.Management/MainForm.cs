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

        public MainForm()
        {
            InitializeComponent();
            objSpace = new FMObjectSpace();
            expressionHandler = new FMExpressionHandler(objSpace); 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FMObject vObj = new FMObject(objSpace);
            vObj.FMClass = "User";
            vObj.CreateAttribute("Name", "Johan");
            ObjectForm.OpenObject(vObj);
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
    }
}
