using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ODU_JIG
{
    public partial class formPrincipal : Form
    {
        string rxData;
        string recebeTempoDoTeste;
        string caminhoConnect = Application.StartupPath.ToString() + "/Connection.ini";
        string script = Application.StartupPath.ToString() + "/Script.txt";
        string statusDoTeste;
        string valorDosIntervalos;

        bool AlteraCor = false;

        const int DelayPadraoPorLinha = 250;
        int IndexLinhaAtual = -1;
        int IndexLinhaAtual2 = -1;
        int verdeClaro = 0;
        int dados_aprovado = 0;

        Stopwatch tempoParadoCronometro = new Stopwatch();
        Stopwatch cronometro = new Stopwatch();
        Timer cronometroTimer = new Timer();
        IniFile _myIni = new IniFile(Application.StartupPath.ToString() + "\\Connection.ini");
        DateTime tempoInicioTeste;
        List<TimeSpan> temposDosTestes = new List<TimeSpan>();
        DateTime inicioTempoParado;
        DateTime fimTempoParado;

        public formPrincipal()
        {
            InitializeComponent();
        }

        private async void parametros(string filePath)
        {
            string[] linhasDoArquivo = File.ReadAllLines(filePath);
            bool skipMode = false;

            for (int i = 0; i < linhasDoArquivo.Length; i++)
            {
                if (skipMode)
                {
                    if (linhasDoArquivo[i].Trim() == "cmdTRACK")
                    {
                        skipMode = false;
                    }
                    continue;
                }

                string line = linhasDoArquivo[i];
                string[] partes_split = line.Split('\t');
                switch (partes_split[0])
                {
                    case "cmdINICIO":
                        InicioContagem();
                        break;
                    case "cmdFIM":
                        fimContagem();
                        status_fimTeste();
                        GravarLog();
                        break;
                    case "cmdTST":
                        IndexLinhaAtual = int.Parse(partes_split[1].Substring(4));
                        IndexLinhaAtual2 = int.Parse(partes_split[1].Substring(4));
                        break;
                    case "cmdDLY":
                        await Delay(int.Parse(partes_split[1]));
                        break;
                    case "cmdENV":
                        // Chama o método para ambos os DataGrids
                        await ProcessarEnv(partes_split[1], dataGridView1);
                        await ProcessarEnv(partes_split[1], dataGridView2);
                        break;
                }
            }

            fimContagem();
        } 

        private async Task ProcessarEnv(string comando, DataGridView dataGrid)
        {
            string[] Valor_comandoENV = comando.Split(';');
            if (Valor_comandoENV.Length >= 1)
            {
                comandosSeriais(Valor_comandoENV[0]);

                if (Valor_comandoENV.Length >= 3)
                {

  
                    string valorRecebido1 = Valor_comandoENV[1];

         
                    string valorRecebido2 = Valor_comandoENV[2];

                    if (int.TryParse(valorRecebido1, out int minRange) && int.TryParse(valorRecebido2, out int maxRange))
                    {
                        if (IndexLinhaAtual >= 0 && IndexLinhaAtual < dataGrid.Rows.Count)
                        {
                            dataGrid.Rows[IndexLinhaAtual].Cells[1].Value = minRange;
                            dataGrid.Rows[IndexLinhaAtual].Cells[2].Value = maxRange;
                        }
                        if (IndexLinhaAtual2 >= 0 && IndexLinhaAtual2 < dataGrid.Rows.Count)
                        {
                            dataGrid.Rows[IndexLinhaAtual2].Cells[1].Value = minRange;
                            dataGrid.Rows[IndexLinhaAtual2].Cells[2].Value = maxRange;
                        }
                    }
                }

                txtRXData.Text = "";
                await Delay(DelayPadraoPorLinha);
            }
        }


        private void GravarLog() 
        {
            string logFilePath = Application.StartupPath + "\\LOG.txt";
            int ultimoID = 0;


            if (File.Exists(logFilePath))
            {
                using (StreamReader sr = new StreamReader(logFilePath))
                {
                    string ultimaLinha = null;
                    while ((ultimaLinha = sr.ReadLine()) != null)
                    {
                        if (ultimaLinha.StartsWith("Teste ID: "))
                        {
                            string idString = ultimaLinha.Substring(10);
                            if (int.TryParse(idString, out int id))
                            {
                                ultimoID = id;
                            }
                        }
                    }
                }
            }

            // soma o ultimo id + 1
            int proximoID = ultimoID + 1;

            // pega o resulto da soma acima e joga no LOG que foi gerado
            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                sw.WriteLine($"Teste ID: {proximoID}");
                sw.WriteLine($"Status Teste: {statusDoTeste}"); //Variavel para saber se deu falha ou aprovou
                sw.WriteLine($"Teste realizado em: {DateTime.Now}");

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells[0].Value != null && row.Cells[3].Value != null)
                    {
                        string nomeDoTeste = row.Cells[0].Value.ToString();
                        string valorDoTeste = row.Cells[3].Value.ToString();
                        sw.WriteLine($"{nomeDoTeste} = {valorDoTeste}");
                    }
                }

                sw.WriteLine();
            }
        }

        //cmdINICIO
        private void InicioContagem()
        {
            cronometro.Restart();
            cronometroTimer.Start();
        }

        //cmdFIM
        private void fimContagem()
        {
            timer3.Stop();
        }

        //Delay usando em cmdDLY
        private Task Delay(int milisegundos)
        {
            return Task.Delay(milisegundos);
        }

        //função para enviar fcomandos seriais, eu chamo na função parametros para enviar os comandos de: cmdENV
        private void comandosSeriais(string comando)
        {
            serialPort1.WriteLine(comando);

        }

        private void formPrincipal_Load(object sender, EventArgs e)
        {
            inicioTempoParado = DateTime.Now;
            tempoParadoCronometro.Start();

            if (Directory.Exists("/home"))
            {
                string[] lines = File.ReadAllLines(caminhoConnect);

                foreach (string line in lines)
                {
                    string[] str = line.Split('=');
                    if (str[0] == "port") { serialPort1.PortName = str[1]; }
                    if (str[0] == "baudRate") { serialPort1.BaudRate = int.Parse(str[1]); }

                    StreamReader sr = new StreamReader(caminhoConnect);
                    richTextBox1.Text = sr.ReadToEnd();
                    sr.Close();
                }
            }
            else
            {
                serialPort1.BaudRate = int.Parse(_myIni.Read("baudRate", "Connect"));
                serialPort1.PortName = _myIni.Read("port", "Connect");
            }

            try
            {
                if (!serialPort1.IsOpen)
                {
                    serialPort1.Open();
                    label5.Visible = true;
                    label5.Text = "Conectado";
                    timer_aviso.Start();
                }
                else
                {

                }
            }
            catch (Exception)
            {
                MessageBox.Show("Verifique a Conexão Serial. O Software continuará em modo Offline.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label5.Visible = true;
                label5.Text = "Off...";
            }

            richTextBox3.LoadFile(script, RichTextBoxStreamType.PlainText);
            valores_iniciarDaGrid();
        }

        //Função para inciar os valores de cmdMSG (descrição) e cmdENV (ranges)
        private void valores_iniciarDaGrid()
        {
            int indexDaLinha = 0;
            int indexDalinha2 = 0;

            using (StreamReader sr = new StreamReader(script))
            {
                string descricaoDoTeste = null;
                string linha;
                while ((linha = sr.ReadLine()) != null)
                {
                   
              
                    if (!string.IsNullOrWhiteSpace(linha))
                    {
                        //Tudo que estiver no cmdMSG vai iniciar na coluna 0 do dataGrid
                        if (linha.StartsWith("cmdMSG"))
                        {
                            //faz o split por TAB
                            string[] partes = linha.Split('\t');
                            descricaoDoTeste = partes[1].Trim();

                            //Guarda o valor em DescricaoDoTeste
                            dataGridView1.Rows.Add(descricaoDoTeste);
                            dataGridView2.Rows.Add(descricaoDoTeste);



                            //Sempre atulizada o indexAtual com o index da linha que foi adicionada
                            IndexLinhaAtual = indexDaLinha++;
                            IndexLinhaAtual2 = indexDalinha2++;
                        }
                        
                        //Aqui é onde o range é adicionado, pois o range está na mesma linha do que é enviado para serial
                        if (linha.StartsWith("cmdENV"))
                        {
                            //faço o split e pego apenas o que está após o ';' que no caso são: RANGE MIN e RANGE MAX
                            string[] parts = linha.Split(';');
                            if (parts.Length >= 3)
                            {
                                //guardo os valores capturados para add na grid
                                string value1 = parts[1];
                                string value2 = parts[2];

                                if (IndexLinhaAtual >= 0 && IndexLinhaAtual < dataGridView1.Rows.Count)
                                {
                                    //adicionando os valores na grid
                                    dataGridView1.Rows[IndexLinhaAtual].Cells[1].Value = value1;
                                    dataGridView1.Rows[IndexLinhaAtual].Cells[2].Value = value2;

                                   
                                }
                                if (IndexLinhaAtual >= 0 && IndexLinhaAtual < dataGridView2.Rows.Count)
                                {
                                    dataGridView2.Rows[IndexLinhaAtual2].Cells[1].Value = value1;
                                    dataGridView2.Rows[IndexLinhaAtual].Cells[2].Value = value2;
                                }
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(descricaoDoTeste))
                {
                    dataGridView1.Rows.Add(descricaoDoTeste);
                }
            }
        }















        //Fechar Software
        private void label1_Click(object sender, EventArgs e)
        {
            Close();
        }

        //Fechar Software
        private void circularPanel1_Click(object sender, EventArgs e)
        {
            Close();
        }


        //timer_aviso -> Função para deixar label piscando
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (AlteraCor)
            {
                label3.ForeColor = Color.White;
                AlteraCor = false;
            }
            else
            {
                label3.ForeColor = Color.Orange;
                AlteraCor = true;
            }
        }

        private void rjButton1_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            timer_aviso.Stop();
            label5.Visible = false;
            Setup s = new Setup();
            s.ShowDialog();
        }

        private void rjButton2_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            timer_aviso.Stop();
            label5.Visible = false;
            config cf = new config();
            cf.ShowDialog();
        }

        //Botão para Recarregar
        private void rjButton3_Click(object sender, EventArgs e)
        {
            fimTempoParado = DateTime.Now;
            TimeSpan intervaloDeTempo = fimTempoParado - inicioTempoParado;

            MessageBox.Show("Intervalo: " + intervaloDeTempo.ToString());
            ;
            if (Directory.Exists("/home"))
            {
                string[] lines = File.ReadAllLines(caminhoConnect);
                foreach (string line in lines)
                {
                    string[] str = line.Split('=');
                    if (str[0] == "port") { serialPort1.PortName = str[1]; }
                    if (str[0] == "baudRate") { serialPort1.BaudRate = int.Parse(str[1]); }

                    StreamReader sr = new StreamReader(caminhoConnect);
                    richTextBox1.Text = sr.ReadToEnd();
                    sr.Close();
                }
            }
            else
            {
                if (label5.Text != "Conectado")
                {
                    serialPort1.BaudRate = int.Parse(_myIni.Read("baudRate", "Connect"));
                    serialPort1.PortName = _myIni.Read("port", "Connect");
                }
            }

            try
            {
                if (!serialPort1.IsOpen)
                {
                    serialPort1.Open();
                    label5.Visible = true;
                    label5.Text = "Conectado";
                    timer_aviso.Start();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Verifique a Conexão Serial.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                timer_aviso.Stop();

                label5.Visible = true;
                label5.Text = "Off...";
            }
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            rxData = serialPort1.ReadExisting();
            this.Invoke(new EventHandler(dataReceived));
            Console.WriteLine("Vem da Serial: " + rxData);
        }

        private void dataReceived(object sender, EventArgs e)
        {
            txtRXData.AppendText(rxData);

            //Recebo comando na aba SCRIPT
            richTextBox2.AppendText(txtRXData.Text);
        }

        //Inicio do teste, uso o botão para simular o click da Botoeira de Start

        private async void button2_Click(object sender, EventArgs e)
        {
            fimTempoParado = DateTime.Now;
            TimeSpan intervaloDeTempo = fimTempoParado - inicioTempoParado;
            valorDosIntervalos = intervaloDeTempo.ToString();

            inicioTempoParado = DateTime.Now;
            tempoInicioTeste = DateTime.Now;
            timer2.Start();
            limparColuna_Cor_grid();
            timer3.Start();
            timer_aviso.Stop();
            label3.Visible = false;
            textBox2.Visible = true;
            textBox2.Text = "Teste iniciado, aguarde...";
            textBox1.Visible = true;

            txb_Testando1.BackColor = ColorTranslator.FromHtml("#014e7f");
            txb_Testando1.Texts = "";
            textBox1.BackColor = ColorTranslator.FromHtml("#014e7f");

            InicioContagem();
            await Task.Run(() => parametros(script));
        }

        //timer que passa a contagem
        private void timer3_Tick(object sender, EventArgs e)
        {
            textBox1.Text = cronometro.Elapsed.ToString(@"hh\:mm\:ss");
        }

        //Limpa a coluna 3 com os valores recebidos da serial e coloca o fundo Padrao do dataGrid, isso precisa estar no inicio do teste
        private void limparColuna_Cor_grid()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells[3].Value = null;
                row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#014e7f");
            }
        }

        //Timer onde acontece o teste, pega o valor, splita, verifica o range, reprova ou aprova e segue para próximo (se aprovar) ou para o teste (se reprovar)
        private void timer2_Tick(object sender, EventArgs e)
        {
            //Pega o valor da Serial
            string data = txtRXData.Text.Trim();

            if (data.Contains("-II"))
            {
                string[] valorRecebido = data.Split('-');

                if (valorRecebido.Length > 1)
                {
                    if (IndexLinhaAtual >= 0 && IndexLinhaAtual < dataGridView1.Rows.Count)
                    {
                        if (!data.StartsWith("cmdMSG"))
                        {
                            // recebe o valor e converte
                            if (int.TryParse(valorRecebido[0], out int valorFinal))
                            {
                                // pega o valor das colunas de range
                                if (int.TryParse(dataGridView1.Rows[IndexLinhaAtual].Cells[1].Value?.ToString(), out int minValor) &&
                                    int.TryParse(dataGridView1.Rows[IndexLinhaAtual].Cells[2].Value?.ToString(), out int maxValor))
                                {
                                    // pega o valor final e joga na coluna valor
                                    dataGridView1.Rows[IndexLinhaAtual].Cells[3].Value = valorFinal;

                                    // joga o valor final no range, que é o valor da coluna 1 e 2
                                    if (valorFinal >= minValor && valorFinal <= maxValor)
                                    {
                                        // se estiver tudo certo, colore de verde
                                        // verdeClaro é apenas para intercalar nas cores (Verde Escuro e Verde Claro)
                                        if (verdeClaro == 0)
                                        {
                                            //Entre nessa e na próxima já entra no else, pois verdeClaro é 1
                                            dataGridView1.Rows[IndexLinhaAtual].DefaultCellStyle.BackColor = Color.DarkGreen;
                                            verdeClaro++;
                                        }
                                        else
                                        {
                                            //Entra nessa e na próxima já entra no if, pois verdeClaro é 0
                                            dataGridView1.Rows[IndexLinhaAtual].DefaultCellStyle.BackColor = Color.Green;
                                            verdeClaro = 0;
                                        }
                                    }
                                    else
                                    {
                                        // Se nao estiver dentro do range a linha fica vermelha e o teste para (para no falhou = true)
                                        dataGridView1.Rows[IndexLinhaAtual].DefaultCellStyle.BackColor = Color.DarkRed;
                                        timer2.Stop();
                                        textBox1.Visible = false;
                                        txb_Testando1.BackColor = Color.DarkRed;
                                        txb_Testando1.Texts = "Falha";

                                        textBox2.Visible = true;
                                        textBox2.Text = "Ocorreu falha durante o teste, aguarde finalizar.";
                                    }
                                }
                                else
                                {
                                    // isso é se caso estiver sem range, aí segue e joga o valorFinal na coluna 3, sem teste de range
                                    dataGridView1.Rows[IndexLinhaAtual].Cells[3].Value = valorFinal;
                                }

                                dataGridView1.ClearSelection();
                                dataGridView1.CurrentCell = null;
                            }
                        }
                    }
                    //limpar o textBox da Serial
                    txtRXData.Clear();
                }
            }
        }

        //Status do Teste, lógica de aprovação e gera LOG ou Entra na lógica da falha (linhaVermelha = true;) e também gera LOG, porém na função: parametros()
        private void status_fimTeste()
        {
            bool linhaVermelha = false;
            foreach (DataGridViewRow linha in dataGridView1.Rows)
            {
                if (linha.DefaultCellStyle.BackColor == Color.DarkRed)
                {
                    linhaVermelha = true;
                    break;
                }
            }

            if (!linhaVermelha)
            {
                if (txb_Testando1.InvokeRequired)
                {
                    txb_Testando1.Invoke(new Action(() =>
                    {

                        dados_aprovado += 1;
                        statusDoTeste = "Aprovado";
                        TimeSpan tempoDecorrido = DateTime.Now - tempoInicioTeste;
                        recebeTempoDoTeste = tempoDecorrido.ToString(@"hh\:mm\:ss");

                        txb_Testando1.Texts = "Aprovado" + " - " + recebeTempoDoTeste;
                        txb_Testando1.BackColor = Color.DarkGreen;

                        textBox1.Visible = false;
                        textBox2.Visible = true;
                        textBox2.Text = "Teste finalizado, liberado para um novo ciclo.";

                        temposDosTestes.Add(tempoDecorrido);

                        inicioTempoParado = DateTime.Now;

                        // Calcular média dos tempos
                        TimeSpan mediaDosTempos = TimeSpan.FromMilliseconds(temposDosTestes.Average(t => t.TotalMilliseconds));
                        Console.WriteLine($"Tempo médio dos testes: {mediaDosTempos}");

                        CriarArquivoResultado("Aprovado", mediaDosTempos);


                    }));
                }
                else
                {
                    GravarLog();
                    statusDoTeste = "Aprovado";
                    TimeSpan tempoDecorrido = DateTime.Now - tempoInicioTeste;
                    recebeTempoDoTeste = tempoDecorrido.ToString(@"hh\:mm\:ss");

                    txb_Testando1.Texts = "Aprovado" + " - " + recebeTempoDoTeste;
                    txb_Testando1.BackColor = Color.DarkGreen;

                    textBox1.Visible = false;
                    textBox2.Visible = true;
                    textBox2.Text = "Teste finalizado, liberado para um novo ciclo.";

                    temposDosTestes.Add(tempoDecorrido);

                    inicioTempoParado = DateTime.Now;
                    // Calcular média dos tempos
                    TimeSpan mediaDosTempos = TimeSpan.FromMilliseconds(temposDosTestes.Average(t => t.TotalMilliseconds));
                    Console.WriteLine($"Tempo médio dos testes: {mediaDosTempos}");

                    CriarArquivoResultado("Aprovado", mediaDosTempos);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox2.Clear();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (button3.Text == "Editar")
            {
                richTextBox3.ReadOnly = false;
                richTextBox3.BackColor = Color.Orange;
                button3.Text = "Fechar Edição";
            }
            else
            {
                richTextBox3.ReadOnly = true;
                richTextBox3.BackColor = Color.White;
                button3.Text = "Editar";
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(script, richTextBox3.Text);
                MessageBox.Show("Alterações salvas com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                richTextBox3.ReadOnly = true;
                richTextBox3.BackColor = Color.White;
                button3.Text = "Editar";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar as alterações: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void rjButton6_Click(object sender, EventArgs e)
        {
            if (rjTextBox2.Texts == "54321")
            {
                panel3.Visible = false;
            }
        }

        private void CriarArquivoResultado(string resultado, TimeSpan tempoParado = default, TimeSpan? tempoMedio = null)
        {
            string filePath = Application.StartupPath + "\\Resultado.txt";
            string aprovadoPrefixo = "teste_aprovado=";
            string reprovadoPrefixo = "teste_reprovado=";
            string dataPrefixo = "data_ultimoTeste=";
            string mediaTempoPrefixo = "tempo_medio_testes=";
            string tempoParadoPrefixo = "tempo_parado=";

            try
            {
                // Lê o conteúdo atual do arquivo para atualizar os contadores
                int aprovados = 0;
                int reprovados = 0;
                string dataUltimoTeste = DateTime.Now.ToString();
                TimeSpan tempoParadoAnterior = TimeSpan.Zero;

                if (File.Exists(filePath))
                {
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.StartsWith(aprovadoPrefixo))
                            {
                                int.TryParse(line.Substring(aprovadoPrefixo.Length), out aprovados);
                            }
                            else if (line.StartsWith(reprovadoPrefixo))
                            {
                                int.TryParse(line.Substring(reprovadoPrefixo.Length), out reprovados);
                            }
                            else if (line.StartsWith(tempoParadoPrefixo))
                            {
                                TimeSpan.TryParse(line.Substring(tempoParadoPrefixo.Length), out tempoParadoAnterior);
                            }
                        }
                    }
                }

                // Atualiza os contadores baseado no resultado atual
                switch (resultado)
                {
                    case "Aprovado":
                        aprovados++;
                        break;
                    case "Reprovado":
                        reprovados++;
                        break;
                }

                // Atualiza o tempo parado total
                TimeSpan tempoParadoTotal = tempoParadoAnterior + tempoParado;

                // Escreve os novos contadores, a data do último teste e o tempo parado no arquivo
                using (StreamWriter sw = new StreamWriter(filePath, true))
                {
                    sw.WriteLine($"{aprovadoPrefixo}{aprovados}");
                    sw.WriteLine($"{reprovadoPrefixo}{reprovados}");
                    sw.WriteLine($"{dataPrefixo}{dataUltimoTeste}");
                    sw.WriteLine($"{tempoParadoPrefixo}{valorDosIntervalos}");

                    // Escrever tempo médio, se disponível
                    if (tempoMedio.HasValue)
                    {
                        sw.WriteLine($"{mediaTempoPrefixo}{tempoMedio}");
                    }

                    sw.WriteLine(); // linha em branco para separar os resultados
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar o arquivo de resultado: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void timer1_mediaTempo_Tick(object sender, EventArgs e)
        {
            if (dados_aprovado > 0)
            {
                TimeSpan tempoDecorrido = DateTime.Now - tempoInicioTeste;
                double mediaEmMilissegundos = temposDosTestes.Select(t => t.TotalMilliseconds).Average();
                TimeSpan mediaDosTempos = TimeSpan.FromMilliseconds(mediaEmMilissegundos);
                CriarArquivoResultado("Aprovado", mediaDosTempos);
                dados_aprovado = 0;
                temposDosTestes.Clear();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            inicioTempoParado = DateTime.Now;
        }

        private void rjButton5_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            timer_aviso.Stop();
            label5.Visible = false;
            Manutencao m = new Manutencao();
            m.ShowDialog();
        }

        private void ProcessarDadosSerial(string dadosSerial)
        {

            if (dadosSerial.Contains("B1"))
            {
                // Remove o "-B1" e faz o split pelos ";"
                string dadosLimpos = dadosSerial.Replace("-B1", "");
                string[] valores = dadosLimpos.Split(';');

                if (valores.Length == 7)
                {
                    // Atribui os valores às linhas correspondentes da grid (TEST3 a TEST9)
                    for (int i = 0; i < valores.Length; i++)
                    {
                        int indexTest = 3 + i; // TEST3 corresponde ao índice 3 na grid
                        if (indexTest <= 9) // Garante que não ultrapasse TEST9
                        {
                            dataGridView1.Rows[indexTest].Cells[3].Value = valores[i]; // Atribui aos valores da coluna 3 (supondo que seja a coluna correta)
                        }
                    }
                }
            }

            txtRXData.Text = "";

        }

        private void txtRXData_TextChanged(object sender, EventArgs e)
        {
            string dadosRecebidos = txtRXData.Text;
            ProcessarDadosSerial(dadosRecebidos);
        }

        private void roundedPanel2_Click(object sender, EventArgs e)
        {

        }
    }
}
