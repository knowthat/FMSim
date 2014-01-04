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
    public partial class ObjectForm : Form
    {
        const int cAttributeVerticalOffset = 25;

        public static ObjectForm OpenObject(FMObject aRootObject)
        {
            ObjectForm vFrm = new ObjectForm();
            vFrm.rootObject = aRootObject;
            vFrm.rootObject_PropertyChanged(null, null);
            vFrm.Show();
            return vFrm;
        }
        FMObject rootObject;

        public ObjectForm()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;    
        }

        private void rootObject_PropertyChanged(object sender, EventArgs e)
        {
            panel1.Controls.Clear();
            int I = 0;
            foreach (KeyValuePair<string, FMSim.ORM.FMObject.FMAbstractAttribute> kvp in rootObject.attributes)
            {                
                Label vLabel = new Label();
                vLabel.Text = kvp.Value.name;
                vLabel.Name = "lbl" + kvp.Value.name;
                vLabel.Left = 5;
                vLabel.Top = I * cAttributeVerticalOffset;
                vLabel.Width = 90;
                panel1.Controls.Add(vLabel);

                TextBox vTextBox = new TextBox();
                vTextBox.Name = "txt" + kvp.Value.name;
                vTextBox.Left = 100;
                vTextBox.Width = 90;
                vTextBox.Top = I * cAttributeVerticalOffset;
                Binding b = new Binding("Text", kvp.Value, "FMValue");
                vTextBox.DataBindings.Add(b);
                panel1.Controls.Add(vTextBox);

                Button vDeleteButton = new Button();
                vDeleteButton.Name = "btnDelete" + kvp.Value.name;
                vDeleteButton.Top = I * cAttributeVerticalOffset;
                vDeleteButton.Left = 200;
                vDeleteButton.Text = "-";
                vDeleteButton.Width = 20;
                vDeleteButton.Height = 20;
                vDeleteButton.Tag = kvp.Value.name;
                vDeleteButton.Click += vDeleteButton_Click;
                panel1.Controls.Add(vDeleteButton);
                I++;
            }
            this.Height = I * cAttributeVerticalOffset + 100;
        }

        private void ObjectForm_Load(object sender, EventArgs e)
        {
            rootObject.PropertyChanged += new PropertyChangedEventHandler(rootObject_PropertyChanged);
            Binding b = new Binding("Text", rootObject, "FMClass");
            txtClass.DataBindings.Add(b);
        }

        private void vDeleteButton_Click(object sender, EventArgs e)
        {
            rootObject.DeleteAttribute((sender as Button).Tag as string);
        }        

        private void btnAdd_Click(object sender, EventArgs e)
        {
            switch (comboBox1.Text)
            {
                case ("int"):
                    rootObject.CreateAttribute("Int32", textBox1.Text, 0);
                    break;                    
                case "string":
                    rootObject.CreateAttribute("String", textBox1.Text, "");
                    break;
                case "/string":
                    rootObject.CreateDerivedAttribute("String", textBox1.Text, textBox2.Text);
                    break;
                case "/int":
                    rootObject.CreateDerivedAttribute("Int32", textBox1.Text, textBox2.Text);
                    break;

            }            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text.IndexOf('/') > -1)
                textBox2.Visible = true;
            else
                textBox2.Visible = false;
        }       
    }
}
