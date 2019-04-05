using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Globalization;
using System.Threading;


namespace MapearMovimentoArduino
{
    public partial class Form1 : Form
    {
        private SerialPort ArduinoPort;
        byte[] error;
        public List<short> dataDiag1;
        public List<short> dataDiag2;
        public List<short> dataP1;
        public List<short> dataP2;
        public List<short> dataI1;
        public List<short> dataI2;
        public byte[] lastData;
        public byte[] handShaker;
        public short X, Y;

        public Form1()
        {
            InitializeComponent();

        }

        private void btStart_Click(object sender, EventArgs e)
        {
            handShaker = new byte[1];
            handShaker[0] = 0x20;
                        
            error = new byte[1];
            dataDiag1 = new List<short>();
            dataDiag2 = new List<short>();
            dataP1 = new List<short>();
            dataP2 = new List<short>();
            dataI1 = new List<short>();
            dataI2 = new List<short>();
            lastData = new byte[12];
            X = 0;
            Y = 0;
            dataDiag1.Add(X);
            dataDiag2.Add(Y);

            ArduinoPort = new SerialPort();
            ArduinoPort.BaudRate = 115200;
            ArduinoPort.PortName = tbPortName.Text;
            ArduinoPort.Parity = Parity.None;
            ArduinoPort.DataBits = 8;
            ArduinoPort.StopBits = StopBits.One;
            try
            {
                ArduinoPort.Open();
                pbStatus.BackColor = Color.Blue;
                lbStatus.Text = "Porta Conectada";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            //ArduinoPort.Write(handShaker, 0, 1);
            while (handShaker[0] != 0x7E)
            {
                ArduinoPort.Read(handShaker, 0, 1);
            }
            pbStatus.BackColor = Color.LimeGreen;
            lbStatus.Text = "Comunicação estabelecida";
            ArduinoPort.DataReceived += ArduinoPort_DataRecieved;
        }

        private void ArduinoPort_DataRecieved(object sender, SerialDataReceivedEventArgs e)
        {
            bool leuCerto = true;
            handShaker[0] = 0x00;
            while (handShaker[0] != 0x7E)
            {
                ArduinoPort.Read(handShaker, 0, 1);
            }
            //ArduinoPort.ReadTo("Z");
            ArduinoPort.Read(lastData, 0, 10);
            short P1 = BitConverter.ToInt16(lastData, 0);
            short P2 = BitConverter.ToInt16(lastData, 2);
            P1 = Convert.ToInt16(P1 / 80);
            P2 = Convert.ToInt16(P2 / 80);
            short I1 = BitConverter.ToInt16(lastData, 4);
            short I2 = BitConverter.ToInt16(lastData, 6);
            Int16 erro = 0;
            for (int i = 1; i < 8; i++)
            {
                erro += lastData[i];
            }
            error[0] = Convert.ToByte(erro & 0xFF);
            if (error[0] != lastData[8])
            {
                leuCerto = false;
            }
            if (leuCerto == true && lastData[9] == 0x81)
            {
                //ArduinoPort.Read(lastData, 0, 10);

                short dif1 = Convert.ToInt16(P1 - dataDiag1[dataDiag1.Count-1]);
                short dif2 = Convert.ToInt16(P2 - dataDiag2[dataDiag2.Count-1]);
                X += Convert.ToInt16(-dif1 + dif2);
                Y += Convert.ToInt16(dif1 + dif2);
                this.InvokeEx(f => f.label5.Text = P1.ToString());
                this.InvokeEx(f => f.label6.Text = P2.ToString());
                this.InvokeEx(f => f.label7.Text = X.ToString());
                this.InvokeEx(f => f.label8.Text = Y.ToString());

                dataDiag1.Add(P1);
                dataDiag2.Add(P2);
                if (X > 300)
                    X = 300;
                if (X < 0)
                    X = 0;
                if (Y > 300)
                    Y = 300;
                if (Y < 0)
                    Y = 0;
                dataP1.Add(X);
                dataP2.Add(Y);
                dataI1.Add(I1);
                dataI2.Add(I2);
                this.InvokeEx(f => f.pbManopla.Left = X);

                this.InvokeEx(f => f.pbManopla.Top = 300 - Y);
                Application.DoEvents();
            }
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            ArduinoPort.Close();
        }

        private void tbX_TextChanged(object sender, EventArgs e)
        {
            if (tbX.Text != null && tbX.Text != "")
                this.InvokeEx(f => f.pbObjetivo.Left = Convert.ToInt32(tbX.Text)+5);
        }

        private void tbY_TextChanged(object sender, EventArgs e)
        {
            if(tbY.Text!=null && tbY.Text != "")
                this.InvokeEx(f => f.pbObjetivo.Top = 300 - Convert.ToInt32(tbY.Text)+5);
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            string fn;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            StreamWriter writer;

            if (MessageBox.Show("Deseja salvar os dados?", "Aviso", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {

                string exePath = Application.ExecutablePath; //diretório do executável
                string fnConfig = Path.GetDirectoryName(exePath) + "\\";
                fn = fnConfig + tbVoluntario.Text + "_" + tbSessao.Text + ".txt";

                using (writer = new StreamWriter(@fn))
                {
                    // Write the contents.

                    writer.Write("Voluntário: " + tbVoluntario.Text + "\tSessão: " + tbSessao.Text);
                    //String identificador da sessão

                    writer.Write("\r\n" + "P1\tP2\tI1\tI2");
                    //conteúdo de cada coluna

                    int count = dataP1.Count;
                    for (int i=0; i < count; i++)
                    {
                        writer.Write("\r\n");
                        writer.Write(dataP1[i].ToString() + "\t" + dataP2[i].ToString() + "\t" + dataI1[i].ToString() + "\t" + dataI2[i].ToString() + "\t");
                    }
                    writer.Write("\r\n" + "FIM DA COLETA");
                    //conteúdo

                    writer.Close();
                }
            }
        }
    }
}
