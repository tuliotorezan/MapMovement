using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// Bilbiotecas adicionadas
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Globalization;
using System.Management; // Tive que adicionar manualmente
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace IMH___MANIPULADOR
{
    public partial class Form1 : Form
    {
        // Variaveis Recebe dados
        //string RxString;
        //int LSB, MSB;
        //int num;

        // Classes
        TratamentoDados td; // Objeto para manipular a classe

        // Thread
        private Thread AquisicaoThread; // Manipulador thread para pegar dados
        private Thread GravaDados;
        private Thread PlotMapeamento;
        //static Thread PlotSinaisThread; // manipulador thread para plotar graficos
        //static Thread CFFTThread; // manipulador thread para calcular FFT

        SaveFileDialog saveFileDialog1 = new SaveFileDialog(); // Cria pasta do arquivo
        private System.IO.TextWriter dadotxt;

        // VARIAVEIS GLOBAIS
        bool flag_save; // Fala se o arquivo ja foi salvo
        bool flag_novo;
        short FatorMov;
        short AlvoX, AlvoY;
        String Nome_arquivo, Local_arquivo; // Nome e local dos arquivos Salvos
        byte comando;
        Int16 fa; // Freq. de amostragem

        public Form1()
        {
            InitializeComponent();
        }
        // ---------------------------------------Iniciando programa----------------------------------------------
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (td.run)
            {
                td.run = false; // Finaliza thread
                btn_inicia.Text = "Iniciar";

                //td.StateMutex(false); // Finaliza Mutex
                
                PlotMapeamento.Abort();
                GravaDados.Abort();
                AquisicaoThread.Abort();
                dadotxt.Close();

                const string message = "Sair sem salvar?";
                const string caption = "Form Closing";
                var result = MessageBox.Show(message, caption,
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Question);

                // If the no button was pressed ...
                if (result == DialogResult.No)
                {
                    // cancel the closure of the form.
                    e.Cancel = true;
                }
                else
                {
                    
                }
                //while (td.Finish != true) ;
            }
            if (serialPort1.IsOpen == true)  // se porta aberta 
            {
                // Transformando os dados em byte para envio
                Int16 assistencia = Convert.ToInt16(numUD_assistencia.Value);
                byte MSB = Convert.ToByte((assistencia >> 8) & 0xFF);
                byte LSB = Convert.ToByte(assistencia & 0xFF); // Divide valores em 2 bits 
                byte CheckSun = Convert.ToByte(MSB + LSB); // Chave verifiacadora
                comando = 0x10; // Abortar
                var dataByte = new byte[] { 0x7E, comando, MSB, LSB, CheckSun, 0x81 };
                serialPort1.Write(dataByte, 0, 4); // Envio do pacote

                serialPort1.Close();

            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Inicializando(); // Inicaliza todos os comandos

        }
        public void Inicializando() // inicializa variaveis
        {
            // INCIANDO VARIAVEIS
            flag_save = false;
            flag_novo = false;
            numUD_AvoX.Value = 150;
            numUD_AvoY.Value = 150;
            FatorMov = 1;
            AlvoX = Convert.ToInt16(numUD_AvoX.Value);
            AlvoY = Convert.ToInt16(pbMovArea.Height - numUD_AvoY.Value - 20);
            comando = 0x20;

            // Tamanho dos paineis
            pbMovArea.Size = new Size(320, 320);
            // CADASTRO NOVO PACIENTE;
            // O que começa visivel e invisivel
            gpbox_novoPaciente.Visible = false; // Cadastro do paciente começa invisivel
            gpbox_novoPaciente.Location = new Point(15, 30); // Indica a posição que deve estar o GroupBox
            gpbox_novoPaciente.Size = new Size(860, 500);
            gpbox_Config.Visible = false;
            gpbox_Config.Location = new Point(15, 30);
            gpbox_Config.Size = new Size(860, 500);
            gpbox_CampoForca.Visible = false;
            gpbox_CampoForca.Location = new Point(15, 30);
            gpbox_CampoForca.Size = new Size(860, 500);
            gpbox_Mapeamento.Visible = true;
            gpbox_Mapeamento.Location = new Point(15, 30);
            gpbox_Mapeamento.Size = new Size(860, 500);
            gp_sobre.Location = new Point(15, 30);
            gp_sobre.Size = new Size(860, 500);
            gp_sobre.Visible = false;

            pbAlvo.Size = new Size(pbMovArea.Width / 15, pbMovArea.Width / 15);
            pbManopla.Size = new Size(pbMovArea.Width / 30, pbMovArea.Width / 30);

            // Definições iniciais da Porta Serial
            txb_BaundRate_Conf.Text = "115200";
            txb_dataBits_conf.Text = "8";
            txb_ReadBuf_Conf.Text = "4096";
            cb_StopBits_Conf.SelectedIndex = 0;
            txb_WriteBuf_Conf.Text = "2048";

            String PortaCOM = DetectArduino(); // Detecta a porta arduino

            if (serialPort1.PortName != "NOPORT") // Conecta ao arduino
            {
                fa = Convert.ToInt16(txb_Freq_Amostragem.Text);
                serialPort1.Handshake = Handshake.None; //------------------------------------------- Teste
                serialPort1.PortName = PortaCOM;
                lbl_conectado.Text = "Connect";
                //serialPort1.Open();
                btn_conect_Conf.Text = "Desconectar";
                cb_portas.Enabled = false;
                timerCOM.Enabled = false; // Desabilita a procura das portas COM no computador
                                          // Classes
                td = new TratamentoDados(serialPort1, dadotxt, AquisicaoThread, pbMovArea, pbManopla, pbAlvo, pbAcerto, lbl1, lbl2, lbl3, lbl4);
            }
            else // Pede conexão
            {
                gpbox_CampoForca.Visible = false;
                gpbox_Mapeamento.Visible = false;
                gpbox_Config.Visible = true;
                timerCOM.Enabled = true; // Hbilita a procura das portas COM no computador
            }
        }

        //----------------------------------------- SALVANDO ARQUIVO ----------------------------------------------
        public void CriaPastaArquivo()
        {
            saveFileDialog1.FileName = "Mapeamento_" + txb_Nome_CP.Text;
            saveFileDialog1.DefaultExt = ".txt";
            saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.InitialDirectory = @"C:\";
            saveFileDialog1.Title = "Save text Files";
            //saveFileDialog1.CheckFileExists = true;
            //saveFileDialog1.CheckPathExists = true;
            //saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                    SalvaDados();
            }
  
        }
        public void SalvaDados()
        {
            flag_save = true; // Avisa que ja foi salvo
            flag_novo = true;
            Nome_arquivo = saveFileDialog1.FileName;
            Local_arquivo = Path.GetDirectoryName(Nome_arquivo); // Devolve onde está o arquivo salvo
                                                                 //Cria um stream usando o nome do arquivo
            StreamWriter dadotxt = new StreamWriter(Nome_arquivo); // Abre o local da pasta
            dadotxt.WriteLine("Nome: " + txb_Nome_CP.Text); // Escrevendo nova linha de dado
            dadotxt.WriteLine("Data de Nascimento: " + mtxb_DataNasc_CP.Text);
            dadotxt.WriteLine("Acompanhante: " + txb_Acompanhante_CP.Text);
            dadotxt.WriteLine("Cidade: " + txb_Cidade_CP.Text);
            dadotxt.WriteLine("Estado: " + txb_Estado_CP.Text);
            dadotxt.WriteLine("Telefone: " + mtxb_telefone_CP.Text);
            dadotxt.WriteLine("Celular: " + mtxb_celular_CP.Text);
            dadotxt.WriteLine("Dominancia: " + txb_Dominancia_CP.Text);
            dadotxt.WriteLine("Patologia: " + txb_Patologia_CP.Text);
            dadotxt.WriteLine("Observação: " + txb_Observacao_CP.Text);
            dadotxt.WriteLine("Freq. de Amostragem (Hz): " + txb_Freq_Amostragem.Text);
            dadotxt.WriteLine("");
            dadotxt.WriteLine("EixoX" + "   " + "EixoY" + "   " + "CorrenteM1" + "   " + "CorrenteM2");
            dadotxt.Close(); // Fecha pasta
                             //dadotxt.Dispose(); //Limpando a referencia dele da memória

        }
        //-----------------------------------Menu Superior (Novo)-------------------------------------------------
        private void novoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            flag_novo = true; // Avisa que é um arquivo novo
            gpbox_novoPaciente.Visible = true;
            gpbox_Config.Visible = false;
            gpbox_Mapeamento.Visible = false;
            gpbox_CampoForca.Visible = false;

        }
        // Menun Superior (Sair)
        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!flag_save)
            {
                DialogResult result = MessageBox.Show("Sair sem Salvar?", "IHM", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    CriaPastaArquivo();
                    //serialPort1.Close();
                    this.Close();
                    Application.Exit();
                }
                if (result == DialogResult.Yes)
                {
                    //serialPort1.Close();
                    this.Close();
                    Application.Exit();
                }

            }
            else
            {
                this.Close();
                Application.Exit();
            }
        }
        // Menu Superior (Abrir)
        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Multiselect = false; // Não deixa selecionar mais de um arquivo
            openFileDialog1.Title = "Selecione Arquivo";
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Nome_arquivo = openFileDialog1.FileName;
                Local_arquivo = Path.GetDirectoryName(Nome_arquivo); // Devolve onde está o arquivo salvo
                flag_novo = true;
                //saveFileDialog1.Dispose();
                //StreamWriter dadotxt = new StreamWriter(Nome_arquivo); // Abre o local da pasta

                //openFileDialog1.Dispose();

                //dadotxt.Close();
            }
        }

        // Menu superior (SALVAR)
        private void salvarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btn_inicia.PerformClick();
        }
        private void sobreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gpbox_novoPaciente.Visible = false;
            gpbox_Config.Visible = false;
            gpbox_Mapeamento.Visible = false;
            gpbox_CampoForca.Visible = false;
            gp_sobre.Visible = true;
        }

        // --------------------------------INICIO MAPEAMENTO--------------------------------
        private void numUD_PlataformaX_ValueChanged(object sender, EventArgs e)
        {
            pbMovArea.Size = new Size(Convert.ToInt16(numUD_PlataformaX.Value), Convert.ToInt16(numUD_PlataformaY.Value));
        }
        private void numUD_PlataformaY_ValueChanged(object sender, EventArgs e)
        {
            pbMovArea.Size = new Size(Convert.ToInt16(numUD_PlataformaX.Value), Convert.ToInt16(numUD_PlataformaY.Value));
        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            AlvoX = Convert.ToInt16(numUD_AvoX.Value);
            AlvoY = Convert.ToInt16(pbMovArea.Height - numUD_AvoY.Value - 20);
            pbAlvo.Location = new Point(AlvoX, AlvoY);
        }
        private void numUD_AvoY_ValueChanged(object sender, EventArgs e)
        {
            AlvoX = Convert.ToInt16(numUD_AvoX.Value);
            AlvoY = Convert.ToInt16(pbMovArea.Height - numUD_AvoY.Value - 20);
            pbAlvo.Location = new Point(AlvoX, AlvoY);
        }
        private void btn_Expandir_Click(object sender, EventArgs e)
        {
            if(btn_Expandir.Text == "Expandir")
            {
                btn_Expandir.Text = "Reduzir";
                gp_parametros.Visible = false;
                this.WindowState = FormWindowState.Maximized;
                btn_Expandir.Location = new Point(gpbox_Mapeamento.Width - btn_Expandir.Width, gpbox_Mapeamento.Height - btn_Expandir.Height);
                btn_inicia.Location = new Point(gpbox_Mapeamento.Width - btn_inicia.Width, gpbox_Mapeamento.Height - btn_Expandir.Height - btn_inicia.Height - 5);
                pbMovArea.Size = new Size(Convert.ToInt16(gpbox_Mapeamento.Height * 0.75), Convert.ToInt16(gpbox_Mapeamento.Height * 0.75));             
                pbMovArea.Location = new Point((gpbox_Mapeamento.Width/2)- (pbMovArea.Width / 2), (gpbox_Mapeamento.Height/2)- (pbMovArea.Height / 2));
                FatorMov = 2;
                pbAlvo.Size = new Size(pbMovArea.Width / 15, pbMovArea.Width / 15);
                pbManopla.Size = new Size(pbMovArea.Width / 30, pbMovArea.Width / 30);
                lbl1.Text = Convert.ToString(gpbox_Mapeamento.Width);
                lbl2.Text = Convert.ToString(gpbox_Mapeamento.Height);
            }
            else
            {
                if (btn_inicia.Text == "Finalizar")
                {
                    MessageBox.Show("Finalize a coleta", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    btn_Expandir.Text = "Expandir";
                    this.WindowState = FormWindowState.Normal;
                    gp_parametros.Visible = true;
                    
                    pbAlvo.Size = new Size(Convert.ToInt16(numUD_PlataformaX.Value) / 15, Convert.ToInt16(numUD_PlataformaY.Value) / 15);
                    pbAlvo.Location = new Point(pbAlvo.Location.X / FatorMov, pbAlvo.Location.Y / FatorMov);
                    pbManopla.Size = new Size(Convert.ToInt16(numUD_PlataformaX.Value) / 30, Convert.ToInt16(numUD_PlataformaY.Value) / 30);
                    pbManopla.Location = new Point(pbManopla.Location.X / FatorMov, pbManopla.Location.Y / FatorMov);
                    pbMovArea.Size = new Size(Convert.ToInt16(numUD_PlataformaX.Value), Convert.ToInt16(numUD_PlataformaY.Value));
                    pbMovArea.Location = new Point(6, 22);
                    btn_Expandir.Location = new Point(94, 365);
                    btn_inicia.Location = new Point(573, 424);
                    FatorMov = 1;
                    lbl1.Text = Convert.ToString(gpbox_Mapeamento.Width);
                    lbl2.Text = Convert.ToString(gpbox_Mapeamento.Height);
                }
            }
            td.FatorMov = FatorMov;
            
        }
        private void btn_inicia_Click(object sender, EventArgs e)
        {
            /*
            if (flag_novo) // Verifica se ja existe cadastro
            {
                CriaPastaArquivo();
                return;
            }
            */
            if (btn_inicia.Text == "Iniciar")
            {

                if (!flag_novo)
                {
                    MessageBox.Show("Abra ou Crie um arquivo.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    btn_inicia.Text = "Finalizar";
                    salvarToolStripMenuItem.Enabled = true;
                    this.Text = "IHM - MANIPULADOR - " + Local_arquivo + "\\" + Nome_arquivo; // Coloca caminho no título

                    // Finaliza Thread se precisar
                    if (comando == 0x10)
                    {
                        PlotMapeamento.Abort();
                        GravaDados.Abort();
                        AquisicaoThread.Abort();
                        //dadotxt.Close();
                    }
                    
                    dadotxt = System.IO.File.AppendText(Nome_arquivo); // Coloca novas informações como novas aquisições

                    // Ativa Threads
                    // Thread
                    AquisicaoThread = new Thread(td.Aquisicao);
                    AquisicaoThread.Priority = ThreadPriority.Highest;

                    PlotMapeamento = new Thread(td.PlotMovimentacao);
                    PlotMapeamento.Priority = ThreadPriority.BelowNormal;

                    GravaDados = new Thread(td.GravaDados);
                    GravaDados.Priority = ThreadPriority.AboveNormal;

                    //AquisicaoThread.IsBackground = true;
                    //AquisicaoThread.Join();
                    Thread.Sleep((1/Convert.ToInt16(txb_Freq_Amostragem.Text)) * 1000); // Calcula frequencia de amostragem

                    td.run = true;
                    td.StateMutex(true);
                    AquisicaoThread.Start(); // Inicia a aquisição de dados
                    td.LocalArquivos(Nome_arquivo, Local_arquivo, dadotxt);
                    GravaDados.Start();
                    PlotMapeamento.Start();

                    switch(cb_reabilitacao.SelectedIndex)
                    {
                        case 0: // Admitancia
                            comando = 0x20;
                            break;
                        case 1: // Campo de Força
                            comando = 0x40;
                            break;
                        case 2: // Impedância
                            comando = 0x30;
                            break;
                        case 3: // Referência
                            comando = 0x60;
                            break;
                    }
                }
            }
            else
            {
                btn_inicia.Text = "Iniciar";
                salvarToolStripMenuItem.Enabled = false;
                comando = 0x10; // Abortar
                
                //td.StateMutex(false); // Finaliza Mutex
                td.run = false; // Finaliza thread         
            }

                // Transformando os dados em byte para envio
                Int16 assistencia = Convert.ToInt16(numUD_assistencia.Value);
                byte MSB = Convert.ToByte((assistencia >> 8) & 0xFF);
                byte LSB = Convert.ToByte(assistencia & 0xFF); // Divide valores em 2 bits 
                byte CheckSum = Convert.ToByte(MSB + LSB); // Chave verifiacadora
                CheckSum = Convert.ToByte(CheckSum & 0xFF);
                var dataByte = new byte[] { 0x7E, comando, MSB, LSB, CheckSum, 0x81 };
                serialPort1.Write(dataByte, 0, 4); // Envio do pacote
            }
        // --------------------------------FIM MAPEAMENTO--------------------------------

        // --------------------------------INCIO DA PARTE CADASTRO-------------------------------------------------------*
        private void novoPacienteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // NÃO ESQUECER DE DEIXAR OS OUTROS INVISIVEIS
            gpbox_novoPaciente.Visible = true;
            gpbox_Config.Visible = false;
            gpbox_Mapeamento.Visible = false;
            gpbox_CampoForca.Visible = false;
        }
        private void btn_voltar_nv_paciente_Click(object sender, EventArgs e)
        {
            // NÃO ESQUECER DE DEIXAR OS OUTROS INVISIVEIS
            gpbox_novoPaciente.Visible = false;
            gpbox_Config.Visible = false;
            gpbox_Mapeamento.Visible = true;
            gpbox_CampoForca.Visible = false;
        }
        private void btn_ok_nv_paciente_Click(object sender, EventArgs e)
        {
            // Salvando
            if (flag_save == false)
            {
                CriaPastaArquivo(); // Cria uma pasta para salvar arquivo
            }
            else
            {
                SalvaDados(); // SALVA DOS DADOS
            }

            // NÃO ESQUECER DE DEIXAR OS OUTROS INVISIVEIS
            gpbox_novoPaciente.Visible = false;
            gpbox_Config.Visible = false;
            gpbox_Mapeamento.Visible = true;
            gpbox_CampoForca.Visible = false;
        }
        // ---------------------------------FIM DA PARTE CADASTRO-------------------------------------------------------

        // ---------------------------------INICIO CONFIGURAÇÕES-------------------------------------------------------
        private void configurarPortasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gpbox_novoPaciente.Visible = false;
            gpbox_Config.Visible = true;
            gpbox_Mapeamento.Visible = false;
            gpbox_CampoForca.Visible = false;


            txb_BaundRate_Conf.Text = Convert.ToString(serialPort1.BaudRate);
            txb_dataBits_conf.Text = Convert.ToString(serialPort1.DataBits);
            txb_ReadBuf_Conf.Text = Convert.ToString(serialPort1.ReadBufferSize);
            cb_StopBits_Conf.SelectedIndex = 0;
            txb_WriteBuf_Conf.Text = Convert.ToString(serialPort1.WriteBufferSize);
            //timer1.Start(); // Inicia o Timer para verificar portas COM's

        }
        private void btn_voltar_Conf_Click(object sender, EventArgs e)
        {
            gpbox_novoPaciente.Visible = false;
            gpbox_Config.Visible = false;
            gpbox_Mapeamento.Visible = true;
            gpbox_CampoForca.Visible = false;
        }
        private string DetectArduino() // Detecta a porta COM Arduino automaticamente
        {
            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);
            try
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string desc = item["Description"].ToString();
                    string deviceId = item["DeviceID"].ToString();

                    if (desc.Contains("Arduino"))
                    {
                        //DebugOut("Arduino details: " + desc + " on " + deviceId);
                        return deviceId;
                    }
                }
            }

            catch (ManagementException e)
            {

                MessageBox.Show("Ocorreu um erro ao consultar portas COM: " + e.Message);
            }

            return "NOPORT";

        }
        // CONFIGURAÇÃO DO TIMER PARA ENCONTRAR SERIAL
        private void timer1_Tick(object sender, EventArgs e)
        {
            int i;
            bool quantDiferente; //flag para sinalizar que a quantidade de portas mudou

            i = 0;
            quantDiferente = false;

            //se a quantidade de portas mudou
            if (cb_portas.Items.Count == SerialPort.GetPortNames().Length)
            {
                foreach (string s in SerialPort.GetPortNames())
                {
                    if (cb_portas.Items[i++].Equals(s) == false)
                    {
                        quantDiferente = true;
                    }
                }
            }
            else
            {
                quantDiferente = true;
            }

            //Se não foi detectado diferença
            if (quantDiferente == false)
            {
                return;                     //retorna
            }

            //limpa comboBox
            cb_portas.Items.Clear();

            //adiciona todas as COM diponíveis na lista
            foreach (string s in SerialPort.GetPortNames())
            {
                cb_portas.Items.Add(s);
            }
            //seleciona a primeira posição da lista
            cb_portas.SelectedIndex = 0;
            /*
            if (serialPort1.BytesToRead > 0) // Verifica se chegou algum dado
            {
                txb_RecebeDados_Conf.Text += serialPort1.ReadExisting(); // Escreve o dado na textbox
            }
            */
        }
        private void btn_conect_Conf_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                serialPort1.BaudRate = Convert.ToInt32(txb_BaundRate_Conf.Text);
                serialPort1.DataBits = Convert.ToInt32(txb_dataBits_conf.Text);
                serialPort1.ReadBufferSize = Convert.ToInt32(txb_ReadBuf_Conf.Text);
                serialPort1.WriteBufferSize = Convert.ToInt32(txb_WriteBuf_Conf.Text);
                try
                {
                    serialPort1.PortName = cb_portas.Items[cb_portas.SelectedIndex].ToString();
                    serialPort1.Open();
                    // Classes
                    td = new TratamentoDados(serialPort1, dadotxt, AquisicaoThread, pbMovArea, pbManopla, pbAlvo, pbAcerto, lbl1, lbl2, lbl3, lbl4);
                    fa = Convert.ToInt16(txb_Freq_Amostragem.Text);
                }
                catch
                {
                    return;

                }
                if (serialPort1.IsOpen)
                {
                    lbl_conectado.Text = "Connect";
                    btn_conect_Conf.Text = "Desconectar";
                    cb_portas.Enabled = false;
                    timerCOM.Enabled = false; // Desabilita a procura das portas COM no computador
                }
            }
            else
            {
                try
                {
                    serialPort1.Close();
                    cb_portas.Enabled = true;
                    timerCOM.Enabled = true; // Habilita a procura das portas COM no computador
                    lbl_conectado.Text = "No Connect";
                    btn_conect_Conf.Text = "Conectar";
                }
                catch
                {
                    return;
                }
            }
        }
        private void txb_Freq_Amostragem_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != (char)8) // Restringindo apenas para numero a textbox
            {
                e.Handled = true;
            }
        }

        private void criarCampoToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void paraleloToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gpbox_novoPaciente.Visible = false;
            gpbox_Config.Visible = false;
            gpbox_Mapeamento.Visible = false;
            gpbox_CampoForca.Visible = true;
        }

        private void btVisualize_Click(object sender, EventArgs e)
        {
            int posx = 0;
            int posy = 0;
            int sizex = 0;
            int sizey;
            //Setando a area de fundo sob a qual o campo irá atuar
            if(X1.Value > X2.Value)
            {
                posx = Convert.ToInt16(X2.Value);
                sizex = Convert.ToInt16(X1.Value - X2.Value);
            }else{
                posx = Convert.ToInt16(X1.Value);
                sizex = Convert.ToInt16(X2.Value - X1.Value);
            }
            if (Y1.Value > Y2.Value)
            {
                posy = Convert.ToInt16(Y2.Value);
                sizey = Convert.ToInt16(Y1.Value - Y2.Value);
            }else{
                posy = Convert.ToInt16(Y1.Value);
                sizey = Convert.ToInt16(Y2.Value - Y1.Value);
            }
            pbArea.Size = new Size(sizex, sizey);
            pbArea.Location = new Point(posx, posy);

            //Plotando vetor AB
            pbA.Location = new Point(Convert.ToInt16(XA.Value), Convert.ToInt16(YA.Value));
            pbB.Location = new Point(Convert.ToInt16(XB.Value), Convert.ToInt16(YB.Value));
            int[,] Matriz = new int [sizex,sizey];
            for(int i =0; i<sizex; i++)
            {
                for(int j=0; j<sizey; j++)
                {

                }
            }

        }

        private void btn_enviar_Conf_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == true)
            {
                serialPort1.Write(txb_EnviaDado_Conf.Text);
            }
            else
            {
                MessageBox.Show("Realize a Conexão!");
            }
        }
        // ---------------------------------FIM CONFIGURAÇÕES-------------------------------------------------------

        // ---------------------------------SOBRE-----------------------------------------------
        private void btn_sobre_voltar_Click(object sender, EventArgs e)
        {
            gpbox_novoPaciente.Visible = false;
            gpbox_Config.Visible = false;
            gpbox_Mapeamento.Visible = true;
            gp_sobre.Visible = false;
        }
    }
}



