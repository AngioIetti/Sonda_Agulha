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


namespace Sonda_Agulha
{
    public partial class Form1 : Form
    {
        double x1, x2;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Teste de derivada

            //Cria uma função y = f(x)

            //Vetores que guardam os valores da aquisição
            double[] x = new double[50];
            double[] y = new double[50];

            //Cria uma função
            //Depois deve ser retirada pq já teremos os valores de x e y
            for (int i = 0; i < x.Length; i++)
            {
                if(i < 10)
                {
                    x[i] = i;
                    y[i] = x[i]*x[i]*x[i];
                }
                
                if(i >= 10 && i < 40)
                {
                    x[i] = i;
                    y[i] = x[i];
                }

                if(i >= 40)
                {
                    x[i] = i;
                    y[i] = x[i - 40] * x[i - 40];
                }

                chart1.Series[0].Points.AddXY(x[i], y[i]);
                chart1.Series[0].BorderWidth = 2;
            }

            //Calcula a primeira e segunda derivada
            var dydx = Derivate(x, y);
            var d2ydx = Derivate(x, dydx);

            //Conta quantos valores são próximos de zero para depois criar o vetor com esse número de valores
            //Pode ser retirado também
            int cont = 0;
            for (int i = 0; i < x.Length; i++)
            {
                chart1.Series[1].Points.AddXY(x[i], d2ydx[i]);
                if (d2ydx[i] < 0.1 && d2ydx[i] > -0.1) { cont++;}
            }

            //Vetores que guardam a parte linear da curva
            double[] xLin = new double[cont];
            double[] yLin = new double[cont];

            //Assigna os valores da parte linear da curva aos vetores
            cont = 0;
            for (int i = 0; i < x.Length; i++)
            {
                if (d2ydx[i] < 0.1 && d2ydx[i] > -0.1)
                {
                    xLin[i - cont] = x[i];
                    yLin[i - cont] = y[i];
                    cont++;
                }
                else { cont++; }
            }

            //Cria um objeto "LinearSpline" da parte linear
            var ls = MathNet.Numerics.Interpolation.LinearSpline.Interpolate(xLin, yLin);

            //Vetor que guarda os valores interpolados da parte linear da curva
            double[] yInterpolated = new double[cont];

            //Interpola a parte linear e plota
            for (int i = 0; i < cont; i++)
            {
                yInterpolated[i] = ls.Interpolate(i);
                chart1.Series[2].Points.AddXY(i, yInterpolated[i]);
            }

            //Deixa a curva linear pontilhada
            chart1.Series[2].BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;


            chart1.ChartAreas[0].CursorX.IsUserEnabled = false;         // red cursor at SelectionEnd
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = false;      // zoom into SelectedRange
            chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;
            chart1.ChartAreas[0].CursorX.Interval = 1;               // set "resolution" of CursorX
            
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

        private void chart1_SelectionRangeChanged(object sender, System.Windows.Forms.DataVisualization.Charting.CursorEventArgs e)
        {
            x1 = e.NewSelectionStart; // or: chart1.ChartAreas[0].CursorX.SelectionStart;
            x2 = e.NewSelectionEnd;        // or: x2 = chart1.ChartAreas[0].CursorX.SelectionEnd;

            Console.Write("x1 = ");
            Console.WriteLine(x1);
            Console.Write("x2 = ");
            Console.WriteLine(x2);

            var a = chart1.Series.Select(series => series.Points.Where(point => point.XValue == x1).ToList()).ToList();

            var arr = a[0].ToArray();

            Console.WriteLine(arr[0].YValues.ToString());

        }
    }
}
