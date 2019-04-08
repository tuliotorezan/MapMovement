using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IMH___MANIPULADOR
{
    public partial class Form_Sobre : Form
    {
        bool flag_btn = false; // Avisa se o botão voltar foi ativado

        public Form_Sobre()
        {
            InitializeComponent();
            flag_btn = false;
        }

        private void btn_voltar_sobre_Click(object sender, EventArgs e)
        {
            Form1 frap = new Form1();
            frap.Visible = true;
            flag_btn = true; // btn ativado
            this.Close();
        }

        private void Form_Sobre_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (flag_btn == false)
            {
                Form1 frap = new Form1();
                frap.Visible = true;
            }
        }
    }
}