public class TratamentoDados : Form
{
    // Variaveis gerais da classe
    public Mutex mut;
    private SerialPort serialPort1;
    private Thread ThrAquis;
    private buffercircular BFFCplot, BFFCGrav; // Buffer Circular para plotagem e gravação
    //private Form1 classMain; // Buffer Circular para plotagem
    //private int LSB, MSB;
    //private Int16 num;
    public bool run;//, Finish1, Finish2, Finish3;
    public short FatorMov;
    private const int SizeBuffer = 5000; // tamanho do vetor do buffer circular
    private byte[] Dados;
    private Int16 CheckSum;

    // Gravar dados
    private System.IO.TextWriter dadotxt;
    OpenFileDialog openFileDialog1;
    private String Nome_arquivo, Local_arquivo;
    private Int16 Posicao1 = 0;
    private Int16 Posicao2 = 0;
    private Int16 Corrente1 = 0;
    private Int16 Corrente2 = 0;

    private Panel pbMovArea = new Panel();
    private PictureBox pbManopla = new PictureBox();
    private PictureBox pbAlvo = new PictureBox();
    private Panel pbAcerto = new Panel();

    // teste lbl
    private Label lbl1, lbl2, lbl3, lbl4;

    public TratamentoDados(SerialPort _serialPort1, System.IO.TextWriter dadotxt, Thread _ThrAquis, Panel _pbMovArea, PictureBox _pbObjeto, PictureBox _pbPainel, Panel _pbAcertoAlvo, Label _lbl1, Label _lbl2, Label _lbl3, Label _lbl4)
    {
        openFileDialog1 = new OpenFileDialog();
        
        BFFCplot = new buffercircular(SizeBuffer);
        //BFFCGrav = new buffercircular(SizeBuffer);
        serialPort1 = _serialPort1;
        ThrAquis = _ThrAquis;
        Dados = new byte[10];
        pbManopla = _pbObjeto;
        pbAlvo = _pbPainel;
        pbAcerto = _pbAcertoAlvo;
        pbMovArea = _pbMovArea;
        FatorMov = 1;
        pbManopla.Location = new Point(0, pbMovArea.Height); // Começando na posição 0,0
        run = false;
        //Finish1 = false;
        //Finish2 = false;
        //Finish2 = false;

        //teste lbl
        lbl1 = _lbl1;
        lbl2 = _lbl2;
        lbl3 = _lbl3;
        lbl4 = _lbl4;
    }

