using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MathNet.Numerics; //Para calcular derivada
using System.IO.Ports;
using System.IO;


namespace Sonda_Agulha
{
    public partial class Form1 : Form
    {
        int cont_portas = 0; //Controla em qual porta será tentada a conexão
        int cont_tentativas = 0; //Controla o número de tentativas de conexão
        bool connected = false; //É true quando o arduino responde com um "Connected"
        string[] ports; //Portas Serial disponíveis
        int ports_length; //Armazena o número de portas disponíveis

        string dataIn; //Dado recebido do arduino
        int cont_plot = 0; //Marca o "tempo" para plotar

        List<double> x = new List<double>();
        List<double> y = new List<double>();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Teste de derivada

            //Cria uma função y = f(x)

            //Cria uma função
            //Depois deve ser retirada pq já teremos os valores de x e y
            for (int i = 0; i < 50; i++)
            {
                if(i < 10)
                {
                    x.Add(i);
                    y.Add(x[i]*x[i]*x[i]);
                }
                
                if(i >= 10 && i < 40)
                {
                    x.Add(i);
                    if (i % 2 == 0)
                    {
                        y.Add(5*x[i]);
                    }
                    else
                    {
                        y.Add(6 * x[i]);
                    }                   
                }

                if(i >= 40)
                {
                    x.Add(i);
                    y.Add(x[i - 40] * x[i - 40]);
                }

                chart1.Series[0].Points.AddXY(x[i], y[i]);
                chart1.Series[0].BorderWidth = 2;
            }

            var points = Find_Linear_Interval();
            Calculate_slope(points.Item1, points.Item2);

            //Algumas configurações para ser possível a seleção manual
            chart1.ChartAreas[0].CursorX.IsUserEnabled = false;         // red cursor at SelectionEnd
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = false;      // zoom into SelectedRange
            chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;
            chart1.ChartAreas[0].CursorX.Interval = 1;               // set "resolution" of CursorX
        }

        private bool Show_connection_error(string message)
        {
            timerPortas.Enabled = false;
            DialogResult result = MessageBox.Show(message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

            if (result == DialogResult.Retry)
            {
                timerPortas.Enabled = true;
                return true;
            }
            else
            {
                Environment.Exit(1);
                return false;
            }
        }

        //É executado quando o Arduino manda algo pela Serial
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            dataIn = serialPort1.ReadLine(); //Armazena o dado recebido

            //Se ainda não está conectado, verifica se o dado recebido é a palavra "Connected" que deve ser mandada pelo arduino quando a conexão é requisitada
            //É colocado o limite de tamanho pois o Arduino também estará mandando outros dados
            if (!connected && dataIn.Length < 11)
            {
                dataIn = dataIn.Remove(dataIn.Length - 1); //Remove o ultimo char porque é vazio

                //Se o Arduino mandou a palavra correta, passa a bool connected para true, e a rotina de plotar dados será executada quando chegar um novo dado
                if (dataIn == "Connected")
                {
                    connected = true;
                    timerPortas.Enabled = false; //Desabilita o timer de conexão de portas
                }
                else if (dataIn == "Fim")
                {
                    var points = Find_Linear_Interval();
                }
            }

            else if (connected)
            {
                Plot_data(double.Parse(dataIn));
                y.Add(double.Parse(dataIn));
                x.Add(cont_plot);
                cont_plot++;
            }
        }

        //Os dados recebidos são executados na thread da serialPort1 e os métodos de plotar, escrever nas textboxes, etc. em outra thread.
        //É necessário o método "delegate" e "CallBack" que garante que a função será executada em seu thread
        //O thread do chart1 e textboxes é o mesmo, então são chamados na mesma função

        delegate void Plot_dataCallback(double value); //Declara o método do delegate da função Plot_data


        //Método para plotar dados e escrever nas textBox
        private void Plot_data(double value)
        {
            if (this.chart1.InvokeRequired) //Verifica se o thread do chart1 é diferente do thread atual. Se sim, retorna true
            {
                Plot_dataCallback d = new Plot_dataCallback(Plot_data); //Cria o método delegate e armazena na variável d
                this.BeginInvoke(d, new object[] { value }); //Invoca esta mesma função Plot_data mas agora no thread do chart1. Com isto o InvokeRequired retornará false                
            }
            else
            {
                //Plota o gráfico
                this.chart1.Series[0].Points.AddXY(cont_plot, value);
            }
        }

