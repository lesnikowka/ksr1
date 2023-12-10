using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections;
using System.Windows.Forms.VisualStyles;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Net.NetworkInformation;

namespace lab1_dotnet_framework
{
    enum TaskType
    {
        Test,
        Main1,
        Main2
    }


    public partial class MainForm : Form
    {
        private DataBase db = null;
        private DataTable table = new DataTable();
        private DataTable table2 = new DataTable();

        private List<string> columnNames = new List<string> { "id", "xi", "vi", "v2i", "|vi-v2i|", "|olp|", "hi", "Делений", "Удвоений", "u", "|u-v|" };
        private List<string> columnNamesForDerivative = new List<string> { "id", "xi", "v2i", "v22i", "|v2i-v22i|", "|olp|", "hi", "Делений", "Удвоений" };

        private Dictionary<Tuple<double, double, double>, List<Series>> SeriesForStartConditions = new Dictionary<Tuple<double, double, double>, List<Series>>();

        private TaskType selectedTask = TaskType.Main2;
        private string currentTableDB = "main2";

        private string bdFolder = "/../../../database/lab1.sqlite3";
        private string scriptFolder = "\\..\\..\\..\\script";

        private string bdFolderReserved = "/database/lab1.sqlite3";
        private string scriptFolderReserved = "\\script";

        public string getDBFolder()
        { 
            try
            {
                StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + bdFolder);
                sr.Close();
            }
            catch
            {
                return bdFolderReserved;
            }

            return bdFolder;
        }

        public string getScriptFolder()
        {
            try
            {
                StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + scriptFolder + "/RK2.py");
                sr.Close();
            }
            catch
            {
                return scriptFolderReserved;
            }

