using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;

namespace PaymentsExampleApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PaymentBaseEntities _context = new PaymentBaseEntities();

        public MainWindow()
        {
            InitializeComponent();
            ChartPayments.ChartAreas.Add(new System.Windows.Forms.DataVisualization.Charting.ChartArea("Main"));

            var currentSeries = new Series("Payments")
            {
                IsValueShownAsLabel = true
            };
            ChartPayments.Series.Add(currentSeries);

            ComboUser.ItemsSource = _context.Users.ToList();
            ComboCharTypes.ItemsSource = Enum.GetValues(typeof(SeriesChartType));
        }

        private void UpdateChart (object sender, SelectionChangedEventArgs e)
        {
            if (ComboUser.SelectedItem is User currentUser &&
                ComboCharTypes.SelectedItem is SeriesChartType currentTepe)
            {
                Series currentSeries = ChartPayments.Series.FirstOrDefault();
                currentSeries.ChartType = currentTepe;
                currentSeries.Points.Clear();

                var categoriesList = _context.Categories.ToList();
                foreach(var category in categoriesList)
                {
                    currentSeries.Points.AddXY(category.Name,
                        _context.Payments.ToList().Where(p => p.User == currentUser
                        && p.Category == category).Sum(p => p.Price * p.Num));
                }
            }
        }


        private void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var allUser = _context.Users.ToList().OrderBy(p => p.FLO).ToList();

            var application = new Excel.Application();
            application.SheetsInNewWorkbook = allUser.Count();

            Excel.Workbook workbook = application.Workbooks.Add(Type.Missing);

            


            for (int i = 0; i < allUser.Count(); i++)
            {
                int startRowIndex = 1;
                Excel.Worksheet worksheet = application.Worksheets.Item[i + 1];
                worksheet.Name = allUser[i].FLO;

                worksheet.Cells[1][startRowIndex] = "Дата платежа";
                worksheet.Cells[2][startRowIndex] = "Название";
                worksheet.Cells[3][startRowIndex] = "Стоимость";
                worksheet.Cells[4][startRowIndex] = "Количество";
                worksheet.Cells[5][startRowIndex] = "Сумма";

                startRowIndex++;

                var usersCategories = allUser[i].Payments.OrderBy(p => p.Data).GroupBy(p => p.Category).OrderBy(p => p.Key.Name);

                foreach (var groupCaregory in usersCategories)
                {
                    Excel.Range headerRange = worksheet.Range[worksheet.Cells[1][startRowIndex], worksheet.Cells[5][startRowIndex]];
                    headerRange.Merge();
                    headerRange.Value = groupCaregory.Key.Name;
                    headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    headerRange.Font.Italic = true;

                    startRowIndex++;

                    foreach (var payment in groupCaregory)
                    {
                        worksheet.Cells[1][startRowIndex] = payment.Data.ToString("dd.MM.yyyy HH:mm");
                        worksheet.Cells[2][startRowIndex] = payment.Name;
                        worksheet.Cells[3][startRowIndex] = payment.Price;
                        worksheet.Cells[4][startRowIndex] = payment.Num;

                        worksheet.Cells[5][startRowIndex].Formula = $"=C{startRowIndex}*D{startRowIndex}";

                        worksheet.Cells[3][startRowIndex].NumberFormat =
                            worksheet.Cells[3][startRowIndex].NumberFormat = "#,###.00";
                        startRowIndex++;
                    }

                    Excel.Range sumRange = worksheet.Range[worksheet.Cells[1][1], worksheet.Cells[5][startRowIndex - 1]];
                    sumRange.Merge();
                    sumRange.Value = "Итого:";
                    sumRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                    worksheet.Cells[5][startRowIndex].Formula = $"=SUM(E{startRowIndex - groupCaregory.Count()}:" +
                        $"E{startRowIndex - 1})";

                    sumRange.Font.Bold = worksheet.Cells[5][startRowIndex].Font.Bold = true;
                    worksheet.Cells[5][startRowIndex].NumberFormat = "#, ###.00";

                    startRowIndex++;

                    Excel.Range rangeBorders = worksheet.Range[worksheet.Cells[1][1], worksheet.Cells[5][startRowIndex - 1]];
                    rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeLeft].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeRight].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeTop].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlInsideHorizontal].LineStyle =
                    rangeBorders.Borders[Excel.XlBordersIndex.xlInsideVertical].LineStyle = Excel.XlLineStyle.xlContinuous;

                    worksheet.Columns.AutoFit();
                }
            }
            application.Visible = true;
        }
    }


}