        private void timerPortas_Tick(object sender, EventArgs e)
        {
            //Se a porta serial está aberta tenta mandar a mensagem de conexão 5 vezes
            //Se está fechada procura a próxima porta e tenta conectar
            if (serialPort1.IsOpen)
            {
                Console.WriteLine(serialPort1.PortName);
                Console.WriteLine("Tentativa " + cont_tentativas.ToString());
                serialPort1.WriteLine("Connect");
                cont_tentativas++;

                if (cont_tentativas == 5)
                {
                    cont_tentativas = 0;
                    serialPort1.Close();
                }
            }
            else
            {
                //Se o contador de portas está em zero, pega as portas disponíveis
                //Se o contador chegou na última porta sem sucesso de conexão, mostra o erro
                if (cont_portas == 0)
                {
                    ports = SerialPort.GetPortNames(); //Pega o nome das portas seriais disponíveis
                    ports_length = ports.Length;

                    //Se não há nenhuma porta disponível mostra o erro
                    if (ports_length == 0)
                    {
                        if (Show_connection_error("Nenhuma porta COM encontrada"))
                        {
                            return;
                        }
                    }

                }

                else if (cont_portas == ports_length)
                {
                    cont_portas = 0;

                    //Mostra o erro caso aconteça
                    Show_connection_error("Arduino não encontrado");
                    return;
                }

                //Associa o nome da porta correspondente à porta serial e tenta conectar
                serialPort1.PortName = ports[cont_portas];

                try
                {
                    serialPort1.Open();
                }
                catch
                {

                }

                cont_portas++;
            }
        }

        //Manda o arduino iniciar o aquecimento
        private void btnInit_Click(object sender, EventArgs e)
        {
            serialPort1.Write("Iniciar");
        }

        //Pega os valores inicial e final de seleção e coloca nas tBox
        private void chart1_SelectionRangeChanging(object sender, System.Windows.Forms.DataVisualization.Charting.CursorEventArgs e)
        {
            double start = chart1.ChartAreas[0].CursorX.SelectionStart;
            double end = chart1.ChartAreas[0].CursorX.SelectionEnd;

            if(start > end)
            {
                tBoxInit.Text = end.ToString();
                tBoxFin.Text = start.ToString();
            }
            else
            {
                tBoxInit.Text = start.ToString();
                tBoxFin.Text = end.ToString();
            }

        }

        private void chart1_SelectionRangeChanged(object sender, System.Windows.Forms.DataVisualization.Charting.CursorEventArgs e)
        {
            List<double> xLin = new List<double>();
            List<double> yLin = new List<double>();

            int start = int.Parse(tBoxInit.Text);
            int end = int.Parse(tBoxFin.Text);
            bool notEqual = start != end;

            for (int i = start; i <= end ; i++)
            {
                if (notEqual)
                {
                    xLin.Add(x[i]);
                    yLin.Add(y[i]);
                }
            }

            if(notEqual)
            {
                Calculate_slope(xLin.ToArray(), yLin.ToArray());
            }           

        }

        //Faz os calculos para a seleção manual
        private void btnCalcLine_Click(object sender, EventArgs e)
        {
           // Calculate_slope(int.Parse(tBoxInit.Text), int.Parse(tBoxFin.Text));
        }

        private Tuple<double[], double[]> Find_Linear_Interval()
        {
            List<double> xLin = new List<double>();
            List<double> yLin = new List<double>();

            //Calcula a primeira e segunda derivada
            var dydx = Derivate(x.ToArray(), y.ToArray());
            var d2ydx = Derivate(x.ToArray(), dydx);

            //Encontra os pontos do intervalo linear
            //Pode mudar ainda essa parte, não sei se funciona para todos os casos

            for (int i = 0; i < x.Count(); i++)
            {
                if(d2ydx[i] < 0.1 && d2ydx[i] > -0.1) 
                {
                    xLin.Add(x[i]);
                    yLin.Add(y[i]);
                }
            }

            return Tuple.Create(xLin.ToArray(), yLin.ToArray());
        }

        void Calculate_slope(double[] xLin, double[] yLin)
        {

            //Faz a regressão linear da parte linear da curva. b + mx ==> b = tuple.1 e m = tuple.2
            Tuple<double, double> linear_regression = MathNet.Numerics.Fit.Line(xLin, yLin);
            double b = linear_regression.Item1;
            double m = linear_regression.Item2;

            //Apaga a reta anterior
            chart1.Series[2].Points.Clear();

            //Escreve na tBox
            tBox_m.Text = m.ToString();

            //Adiciona a reta aproximada ao gráfico. Acima de zero para limitar
            double y_lin;
            for (int i = 0; i < x.Count(); i++)
            {
                if ((y_lin = b + m * x[i]) > 0)
                {
                    chart1.Series[2].Points.AddXY(x[i], y_lin);
                }
            }

            //Deixa a curva linear pontilhada
            chart1.Series[2].BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;

        }

        private double[] Derivate(double[] x, double[] y)
        {
            var l = x.Length;
            double[] dydx = new double[l];
            var cs = MathNet.Numerics.Interpolation.CubicSpline.InterpolateNatural(x, y);

            for (int i = 0; i < x.Length; i++)
            {
                dydx[i] = cs.Differentiate(x[i]);
            }

            return dydx;
        }

    }
}