            return scriptFolder;
        }

        public MainForm()
        {
            InitializeComponent();

            chart1.ChartAreas[0].BackColor = Color.Transparent;
            chart2.ChartAreas[0].BackColor = Color.Transparent;
            chart3.ChartAreas[0].BackColor = Color.Transparent;

            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "0.00001";
            chart2.ChartAreas[0].AxisX.LabelStyle.Format = "0.00001";
            chart3.ChartAreas[0].AxisX.LabelStyle.Format = "0.00001";

            chart1.ChartAreas[0].AxisX.Title = "X";
            chart1.ChartAreas[0].AxisY.Title = "U1";

            chart1.ChartAreas[0].AxisX.TitleFont = new Font("Arial", 14);
            chart1.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 14);


            chart3.ChartAreas[0].AxisX.Title = "X";
            chart3.ChartAreas[0].AxisY.Title = "U2";

            chart2.ChartAreas[0].AxisX.TitleFont = new Font("Arial", 14);
            chart2.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 14);

            chart2.ChartAreas[0].AxisX.Title = "U1";
            chart2.ChartAreas[0].AxisY.Title = "U2";

            chart3.ChartAreas[0].AxisX.TitleFont = new Font("Arial", 14);
            chart3.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 14);

            chart1.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.LightGray;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;

            chart2.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart2.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart2.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.LightGray;
            chart2.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;

            chart3.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart3.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart3.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.LightGray;
            chart3.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                db = new DataBase(getDBFolder());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }


            for (int i = 0; i < columnNames.Count - 2; i++)
            {
                table.Columns.Add(columnNames[i], typeof(string));
            }

            for (int i = 0; i < columnNamesForDerivative.Count; i++)
            {
                table2.Columns.Add(columnNamesForDerivative[i], typeof(string));
            }

            dataGridView1.DataSource = table;
            dataGridView2.DataSource = table2;

            dataGridView1.Columns["|olp|"].Width = 200; 
            dataGridView1.Columns["|vi-v2i|"].Width = 200; 
            dataGridView1.Columns["id"].Width = 100; 
            dataGridView1.Columns["Делений"].Width = 95; 
            dataGridView1.Columns["Удвоений"].Width = 95;

            dataGridView2.Columns["|olp|"].Width = 200;
            dataGridView2.Columns["|v2i-v22i|"].Width = 200;
            dataGridView2.Columns["id"].Width = 100;
            dataGridView2.Columns["Делений"].Width = 95;
            dataGridView2.Columns["Удвоений"].Width = 95;


            /*textBox8.Enabled = false;
            textBox9.Enabled = false;
            textBox10.Enabled = false;*/
        }

        private int catchParams(ref double X0, ref double U0, ref double U0der, ref double startStep, ref double localPrecision, ref double boundPrecision, ref double integrationBound, ref int maxStepNumbers, ref bool withControl)
        {
            string x0Text = pointsToCommas(textBox1.Text);
            string u0Text = pointsToCommas(textBox2.Text);
            string startStepText = pointsToCommas(textBox3.Text);
            string localPrecisionText = pointsToCommas(textBox4.Text);
            string boundPrecisionText = pointsToCommas(textBox5.Text);
            string maxStepNumbersText = pointsToCommas(textBox6.Text);
            string integrationBoundText = pointsToCommas(textBox7.Text);
            //string aText = pointsToCommas(textBox8.Text);
            //string bText = pointsToCommas(textBox9.Text);
            //string cText = pointsToCommas(textBox10.Text);
            string u0derText = pointsToCommas(textBox11.Text);

            if (x0Text.Length == 0 || u0Text.Length == 0 ||
                startStepText.Length == 0 || localPrecisionText.Length == 0 ||
                boundPrecisionText.Length == 0 || maxStepNumbersText.Length == 0 ||
                integrationBoundText.Length == 0 || u0derText.Length == 0
                )
            {
                MessageBox.Show("Вы ввели не все параметры", "Ошибка");
                return -1;
            }

            try
            {
                X0 = Convert.ToDouble(x0Text);
                U0 = Convert.ToDouble(u0Text);
                startStep = Convert.ToDouble(startStepText);
                localPrecision = Convert.ToDouble(localPrecisionText);
                boundPrecision = Convert.ToDouble(boundPrecisionText);
                maxStepNumbers = Convert.ToInt32(maxStepNumbersText);
                integrationBound = Convert.ToDouble(integrationBoundText);
                U0der = Convert.ToDouble(u0derText);
            }
            catch
            {
                MessageBox.Show("Неверный формат введенных значений", "Ошибка");
                return -1;
            }

            if (X0 + startStep > integrationBound)
            {
                MessageBox.Show("Некорректная граница интегрирования", "Ошибка");
                return -1;
            }

            withControl = checkBox1.Checked;

            return 0;
        }
        private Bitmap ConvertToBitmap(string fileName)
        {
            Bitmap bitmap;
            using (Stream bmpStream = System.IO.File.Open(fileName, System.IO.FileMode.Open))
            {
                Image image = Image.FromStream(bmpStream);

                bitmap = new Bitmap(image);

            }
            return bitmap;
        }

        private void executeMethod()
        {
            ProcessStartInfo deleteInfoStartProcess = new ProcessStartInfo();

            Process deleteValuesProcess = new Process();

            deleteInfoStartProcess.WorkingDirectory = Directory.GetCurrentDirectory() + getScriptFolder();
            deleteInfoStartProcess.FileName = "clear_tables.py";
            deleteInfoStartProcess.WindowStyle = ProcessWindowStyle.Hidden;

            deleteValuesProcess.StartInfo = deleteInfoStartProcess;

            deleteValuesProcess.Start();

            deleteValuesProcess.WaitForExit();

            double X0 = 0, U0 = 0, U0der = 0, startStep = 0, localPrecision = 0, boundPrecision = 0, integrationBound = 0;
            int maxStepNumbers = 0;
            bool withControl = true;

            string tableName = getTableString();

            int valid = catchParams(ref X0, ref U0, ref U0der, ref startStep, ref localPrecision, ref boundPrecision, ref integrationBound, ref maxStepNumbers, ref withControl);

            if (valid != 0) return;
            if (tableName != "main2") U0der = 0;
            
            string args = "";

            args += toStringPoint(X0) + " ";
            args += toStringPoint(U0) + " ";
            args += toStringPoint(startStep) + " ";
            args += toStringPoint(maxStepNumbers) + " ";
            args += toStringPoint(localPrecision) + " ";
            args += toStringPoint(boundPrecision) + " ";
            args += (withControl ? 1 : 0).ToString() +  " ";
            //args += toStringPoint(a) + " ";
            //args += toStringPoint(b) + " ";
            //args += toStringPoint(c) + " ";
            //args += tableName + " ";
            args += toStringPoint(integrationBound) + " ";
            args += toStringPoint(U0der) + " ";

            ProcessStartInfo infoStartProcess = new ProcessStartInfo();
            Process methodProcess = new Process();
            infoStartProcess.WorkingDirectory = Directory.GetCurrentDirectory() + getScriptFolder();
            infoStartProcess.FileName = "RK2.py";
            infoStartProcess.Arguments = args;
            infoStartProcess.WindowStyle = ProcessWindowStyle.Hidden;
            methodProcess.StartInfo = infoStartProcess;
            methodProcess.Start();
            methodProcess.WaitForExit();



            //var graphImg = new Bitmap(Directory.GetCurrentDirectory() + "/../../../script/graph.png");
            var graphImg = ConvertToBitmap(Directory.GetCurrentDirectory() + "/../../../script/graph.png");
            double mult = 0.65 * 4.0 / 3.0;
            Bitmap resized = new Bitmap(graphImg, new Size((int)(graphImg.Width * mult), (int)(graphImg.Height * mult)));
            pictureBox1.Image = resized;

            

            bool onlyMat = checkBox2.Checked;

            if (!onlyMat)
            {
                System.GC.Collect();
                ShowDataForStartCondition(doubleConditionsToList(X0, U0, U0der));
                System.GC.Collect();
                drawGraphs(doubleConditionsToList(X0, U0, U0der));
            }

            System.GC.Collect();

            int cntrl = checkBox1.Checked ? 1 : 0;

            richTextBox1.Text = getInfo();
        }

        double trueSol1(double x) {
            return -3 * Math.Exp(-1000.0 * x) + 10 * Math.Exp(-0.01 * x);
        }

        double trueSol2(double x)
        {
            return 3 * Math.Exp(-1000.0 * x) + 10 * Math.Exp(-0.01 * x);
        }

        private string getTableString()
        {
            string tableName;
            if (selectedTask == TaskType.Test) tableName = "test";
            else if (selectedTask == TaskType.Main1) tableName = "main1";
            else tableName = "main2";

            return tableName;
        }

        private List<string> doubleConditionsToList(double X0, double U0, double U0der)
        {
            return new List<string> { toStringPoint(X0), toStringPoint(U0), toStringPoint(U0der) };
        }

        private string toStringPoint(double val)
        {
            return val.ToString().Replace(",", ".");
        }

        private void drawGraphs(List<string> startCondition)
        {
            var x0u0Tuple = createTuple(startCondition);

            deleteOldSeries(startCondition);

            List<Series> newSeriesList = new List<Series>();

            Series newNumericSeries = new Series();
            newNumericSeries.Name = "Численное при X0 = " + startCondition[0] + " U10 = " + startCondition[1];
            if (selectedTask == TaskType.Main2)
                newNumericSeries.Name += " U20 = " + startCondition[2];
            newNumericSeries.ChartType = SeriesChartType.Line;
            newNumericSeries.BorderWidth = 2;
            this.chart1.Series.Add(newNumericSeries);
            newSeriesList.Add(newNumericSeries);


            Series newTrueSeries = new Series();
            newTrueSeries.Name = "Истинное при X0 = " + startCondition[0] + " U10 = " + startCondition[1] + " U20 = " + startCondition[2];
            newTrueSeries.ChartType = SeriesChartType.Line;
            newTrueSeries.BorderWidth = 1;
            this.chart1.Series.Add(newTrueSeries);
            newSeriesList.Add(newTrueSeries);
            newTrueSeries.BorderDashStyle = ChartDashStyle.Dash;

            Series newTrueDerivativeSeries = new Series();
            newTrueDerivativeSeries.Name = "Истинное при X0 = " + startCondition[0] + " U10 = " + startCondition[1] + " U20 = " + startCondition[2];
            newTrueDerivativeSeries.ChartType = SeriesChartType.Line;
            newTrueDerivativeSeries.BorderWidth = 1;
            this.chart3.Series.Add(newTrueDerivativeSeries);
            newSeriesList.Add(newTrueDerivativeSeries);
            newTrueDerivativeSeries.BorderDashStyle = ChartDashStyle.Dash;

            Series newTruePhaseSeries = new Series();
            newTruePhaseSeries.Name = "Истинное при X0 = " + startCondition[0] + " U10 = " + startCondition[1] + " U20 = " + startCondition[2];
            newTruePhaseSeries.ChartType = SeriesChartType.Line;
            newTruePhaseSeries.BorderWidth = 1;
            this.chart2.Series.Add(newTruePhaseSeries);
            newSeriesList.Add(newTruePhaseSeries);
            newTruePhaseSeries.BorderDashStyle = ChartDashStyle.Dash;


            Series newDerivativeSeries = new Series();
            newDerivativeSeries.Name = "Численное при X0 = " + startCondition[0] + " U10 = " + startCondition[1] + " U20 = " + startCondition[2];
            newDerivativeSeries.ChartType = SeriesChartType.Line;
            newDerivativeSeries.BorderWidth = 2;
            this.chart3.Series.Add(newDerivativeSeries);
            newSeriesList.Add(newDerivativeSeries);


            Series newPhaseSeries = new Series();
            newPhaseSeries.Name = "Численное при X0 = " + startCondition[0] + " U10 = " + startCondition[1] + " U20 = " + startCondition[2];
            newPhaseSeries.ChartType = SeriesChartType.Line;
            newPhaseSeries.BorderWidth = 2;
            this.chart2.Series.Add(newPhaseSeries);
            newSeriesList.Add(newPhaseSeries);

            DrawNumericSolution(newNumericSeries, newDerivativeSeries, newPhaseSeries, startCondition, newTrueSeries, newTrueDerivativeSeries, newTruePhaseSeries);
            

            SeriesForStartConditions.Add(x0u0Tuple, newSeriesList);

        }

        private Tuple<double, double, double> createTuple(List<string> startCondition)
        {
            double X0 = stringToDouble(startCondition[0]);
            double U0 = stringToDouble(startCondition[1]);
            double U0der = stringToDouble(startCondition[2]);

            Tuple<double, double, double> x0u0Tuple = new Tuple<double, double, double>(X0, U0, U0der);

            return x0u0Tuple;
        }

        private void deleteOldSeries(List<string> startCondition)
        {
            var x0u0Tuple = createTuple(startCondition);

            if (SeriesForStartConditions.ContainsKey(x0u0Tuple))
            {

                List<Series> oldSeries = SeriesForStartConditions[x0u0Tuple];

                for (int i = 0; i < oldSeries.Count; i++)
                {
                    this.chart1.Series.Remove(oldSeries[i]);
                    this.chart2.Series.Remove(oldSeries[i]);
                    this.chart3.Series.Remove(oldSeries[i]);
                }

                SeriesForStartConditions.Remove(x0u0Tuple);

            }
        }

        private string pointsToCommas(string s)
        {
            return s.Replace('.', ',');
        }

        private double stringToDouble(string s)
        {
            return Convert.ToDouble(s.Replace(".", ","));
        }

        private double constant(double x0, double v0)
        {
            return v0 / Math.Exp(2 * x0);
        }

        private void DrawNumericSolution(Series mainSeries, Series derSeries, Series phaseSeries, List<string> startCondition, Series trueSeries1, Series trueSeries2, Series truePhaseSeries)
        {
            string tableName = getTableString();

            List<List<string>> data = db.GetDataForStartCondition(tableName, startCondition);


            for (int i = 0; i < data.Count; i++)
            {
                double xi = stringToDouble(data[i][2]);
                double y1i = stringToDouble(data[i][3]);
                double y2i = stringToDouble(data[i][0]);

                mainSeries.Points.AddXY(xi, y1i);
                derSeries.Points.AddXY(xi, y2i);
                phaseSeries.Points.AddXY(y1i, y2i);

                trueSeries1.Points.AddXY(xi, trueSol1(xi));
                trueSeries2.Points.AddXY(xi, trueSol2(xi));
                truePhaseSeries.Points.AddXY(trueSol1(xi), trueSol2(xi));
            }
        }

        private void ShowDataForStartCondition(List<string> startCondition)
        {
            table.Rows.Clear();
            table2.Rows.Clear();

            addDataToPrimaryTable(startCondition);

            if (selectedTask == TaskType.Main2)
                addDataToDerivativeTable(startCondition);
        }

        private void addDataToPrimaryTable(List<string> startCondition)
        {
            string tableName = getTableString();

            List<List<string>> dataForStartCondition = db.GetDataForStartCondition(tableName, startCondition);

            int columnNamesSize = tableName == "test" ? columnNames.Count : columnNames.Count - 2;

            for (int i = 0; i < dataForStartCondition.Count; i++)
            {
                DataRow row = table.NewRow();

                for (int j = 0; j < columnNamesSize; j++)
                {
                    int j_index = tableName == "main2" ? j + 1 : j;
                    row[columnNames[j]] = dataForStartCondition[i][j_index];
                }

                table.Rows.Add(row);
            }
        }

        private void addDataToDerivativeTable(List<string> startCondition)
        {
            string tableName = "main2der";
            List<List<string>> dataForStartConditionDer = db.GetDataForStartCondition(tableName, startCondition);

            for (int i = 0; i < dataForStartConditionDer.Count; i++)
            {
                DataRow row = table2.NewRow();

                for (int j = 0; j < columnNamesForDerivative.Count; j++)
                {
                    row[columnNamesForDerivative[j]] = dataForStartConditionDer[i][j + 1];
                }

                table2.Rows.Add(row);
            }
        }
        
        private List<string> stringConditionToList(string startConditionString) 
        {
            List<string> startCondition = new List<string>();
            int commaIndex = startConditionString.IndexOf(',');
            startCondition.Add(startConditionString.Substring(0, commaIndex));
            string cropped = startConditionString.Substring(commaIndex + 2);
            int secondCommaIndex = cropped.IndexOf(",");

            if (secondCommaIndex != -1) 
            {
                startCondition.Add(startConditionString.Substring(commaIndex + 2, secondCommaIndex));
                startCondition.Add(startConditionString.Substring(commaIndex + secondCommaIndex + 4));
            }
            else
            {
                startCondition.Add(startConditionString.Substring(commaIndex + 2));
                startCondition.Add("0");
            }

            return startCondition;
        }

        private string getInfo()
        {
            string text = File.ReadAllText((Directory.GetCurrentDirectory() + "/../../../script/info.txt"), Encoding.UTF8);

            return text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.chart1.Series.Clear();
            this.chart2.Series.Clear();
            this.chart3.Series.Clear();
            table.Clear();
            table2.Clear();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            executeMethod();
        }

        private void dsaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Help()).Show();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
