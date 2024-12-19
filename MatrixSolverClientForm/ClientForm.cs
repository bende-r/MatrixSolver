using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MatrixSolverClientForm
{
    public partial class ClientForm : Form
    {
        private TabControl tabControl;
        private TabPage tabMain;
        private TabPage tabConversion;

        private TextBox txtMatrixFile;
        private TextBox txtVectorFile;
        private TextBox txtCombinedFile;
        private RadioButton rbCombined;
        private RadioButton rbSeparate;
        private Button btnSelectMatrix;
        private Button btnSelectVector;
        private Button btnSelectCombined;
        private Button btnSend;
        private Button btnSaveResult;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
        private TextBox txtResult;

        private TextBox txtMatrixAFile;
        private TextBox txtVectorBFile;
        private TextBox txtAnswerDesFile;
        private Button btnSelectAFile;
        private Button btnSelectBFile;
        private Button btnSelectDesFile;
        private Button btnConvertAndCompare;
        private TextBox txtConversionResult;

        public ClientForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Matrix Solver Client";
            this.Size = new System.Drawing.Size(900, 700);

            tabControl = new TabControl { Dock = DockStyle.Fill };

            tabMain = new TabPage("Основное") { Padding = new Padding(10) };
            tabConversion = new TabPage("Конвертация и проверка") { Padding = new Padding(10) };

            InitializeMainTab();
            InitializeConversionTab();

            tabControl.TabPages.Add(tabMain);
            tabControl.TabPages.Add(tabConversion);

            this.Controls.Add(tabControl);

            // Инициализация OpenFileDialog
            openFileDialog = new OpenFileDialog
            {
                Filter = "All Files|*.*|JSON Files|*.json|Matrix Files|*.A|Vector Files|*.B|Result Files|*.DES",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files|*.json|All Files|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };
        }


        private void InitializeMainTab()
        {
            Label lblMode = new Label { Text = "Выберите режим загрузки:", Left = 10, Top = 10, Width = 200 };

            rbCombined = new RadioButton { Text = "Один JSON файл", Left = 20, Top = 40, Width = 150, Checked = true };
            rbCombined.CheckedChanged += ToggleMode;

            rbSeparate = new RadioButton { Text = "Отдельные файлы", Left = 200, Top = 40, Width = 150 };

            Label lblMatrixFile = new Label { Text = "Файл матрицы (A):", Left = 10, Top = 80, Width = 150 };
            txtMatrixFile = new TextBox { Left = 170, Top = 80, Width = 400, ReadOnly = true, Enabled = false };
            btnSelectMatrix = new Button { Text = "Обзор", Left = 600, Top = 80, Width = 100, Enabled = false };
            btnSelectMatrix.Click += (sender, e) => SelectFile(txtMatrixFile);

            Label lblVectorFile = new Label { Text = "Файл вектора (B):", Left = 10, Top = 120, Width = 150 };
            txtVectorFile = new TextBox { Left = 170, Top = 120, Width = 400, ReadOnly = true, Enabled = false };
            btnSelectVector = new Button { Text = "Обзор", Left = 600, Top = 120, Width = 100, Enabled = false };
            btnSelectVector.Click += (sender, e) => SelectFile(txtVectorFile);

            Label lblCombinedFile = new Label { Text = "Один JSON файл:", Left = 10, Top = 160, Width = 150 };
            txtCombinedFile = new TextBox { Left = 170, Top = 160, Width = 400, ReadOnly = true };
            btnSelectCombined = new Button { Text = "Обзор", Left = 600, Top = 160, Width = 100 };
            btnSelectCombined.Click += (sender, e) => SelectFile(txtCombinedFile);

            btnSend = new Button { Text = "Отправить", Left = 10, Top = 200, Width = 150 };
            btnSend.Click += BtnSend_Click;

            btnSaveResult = new Button { Text = "Сохранить результат", Left = 200, Top = 200, Width = 150, Enabled = false };
            btnSaveResult.Click += BtnSaveResult_Click;

            txtResult = new TextBox { Left = 10, Top = 250, Width = 750, Height = 250, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            tabMain.Controls.Add(lblMode);
            tabMain.Controls.Add(rbCombined);
            tabMain.Controls.Add(rbSeparate);
            tabMain.Controls.Add(lblMatrixFile);
            tabMain.Controls.Add(txtMatrixFile);
            tabMain.Controls.Add(btnSelectMatrix);
            tabMain.Controls.Add(lblVectorFile);
            tabMain.Controls.Add(txtVectorFile);
            tabMain.Controls.Add(btnSelectVector);
            tabMain.Controls.Add(lblCombinedFile);
            tabMain.Controls.Add(txtCombinedFile);
            tabMain.Controls.Add(btnSelectCombined);
            tabMain.Controls.Add(btnSend);
            tabMain.Controls.Add(btnSaveResult);
            tabMain.Controls.Add(txtResult);
        }





        private void ToggleMode(object sender, EventArgs e)
        {
            bool isCombined = rbCombined.Checked;
            txtMatrixFile.Enabled = !isCombined;
            btnSelectMatrix.Enabled = !isCombined;
            txtVectorFile.Enabled = !isCombined;
            btnSelectVector.Enabled = !isCombined;
            txtCombinedFile.Enabled = isCombined;
            btnSelectCombined.Enabled = isCombined;
        }


        private async void BtnSend_Click(object sender, EventArgs e)
        {
            string combinedFile = txtCombinedFile.Text;
            string matrixFile = txtMatrixFile.Text;
            string vectorFile = txtVectorFile.Text;

            string jsonData;
            if (rbCombined.Checked)
            {
                if (string.IsNullOrWhiteSpace(combinedFile) || !File.Exists(combinedFile))
                {
                    MessageBox.Show("Выберите существующий файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                jsonData = File.ReadAllText(combinedFile);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(matrixFile) || !File.Exists(matrixFile) ||
                    string.IsNullOrWhiteSpace(vectorFile) || !File.Exists(vectorFile))
                {
                    MessageBox.Show("Выберите оба файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var data = new
                {
                    Matrix = JsonSerializer.Deserialize<double[][]>(File.ReadAllText(matrixFile)),
                    Vector = JsonSerializer.Deserialize<double[]>(File.ReadAllText(vectorFile))
                };

                jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            }

            await SendToServer(jsonData);
        }

        private async Task SendToServer(string jsonData)
        {
            try
            {
                ClientWebSocket client = new ClientWebSocket();
                using (client)
                {
                    txtResult.Text = "Подключение к серверу...\r\n";
                    await client.ConnectAsync(new Uri("ws://localhost:5145/ws"), CancellationToken.None);
                    txtResult.AppendText("Соединение установлено.\r\n");

                    byte[] messageBytes = Encoding.UTF8.GetBytes(jsonData);
                    await client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await client.ReceiveAsync(buffer, CancellationToken.None);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        string response = Encoding.UTF8.GetString(ms.ToArray());
                        txtResult.AppendText("Ответ от сервера: \r\n" + response);
                        btnSaveResult.Enabled = true;

                        // Сохраняем результат для возможного сохранения
                        LastServerResponse = response;
                    }

                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Клиент завершил работу", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string LastServerResponse;

        private void BtnSaveResult_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, LastServerResponse);
                MessageBox.Show("Результат успешно сохранён.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

















        private void InitializeConversionTab()
        {
            Label lblMatrixAFile = new Label { Text = "Файл матрицы (.A):", Left = 10, Top = 10, Width = 150 };
            txtMatrixAFile = new TextBox { Left = 170, Top = 10, Width = 400, ReadOnly = true };
            btnSelectAFile = new Button { Text = "Обзор", Left = 600, Top = 10, Width = 100 };
            btnSelectAFile.Click += (sender, e) => SelectFile(txtMatrixAFile);

            Label lblVectorBFile = new Label { Text = "Файл вектора (.B):", Left = 10, Top = 50, Width = 150 };
            txtVectorBFile = new TextBox { Left = 170, Top = 50, Width = 400, ReadOnly = true };
            btnSelectBFile = new Button { Text = "Обзор", Left = 600, Top = 50, Width = 100 };
            btnSelectBFile.Click += (sender, e) => SelectFile(txtVectorBFile);

            Label lblAnswerDesFile = new Label { Text = "Файл ответов (.DES):", Left = 10, Top = 90, Width = 150 };
            txtAnswerDesFile = new TextBox { Left = 170, Top = 90, Width = 400, ReadOnly = true };
            btnSelectDesFile = new Button { Text = "Обзор", Left = 600, Top = 90, Width = 100 };
            btnSelectDesFile.Click += (sender, e) => SelectFile(txtAnswerDesFile);

            btnConvertAndCompare = new Button { Text = "Конвертировать и сравнить", Left = 10, Top = 130, Width = 200 };
            btnConvertAndCompare.Click += BtnConvertAndCompare_Click;

            txtConversionResult = new TextBox { Left = 10, Top = 180, Width = 750, Height = 250, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            tabConversion.Controls.Add(lblMatrixAFile);
            tabConversion.Controls.Add(txtMatrixAFile);
            tabConversion.Controls.Add(btnSelectAFile);
            tabConversion.Controls.Add(lblVectorBFile);
            tabConversion.Controls.Add(txtVectorBFile);
            tabConversion.Controls.Add(btnSelectBFile);
            tabConversion.Controls.Add(lblAnswerDesFile);
            tabConversion.Controls.Add(txtAnswerDesFile);
            tabConversion.Controls.Add(btnSelectDesFile);
            tabConversion.Controls.Add(btnConvertAndCompare);
            tabConversion.Controls.Add(txtConversionResult);
        }

        private void SelectFile(TextBox textBox)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = openFileDialog.FileName;
            }
        }

        private async void BtnConvertAndCompare_Click(object sender, EventArgs e)
        {
            string matrixAFile = txtMatrixAFile.Text;
            string vectorBFile = txtVectorBFile.Text;
            string answerDesFile = txtAnswerDesFile.Text;

            if (string.IsNullOrWhiteSpace(matrixAFile) || !File.Exists(matrixAFile) ||
                string.IsNullOrWhiteSpace(vectorBFile) || !File.Exists(vectorBFile) ||
                string.IsNullOrWhiteSpace(answerDesFile) || !File.Exists(answerDesFile))
            {
                MessageBox.Show("Выберите все необходимые файлы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Чтение и конвертация файла .A
                var matrix = File.ReadAllLines(matrixAFile)
                    .Where(line => !string.IsNullOrWhiteSpace(line)) // Удаление пустых строк
                    .Select(line =>
                    {
                        try
                        {
                            return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(value => double.Parse(value, CultureInfo.InvariantCulture))
                                       .ToArray();
                        }

                        catch
                        {
                            throw new FormatException($"Ошибка при разборе строки матрицы: \"{line}\"");
                        }
                    })
                    .ToArray();

                // Чтение и конвертация файла .B
                var vector = File.ReadAllLines(vectorBFile)
                    .Where(line => !string.IsNullOrWhiteSpace(line)) // Удаление пустых строк
                    .Select(value =>
                    {
                        try
                        {
                            return double.Parse(value, CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            throw new FormatException($"Ошибка при разборе строки вектора: \"{value}\"");
                        }
                    })
                    .ToArray();

                // Формирование JSON для отправки
                var data = new { Matrix = matrix, Vector = vector };
                string jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

                // Отправка на сервер
                string serverResponse = await SendAndReceive(jsonData);

                // Чтение файла .DES
                var expectedAnswers = File.ReadAllLines(answerDesFile)
                    .Where(line => !string.IsNullOrWhiteSpace(line)) // Удаление пустых строк
                    .Select(value =>
                    {
                        try
                        {
                            return double.Parse(value, CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            throw new FormatException($"Ошибка при разборе строки ответа: \"{value}\"");
                        }
                    })
                    .ToArray();

                // Сравнение результатов
                var actualAnswers = JsonSerializer.Deserialize<double[]>(serverResponse);
                bool isEqual = expectedAnswers.SequenceEqual(actualAnswers);

                txtConversionResult.Text = isEqual
                    ? "Результаты совпадают.\r\n"
                    : "Результаты НЕ совпадают.\r\n";

                txtConversionResult.AppendText("Ответ сервера:\r\n" + serverResponse + "\r\n");
                txtConversionResult.AppendText("Ожидаемые ответы:\r\n" + string.Join(", ", expectedAnswers));
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"Ошибка формата данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async Task<string> SendAndReceive(string jsonData)
        {
            try
            {
                ClientWebSocket client = new ClientWebSocket();
                using (client)
                {
                    txtConversionResult.Text = "Подключение к серверу...\r\n";
                    await client.ConnectAsync(new Uri("ws://localhost:5145/ws"), CancellationToken.None);

                    byte[] messageBytes = Encoding.UTF8.GetBytes(jsonData);
                    await client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await client.ReceiveAsync(buffer, CancellationToken.None);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при отправке данных на сервер: " + ex.Message);
            }
        }


    }
}