    public void StateMutex(bool _State) // Cancela Mutex
    {
        if (!_State)
        {
            mut.WaitOne();
            mut.ReleaseMutex();
            mut.Dispose();
            mut.Close();
        }
        else
            mut = new Mutex();
    }

    public void Aquisicao()
    {
        while (run)
        {
            mut.WaitOne(); // Comando para aguardar conincidencia de Thread
            while (serialPort1.BytesToRead > 0) // Observa quando o buffer do USB esta com 100 bytes
            {
                for (Int16 i = 0; i < serialPort1.BytesToRead; i++) // Pega em pares os bytes e coloca no buffer circuilar
                {
                    byte initD = Convert.ToByte(serialPort1.ReadByte());
                    if (initD == 0x7E) // Confere se o primeiro dado é 0x7e
                    {
                        CheckSum = 0;
                        for (Int16 j = 0; j < 10; j++) // Pega os proximos 9 dados
                        {
                            i++; // Adiciona os dados 
                            while (serialPort1.BytesToRead < 1) ; // Aguarda dado
                            Dados[j] = Convert.ToByte(serialPort1.ReadByte());
                        }
                        if (Dados[9] == 0x81) // Observa se o ultimo dado é 0x81
                        {
                            for (Int16 l = 0; l < 8; l++) // Somatória do Checksun
                            {
                                CheckSum += Dados[l];
                            }
                            if (Dados[8] == Convert.ToByte(CheckSum & 0xFF)) // Pega o valor lsb ) // Compara com o mandado
                            {
                                /*
                                Int16 x = Convert.ToInt16(Dados[0] * 256 + Dados[1]);
                                Int16 y = Convert.ToInt16(Dados[2] * 256 + Dados[3]);
                                Int16 w = Convert.ToInt16(Dados[4] * 256 + Dados[5]);
                                Int16 z = Convert.ToInt16(Dados[6] * 256 + Dados[7]);
                                */
                                BFFCplot.write(Convert.ToInt16(Dados[0] * 256 + Dados[1]));
                                BFFCplot.write(Convert.ToInt16(Dados[2] * 256 + Dados[3]));
                                BFFCplot.write(Convert.ToInt16(Dados[4] * 256 + Dados[5]));
                                BFFCplot.write(Convert.ToInt16(Dados[6] * 256 + Dados[7]));

                                BFFCGrav.write(Convert.ToInt16(Dados[0] * 256 + Dados[1]));
                                BFFCGrav.write(Convert.ToInt16(Dados[2] * 256 + Dados[3]));
                                BFFCGrav.write(Convert.ToInt16(Dados[4] * 256 + Dados[5]));
                                BFFCGrav.write(Convert.ToInt16(Dados[6] * 256 + Dados[7]));
                            }
                        }
                    }
                }
            }
            mut.ReleaseMutex();// Libera outra Thread 
        }
        //Finish1 = true;
    }

    public void LocalArquivos(String _Nome_arquivo, String _Local_arquivo, System.IO.TextWriter _dadotxt)
    {
        BFFCGrav = new buffercircular(SizeBuffer);
        Nome_arquivo = _Nome_arquivo;
       Local_arquivo = _Local_arquivo;

        openFileDialog1.InitialDirectory = Local_arquivo; // Devolve onde está o arquivo salvo
        openFileDialog1.FileName = Nome_arquivo;

        dadotxt = _dadotxt; // Abre o local da pasta
        //dadotxt = StreamWriter(Nome_arquivo); // Abre o local da pasta
        //dadotxt.Invoke(new Action(() => dadotxt = System.IO.File.AppendText(Nome_arquivo)));
        //this.Invoke(new Action(() => dadotxt = System.IO.File.AppendText(Nome_arquivo)));
        //dadotxt = System.IO.File.AppendText(Nome_arquivo);
    }
    public void FinalizaColeta()
    {
        dadotxt.WriteLine(""); // Escrevendo nova linha de dado
        dadotxt.WriteLine("COLETA FINALIZADA " + DateTime.Now.Day + "/" + DateTime.Now.Month + "/" + DateTime.Now.Year + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second); //.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString()); // Escrevendo nova linha de dado
        dadotxt.WriteLine("");
        MessageBox.Show("Dados Salvos.", "Salvo!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        dadotxt.Close();
    }
    public void GravaDados()
    {
        while (run)
        { 
            while (BFFCGrav.cont > 3)
            {
                mut.WaitOne(); // Comando para aguardar conincidencia de Thread
                String P1 = Convert.ToString(BFFCGrav.read());
                String P2 = Convert.ToString(BFFCGrav.read());
                String C1 = Convert.ToString(BFFCGrav.read());
                String C2 = Convert.ToString(BFFCGrav.read());

                //dadotxt = System.IO.File.AppendText(Nome_arquivo);
                dadotxt.WriteLine(P1 + "  " + P2 + "  " + C1 + "  " + C2); // Escrevendo nova linha de dado
                mut.ReleaseMutex(); // Libera outra Thread
            }   
        }
        FinalizaColeta();
        //Finish2 = true;
    }

    public void PlotMovimentacao()
    {
        while (run)
        {
            mut.WaitOne(); // Comando para aguardar conincidencia de Thread
            if (BFFCplot.cont > 3)
            {
                Posicao1 = BFFCplot.read();
                Posicao2 = BFFCplot.read();
                Corrente1 = BFFCplot.read();
                Corrente2 = BFFCplot.read();

                pbManopla.Invoke(new Action(() => pbManopla.Location = new Point(Posicao1 * FatorMov, pbMovArea.Height - pbManopla.Height - (Posicao2 * FatorMov) - 1)));

                lbl1.Invoke(new Action(() => lbl1.Text = Convert.ToString(Posicao1)));
                lbl2.Invoke(new Action(() => lbl2.Text = Convert.ToString(Posicao2)));
                lbl3.Invoke(new Action(() => lbl3.Text = Convert.ToString(Corrente1)));
                lbl4.Invoke(new Action(() => lbl4.Text = Convert.ToString(Corrente2)));

                if (pbManopla.Location == pbAlvo.Location)
                {
                    pbAcerto.BackColor = Color.Green;
                }
                else
                {
                    pbAcerto.BackColor = Color.Red;
                }
            }
            mut.ReleaseMutex(); // Libera outra Thread
        }
        //Finish3 = true;
    }
}


// BUFFER CIRCULAR
public class buffercircular
{
    private Int16 sizeBuffer;
    private Int16[] buffer;
    private Int16 wr, rd;
    public Int16 cont;

    public buffercircular(Int16 _sizeBuffer)
    {
        sizeBuffer = _sizeBuffer;
        buffer = new Int16[_sizeBuffer];
        wr = 0;
        rd = 0;
        cont = 0;
    }

    public Int16 Count()
    {
        return cont;
    }

    public bool write(Int16 valor)
    {
        if (cont < sizeBuffer)
        {
            buffer[wr] = valor;
            cont++;
            wr++;

            if (wr >= sizeBuffer)
            {
                wr = 0;
            }
            return true;
        }

        else
        {
            MessageBox.Show("Estouro do Buffer", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    public Int16 read()
    {
        if (cont == 0)
        {
            return 9876; // Valor irreal para dizer que não é preciso plotar
        }

        else
        {
            Int16 val = buffer[rd];
            cont--;
            rd++;
            if (rd >= sizeBuffer)
            {
                rd = 0;
            }
            return val;
        }
    }
}







//----------------------------------------------------------------------------------------------------------------
// CLASSE PARA ESCREVER EM ARQUIVOS .TXT
/*
public class Escreve_txt
{

    public void EscreverDado(Int16 valor, String Local) // Função para escrever o dado
    {
        StreamWriter dadotxt = new StreamWriter(Local); // Abre o local da pasta
        dadotxt.WriteLine(valor); // Escrevendo nova linha de dado
        dadotxt.Close(); // Fecha pasta
        dadotxt.Dispose(); //Limpando a referencia dele da memória
    }
}
*/



/*
// Calculos e gráficos
public class CalculosGraficos : Form
{
    public double fa = 0.0;                    // frequência de amostragem
    public double amplitude = 0.0;             // Amplitude do sinal
    public double freq = 0.0;                  // frequência do sinal
    public double nPontos = 0.0;              // numero de amostras do sinal
    public double deltaT = 0.0;                // valor x no tempo
    public double deltaY = 0.0;                // valor y no tempo
    public double Yinicio = 0.0;               // valor de deltaY para somatorio ou decremento na onda triangular
    public int contSinal = 0;                 // auxiliar para contagem de amostras do sinal para zera-lo
    public int contTempo = 0;                 // Contagem do tempo 
    public double valorY;                 // valor vindo da fifo para plotagem e calculos eixo Y

    //public int k = 0; // Controle de Amostras
    private Chart MyChartControl; // Manuseador do chart que plota sinais em form 1
    private Chart MyChartControlF; // Manuseador do chart que plota fourrier em form 1
    public bool pausaGS = false; // Variavel para pausar grafico

    public bool run = false; // Indica quando parar de plotar sinais
    public bool runCalc = false; // Indica quando parar de calcular FFT
    public bool pauseFFT = false; // Indica quando parar de plotar FFT
    public bool ParaAquisicao = false; // Quando parar aquisição
    private bool plotFFT = false; // Libera ou não a plotagem da FFT
    private int m = 0; // contador plotagem

    // Variaveis para calculo FFT
    FFT2 CacluloFFT; // Manipulador da classe FFT2
    private int contFFT; // Contador de pontos para quando tiver 256 pontos acionar FFT
    public double[] re = new double[1256]; // armazena valores para fft do eixo x
    public double[] im = new double[1256]; // armazena valores para fft do eixo y
    public double[] mag = new double[1256]; // modulo da FFT

    public Mutex M1;
    private buffercircular myBFFC; // Buffer Circular para plotagem
    private buffercircular myBFFC2; // Buffer Circular para FFT
    private int v;
    private int num;
    private int LSB, MSB;
    public SerialPort serialPort1;
    private Thread ThrAquis;
    private Thread ThrPlot;
    private Thread ThrFFT;

    private bool contplot = false;

    // Contrutores da classe
    public CalculosGraficos(Chart ChartSinal, Chart ChartFFT, int _num, SerialPort _serialPort1, Thread _ThrAquis, Thread _ThrPlot, Thread _ThrFFT)
    {
        MyChartControl = ChartSinal;
        MyChartControlF = ChartFFT;
        myBFFC = new buffercircular();
        myBFFC2 = new buffercircular();
        v = 0;
        num = _num;
        serialPort1 = _serialPort1;
        valorY = 0.0;
        ThrAquis = _ThrAquis;
        ThrPlot = _ThrPlot;
        ThrFFT = _ThrFFT;
    }

    // Aquisição
    public void Aquisicao()
    {
        while (ParaAquisicao)
        {
            if (serialPort1.BytesToRead > 0) // Observa quando o buffer do USB esta com 100 bytes
            {
                for (int i = 0; i < serialPort1.BytesToRead; i++) // Pega em pares os bytes e coloca no buffer circuilar
                {
                    LSB = serialPort1.ReadByte(); // Primeiro byte vindo 
                    MSB = serialPort1.ReadByte(); // Segundo byte vindo
                    num = ((MSB * 256) + LSB) - 512; // Transforma em numero novamente
                    myBFFC.write(num); // Gravando no buffer do grafico
                    myBFFC2.write(num); // Gravando no buffer da FFT
                }
            }
        }
    }

    // Plotando Grafico Sinal
    public void PlotarSinal()
    {
        int q = 0;
        while (run)
        {
            if (myBFFC.cont > 0) // pode ser lido
            {
                valorY = myBFFC.read(); // Lendo valor do buffer circular
                MyChartControl.Invoke(new Action(() => MyChartControl.Series["SinalGerado"].Points.AddXY(v / fa, valorY)));
                // Apagando com o gráfico
                if (v > 7000) //contplot == true) 
                {
                    //MyChartControl.Invoke(new Action(() => MyChartControl.ChartAreas[0].AxisX.Minimum += (1.0 / fa))); //(1.0 / fa)
                    //MyChartControl.Invoke(new Action(() => MyChartControl.ChartAreas[0].AxisX.Maximum += (1.0 / fa))); //(1.0 / fa)
                    //MyChartControl.Invoke(new Action(() => MyChartControl.Series["SinalGerado"].Points.RemoveAt(0)));
                    MyChartControl.Invoke(new Action(() => MyChartControl.Series["SinalGerado"].Points.Clear())); // Apagando grafico
                    v = 0;
                }
                v++;
                q++;
                if (q == 50) // Só plota quando tiver 50 pontos
                {
                    MyChartControl.Invoke(new Action(() => MyChartControl.Series["SinalGerado"].Points.ResumeUpdates())); // Atualiza pontos na tela
                    MyChartControl.Invoke(new Action(() => MyChartControl.Series["SinalGerado"].Points.SuspendUpdates())); // Supende a plotagem de pontos
                    q = 0; // Zera variavel para esperar os próximos 50 dados
                }

                if (plotFFT == true) // Espera a FFT ser calculada para plotar
                {
                    MyChartControlF.Invoke(new Action(() => MyChartControlF.Series["GraficoFourier"].Points.ResumeUpdates())); // Atualiza pontos na tela
                    MyChartControlF.Invoke(new Action(() => MyChartControlF.Series["GraficoFourier"].Points.SuspendUpdates())); // Supende a plotagem de pontos
                    plotFFT = false;
                }
            }
        }
    }

    // FFT
    public void CalculoFFT()
    {
        while (runCalc)
        {
            while (pauseFFT)
            {
                if (myBFFC2.cont >= 1023) // pode ser lido
                {
                    for (int i = 0; i < 1024; i++)
                    {
                        re[contFFT] = myBFFC2.read(); // Amazenando dados para fft
                        im[contFFT] = 0.0;
                        contFFT++;
                    }
                }

                if (contFFT >= 1024)
                {
                    contFFT = 1024;
                    CacluloFFT = new FFT2();
                    CacluloFFT.init(Convert.ToUInt32(Math.Log(Convert.ToDouble(contFFT), 2))); // Convertendo para log o numero de pontos
                    CacluloFFT.run(re, im, false);
                    MyChartControlF.Invoke(new Action(() => MyChartControlF.Series["GraficoFourier"].Points.Clear())); // Apagando grafico antigo
                    for (int p = 0; p <= (contFFT / 2); p++)
                    {
                        mag[p] = Math.Sqrt((re[p] * re[p]) + (im[p] * im[p])) / contFFT;
                        MyChartControlF.Invoke(new Action(() => MyChartControlF.Series["GraficoFourier"].Points.AddXY(p * fa / nPontos, mag[p])));
                    }
                    contFFT = 0;
                    plotFFT = true; // Libera a Plotagem da FFT
                    contplot = true;
                }
            }
        }
    }
}
*/


/*
// CALCULANDO FFT 
public class FFT2
{
// Element for linked list in which we store the
// input/output data. We use a linked list because
// for sequential access it's faster than array index.
class FFTElement
{
    public double re = 0.0;     // Real component
    public double im = 0.0;     // Imaginary component
    public FFTElement next;     // Next element in linked list
    public uint revTgt;         // Target position post bit-reversal
}

private uint m_logN = 0;        // log2 of FFT size
private uint m_N = 0;           // FFT size
private FFTElement[] m_X;       // Vector of linked list elements

/**
 *
 */
/*
public FFT2()
{
}

/**
* Initialize class to perform FFT of specified size.
*
* @param   logN    Log2 of FFT length. e.g. for 512 pt FFT, logN = 9.
*/
/*   
  public void init(
      uint logN)
  {
      m_logN = logN;
      m_N = (uint)(1 << (int)m_logN);

      // Allocate elements for linked list of complex numbers.
      m_X = new FFTElement[m_N];
      for (uint k = 0; k < m_N; k++)
          m_X[k] = new FFTElement();

      // Set up "next" pointers.
      for (uint k = 0; k < m_N - 1; k++)
          m_X[k].next = m_X[k + 1];

      // Specify target for bit reversal re-ordering.
      for (uint k = 0; k < m_N; k++)
          m_X[k].revTgt = BitReverse(k, logN);
  }

  /**
   * Performs in-place complex FFT.
   *
   * @param   xRe     Real part of input/output
   * @param   xIm     Imaginary part of input/output
   * @param   inverse If true, do an inverse FFT
   */
/*
public void run(
   double[] xRe,
   double[] xIm,
   bool inverse = false)
{
   uint numFlies = m_N >> 1; // Number of butterflies per sub-FFT
   uint span = m_N >> 1;     // Width of the butterfly
   uint spacing = m_N;         // Distance between start of sub-FFTs
   uint wIndexStep = 1;        // Increment for twiddle table index

   // Copy data into linked complex number objects
   // If it's an IFFT, we divide by N while we're at it
   FFTElement x = m_X[0];
   uint k = 0;
   double scale = inverse ? 1.0 / m_N : 1.0;
   while (x != null)
   {
       x.re = scale * xRe[k];
       x.im = scale * xIm[k];
       x = x.next;
       k++;
   }

   // For each stage of the FFT
   for (uint stage = 0; stage < m_logN; stage++)
   {
       // Compute a multiplier factor for the "twiddle factors".
       // The twiddle factors are complex unit vectors spaced at
       // regular angular intervals. The angle by which the twiddle
       // factor advances depends on the FFT stage. In many FFT
       // implementations the twiddle factors are cached, but because
       // array lookup is relatively slow in C#, it's just
       // as fast to compute them on the fly.
       double wAngleInc = wIndexStep * 2.0 * Math.PI / m_N;
       if (inverse == false)
           wAngleInc *= -1;
       double wMulRe = Math.Cos(wAngleInc);
       double wMulIm = Math.Sin(wAngleInc);

       for (uint start = 0; start < m_N; start += spacing)
       {
           FFTElement xTop = m_X[start];
           FFTElement xBot = m_X[start + span];

           double wRe = 1.0;
           double wIm = 0.0;

           // For each butterfly in this stage
           for (uint flyCount = 0; flyCount < numFlies; ++flyCount)
           {
               // Get the top & bottom values
               double xTopRe = xTop.re;
               double xTopIm = xTop.im;
               double xBotRe = xBot.re;
               double xBotIm = xBot.im;

               // Top branch of butterfly has addition
               xTop.re = xTopRe + xBotRe;
               xTop.im = xTopIm + xBotIm;

               // Bottom branch of butterly has subtraction,
               // followed by multiplication by twiddle factor
               xBotRe = xTopRe - xBotRe;
               xBotIm = xTopIm - xBotIm;
               xBot.re = xBotRe * wRe - xBotIm * wIm;
               xBot.im = xBotRe * wIm + xBotIm * wRe;

               // Advance butterfly to next top & bottom positions
               xTop = xTop.next;
               xBot = xBot.next;

               // Update the twiddle factor, via complex multiply
               // by unit vector with the appropriate angle
               // (wRe + j wIm) = (wRe + j wIm) x (wMulRe + j wMulIm)
               double tRe = wRe;
               wRe = wRe * wMulRe - wIm * wMulIm;
               wIm = tRe * wMulIm + wIm * wMulRe;
           }
       }

       numFlies >>= 1;   // Divide by 2 by right shift
       span >>= 1;
       spacing >>= 1;
       wIndexStep <<= 1;     // Multiply by 2 by left shift
   }

   // The algorithm leaves the result in a scrambled order.
   // Unscramble while copying values from the complex
   // linked list elements back to the input/output vectors.
   x = m_X[0];
   while (x != null)
   {
       uint target = x.revTgt;
       xRe[target] = x.re;
       xIm[target] = x.im;
       x = x.next;
   }
}

/**
* Do bit reversal of specified number of places of an int
* For example, 1101 bit-reversed is 1011
*
* @param   x       Number to be bit-reverse.
* @param   numBits Number of bits in the number.
*/
/*
private uint BitReverse(
   uint x,
   uint numBits)
{
   uint y = 0;
   for (uint i = 0; i < numBits; i++)
   {
       y <<= 1;
       y |= x & 0x0001;
       x >>= 1;
   }
   return y;
}

}
*/
